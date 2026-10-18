using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("UI References")] public GameObject shopPanel;
    public GameObject itemContainer;

    [Header("Shop Controls")] public Button nextLevelButton;
    public Button mainMenuButton;
    public TextMeshProUGUI playerScoreText;


    [Header("Animation")] public float slideAnimationDuration = 1f;

    [Header("Scroll Controls")] public Button scrollUpButton;
    public Button scrollDownButton;
    public ScrollRect itemScrollRect;
    public float scrollAmount = 800f;
    public float scrollDuration = 1f;

    [Header("Purchase Popup")] public GameObject purchaseConfirmationPopup;
    public Image popupItemIcon;
    public TextMeshProUGUI popupItemInfo;
    public Button confirmPurchaseButton;
    public Button cancelPurchaseButton;

    private ShopItemController currentPurchaseItem;

    // Feedback shown in the score label: a serial lets an older restore skip if a newer message replaced it
    private int scoreMessageSerial = 0;
    private Color playerScoreOriginalColor = Color.white;

    private void Start()
    {
        if (playerScoreText != null)
            playerScoreOriginalColor = playerScoreText.color;

        SetupButtonListeners();
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayShopMusic();
        // Chapter 2 (16 October): the shop in front of the beach (Resources/Chapter2/shop_bg, the owner's pick)
        var shopArt = Chapter.Art<Sprite>(SessionManager.CurrentChapter, "shop_bg");
        var shopBg = shopArt != null ? GameObject.Find("bg") : null;
        if (shopBg != null && shopBg.GetComponent<Image>() != null) shopBg.GetComponent<Image>().sprite = shopArt;
        // cards of foods held back from this release are not shown (Cookies, 13 October)
        foreach (var card in FindObjectsOfType<ShopItemController>(true))
            if (IsHeldBackCard(card)) card.gameObject.SetActive(false);
        EvenOutCharacterIcons();
        LayoutCards();
        if (itemScrollRect != null) itemScrollRect.onValueChanged.AddListener(_ => UpdateScrollButtons());   // a finger drag updates the lock too

        // FIX: Wait for all ShopItemController.Start() methods to complete
        StartCoroutine(DelayedInitializeShop());
    }

