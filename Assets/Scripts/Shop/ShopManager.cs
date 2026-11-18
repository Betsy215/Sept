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

        ShopCookies.Reset();
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
        EnsureRemoveAdsCard();   // the one-time Remove Ads card (owner, 9 November 2026)
        EnsureStarPackCard();    // 15 stars for money (owner, 15 November 2026)
        EnsureStarYogaCard();    // the star shop's first card: the yoga lady (owner, 14 November 2026)
        EvenOutCharacterIcons();
        foreach (var card in GetAllShopItems()) ShopCookies.Dress(card);   // the cards' pictures as iced cookies (owner, 17 November 2026)
        EnsureTabs();            // Stars | Coins | $ (owner, 10 November 2026); Coins is the default
        LayoutCards();
        EnsureCookieBook();      // owned things live in a book that is always in the shop (owner, 17 November 2026)
        ShopFlourish.BeginVisit(); ShopFlourish.ApplyTags(GetAllShopItems());   // NEW tags on cards first open this visit (owner, 9 November 2026)
        EnsureRestoreButton();
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
                if (LeavesShop(shopItem)) { shopItem.gameObject.SetActive(false); continue; }       // owned: in the book, not in the shop (17 November)
                if (TabOf(shopItem) != currentTab) { shopItem.gameObject.SetActive(false); continue; }   // other tabs stay hidden (10 November)
                shopItem.gameObject.SetActive(true);
                if (shopItem.itemName == RemoveAdsCardName)
                {
                    if (RemoveAds.Owned) shopItem.MarkAsPurchased(); else shopItem.UpdateAffordability(true);
                    continue;
                }
                if (shopItem.itemName == StarPackCardName) { shopItem.UpdateAffordability(true); continue; }   // can always be bought again
                if (TabOf(shopItem) == ShopTab.Stars)   // paid in stars, bought once
                {
                    if (session != null && session.OwnsStarItem(shopItem.itemName)) shopItem.MarkAsPurchased();
                    else shopItem.UpdateAffordability(session != null && session.StarsAvailable >= shopItem.price);
                    continue;
                }
                var canAfford = session != null && session.CanAfford(shopItem.price);
                if (shopItem.itemType == ItemType.Decor && shopItem.itemName == "Candies" && session != null && session.Candies >= CandyJar.Full)
                    canAfford = false;   // a full jar greys the card out and stops taps (owner, 26 October 2026: "when candy jar is full, disable the candy purchasing")
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

        if (item.itemName == StarPackCardName)
        {
            if (purchaseConfirmationPopup != null) purchaseConfirmationPopup.SetActive(false);
            ShowPurchaseSuccessFeedback("Opening the App Store...");
            StarPack.Buy((ok, msg) =>
            {
                if (ok) { if (AudioManager.Instance != null) AudioManager.Instance.PlayPurchaseSound(); RefreshShopDisplay(); }
                if (!string.IsNullOrEmpty(msg)) { if (ok) ShowPurchaseSuccessFeedback(msg); else ShowPurchaseFailedFeedback(msg); }
            });
            return false;   // no coins change hands; the stars arrive when the App Store confirms
        }

        if (item.itemName == RemoveAdsCardName)
        {
            if (purchaseConfirmationPopup != null) purchaseConfirmationPopup.SetActive(false);
            ShowPurchaseSuccessFeedback("Opening the App Store...");
            RemoveAds.Buy((ok, msg) =>
            {
                if (ok) { item.MarkAsPurchased(); if (AudioManager.Instance != null) AudioManager.Instance.PlayPurchaseSound(); EatBought(item); }
                if (!string.IsNullOrEmpty(msg)) { if (ok) ShowPurchaseSuccessFeedback(msg); else ShowPurchaseFailedFeedback(msg); }
            });
            return false;   // no coins change hands
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

        if (item.itemType == ItemType.Decor && item.itemName == "Candies" && session.Candies >= CandyJar.Full)
        {
            ShowPurchaseFailedFeedback("The candy jar is full");   // nothing charged (26 October 2026)
            return false;
        }

        if (TabOf(item) == ShopTab.Stars)
        {
            // the star shop: the price is in stars, taken and recorded in one save
            if (!session.BuyWithStars(item.itemName, item.price))
            {
                ShowPurchaseFailedFeedback($"Not enough stars!\nNeed: {item.price} | Have: {session.StarsAvailable}");
                return false;
            }
            UpdateScoreDisplay();
            item.MarkAsPurchased();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPurchaseSound();
            EatBought(item);
            RefreshShopDisplay();
            if (purchaseConfirmationPopup != null) purchaseConfirmationPopup.SetActive(false);
            ShowPurchaseSuccessFeedback($"Purchased {DisplayName(item)}!");
            return true;
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
        else if (item.itemType == ItemType.Decor && item.itemName == "Candies")
            purchaseSuccess = session.BuyCandies();                  // the jar's refill, bought again whenever it is not full (owner, 26 October 2026)
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

        // Mark as purchased in UI; the candies stay on sale (owner, 26 October 2026)
        // (the oven card sells twice: after the second oven it stays on sale for the third; it used to be marked owned
        // at once, so the third oven could never be bought)
        if (!(item.itemType == ItemType.Decor && item.itemName == "Candies") && !OvenStillSells(item)) item.MarkAsPurchased();
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPurchaseSound();
        EatBought(item);   // eaten as a cookie (owner, 17 November 2026); before, the picture flew to the way to the table

        // Refresh all items to update affordability
        RefreshShopDisplay();
        ShopFlourish.ApplyTags(GetAllShopItems());

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
            else if (shopItem.itemType == ItemType.Decor && shopItem.itemName == "Candies")
            {
                // always on sale (owner, 26 October 2026); a full jar refuses the buy and the coins come back
            }
            else if (shopItem.itemType == ItemType.Decor && SessionManager.Instance.IsDecorPurchased(shopItem.itemName))
            {
                shopItem.MarkAsPurchased();
            }
            else if (shopItem.itemType == ItemType.Upgrade)
            {
                var currentLevel = SessionManager.Instance.GetFoodUpgradeLevel(shopItem.upgradeFoodType);
                var maxed = !SessionManager.Instance.CanUpgradeFood(shopItem.upgradeFoodType);
                if (shopItem.upgradeFoodType == "Oven" ? maxed : (currentLevel > 1 || maxed)) // upgraded once, or at max level; the oven card sells twice
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
            // "EARNED: $900 [star] 12": the coins, then the stars earned so far behind a little star (owner, 11 November 2026).
            // The star is an Image laid into a gap left in the text, placed from the text's own glyph positions.
            var stars = SessionManager.Instance != null ? SessionManager.Instance.StarsAvailable : 0;   // what is left to spend (the star shop, 14 November)
            playerScoreText.enableWordWrapping = false;
            if (scoreFontMax <= 0f) scoreFontMax = playerScoreText.fontSize;
            playerScoreText.enableAutoSizing = true; playerScoreText.fontSizeMax = scoreFontMax; playerScoreText.fontSizeMin = scoreFontMax * 0.55f;
            playerScoreText.text = $"EARNED: ${Mathf.FloorToInt(currentScore)}<space=1.5em>{stars}";   // one line, whole dollars (owner, 13 October: it wrapped)
            playerScoreText.color = playerScoreOriginalColor;
            scoreMessageSerial++; // any pending feedback restore is now stale
            PlaceStarIcon(stars.ToString().Length);
        }
    }

    private float scoreFontMax;
    private Image scoreStarIcon;

    private void PlaceStarIcon(int digits)
    {
        if (scoreStarIcon == null)
        {
            var sprite = Resources.Load<Sprite>("UI/star_icon"); if (sprite == null) return;
            var go = new GameObject("StarIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(playerScoreText.transform, false);
            scoreStarIcon = go.GetComponent<Image>(); scoreStarIcon.sprite = sprite; scoreStarIcon.preserveAspect = true; scoreStarIcon.raycastTarget = false;
            var rt0 = scoreStarIcon.rectTransform; rt0.anchorMin = rt0.anchorMax = new Vector2(0.5f, 0.5f); rt0.pivot = new Vector2(0.5f, 0.5f);
        }
        playerScoreText.ForceMeshUpdate();
        var info = playerScoreText.textInfo; var first = info.characterCount - digits;
        if (first < 1 || first >= info.characterCount) { scoreStarIcon.enabled = false; return; }
        var digit = info.characterInfo[first]; var em = playerScoreText.fontSize;
        var midY = (digit.bottomLeft.y + digit.topRight.y) * 0.5f;
        var pivotOff = new Vector2(playerScoreText.rectTransform.rect.width * (0.5f - playerScoreText.rectTransform.pivot.x), playerScoreText.rectTransform.rect.height * (0.5f - playerScoreText.rectTransform.pivot.y));
        var rt = scoreStarIcon.rectTransform;
        rt.sizeDelta = new Vector2(em * 1.05f, em * 1.05f);
        rt.anchoredPosition = new Vector2(digit.bottomLeft.x - em * 0.68f, midY + em * 0.04f) - pivotOff;
        scoreStarIcon.enabled = true;
    }

    private void LateUpdate()
    {
        // the star belongs to the EARNED line only; the purchase messages that borrow the same text hide it
        if (scoreStarIcon != null && playerScoreText != null) scoreStarIcon.enabled = playerScoreText.text.StartsWith("EARNED:");
        // the store answers a moment after the shop opens: show or hide the $ tab when its answer changes
        if (tabButtons.Count > 0 && (DollarTabOffered != dollarShown || StarPack.Available != starPackShown)) { starPackShown = StarPack.Available; PaintTabs(); LayoutCards(); RefreshShopDisplay(); }
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
        ShopFlourish.MarkSeen(GetAllShopItems());
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
        // (0.59 grey at half strength: the 0.75 grey it had under the button's grey half tint, which is neutral now)
        var ii = icon.GetComponent<Image>(); if (ii != null && locked) { ii.color = new Color(0.59f, 0.59f, 0.59f, 0.5f); ii.canvasRenderer.SetColor(Color.white); }
    }

    /// Cards that left the shop for good: Apple is owned from the start (owner, 25 October 2026: apple, bread and coffee as the three default items)
    private static readonly string[] RemovedCards = { "Apple" };
    private static bool IsHeldBackCard(ShopItemController card) =>
        card != null && (SessionManager.IsHeldBack(card.itemName?.Trim()) || SessionManager.IsHeldBack(card.upgradeFoodType?.Trim()) || SessionManager.IsHeldBack(card.gameObject.name)
                         || (card.itemName == StarPackCardName && !StarPack.Available)   // only while the App Store offers it
                         || System.Array.IndexOf(RemovedCards, card.itemName?.Trim()) >= 0);

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
    private void LayoutCards(bool keepScroll = false)
    {
        if (itemContainer == null) return;
        var cont = itemContainer.GetComponent<RectTransform>(); if (cont == null) return;
        var scrolledTo = cont.anchoredPosition.y;
        var cards = new List<ShopItemController>();
        foreach (var c in GetAllShopItems())
        {
            if (c == null || IsHeldBackCard(c)) continue;
            if (LeavesShop(c)) { c.gameObject.SetActive(false); continue; }     // owned things are in the book (17 November)
            var onTab = TabOf(c) == currentTab;
            c.gameObject.SetActive(onTab);                                      // only the current tab's cards show
            if (onTab) cards.Add(c);
        }
        cards.Sort((a, b) =>
        {
            var ra = a.itemName == RemoveAdsCardName; var rb = b.itemName == RemoveAdsCardName;
            if (ra != rb) return ra ? 1 : -1;                                   // Remove Ads at the very end
            return a.price != b.price ? a.price.CompareTo(b.price) : string.CompareOrdinal(a.itemName, b.itemName);
        });
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
            // sideways the visual centre goes on the slot; vertically the card's BOTTOM (its price line) goes on the row's
            // baseline, so every card in a row stands on the same line whatever its picture's height (owner, 26 October
            // 2026: the small plant floated above the plates and the oven once the price sort moved it into their row)
            var offset = bounds[i].center - (Vector2)rt.localPosition;
            var bottomOffset = bounds[i].yMin - rt.localPosition.y;
            var baseline = slot.y - rowPitch / 2f + 14f;
            rt.localScale = Vector3.one * cardScale;
            rt.localPosition = new Vector3(slot.x - offset.x * cardScale, baseline - bottomOffset * cardScale, rt.localPosition.z);
        }
        var span = Mathf.Max(0f, (contentH - viewH) / 2f);
        topBoundary = -span; bottomBoundary = span;
        scrollAmount = rowPitch;
        // after a purchase the cards close up where the player was looking; a fresh layout starts at the top
        cont.anchoredPosition = new Vector2(cont.anchoredPosition.x, keepScroll ? Mathf.Clamp(scrolledTo, topBoundary, bottomBoundary) : topBoundary);
    }

    // --- Owned things leave the shop for the book (owner, 17 November 2026) ------------------------------------------

    /// Whether the player has what a card sells, read from the save itself (not from the card's own flag, which is only
    /// set once the card has been shown).
    private static bool IsOwned(ShopItemController c)
    {
        if (c == null) return false;
        if (c.itemName == RemoveAdsCardName) return RemoveAds.Owned;
        if (c.itemName == StarPackCardName) return false;                       // can always be bought again
        var s = SessionManager.Instance; if (s == null || !s.HasActiveSession()) return false;
        if (TabOf(c) == ShopTab.Stars) return s.OwnsStarItem(c.itemName);
        var data = s.GetCurrentSession();
        switch (c.itemType)
        {
            case ItemType.Food: return data.purchasedFoodItems.Contains(c.itemName);
            case ItemType.Character: return data.purchasedCharacters.Contains(c.itemName);
            case ItemType.Decor: return c.itemName != "Candies" && s.IsDecorPurchased(c.itemName);   // the candies stay on sale
            case ItemType.Upgrade: return s.GetFoodUpgradeLevel(c.upgradeFoodType) > 1 || !s.CanUpgradeFood(c.upgradeFoodType);
        }
        return false;
    }

    /// The oven card sells the second and then the third oven: it stays in the shop until all three are owned.
    private static bool OvenStillSells(ShopItemController c) =>
        c != null && c.itemType == ItemType.Upgrade && c.upgradeFoodType == "Oven" && SessionManager.Instance != null && SessionManager.Instance.CanUpgradeFood("Oven");

    private static bool LeavesShop(ShopItemController c) => IsOwned(c) && !OvenStillSells(c);

    private void EnsureCookieBook()
    {
        if (scrollUpButton == null) return;
        var chalk = Resources.Load<TMP_FontAsset>("Fonts & Materials/GloriaHallelujah SDF");
        ShopCookies.EnsureBookButton(scrollUpButton.transform as RectTransform, () => !IsPurchasePopupOpen, OwnedEntries,
            playerScoreText != null ? playerScoreText.font : null, chalk, panelImage);
    }

    /// What the book shows: every owned thing as its cookie, cheapest first, with its name and what it does.
    private List<ShopCookies.Entry> OwnedEntries()
    {
        var list = new List<ShopCookies.Entry>();
        var cards = new List<ShopItemController>(GetAllShopItems());
        cards.Sort((a, b) => a.price != b.price ? a.price.CompareTo(b.price) : string.CompareOrdinal(a.itemName, b.itemName));
        foreach (var c in cards)
        {
            if (c == null || !IsOwned(c)) continue;
            var cookie = ShopCookies.For(c);
            if (cookie == null) { var icon = c.transform.Find("ItemIcon"); var img = icon != null ? icon.GetComponent<Image>() : null; cookie = img != null ? img.sprite : null; }
            if (cookie == null) continue;
            var lines = new List<string>((c.popupInfoText ?? "").Split('\n'));
            if (lines.Count > 0) lines.RemoveAt(0);                              // without the price line
            var info = string.Join(" ", lines).Trim();
            if (!string.IsNullOrWhiteSpace(c.specialNote)) info += "\n" + c.specialNote.Trim();
            var shown = DisplayName(c);
            if (c.itemType == ItemType.Upgrade && c.upgradeFoodType == "Oven")
                shown = SessionManager.Instance != null && SessionManager.Instance.OvensOwned >= 3 ? "Three ovens" : "Two ovens";
            list.Add(new ShopCookies.Entry { cookie = cookie, name = shown, info = info });
        }
        return list;
    }

    /// The bought thing is eaten as a cookie, bite by bite; meanwhile its card has left and the others have closed up,
    /// and at the end the book's button hops (owner, 17 November 2026). A card without a cookie picture flies to the
    /// way out as before.
    private void EatBought(ShopItemController item)
    {
        var cookie = ShopCookies.For(item);
        var root = popupItemIcon != null && popupItemIcon.canvas != null ? popupItemIcon.canvas.rootCanvas.transform as RectTransform : null;
        if (cookie == null || root == null)
        {
            var wayOut = GameObject.Find("NextLevelButton");
            ShopFlourish.Fly(this, popupItemIcon, wayOut != null ? wayOut.transform as RectTransform : null);
            return;
        }
        LayoutCards(true); RefreshShopDisplay(); UpdateScrollButtons();
        ShopCookies.Eat(this, root, cookie, () => ShopCookies.BumpBook(this));
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
        var panelRect = panelGroup != null ? panelGroup : shopPanel.GetComponent<RectTransform>();   // the group carries panel and tabs together
        // slide in from the left to the panel's own saved place (the scene keeps it at y 20; the old end point y 0 dropped
        // the panel 20 units after the tabs had been laid on its edge, which left a band between them: owner, 10 November)
        var endPos = panelRect.anchoredPosition;
        var startPos = new Vector2(-1284, endPos.y);
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
            popupItemIcon.preserveAspect = true;   // the cookies are not all square (17 November)

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
            // the price line in blue chalk, for a price in stars as well (owner, 14 November 2026: the star card "use the same layout as the coins tab")
            if (lines.Length > 0 && (lines[0].TrimStart().StartsWith("$") || TabOf(item) == ShopTab.Stars)) lines[0] = "<color=#9FD2FF>" + lines[0] + "</color>";
            var shown = DisplayName(item);                                                                    // the card's name on top (owner, 18 October: "why is it a big eater instead of chef")
            popupItemInfo.text = TextStars.Mark((shown != null ? shown + "\n" : "") + string.Join("\n", lines));   // a star picture where the text says [*]
            popupItemInfo.color = Color.white;
            popupItemInfo.richText = true;
            popupItemInfo.fontSize = 70f; popupItemInfo.lineSpacing = -18f; popupItemInfo.alignment = TextAlignmentOptions.Center;   // one size, tighter lines (owner, 18 October)
            popupItemInfo.fontStyle = FontStyles.Bold;
            // One text size on every card (owner, 15 November 2026: "Unity the font size in shop item panels, the size in
            // coins tab looks good"). Remove Ads has two full sentences under its price, six lines at 70: instead of the
            // smaller letters it had since 14 November, its lines sit a little closer and the block starts higher.
            var longText = item.itemName == RemoveAdsCardName;
            popupItemInfo.enableAutoSizing = false;
            if (longText) popupItemInfo.lineSpacing = -34f;
            popupItemInfo.rectTransform.anchorMin = popupItemInfo.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); popupItemInfo.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            if (popupItemIcon != null) popupItemIcon.rectTransform.anchoredPosition = new Vector2(0f, 380f);   // dead centre, like the texts (owner, 18 October)
            popupItemInfo.margin = Vector4.zero;                                                               // the scene's right margin pushed the texts left
            var mat = popupItemInfo.fontMaterial; if (mat != null && mat.HasProperty("_FaceDilate")) mat.SetFloat("_FaceDilate", 0.08f);
            var note = EnsureNoteLabel(chalk);
            var hasNote = note != null && !string.IsNullOrWhiteSpace(item.specialNote);
            if (note != null) { note.text = item.specialNote ?? ""; note.gameObject.SetActive(hasNote); }
            // fixed places (owner, 16 October): the price and a two-line description in one label, the one-line note
            // under it; every card's text was cut to fit, so nothing is measured and nothing can overlap
            var shift = 0f;
            if (popupItemIcon != null) shift = Mathf.Clamp(190f - (popupItemIcon.rectTransform.anchoredPosition.y - popupItemIcon.rectTransform.sizeDelta.y / 2f), 0f, 40f);   // tall pictures push the block down a little
            popupItemInfo.alignment = TextAlignmentOptions.Top;
            popupItemInfo.rectTransform.sizeDelta = new Vector2(1000f, 420f);
            popupItemInfo.rectTransform.anchoredPosition = new Vector2(0f, -50f - shift);         // spans about +160 to -260
            if (longText)
            {
                popupItemInfo.rectTransform.anchoredPosition = new Vector2(0f, 20f);            // six lines: the block starts 70 higher
                if (popupItemIcon != null) popupItemIcon.rectTransform.anchoredPosition = new Vector2(0f, 440f);
            }
            if (hasNote) { note.fontSize = 66f; note.lineSpacing = -18f; note.rectTransform.sizeDelta = new Vector2(1000f, 200f); note.rectTransform.anchoredPosition = new Vector2(0f, -405f - shift * 0.5f); }   // a clear gap under the description   // up to two lines (Chef, Yoga coach: 18 October)
        }

        if (confirmPurchaseButton != null)
        {
            var buttonText = confirmPurchaseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = TextStars.Mark(item.purchaseButtonText);
            }
        }

        purchaseConfirmationPopup.transform.localScale = Vector3.one * PopupScale;   // a little smaller than the screen, room left and right (owner, 16 October)
        purchaseConfirmationPopup.SetActive(true);
        // the star pictures in the texts, now that the popup is on and its letters are laid out (15 November 2026)
        TextStars.Place(popupItemInfo);
        if (confirmPurchaseButton != null) TextStars.Place(confirmPurchaseButton.GetComponentInChildren<TextMeshProUGUI>());
    }

    /// The name shown on the popup's first line (the card keys stay as they are; the two Upgrade cards differ by object name)
    private static readonly Dictionary<string, string> popupNames = new Dictionary<string, string> {
        { "Yoga", "Yoga coach" }, { "PotFlowers", "Flower pot" }, { "PotGreen", "Small plant" }, { "Candies", "Candies" }, { "Cake", "Strawberry cake" }, { "IceCream", "Ice cream" },
        { "CoffeeMachineUpgrade", "Coffee machine" }, { "CoffeeUpgrade", "Extra cup" }, { "IceCreamUpgrade", "Two more flavours" } };
    private static string DisplayName(ShopItemController item)
    {
        if (item == null) return null;
        if (item.gameObject.name == "OvenUpgrade" || item.upgradeFoodType == "Oven")   // the same card sells the second and then the third oven (25 October)
            return SessionManager.Instance != null && SessionManager.Instance.OvensOwned >= 2 ? "Third oven" : "Second oven";
        if (popupNames.TryGetValue(item.gameObject.name, out var byObject)) return byObject;
        var key = item.itemName ?? "";
        if (popupNames.TryGetValue(key, out var byKey)) return byKey;
        return key.Length > 0 ? key : null;
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

    // --- Remove Ads (owner, 9 November 2026): a card cloned from the Candies card, paid through the App Store ---------
    public const string RemoveAdsCardName = "RemoveAds";

    private void EnsureRemoveAdsCard()
    {
        if (itemContainer == null) return;
        foreach (var c in GetAllShopItems()) if (c != null && c.itemName == RemoveAdsCardName) return;
        ShopItemController template = null;
        foreach (var c in GetAllShopItems()) if (c != null && c.itemName == "Candies") { template = c; break; }
        if (template == null) return;
        var go = Instantiate(template.gameObject, template.transform.parent);
        go.name = RemoveAdsCardName; go.SetActive(true);
        var card = go.GetComponent<ShopItemController>();
        card.itemName = RemoveAdsCardName; card.itemType = ItemType.Decor; card.price = 0; card.isPurchased = false;
        card.upgradeFoodType = ""; card.specialNote = "";
        card.popupInfoText = RemoveAds.PriceText + "\nNo more ads between days.\nThe treat and coin ads stay.";
        card.purchaseButtonText = "Buy";
        var icon = Resources.Load<Sprite>("UI/remove_ads");
        var iconT = go.transform.Find("ItemIcon"); var img = iconT != null ? iconT.GetComponent<Image>() : null;
        if (img != null && icon != null) img.sprite = icon;
        var priceT = go.transform.Find("PriceText"); var price = priceT != null ? priceT.GetComponent<TextMeshProUGUI>() : null;
        if (price != null) price.text = RemoveAds.PriceText;
        RemoveAds.Init();
        if (!popupNames.ContainsKey(RemoveAdsCardName)) popupNames[RemoveAdsCardName] = "Remove ads";
    }

    // --- Stars for money (owner, 15 November 2026): one pack of 15 stars on the $ tab, a card cloned like Remove Ads ---
    public const string StarPackCardName = "DollarStars";   // not "Star...": names that start so belong to the star tab
    private bool starPackShown;
    private static bool DollarTabOffered => RemoveAds.Available || StarPack.Available;

    private void EnsureStarPackCard()
    {
        if (itemContainer == null) return;
        foreach (var c in GetAllShopItems()) if (c != null && c.itemName == StarPackCardName) return;
        ShopItemController template = null;
        foreach (var c in GetAllShopItems()) if (c != null && c.itemName == "Candies") { template = c; break; }
        if (template == null) return;
        var go = Instantiate(template.gameObject, template.transform.parent);
        go.name = StarPackCardName; go.SetActive(true);
        var card = go.GetComponent<ShopItemController>();
        card.itemName = StarPackCardName; card.itemType = ItemType.Decor; card.price = 0; card.isPurchased = false;
        card.upgradeFoodType = ""; card.specialNote = "";
        card.popupInfoText = StarPack.PriceText + "\n" + StarPack.Stars + " " + TextStars.Token + " for the star shop.";   // the star picture, not the word (15 November)
        card.purchaseButtonText = "Buy";
        var icon = Resources.Load<Sprite>("UI/star_pack");
        var iconT = go.transform.Find("ItemIcon"); var img = iconT != null ? iconT.GetComponent<Image>() : null;
        if (img != null && icon != null) img.sprite = icon;
        var priceT = go.transform.Find("PriceText"); var price = priceT != null ? priceT.GetComponent<TextMeshProUGUI>() : null;
        if (price != null) price.text = StarPack.PriceText;
        RemoveAds.Init();
        if (!popupNames.ContainsKey(StarPackCardName)) popupNames[StarPackCardName] = StarPack.Stars + " " + TextStars.Token;
        starPackShown = StarPack.Available;
    }

    // --- The star shop's first card (owner, 14 November 2026): the yoga lady, 15 stars, "hire her to calm customers
    // down"; the icon is her sitting on her mat. Cloned from the Candies card like the Remove Ads card. ----------------
    private void EnsureStarYogaCard()
    {
        if (itemContainer == null) return;
        foreach (var c in GetAllShopItems()) if (c != null && c.itemName == YogaHire.ItemName) return;
        ShopItemController template = null;
        foreach (var c in GetAllShopItems()) if (c != null && c.itemName == "Candies") { template = c; break; }
        if (template == null) return;
        var go = Instantiate(template.gameObject, template.transform.parent);
        go.name = YogaHire.ItemName; go.SetActive(true);
        var card = go.GetComponent<ShopItemController>();
        card.itemName = YogaHire.ItemName; card.itemType = ItemType.Decor; card.price = YogaHire.StarPrice; card.isPurchased = false;
        card.upgradeFoodType = ""; card.specialNote = "";
        card.popupInfoText = YogaHire.StarPrice + " " + TextStars.Token + "\nHire her to calm\ncustomers down";   // the star picture, not the word (15 November)
        card.purchaseButtonText = YogaHire.StarPrice + " " + TextStars.Token;
        // her sitting, on a square picture with air under her like the coins tab's pictures, so the price keeps the
        // same distance from the picture as theirs (owner, 14 November 2026: "the icon and text too close")
        var icon = Resources.Load<Sprite>("Yoga/shop_icon");
        var iconT = go.transform.Find("ItemIcon"); var img = iconT != null ? iconT.GetComponent<Image>() : null;
        if (img != null && icon != null) { img.sprite = icon; img.preserveAspect = true; }
        var priceT = go.transform.Find("PriceText"); var price = priceT != null ? priceT.GetComponent<TextMeshProUGUI>() : null;
        if (price != null)
        {
            // "15" with the cookie star in front of it, in place of the "$"
            price.text = YogaHire.StarPrice.ToString();
            var star = Resources.Load<Sprite>("UI/star_icon");
            if (star != null)
            {
                price.ForceMeshUpdate();
                var size = price.fontSize * 0.9f; var gap = price.fontSize * 0.28f; var textWidth = price.preferredWidth;   // the gap of the "$ 100" cards
                var sgo = new GameObject("StarIcon", typeof(RectTransform), typeof(Image));
                sgo.transform.SetParent(price.transform, false);
                var srt = (RectTransform)sgo.transform;
                srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f); srt.pivot = new Vector2(0.5f, 0.5f);
                srt.sizeDelta = new Vector2(size, size);
                srt.anchoredPosition = new Vector2(-textWidth / 2f - gap / 2f, 0f);   // just left of the centred number, which the margin below has moved right
                var si = sgo.GetComponent<Image>(); si.sprite = star; si.preserveAspect = true; si.raycastTarget = false;
                price.margin = new Vector4(size + gap, 0f, 0f, 0f);   // the number moves right by the star's width, so the pair stays centred
            }
        }
        if (!popupNames.ContainsKey(YogaHire.ItemName)) popupNames[YogaHire.ItemName] = "Yoga lady";
    }

    /// Apple wants a way to restore a purchase: a small text button at the bottom of the shop.
    private void EnsureRestoreButton()
    {
        if (nextLevelButton == null || GameObject.Find("RestorePurchases") != null) return;
        var canvas = nextLevelButton.GetComponentInParent<Canvas>(); if (canvas == null) return;
        var go = new GameObject("RestorePurchases", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(Button));
        go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 18f); rt.sizeDelta = new Vector2(600f, 60f);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = "Restore purchases"; t.fontSize = 30f; t.alignment = TextAlignmentOptions.Center; t.color = new Color(0.25f, 0.2f, 0.18f, 0.75f);
        if (playerScoreText != null && playerScoreText.font != null) t.font = playerScoreText.font;
        go.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (IsPurchasePopupOpen || restoreAsking) return;
            // The answer is written on the button itself (owner, 15 November 2026: "I don't see it has any feedback when
            // I click on it"; it was written in the EARNED label at the top of the screen, far from the tap).
            restoreAsking = true; t.text = "Checking with the App Store..."; var asked = ++restoreSerial;
            StartCoroutine(RestoreAnswer(t, asked, "No answer from the App Store", 12f));   // if the store never calls back
            RemoveAds.Restore((ok, msg) =>
            {
                if (asked != restoreSerial || t == null) return;
                RefreshShopDisplay(); StartCoroutine(RestoreAnswer(t, ++restoreSerial, msg, 0f));
            });
        });
        restoreButton = go; go.SetActive(RemoveAds.Available);   // only while the store offers the purchase
    }

    private bool restoreAsking; private int restoreSerial;
    /// Writes the store's answer on the restore button after `wait` seconds, keeps it for three, then the button's own words.
    private IEnumerator RestoreAnswer(TextMeshProUGUI label, int serial, string message, float wait)
    {
        if (wait > 0f) yield return new WaitForSecondsRealtime(wait);
        if (serial != restoreSerial || label == null) yield break;
        if (wait > 0f) restoreSerial++;   // the late answer of a store that did not reply in time is dropped
        label.text = message;
        yield return new WaitForSecondsRealtime(3f);
        if (label != null) label.text = "Restore purchases";
        restoreAsking = false;
    }

    // --- Shop tabs (owner, 10 November 2026): left Stars, middle Coins (default, the existing items), right $ -------
    public enum ShopTab { Stars = 0, Coins = 1, Dollars = 2 }
    private ShopTab currentTab = ShopTab.Coins;
    private readonly List<Button> tabButtons = new List<Button>();
    private TextMeshProUGUI tabNote;

    private static ShopTab TabOf(ShopItemController c)
    {
        if (c == null) return ShopTab.Coins;
        if (c.itemName == RemoveAdsCardName || c.itemName == StarPackCardName) return ShopTab.Dollars;
        if (c.gameObject.name.StartsWith("Star") || (c.itemName != null && c.itemName.StartsWith("Star"))) return ShopTab.Stars;
        return ShopTab.Coins;
    }

    private Image panelImage;
    private RectTransform panelRect;
    private float tabRowLeft, tabRowWidth; private bool dollarShown = true; private GameObject restoreButton;
    // 120 since 15 November 2026 (owner: "the tab itself needs to be taller so icons fits better"); the EARNED line is
    // lifted by the same 34 (ScoreLift), so the gap the owner asked for on 13 November stays
    private const float TabHeight = 120f;
    private static readonly string[] TabIcons = { "tab_star", "tab_coin", "tab_dollar" };   // the tabs' pictures, in the order Stars, Coins, $   // 104 touched the EARNED line above (review captures, 11 November)
    private const float ScoreLift = 56f + 34f;   // the EARNED line still sat on the open tab's top edge (owner, 13 November: "add some space between earned text line and the shop panel"): half a letter height of air
    private RectTransform panelGroup;   // the panel and its tabs live in this one object; the slide-in moves it (owner, round 1: "make tab and panel a whole")

    private void EnsureTabs()
    {
        if (itemContainer == null || tabButtons.Count > 0) return;
        var viewport = itemContainer.transform.parent as RectTransform;              // the masked ShopPanel
        if (viewport == null) return;
        panelRect = viewport; panelImage = viewport.GetComponent<Image>();
        // the panel drops its grey-rimmed built-in sprite for a plain rectangle, the same as the tabs: the open tab and the
        // panel then render the very same colour and read as one piece, no edge between them (owner, 10 November)
        if (panelImage != null) panelImage.sprite = null;
        var canvasRt = viewport.parent as RectTransform; if (canvasRt == null) return;
        var font = playerScoreText != null ? playerScoreText.font : null;
        if (playerScoreText != null) playerScoreText.rectTransform.anchoredPosition += new Vector2(0f, ScoreLift);   // once: this method runs once per shop visit
        // One object for the panel and its tabs: a group that takes the panel's exact rectangle and place, the panel
        // re-parented inside it at zero offset, the tabs beside it. The slide-in moves the group, so panel and tabs
        // are one piece in motion as well as in looks.
        var groupGo = new GameObject("ShopPanelGroup", typeof(RectTransform));
        groupGo.transform.SetParent(canvasRt, false);
        panelGroup = groupGo.GetComponent<RectTransform>();
        panelGroup.anchorMin = viewport.anchorMin; panelGroup.anchorMax = viewport.anchorMax; panelGroup.pivot = viewport.pivot;
        panelGroup.anchoredPosition = viewport.anchoredPosition; panelGroup.sizeDelta = viewport.rect.size; panelGroup.localScale = viewport.localScale;
        panelGroup.SetSiblingIndex(viewport.GetSiblingIndex());
        viewport.SetParent(panelGroup, false);
        viewport.anchorMin = viewport.anchorMax = new Vector2(0.5f, 0.5f); viewport.pivot = new Vector2(0.5f, 0.5f); viewport.anchoredPosition = Vector2.zero;
        viewport.localScale = Vector3.one;   // the group carries the panel's scale now; left on both, the panel shrank a fifth inside the group (found 10 November)
        var parent = panelGroup;
        // the panel's edges in the group's space (the group is the panel's rectangle)
        var tl = new Vector2(-parent.rect.width * 0.5f, parent.rect.height * 0.5f); var br = new Vector2(parent.rect.width * 0.5f, -parent.rect.height * 0.5f);
        var pivotOff = Vector2.zero;
        // folder tabs (owner, 10 November: "tabs a part of panel, like a part sticking out"): the three tabs share the panel's
        // full width with no gaps, the open one is taller and in the panel's own colour so it is one shape with it, the
        // closed ones sit lower and a shade darker behind
        var panelW = br.x - tl.x; var gap = 0f; var tabW = Mathf.Floor(panelW / 3f); var tabH = TabHeight; var overlap = 1f;   // 1 px into the panel: no seam
        tabRowLeft = tl.x - pivotOff.x; tabRowWidth = panelW;
        string[] labels = { "Stars", "Coins", "$" };
        for (var i = 0; i < 3; i++)
        {
            var go = new GameObject("Tab" + labels[i], typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0f);
            var cx = tl.x + tabW * (i + 0.5f);
            var pos = new Vector2(cx, tl.y - overlap) - pivotOff; rt.anchoredPosition = new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));   // whole units: no anti-aliased hairline
            rt.sizeDelta = new Vector2(tabW, tabH + overlap);
            var img = go.GetComponent<Image>();
            img.sprite = null; if (panelImage != null) img.material = panelImage.material;   // a plain rectangle in the panel's colour reads as one piece with it
            var lgo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            lgo.transform.SetParent(go.transform, false);
            var lrt = lgo.GetComponent<RectTransform>(); lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = new Vector2(0f, overlap); lrt.offsetMax = Vector2.zero;
            var t = lgo.GetComponent<TextMeshProUGUI>(); t.text = labels[i]; t.fontSize = 44f; t.alignment = TextAlignmentOptions.Center;
            t.color = new Color(0.30f, 0.22f, 0.18f); t.fontStyle = FontStyles.Bold; if (font != null) t.font = font;
            // a picture instead of the word (owner, 15 November 2026: "On the tab, use icon for coin, star and dollar, and
            // unify their style"): Resources/UI/tab_star, tab_coin, tab_dollar, one painted set (Store/ui/shop_tab_icons_2026-11-15)
            var icon = Resources.Load<Sprite>("UI/" + TabIcons[i]);
            if (icon != null)
            {
                t.text = "";
                var igo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); igo.transform.SetParent(go.transform, false);
                var irt = (RectTransform)igo.transform; irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f); irt.pivot = new Vector2(0.5f, 0.5f);
                irt.sizeDelta = new Vector2(tabH * 0.86f, tabH * 0.86f); irt.anchoredPosition = new Vector2(0f, overlap * 0.5f);   // inside the tab, with a little room around
                var iimg = igo.GetComponent<Image>(); iimg.sprite = icon; iimg.preserveAspect = true; iimg.raycastTarget = false;
            }
            var tab = (ShopTab)i;
            go.GetComponent<Button>().onClick.AddListener(() => SetTab(tab));
            tabButtons.Add(go.GetComponent<Button>());
        }
        var ngo = new GameObject("TabNote", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        ngo.transform.SetParent(parent, false);
        var nrt = ngo.GetComponent<RectTransform>(); nrt.anchorMin = nrt.anchorMax = new Vector2(0.5f, 0.5f);
        nrt.anchoredPosition = new Vector2((tl.x + br.x) * 0.5f, (tl.y + br.y) * 0.5f) - pivotOff; nrt.sizeDelta = new Vector2(800f, 120f);
        tabNote = ngo.GetComponent<TextMeshProUGUI>(); tabNote.fontSize = 44f; tabNote.alignment = TextAlignmentOptions.Center;
        tabNote.color = new Color(0.30f, 0.22f, 0.18f, 0.85f); if (font != null) tabNote.font = font; tabNote.text = ""; ngo.SetActive(false);
        PaintTabs();
    }

    private void SetTab(ShopTab tab)
    {
        if (IsPurchasePopupOpen) return;
        currentTab = tab;
        PaintTabs();
        LayoutCards();
        ShopFlourish.ApplyTags(GetAllShopItems());
        if (itemScrollRect != null) itemScrollRect.verticalNormalizedPosition = 1f;
        RefreshShopDisplay();
        UpdateScrollButtons();
        StartCoroutine(RefreshAfterTabSwitch());
    }

    /// A card shown for the first time runs its Start() after this frame and paints itself unaffordable; refresh once more.
    private IEnumerator RefreshAfterTabSwitch()
    {
        yield return new WaitForEndOfFrame();
        RefreshShopDisplay();
        UpdateScrollButtons();
    }

    private void PaintTabs()
    {
        // one piece with the panel: the open tab wears the panel's own sprite and colour and sits just in front of the
        // panel, so its foot covers the panel's top edge; the closed tabs are a shade darker and sit behind the panel
        var baseCol = panelImage != null ? panelImage.color : new Color(1f, 0.96f, 0.86f, 0.92f);
        var closed = new Color(baseCol.r * 0.90f, baseCol.g * 0.90f, baseCol.b * 0.90f, baseCol.a);
        // the $ tab exists only while the App Store offers Remove Ads; the tabs that show share the panel's width
        dollarShown = DollarTabOffered;
        if (!dollarShown && currentTab == ShopTab.Dollars) currentTab = ShopTab.Coins;
        var shown = 0; for (var i = 0; i < tabButtons.Count; i++) if (i != (int)ShopTab.Dollars || dollarShown) shown++;
        var w = shown > 0 ? Mathf.Floor(tabRowWidth / shown) : 0f; var slot = 0;
        var restore = GameObject.Find("RestorePurchases"); if (restore != null || restoreButton != null) { if (restoreButton == null) restoreButton = restore; restoreButton.SetActive(dollarShown); }
        for (var i = 0; i < tabButtons.Count; i++)
        {
            var visible = i != (int)ShopTab.Dollars || dollarShown;
            tabButtons[i].gameObject.SetActive(visible);
            if (!visible) continue;
            var prt = tabButtons[i].GetComponent<RectTransform>();
            prt.anchoredPosition = new Vector2(Mathf.Round(tabRowLeft + w * (slot + 0.5f)), prt.anchoredPosition.y); prt.sizeDelta = new Vector2(w, prt.sizeDelta.y); slot++;
            var on = i == (int)currentTab;
            var img = tabButtons[i].GetComponent<Image>();
            if (img != null) img.color = on ? baseCol : closed;
            var trt = tabButtons[i].GetComponent<RectTransform>();
            if (trt != null) trt.sizeDelta = new Vector2(trt.sizeDelta.x, (on ? TabHeight : TabHeight - 18f) + 1f);   // the open tab sticks out higher
            var lbl = tabButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null) lbl.alpha = on ? 1f : 0.6f;
            var ico = tabButtons[i].transform.Find("Icon"); var icoImg = ico != null ? ico.GetComponent<Image>() : null;
            if (icoImg != null) { icoImg.color = new Color(1f, 1f, 1f, on ? 1f : 0.6f); ico.localScale = Vector3.one * (on ? 1f : 0.86f); }   // the closed tabs a little paler and smaller
            if (panelRect != null)
            {
                var t = tabButtons[i].transform;
                if (on) t.SetSiblingIndex(panelRect.GetSiblingIndex() + 1); else t.SetSiblingIndex(Mathf.Max(0, panelRect.GetSiblingIndex() - 1));
            }
        }
        if (tabNote != null)
        {
            var any = false;
            foreach (var c in GetAllShopItems()) if (c != null && !IsHeldBackCard(c) && TabOf(c) == currentTab) { any = true; break; }
            tabNote.text = currentTab == ShopTab.Stars ? "The star shop opens soon" : "";
            tabNote.gameObject.SetActive(!any && tabNote.text.Length > 0);
        }
    }
}
