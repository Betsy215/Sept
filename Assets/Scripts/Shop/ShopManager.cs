using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.VisualScripting;

public class ShopManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject shopPanel;           // The white sliding panel
    public GameObject itemContainer;       // Parent for shop item UI elements
    
    [Header("Shop Controls")]
    public Button nextLevelButton;
    public Button mainMenuButton;
    public TextMeshProUGUI playerScoreText; 
    
    [Header("Shop Item Management")]
    // REMOVED: public ShopItemController[] allShopItems; // No longer needed!
    private int availableItemCount = 6; // First 3 items available initially
    
    [Header("Animation")]
    public float slideAnimationDuration = 1f;
    
    [Header("Scroll Controls")]
    public Button scrollUpButton;
    public Button scrollDownButton;
    public ScrollRect itemScrollRect;
    public float scrollAmount = 800f; // Configurable scroll distance
    public float scrollDuration = 1f; // Smooth scroll time
    
    [Header("Purchase Popup")]
    public GameObject purchaseConfirmationPopup;
    public Image popupItemIcon;
    public TextMeshProUGUI popupItemInfo;
    public Button confirmPurchaseButton;
    public Button cancelPurchaseButton;

    private ShopItemController currentPurchaseItem;
    
    // Internal references
    private float playerScore;
    private const string SHOP_SAVE_KEY = "ShopData";
    
    void Start()
    {
        InitializeShop();
        AudioManager.Instance.PlayShopMusic();
        SetupButtonListeners();
        StartCoroutine(DelayedScrollButtonUpdate()); // Initialize scroll button states
    }
    
    // NEW: Ensure button listeners are set up
    void SetupButtonListeners()
    {
        // Purchase popup buttons
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
        
        // Scroll buttons
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
        
        // Continue/Next Level button
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        }
        
        // Main menu button (if you have one)
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }
    
    // NEW: Helper method to get all shop items dynamically from itemContainer
    private ShopItemController[] GetAllShopItems()
    {
        if (itemContainer == null) return new ShopItemController[0];
        
        return itemContainer.GetComponentsInChildren<ShopItemController>(true); // Include inactive items
    }
    
    // NEW: Get total count of shop items
    private int GetTotalShopItemCount()
    {
        return GetAllShopItems().Length;
    }
    
    void RefreshShopDisplay()
    {
        ShopItemController[] allShopItems = GetAllShopItems();
        
        // Show all items and set their availability
        for (int i = 0; i < allShopItems.Length; i++)
        {
            if (allShopItems[i] != null)
            {
                // Always show the item
                allShopItems[i].gameObject.SetActive(true);
                
                bool canAfford = playerScore >= allShopItems[i].price;
                bool isAvailable = canAfford && !allShopItems[i].isPurchased;
                allShopItems[i].SetAvailable(isAvailable, canAfford);
            }
        }

        // Count actually available (not purchased) items for debug
        int actuallyAvailable = 0;
        int purchasedCount = 0;
        for (int i = 0; i < Mathf.Min(availableItemCount, allShopItems.Length); i++)
        {
            if (allShopItems[i].isPurchased)
                purchasedCount++;
            else
                actuallyAvailable++;
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

        // Check if player has enough score
        if (playerScore < item.price)
        {
            ShowPurchaseFailedFeedback($"Not enough points!\nNeed: {item.price} | Have: {playerScore}");
            return;
        }

        // Proceed with purchase
        bool purchaseSuccess = false;
        
        // Purchase based on item type
        if (item.itemType == ItemType.Food)
        {
            purchaseSuccess = SessionManager.Instance.PurchaseFoodItem(item.itemName);
        }
        else if (item.itemType == ItemType.Character)
        {
            purchaseSuccess = SessionManager.Instance.PurchaseCharacter(item.itemName);
        }
        
        if (purchaseSuccess)
        {
            // Deduct score from session
            SessionManager.Instance.DeductScore(item.price);

            // Update local score display
            playerScore -= item.price;
            UpdateScoreDisplay();

            // Mark as purchased in UI
            item.MarkAsPurchased();
            AudioManager.Instance.PlayPurchaseSound();

            // NEW: Unlock next item if there are more to unlock
            int totalItems = GetTotalShopItemCount();
            Debug.Log($"Before increment: availableItemCount={availableItemCount}, totalItems={totalItems}");
            if (availableItemCount < totalItems)
            {
                availableItemCount++;
                Debug.Log($"After increment: availableItemCount={availableItemCount}");
                RefreshShopDisplay();
                Debug.Log($"Purchase unlocked new item! Now {availableItemCount}/{totalItems} items available.");
            }
            else
            {
                Debug.Log("No more items to unlock!");
            }

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
    
    // NEW: Show feedback for failed purchases
    void ShowPurchaseFailedFeedback(string message)
    {
        // Close the popup
        purchaseConfirmationPopup.SetActive(false);
        
        // Simple feedback: Update popup text temporarily to show error
        StartCoroutine(ShowTemporaryMessage(message, Color.red));
    }
    
    // NEW: Show feedback for successful purchases  
    void ShowPurchaseSuccessFeedback(string message)
    {
        // Simple feedback: Flash success message
        StartCoroutine(ShowTemporaryMessage(message, Color.green));
    }
    
    // Helper to show temporary message
    IEnumerator ShowTemporaryMessage(string message, Color color)
    {
        // You could show this on the popup info text or create a dedicated feedback text
        if (popupItemInfo != null)
        {
            string originalText = popupItemInfo.text;
            Color originalColor = popupItemInfo.color;
            
            popupItemInfo.text = message;
            popupItemInfo.color = color;
            
            yield return new WaitForSeconds(2f);
            
            popupItemInfo.text = originalText;
            popupItemInfo.color = originalColor;
        }
    }
    
    void UpdatePurchasedItemsUI()
    {
        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession())
            return;
        
        // Get purchased items from session
        List<string> purchasedFoods = SessionManager.Instance.GetCurrentSession().purchasedFoodItems;
        List<string> purchasedCharacters = SessionManager.Instance.GetCurrentSession().purchasedCharacters;

        // Find all shop item controllers and update their purchased status
        ShopItemController[] shopItems = GetAllShopItems(); // Use our helper method

        foreach (ShopItemController shopItem in shopItems)
        {
            if (shopItem.itemType == ItemType.Food && purchasedFoods.Contains(shopItem.itemName))
            {
                shopItem.MarkAsPurchased();
            }
            else if (shopItem.itemType == ItemType.Character && purchasedCharacters.Contains(shopItem.itemName))
            {
                shopItem.MarkAsPurchased();
            }
        }
    }
    
    void UpdateScoreDisplay()
    {
        if (playerScoreText != null)
        {
            playerScoreText.text = $"EARNED: $ {playerScore}";
        }
    }
    
    void StartShopAnimation()
    {
        // Get the ShopPanel RectTransform
        RectTransform panelRect = shopPanel.GetComponent<RectTransform>();

        // Start position (off-screen left)
        Vector2 startPos = new Vector2(-1284, 0);
        Vector2 endPos = new Vector2(0, 0); // Center position

        // Start the slide animation
        StartCoroutine(SlidePanel(panelRect, startPos, endPos, slideAnimationDuration));
    }
    
    IEnumerator SlidePanel(RectTransform panel, Vector2 start, Vector2 end, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
        
            // Smooth easing
            t = Mathf.SmoothStep(0f, 1f, t);
        
            // Lerp position
            panel.anchoredPosition = Vector2.Lerp(start, end, t);
        
            yield return null;
        }

        // Ensure final position
        panel.anchoredPosition = end;
    }

    public void ShowPurchasePopup(ShopItemController item)
    {
        currentPurchaseItem = item;
        Transform iconTransform = item.transform.Find("ItemIcon");
        Image iconImage = iconTransform.GetComponent<Image>();
        popupItemIcon.sprite = iconImage.sprite;
        RectTransform iconRect = iconTransform.GetComponent<RectTransform>();
        Vector2 iconSize = iconRect.sizeDelta;
        
            
        // Apply to popup icon
        RectTransform popupIconRect = popupItemIcon.rectTransform;
        popupIconRect.sizeDelta = iconSize;
        if (popupItemInfo != null) 
            popupItemInfo.text = item.popupInfoText;

        // Set button text with price
        if (confirmPurchaseButton != null)
        {
            TextMeshProUGUI buttonText = confirmPurchaseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = item.purchaseButtonText;
        }

        // Show popup
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
    
    void InitializeShop()
    {
        if (SessionManager.Instance == null)
        {
            playerScore = 0;
        }
        else
        {
            playerScore = SessionManager.Instance.GetTotalScore();
        }

        // Update score display
        UpdateScoreDisplay();

        // Show initial available items
        RefreshShopDisplay();

        // Check which items are already purchased and update UI
        UpdatePurchasedItemsUI();

        // Update scroll button states after everything is set up
        StartCoroutine(DelayedScrollButtonUpdate());

        // Start the slide-in animation
        StartShopAnimation();
    }
    
    // NEW: Delay scroll button update to ensure layout is complete
    IEnumerator DelayedScrollButtonUpdate()
    {
        yield return new WaitForEndOfFrame();
        UpdateScrollButtons(); // Changed from UpdateSimpleScrollButtons()
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
        SceneTransitionManager.Instance.TransitionToScene("MainMenu");
    }

    public void ScrollUp()
    {
        if (itemContainer != null)
        {
            RectTransform itemContainerRect = itemContainer.GetComponent<RectTransform>();
            if (itemContainerRect != null)
            {
                StartCoroutine(SimpleScroll(itemContainerRect, -scrollAmount)); // Decrease Y
            }
        }
    }

    public void ScrollDown()
    {
        if (itemContainer != null)
        {
            RectTransform itemContainerRect = itemContainer.GetComponent<RectTransform>();
            if (itemContainerRect != null)
            {
                StartCoroutine(SimpleScroll(itemContainerRect, scrollAmount)); // Increase Y
            }
        }
    }

    IEnumerator SimpleScroll(RectTransform containerRect, float amount)
    {
        Vector2 startPos = containerRect.anchoredPosition;
        Vector2 targetPos = startPos + new Vector2(0, amount);
    
        // Your exact boundary values
        float topBoundary = -38f;    // Y = -38 is top boundary
        float bottomBoundary = 2000f; // Y = 2000 is bottom boundary
    
        // Clamp Y between boundaries
        targetPos.y = Mathf.Clamp(targetPos.y, topBoundary, bottomBoundary);
    
        // Check if we're already at boundary (no movement needed)
        if (Mathf.Abs(startPos.y - targetPos.y) < 1f)
        {
            yield break;
        }

        // Animate scroll
        float elapsed = 0f;
        while (elapsed < scrollDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scrollDuration;
            t = Mathf.SmoothStep(0f, 1f, t);
        
            containerRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        containerRect.anchoredPosition = targetPos;
        UpdateScrollButtons();
    }

    void UpdateScrollButtons()
    {
        if (itemContainer == null) return;
    
        RectTransform containerRect = itemContainer.GetComponent<RectTransform>();
        if (containerRect == null) return;
    
        float currentY = containerRect.anchoredPosition.y;
    
        // When Y = -38, disable up button, enable down button
        // When Y > -38, enable up button
        // When Y = 2000, disable down button
    
        bool canScrollUp = currentY > -38f;
        bool canScrollDown = currentY < 2000f;
    
        // Update button states
        if (scrollUpButton != null)
        {
            scrollUpButton.interactable = canScrollUp;
        }
    
        if (scrollDownButton != null)
        {
            scrollDownButton.interactable = canScrollDown;
        }
    }

}