// Add this new method:
    private IEnumerator DelayedInitializeShop()
    {
        // Wait for all Start() methods to complete
        yield return new WaitForEndOfFrame();

        // Now initialize shop with correct affordability
        InitializeShop();
        StartCoroutine(DelayedScrollButtonUpdate());
    }

    private void SetupButtonListeners()
    {
        Wire(confirmPurchaseButton, ConfirmPurchase);
        Wire(cancelPurchaseButton, CancelPurchase);
        Wire(scrollUpButton, ScrollUp);
        Wire(scrollDownButton, ScrollDown);
        Wire(nextLevelButton, OnNextLevelClicked);
        Wire(mainMenuButton, OnMainMenuClicked);
    }

    /// Adds a runtime listener only when the button has no persistent (Inspector) call.
    /// RemoveAllListeners never removes Inspector calls, so Next Level, Purchase and Cancel
    /// used to fire twice per tap in the Shop scene.
    private static void Wire(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        if (button.onClick.GetPersistentEventCount() == 0)
            button.onClick.AddListener(action);
    }

    /// True while the purchase confirmation popup is showing. The popup does not cover the
    /// bottom buttons, so they check this before acting.
    public bool IsPurchasePopupOpen =>
        purchaseConfirmationPopup != null && purchaseConfirmationPopup.activeInHierarchy;

    private ShopItemController[] GetAllShopItems()
    {
        if (itemContainer == null) return new ShopItemController[0];
        return itemContainer.GetComponentsInChildren<ShopItemController>(true);
    }

    private void RefreshShopDisplay()
    {
        var allShopItems = GetAllShopItems();
        var session = SessionManager.Instance; // null when the Shop scene is opened directly in the editor

        // Show all items and check affordability only
        foreach (var shopItem in allShopItems)
            if (shopItem != null)
            {
                if (IsHeldBackCard(shopItem)) { shopItem.gameObject.SetActive(false); continue; }   // Cookies wait for the next release
                shopItem.gameObject.SetActive(true);
                var canAfford = session != null && session.CanAfford(shopItem.price);
                shopItem.UpdateAffordability(canAfford);
                ShowCardLock(shopItem, IsLockedCard(shopItem));
            }
    }

    // Kept void for scene button bindings; the bool version drives the popup flow.
    public void PurchaseItem(ShopItemController item)
    {
        TryPurchaseItem(item);
    }

    /// Returns true when the purchase went through. On failure the confirmation
    /// popup (if open) stays open showing the reason, so the player can cancel.
    private bool TryPurchaseItem(ShopItemController item)
    {
        if (item == null) return false;

        var session = SessionManager.Instance;

        // Check if SessionManager is available
        if (session == null || !session.HasActiveSession())
        {
            ShowPurchaseFailedFeedback("No active session!");
            return false;
        }

        // Check if already purchased
        if (item.isPurchased)
        {
            ShowPurchaseFailedFeedback("Already purchased!");
            return false;
        }

        // Upgrades have a max level (SessionManager default: 3)
        if (item.itemType == ItemType.Upgrade && !session.CanUpgradeFood(item.upgradeFoodType))
        {
            item.MarkAsPurchased(); // show a maxed upgrade like an owned item
            ShowPurchaseFailedFeedback("Max level reached!");
            return false;
        }

        // Check if player has enough score
        if (SessionManager.IsLockedInShop(item.itemName) || SessionManager.IsLockedInShop(item.gameObject.name))
        {
            ShowPurchaseFailedFeedback("Opens with chapter 2");   // the card's lock stops taps; this stops every other path (16 October)
            return false;
        }

        if (!session.CanAfford(item.price))
        {
            var currentScore = session.GetTotalScore();
            ShowPurchaseFailedFeedback($"Not enough points!\nNeed: {item.price} | Have: {currentScore:F2}");
            return false;
        }

        // Pay FIRST, so an app kill between the two steps can never hand out a free item
        if (!session.DeductScore(item.price))
        {
            UpdateScoreDisplay();
            RefreshShopDisplay();
            ShowPurchaseFailedFeedback("Not enough points!");
            return false;
        }

        // Record the purchase based on item type
        var purchaseSuccess = false;
        if (item.itemType == ItemType.Food)
            purchaseSuccess = session.PurchaseFoodItem(item.itemName);
        else if (item.itemType == ItemType.Character)
            purchaseSuccess = session.PurchaseCharacter(item.itemName);
        else if (item.itemType == ItemType.Upgrade)
            purchaseSuccess = session.UpgradeFood(item.upgradeFoodType);
        else if (item.itemType == ItemType.Decor)
            purchaseSuccess = session.PurchaseDecor(item.itemName);

        if (!purchaseSuccess)
        {
            // Refund what we just took
            session.AddScoreImmediately(item.price);
            UpdateScoreDisplay();
            RefreshShopDisplay();

            Debug.LogError($"Failed to purchase {item.itemName} - refunded {item.price} points");
            ShowPurchaseFailedFeedback("Purchase failed!");
            return false;
        }

        // Update score display
        UpdateScoreDisplay();

        // Mark as purchased in UI
        item.MarkAsPurchased();
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPurchaseSound();

        // Refresh all items to update affordability
        RefreshShopDisplay();

        Debug.Log($"Successfully purchased {item.itemName} for {item.price} points!");

        // Close popup on success
        if (purchaseConfirmationPopup != null)
            purchaseConfirmationPopup.SetActive(false);

        // Show success feedback in the (always visible) score label
        ShowPurchaseSuccessFeedback($"Purchased {item.itemName}!");
        return true;
    }


    private void ShowPurchaseFailedFeedback(string message)
    {
        // The info label lives inside the popup: keep the popup open and write the reason there.
        // ShowPurchasePopup resets the text/color the next time an item is opened.
        if (purchaseConfirmationPopup != null && purchaseConfirmationPopup.activeInHierarchy && popupItemInfo != null)
        {
            popupItemInfo.text = message;
            popupItemInfo.color = Color.red;
            return;
        }

        // Popup not open: fall back to the score label
        ShowScoreLabelMessage(message, Color.red);
    }

    private void ShowPurchaseSuccessFeedback(string message)
    {
        ShowScoreLabelMessage(message, playerScoreOriginalColor);   // the label's own black with its cream rim, not green (owner, 16 October)
    }

    private void ShowScoreLabelMessage(string message, Color color)
    {
        if (playerScoreText == null)
        {
            Debug.Log($"Shop feedback: {message}");
            return;
        }

        scoreMessageSerial++;
        StartCoroutine(ShowTemporaryScoreMessage(message, color, scoreMessageSerial));
    }

    private IEnumerator ShowTemporaryScoreMessage(string message, Color color, int serial)
    {
        playerScoreText.text = message;
        playerScoreText.color = color;

        yield return new WaitForSeconds(2f);

        // A newer message replaced this one; let it finish on its own
        if (serial != scoreMessageSerial || playerScoreText == null) yield break;

        playerScoreText.color = playerScoreOriginalColor;
        UpdateScoreDisplay(); // restores the live score instead of a stale snapshot
    }

    private void UpdatePurchasedItemsUI()
    {
        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession())
            return;

        var purchasedFoods = SessionManager.Instance.GetCurrentSession().purchasedFoodItems;
        var purchasedCharacters = SessionManager.Instance.GetCurrentSession().purchasedCharacters;

        var shopItems = GetAllShopItems();

        foreach (var shopItem in shopItems)
            if (shopItem.itemType == ItemType.Food && purchasedFoods.Contains(shopItem.itemName))
            {
                shopItem.MarkAsPurchased();
            }
            else if (shopItem.itemType == ItemType.Character && purchasedCharacters.Contains(shopItem.itemName))
            {
                shopItem.MarkAsPurchased();
            }
            else if (shopItem.itemType == ItemType.Decor && SessionManager.Instance.IsDecorPurchased(shopItem.itemName))
            {
                shopItem.MarkAsPurchased();
            }
            else if (shopItem.itemType == ItemType.Upgrade)
            {
                var currentLevel = SessionManager.Instance.GetFoodUpgradeLevel(shopItem.upgradeFoodType);
                var maxed = !SessionManager.Instance.CanUpgradeFood(shopItem.upgradeFoodType);
                if (currentLevel > 1 || maxed) // was upgraded at least once, or at max level
                    shopItem.MarkAsPurchased();
            }
    }

    // Add anywhere in ShopManager.cs — called by RewardedAdButton after granting coins
    public void RefreshScoreDisplay()
    {
        UpdateScoreDisplay(); // updates the coins text at the top
        RefreshShopDisplay(); // re-checks which items are now affordable
    }

    private void UpdateScoreDisplay()
    {
        if (playerScoreText != null)
        {
            var currentScore = SessionManager.Instance != null ? SessionManager.Instance.GetTotalScore() : 0;
            playerScoreText.text = $"EARNED: ${Mathf.FloorToInt(currentScore)}";   // one line, whole dollars (owner, 13 October: it wrapped)
            playerScoreText.color = playerScoreOriginalColor;
            scoreMessageSerial++; // any pending feedback restore is now stale
        }
    }

    private void InitializeShop()
    {
        // Update score display from session
        UpdateScoreDisplay();

        // Show items and update affordability
        RefreshShopDisplay();

        // Check which items are already purchased
        UpdatePurchasedItemsUI();

        // Update scroll button states
        StartCoroutine(DelayedScrollButtonUpdate());

        // Start slide-in animation
        StartShopAnimation();
    }

    private IEnumerator DelayedScrollButtonUpdate()
    {
        yield return new WaitForEndOfFrame();
        UpdateScrollButtons();
    }

    public void OnNextLevelClicked()
    {
        LoadNextGameLevel();
    }

    public void LoadNextGameLevel()
    {
        if (IsPurchasePopupOpen) return;
        AudioManager.Instance.PlayGameplayMusic();
        SceneTransitionManager.Instance.TransitionToScene("GameSceneOne");
    }

    public void OnMainMenuClicked()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.StopMusic();
        SceneTransitionManager.Instance.TransitionToScene("MainMenu");
    }

    public void ScrollUp()
    {
        if (itemContainer != null)
        {
            var itemContainerRect = itemContainer.GetComponent<RectTransform>();
            if (itemContainerRect != null)
                StartCoroutine(SimpleScroll(itemContainerRect, -scrollAmount));
        }
    }

    public void ScrollDown()
    {
        if (itemContainer != null)
        {
            var itemContainerRect = itemContainer.GetComponent<RectTransform>();
            if (itemContainerRect != null)
                StartCoroutine(SimpleScroll(itemContainerRect, scrollAmount));
        }
    }

    private IEnumerator SimpleScroll(RectTransform containerRect, float amount)
    {
        var startPos = containerRect.anchoredPosition;
        var targetPos = startPos + new Vector2(0, amount);

        targetPos.y = Mathf.Clamp(targetPos.y, topBoundary, bottomBoundary);

        if (Mathf.Abs(startPos.y - targetPos.y) < 1f)
            yield break;

        var elapsed = 0f;
        while (elapsed < scrollDuration)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / scrollDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            containerRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        containerRect.anchoredPosition = targetPos;
        UpdateScrollButtons();
    }

    /// The people cards show their figures at one height (owner, 13 October: some were out of proportion). The
    /// smallest figure sets the height; the others shrink to it, width by their picture's aspect, centred where they were.
    private void EvenOutCharacterIcons()
    {
        var icons = new List<RectTransform>();
        foreach (var card in FindObjectsOfType<ShopItemController>(true))
        {
            if (card == null || card.itemType != ItemType.Character) continue;
            var icon = card.transform.Find("ItemIcon") as RectTransform;
            if (icon != null && icon.GetComponent<Image>() != null && icon.GetComponent<Image>().sprite != null) icons.Add(icon);
        }
        if (icons.Count < 2) return;
        var h = float.MaxValue;
        foreach (var icon in icons) h = Mathf.Min(h, icon.rect.height);
        foreach (var icon in icons)
        {
            var sp = icon.GetComponent<Image>().sprite;
            var aspect = sp.rect.width / sp.rect.height;
            var centre = icon.anchoredPosition + new Vector2((0.5f - icon.pivot.x) * icon.rect.width, (0.5f - icon.pivot.y) * icon.rect.height);
            icon.sizeDelta = new Vector2(h * aspect, h);
            icon.anchoredPosition = centre - new Vector2((0.5f - icon.pivot.x) * icon.rect.width, (0.5f - icon.pivot.y) * icon.rect.height);
        }
    }

    /// A chapter 2 unlock (Cookies, Coconut) shown with a lock until that chapter is started (owner, 16 October)
    private static bool IsLockedCard(ShopItemController card) =>
        card != null && (SessionManager.IsLockedInShop(card.itemName?.Trim()) || SessionManager.IsLockedInShop(card.gameObject.name));

    private static void ShowCardLock(ShopItemController card, bool locked)
    {
        var icon = card.transform.Find("ItemIcon"); if (icon == null) return;
        var t = icon.Find("Lock");
        if (t == null && locked)
        {
            var sprite = Resources.Load<Sprite>("Map/lock_yellow"); if (sprite == null) return;
            var go = new GameObject("Lock", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(icon, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 0f); var size = ((RectTransform)icon).rect.height * 0.5f;
            rt.sizeDelta = new Vector2(size * sprite.rect.width / sprite.rect.height, size);
            var img = go.GetComponent<Image>(); img.sprite = sprite; img.raycastTarget = false; img.preserveAspect = true;
            t = go.transform;
        }
        if (t != null) t.gameObject.SetActive(locked);
        var ii = icon.GetComponent<Image>(); if (ii != null && locked) ii.color = new Color(0.75f, 0.75f, 0.75f, 1f);
    }

    private static bool IsHeldBackCard(ShopItemController card) =>
        card != null && (SessionManager.IsHeldBack(card.itemName?.Trim()) || SessionManager.IsHeldBack(card.upgradeFoodType?.Trim()) || SessionManager.IsHeldBack(card.gameObject.name));

    // Scroll bounds of the card container, set by LayoutCards (16 October); before that the cards were placed by hand
    // in the scene (rows 900 apart, bottom 3290) and a finger drag could not reach the last rows
    private float topBoundary = -38f, bottomBoundary = 3290f;
    private float rowPitch = 800f;

    [Tooltip("The item popup's size; its board is 1,100 wide, wider than the 1,080 screen, so 0.8 leaves about 100 px each side")]
    public float PopupScale = 0.8f;

    [Header("Card layout (16 October)")]
    [Tooltip("The cards are laid out at runtime: sorted by price, three columns, scaled down so more fit a screen")]
    public float cardScale = 0.62f;
    private const int Columns = 3;
    private const float ColumnPitchMax = 245f;     // three columns left of the arrow buttons
    private const float RowPitchMax = 450f;        // three rows on a screen
    private const float ColumnsCentre = -143f;     // in the container's space: the usable width sits left of the arrows

    /// Sorts the cards cheap to expensive and lays them out in a grid, scaled down (owner, 16 October: "sort shop
    /// items from cheap to expensive", "shrink the content so more items fit"). Each card's visual centre (icon and
    /// texts) goes on its grid slot, whatever the card's own rectangle. The content box is sized to the rows so the
    /// arrows and a finger drag share the same bounds.
    private void LayoutCards()
    {
        if (itemContainer == null) return;
        var cont = itemContainer.GetComponent<RectTransform>(); if (cont == null) return;
        var cards = new List<ShopItemController>();
        foreach (var c in GetAllShopItems()) if (c != null && c.gameObject.activeSelf && !IsHeldBackCard(c)) cards.Add(c);
        cards.Sort((a, b) => a.price != b.price ? a.price.CompareTo(b.price) : string.CompareOrdinal(a.itemName, b.itemName));
        var bounds = new List<Rect>();
        foreach (var c in cards) { c.transform.localScale = Vector3.one; bounds.Add(VisualBounds((RectTransform)c.transform, cont)); }
        var maxW = 0f; var maxH = 0f;
        foreach (var b in bounds) { maxW = Mathf.Max(maxW, b.width); maxH = Mathf.Max(maxH, b.height); }
        var colPitch = Mathf.Min(maxW * cardScale + 24f, ColumnPitchMax);
        rowPitch = Mathf.Min(maxH * cardScale + 28f, RowPitchMax);   // the people cards' picture boxes are much taller than their figures
        // The visible window is the masked ShopPanel (the container's parent); the scene's ScrollRect sits on the
        // container itself with no viewport, so it is pointed at the panel too, which makes a finger drag work
        var viewport = cont.parent as RectTransform;
        if (itemScrollRect != null && viewport != null) { itemScrollRect.viewport = viewport; itemScrollRect.content = cont; }
        var viewH = viewport != null ? viewport.rect.height : 1400f;
        var rows = Mathf.CeilToInt(cards.Count / (float)Columns);
        var contentH = Mathf.Max(viewH, rows * rowPitch + 40f);
        cont.sizeDelta = new Vector2(cont.sizeDelta.x, contentH);
        var top = contentH / 2f - 20f - rowPitch / 2f;
        for (var i = 0; i < cards.Count; i++)
        {
            var rt = (RectTransform)cards[i].transform;
            var slot = new Vector2(ColumnsCentre + (i % Columns - 1) * colPitch, top - (i / Columns) * rowPitch);
            var offset = bounds[i].center - (Vector2)rt.localPosition;   // the visual centre, relative to the card's own point
            rt.localScale = Vector3.one * cardScale;
            rt.localPosition = new Vector3(slot.x - offset.x * cardScale, slot.y - offset.y * cardScale, rt.localPosition.z);
        }
        var span = Mathf.Max(0f, (contentH - viewH) / 2f);
        topBoundary = -span; bottomBoundary = span;
        scrollAmount = rowPitch;
        cont.anchoredPosition = new Vector2(cont.anchoredPosition.x, topBoundary);
    }

    /// The box around everything a card draws (icon, price, name), in the container's space
    private static Rect VisualBounds(RectTransform card, RectTransform space)
    {
        var min = new Vector2(float.MaxValue, float.MaxValue); var max = new Vector2(float.MinValue, float.MinValue);
        var corners = new Vector3[4];
        foreach (var g in card.GetComponentsInChildren<Graphic>(false))
        {
            var rt = g.rectTransform;
            var tm = g as TMP_Text;
            if (tm != null)
            {
                // a label's rectangle is much taller than its words (300 for a 100 pt price): measure the words
                var pref = tm.GetPreferredValues(tm.text, rt.rect.width, Mathf.Infinity);
                var r = rt.rect; var cy = r.center.y;
                var va = (int)tm.verticalAlignment;
                if (va == (int)VerticalAlignmentOptions.Top) cy = r.yMax - pref.y / 2f; else if (va == (int)VerticalAlignmentOptions.Bottom) cy = r.yMin + pref.y / 2f;
                var lo = rt.TransformPoint(new Vector3(r.xMin, cy - pref.y / 2f)); var hi = rt.TransformPoint(new Vector3(r.xMax, cy + pref.y / 2f));
                Vector2 a = space.InverseTransformPoint(lo), b = space.InverseTransformPoint(hi);
                min = Vector2.Min(min, Vector2.Min(a, b)); max = Vector2.Max(max, Vector2.Max(a, b));
                continue;
            }
            var img = g as Image;
            if (img != null && img.sprite != null && img.preserveAspect)
            {
                // a tall picture box with a small figure in it: only the drawn part counts
                var r = rt.rect; var sa = img.sprite.rect.width / img.sprite.rect.height;
                var w = r.width; var h = w / sa; if (h > r.height) { h = r.height; w = h * sa; }
                var c = r.center;
                Vector2 a = space.InverseTransformPoint(rt.TransformPoint(new Vector3(c.x - w / 2f, c.y - h / 2f)));
                Vector2 b = space.InverseTransformPoint(rt.TransformPoint(new Vector3(c.x + w / 2f, c.y + h / 2f)));
                min = Vector2.Min(min, Vector2.Min(a, b)); max = Vector2.Max(max, Vector2.Max(a, b));
                continue;
            }
            rt.GetWorldCorners(corners);
            foreach (var w in corners) { Vector2 l = space.InverseTransformPoint(w); min = Vector2.Min(min, l); max = Vector2.Max(max, l); }
        }
        if (min.x > max.x) return new Rect((Vector2)card.localPosition + card.rect.min, card.rect.size);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    /// At the bottom of the shop the down arrow keeps showing, with a small lock on it: more comes in the next
    /// chapter (owner, 13 October). The lock is the map's lock_small, laid over the button.
    private void ShowDownLock(bool show)
    {
        if (scrollDownButton == null) return;
        var t = scrollDownButton.transform.Find("Lock");
        if (t == null)
        {
            var sprite = Resources.Load<Sprite>("Map/lock_yellow");   // the coloured lock (owner, 18 October)
            if (sprite == null) return;
            var go = new GameObject("Lock", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(scrollDownButton.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
            var br = scrollDownButton.GetComponent<RectTransform>().rect;
            rt.anchoredPosition = new Vector2(0f, 2f);                                   // in the middle of the arrow (owner, 13 October)
            var size = br.height * 0.78f;
            rt.sizeDelta = new Vector2(size * sprite.rect.width / sprite.rect.height, size);
            var img = go.GetComponent<Image>(); img.sprite = sprite; img.raycastTarget = false; img.preserveAspect = true;
            t = go.transform;
        }
        t.gameObject.SetActive(show);
    }

    private void UpdateScrollButtons()
    {
        if (itemContainer == null) return;

        var containerRect = itemContainer.GetComponent<RectTransform>();
        if (containerRect == null) return;

        var currentY = containerRect.anchoredPosition.y;
        var canScrollUp = currentY > topBoundary + 1f;
        var canScrollDown = currentY < bottomBoundary;
        ShowDownLock(currentY >= bottomBoundary - 1f);   // only when scrolled all the way down

        if (scrollUpButton != null)
            scrollUpButton.interactable = canScrollUp;

        if (scrollDownButton != null)
            scrollDownButton.interactable = true;   // at the bottom the arrow stays bright with its lock (a tap scrolls nowhere)
    }

    private void StartShopAnimation()
    {
        var panelRect = shopPanel.GetComponent<RectTransform>();
        var startPos = new Vector2(-1284, 0);
        var endPos = new Vector2(0, 0);
        StartCoroutine(SlidePanel(panelRect, startPos, endPos, slideAnimationDuration));
    }

    private IEnumerator SlidePanel(RectTransform panel, Vector2 start, Vector2 end, float duration)
    {
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            panel.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }

        panel.anchoredPosition = end;
    }

    public void ShowPurchasePopup(ShopItemController item)
    {
        if (item == null || purchaseConfirmationPopup == null) return;

        currentPurchaseItem = item;

        var iconTransform = item.transform.Find("ItemIcon");
        var iconImage = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
        if (popupItemIcon != null && iconImage != null)
        {
            popupItemIcon.sprite = iconImage.sprite;

            var iconRect = iconTransform.GetComponent<RectTransform>();
            if (iconRect != null)
                popupItemIcon.rectTransform.sizeDelta = iconRect.sizeDelta;
        }

        if (popupItemInfo != null)
        {
            // Chalk on the board (owner, 16 October): the chalk hand (Gloria Hallelujah, open licence; Chalkduster is
            // Apple's and cannot ship), white and fully opaque, the price line in blue chalk, a special note under it
            var chalk = Resources.Load<TMP_FontAsset>("Fonts & Materials/GloriaHallelujah SDF");
            if (chalk != null) popupItemInfo.font = chalk;
            var lines = (item.popupInfoText ?? "").Split('\n');
            if (lines.Length > 0 && lines[0].TrimStart().StartsWith("$")) lines[0] = "<color=#9FD2FF>" + lines[0] + "</color>";
            popupItemInfo.text = string.Join("\n", lines);
            popupItemInfo.color = Color.white;
            popupItemInfo.richText = true;
            popupItemInfo.fontSize = 68f; popupItemInfo.alignment = TextAlignmentOptions.Center;
            popupItemInfo.fontStyle = FontStyles.Bold;
            popupItemInfo.rectTransform.anchorMin = popupItemInfo.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); popupItemInfo.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            if (popupItemIcon != null) popupItemIcon.rectTransform.anchoredPosition = new Vector2(-6f, 380f);
            var mat = popupItemInfo.fontMaterial; if (mat != null && mat.HasProperty("_FaceDilate")) mat.SetFloat("_FaceDilate", 0.08f);
            var note = EnsureNoteLabel(chalk);
            var hasNote = note != null && !string.IsNullOrWhiteSpace(item.specialNote);
            if (note != null) { note.text = item.specialNote ?? ""; note.gameObject.SetActive(hasNote); }
            // fixed places (owner, 16 October): the price and a two-line description in one label, the one-line note
            // under it; every card's text was cut to fit, so nothing is measured and nothing can overlap
            var shift = 0f;
            if (popupItemIcon != null) shift = Mathf.Clamp(190f - (popupItemIcon.rectTransform.anchoredPosition.y - popupItemIcon.rectTransform.sizeDelta.y / 2f), 0f, 60f);   // tall pictures push the block down a little
            popupItemInfo.alignment = TextAlignmentOptions.Top;
            popupItemInfo.rectTransform.sizeDelta = new Vector2(1000f, 330f);
            popupItemInfo.rectTransform.anchoredPosition = new Vector2(0f, -5f - shift);          // spans about +160 to -170
            if (hasNote) { note.rectTransform.sizeDelta = new Vector2(940f, 90f); note.rectTransform.anchoredPosition = new Vector2(0f, -300f - shift); }
        }

        if (confirmPurchaseButton != null)
        {
            var buttonText = confirmPurchaseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = item.purchaseButtonText;
        }

        purchaseConfirmationPopup.transform.localScale = Vector3.one * PopupScale;   // a little smaller than the screen, room left and right (owner, 16 October)
        purchaseConfirmationPopup.SetActive(true);
    }

    private TextMeshProUGUI noteLabel;
    /// The special-note line under the description: same width, smaller, in a warm chalk yellow
    private TextMeshProUGUI EnsureNoteLabel(TMP_FontAsset chalk)
    {
        if (popupItemInfo == null) return null;
        if (noteLabel != null) return noteLabel;
        var go = new GameObject("PopupNote", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(popupItemInfo.transform.parent, false);
        var rt = (RectTransform)go.transform; var src = popupItemInfo.rectTransform;
        rt.anchorMin = src.anchorMin; rt.anchorMax = src.anchorMax; rt.pivot = src.pivot;
        rt.sizeDelta = new Vector2(880f, 130f);
        rt.anchoredPosition = new Vector2(0f, -300f);                       // close under the description, above the buttons (buttons' top is at -519)
        noteLabel = go.GetComponent<TextMeshProUGUI>();
        if (chalk != null) noteLabel.font = chalk; else noteLabel.font = popupItemInfo.font;
        noteLabel.fontSize = 56f; noteLabel.alignment = TextAlignmentOptions.Center; noteLabel.fontStyle = FontStyles.Bold;
        var nm = noteLabel.fontMaterial; if (nm != null && nm.HasProperty("_FaceDilate")) nm.SetFloat("_FaceDilate", 0.08f);
        noteLabel.color = new Color(0.62f, 0.93f, 0.6f); noteLabel.enableWordWrapping = true; noteLabel.raycastTarget = false;   // green chalk (owner, 16 October)
        return noteLabel;
    }

    public void ConfirmPurchase()
    {
        if (currentPurchaseItem == null) return;

        // On failure the popup stays open with the reason; the player cancels it
        if (TryPurchaseItem(currentPurchaseItem))
        {
            AnalyticsLog.ShopPurchase(currentPurchaseItem.itemName, currentPurchaseItem.price);
            if (purchaseConfirmationPopup != null)
                purchaseConfirmationPopup.SetActive(false);
            currentPurchaseItem = null;
        }
    }

    public void CancelPurchase()
    {
        if (purchaseConfirmationPopup != null)
            purchaseConfirmationPopup.SetActive(false);
        currentPurchaseItem = null;
    }
}