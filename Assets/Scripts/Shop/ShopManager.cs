using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.VisualScripting;

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

    private void Start()
    {
        SetupButtonListeners();
        AudioManager.Instance.PlayShopMusic();

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
        if (confirmPurchaseButton != null)
        {
            confirmPurchaseButton.onClick.RemoveAllListeners();
            confirmPurchaseButton.onClick.AddListener(ConfirmPurchase);
        }

        if (cancelPurchaseButton != null)
        {
            cancelPurchaseButton.onClick.RemoveAllListeners();
            cancelPurchaseButton.onClick.AddListener(CancelPurchase);
        }

        if (scrollUpButton != null)
        {
            scrollUpButton.onClick.RemoveAllListeners();
            scrollUpButton.onClick.AddListener(ScrollUp);
        }

        if (scrollDownButton != null)
        {
            scrollDownButton.onClick.RemoveAllListeners();
            scrollDownButton.onClick.AddListener(ScrollDown);
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    private ShopItemController[] GetAllShopItems()
    {
        if (itemContainer == null) return new ShopItemController[0];
        return itemContainer.GetComponentsInChildren<ShopItemController>(true);
    }

    private void RefreshShopDisplay()
    {
        var allShopItems = GetAllShopItems();
        var currentScore = SessionManager.Instance.GetTotalScore();

        // Show all items and check affordability only
        foreach (var shopItem in allShopItems)
            if (shopItem != null)
            {
                shopItem.gameObject.SetActive(true);
                var canAfford = SessionManager.Instance.CanAfford(shopItem.price);
                shopItem.UpdateAffordability(canAfford);
            }
    }

    public void PurchaseItem(ShopItemController item)
    {
        // Check if SessionManager is available
        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession())
        {
            ShowPurchaseFailedFeedback("No active session!");
            return;
        }

        // Check if already purchased
        if (item.isPurchased)
        {
            ShowPurchaseFailedFeedback("Already purchased!");
            return;
        }

        // Get current score from session
        var currentScore = SessionManager.Instance.GetTotalScore();

        // Check if player has enough score
        if (!SessionManager.Instance.CanAfford(item.price))
        {
            ShowPurchaseFailedFeedback($"Not enough points!\nNeed: {item.price} | Have: {currentScore}");
            return;
        }

        // Proceed with purchase
        var purchaseSuccess = false;

        // Purchase based on item type
        if (item.itemType == ItemType.Food)
            purchaseSuccess = SessionManager.Instance.PurchaseFoodItem(item.itemName);
        else if (item.itemType == ItemType.Character)
            purchaseSuccess = SessionManager.Instance.PurchaseCharacter(item.itemName);
        else if (item.itemType == ItemType.Upgrade)
            purchaseSuccess = SessionManager.Instance.UpgradeFood(item.upgradeFoodType);

        if (purchaseSuccess)
        {
            // Deduct score from session
            SessionManager.Instance.DeductScore(item.price);

            // Update score display
            UpdateScoreDisplay();

            // Mark as purchased in UI
            item.MarkAsPurchased();
            AudioManager.Instance.PlayPurchaseSound();

            // Refresh all items to update affordability
            RefreshShopDisplay();

            Debug.Log($"Successfully purchased {item.itemName} for {item.price} points!");

            // Close popup on success
            purchaseConfirmationPopup.SetActive(false);

            // Show success feedback
            ShowPurchaseSuccessFeedback($"Purchased {item.itemName}!");
        }
        else
        {
            Debug.LogError($"Failed to purchase {item.itemName}");
            ShowPurchaseFailedFeedback("Purchase failed!");
        }
    }


    private void ShowPurchaseFailedFeedback(string message)
    {
        purchaseConfirmationPopup.SetActive(false);
        StartCoroutine(ShowTemporaryMessage(message, Color.red));
    }

    private void ShowPurchaseSuccessFeedback(string message)
    {
        StartCoroutine(ShowTemporaryMessage(message, Color.green));
    }

    private IEnumerator ShowTemporaryMessage(string message, Color color)
    {
        if (popupItemInfo != null)
        {
            var originalText = popupItemInfo.text;
            var originalColor = popupItemInfo.color;

            popupItemInfo.text = message;
            popupItemInfo.color = color;

            yield return new WaitForSeconds(2f);

            popupItemInfo.text = originalText;
            popupItemInfo.color = originalColor;
        }
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
            else if (shopItem.itemType == ItemType.Upgrade)
            {
                var currentLevel = SessionManager.Instance.GetFoodUpgradeLevel(shopItem.upgradeFoodType);
                if (currentLevel > 1) // was upgraded at least once
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
            playerScoreText.text = $"EARNED: $ {currentScore:F2}";
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

        var topBoundary = -38f;
        var bottomBoundary = 2000f;

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

    private void UpdateScrollButtons()
    {
        if (itemContainer == null) return;

        var containerRect = itemContainer.GetComponent<RectTransform>();
        if (containerRect == null) return;

        var currentY = containerRect.anchoredPosition.y;
        var canScrollUp = currentY > -38f;
        var canScrollDown = currentY < 2000f;

        if (scrollUpButton != null)
            scrollUpButton.interactable = canScrollUp;

        if (scrollDownButton != null)
            scrollDownButton.interactable = canScrollDown;
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
        currentPurchaseItem = item;

        var iconTransform = item.transform.Find("ItemIcon");
        var iconImage = iconTransform.GetComponent<Image>();
        popupItemIcon.sprite = iconImage.sprite;

        var iconRect = iconTransform.GetComponent<RectTransform>();
        var popupIconRect = popupItemIcon.rectTransform;
        popupIconRect.sizeDelta = iconRect.sizeDelta;

        if (popupItemInfo != null)
        {
            popupItemInfo.text = item.popupInfoText;
            popupItemInfo.color = Color.white;
        }

        if (confirmPurchaseButton != null)
        {
            var buttonText = confirmPurchaseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = item.purchaseButtonText;
        }

        purchaseConfirmationPopup.SetActive(true);
    }

    public void ConfirmPurchase()
    {
        if (currentPurchaseItem != null)
        {
            PurchaseItem(currentPurchaseItem);
            purchaseConfirmationPopup.SetActive(false);
            currentPurchaseItem = null;
        }
    }

    public void CancelPurchase()
    {
        purchaseConfirmationPopup.SetActive(false);
        currentPurchaseItem = null;
    }
}