using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

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
    public ShopItemController[] allShopItems; // All items in unlock order (set in Inspector)
    private int availableItemCount = 3; // First 3 items available initially
    
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

    private ShopItemController currentPurchaseItem; // Changed type
    // Internal references
    private int playerScore;
    private const string SHOP_SAVE_KEY = "ShopData";
    
    void Start()
    {
        InitializeShop();
        SetupScrollButtons();
        SetupPopupButtons();
    }
    
    void SetupPopupButtons()
    {
        if (confirmPurchaseButton != null)
            confirmPurchaseButton.onClick.AddListener(ConfirmPurchase);
            
        if (cancelPurchaseButton != null)
            cancelPurchaseButton.onClick.AddListener(CancelPurchase);
    }
    
    void SetupScrollButtons()
    {
        if (scrollUpButton != null)
            scrollUpButton.onClick.AddListener(ScrollUp);
        
        if (scrollDownButton != null)
            scrollDownButton.onClick.AddListener(ScrollDown);
    }

    public void ScrollUp()
    {
        if (itemScrollRect != null)
        {
            StartCoroutine(SmoothScroll(scrollAmount));
        }
    }

    public void ScrollDown()
    {
        if (itemScrollRect != null)
        {
            StartCoroutine(SmoothScroll(-scrollAmount));
        }
    }
    
    IEnumerator SmoothScroll(float amount)
    {
        RectTransform content = itemScrollRect.content;
        Vector2 startPos = content.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0, amount);
    
        float elapsed = 0f;
    
        while (elapsed < scrollDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scrollDuration;
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth easing
        
            content.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
    
        content.anchoredPosition = endPos;
    }
    
    public void ShowPurchasePopup(ShopItemController item)
    {
        currentPurchaseItem = item;
    
        // Set popup content
        if (popupItemIcon != null) 
            popupItemIcon.sprite = item.itemIcon;
        
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
        }
    }

    public void CancelPurchase()
    {
        purchaseConfirmationPopup.SetActive(false);
        currentPurchaseItem = null;
    }
    
    void InitializeShop()
    {
        // Get player's current score from SessionManager
        if (SessionManager.Instance != null)
        {
            playerScore = SessionManager.Instance.GetTotalScore();
        }

        // Update score display
        UpdateScoreDisplay();

        // Show initial available items
        RefreshShopDisplay();
    
        // Check which items are already purchased and update UI
        UpdatePurchasedItemsUI();

        // Start the slide-in animation
        StartShopAnimation();
    }
    
    void UpdateScoreDisplay()
    {
        if (playerScoreText != null)
        {
            playerScoreText.text = $"{playerScore}";
        }
    }
    
    void LoadShopData()
    {
        // TODO: Load purchased items from PlayerPrefs
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
    
    public void PurchaseItem(ShopItemController item)
    {
        // Check if SessionManager is available
        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession())
        {
            Debug.LogError("Cannot purchase - no active session!");
            return;
        }
    
        // Check if player has enough score
        if (playerScore >= item.price && !item.isPurchased)
        {
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
    
                // NEW: Unlock next item if there are more to unlock
                if (availableItemCount < allShopItems.Length)
                {
                    availableItemCount++;
                    RefreshShopDisplay();
                    Debug.Log($"Purchase unlocked new item! Now {availableItemCount}/{allShopItems.Length} items available.");
                }
    
                Debug.Log($"Successfully purchased {item.itemName} for {item.price} points!");
            }
            else
            {
                Debug.LogError($"Failed to purchase {item.itemName}");
            }
        }
        else
        {
            Debug.Log("Not enough points or item already purchased!");
        }
    }
    void RefreshShopDisplay()
    {
        // Hide all items first
        for (int i = 0; i < allShopItems.Length; i++)
        {
            if (allShopItems[i] != null)
            {
                allShopItems[i].gameObject.SetActive(false);
            }
        }
    
        // Show only available items
        for (int i = 0; i < availableItemCount && i < allShopItems.Length; i++)
        {
            if (allShopItems[i] != null)
            {
                allShopItems[i].gameObject.SetActive(true);
            }
        }
    
        Debug.Log($"Shop display refreshed: showing {availableItemCount} items");
    }
    public void OnNextLevelClicked()
    {
        // TODO: Save shop data and load next level
    }
    
    public void OnMainMenuClicked()
    {
        // TODO: Save shop data and return to main menu
    }
    
    void UpdatePurchasedItemsUI()
    {
        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession())
            return;
        
        // Get purchased items from session
        List<string> purchasedFoods = SessionManager.Instance.GetCurrentSession().purchasedFoodItems;
        List<string> purchasedCharacters = SessionManager.Instance.GetCurrentSession().purchasedCharacters;
    
        // Find all shop item controllers and update their purchased status
        ShopItemController[] shopItems = itemContainer.GetComponentsInChildren<ShopItemController>();
    
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
    
        Debug.Log($"Updated shop UI - Purchased foods: [{string.Join(", ", purchasedFoods)}], characters: [{string.Join(", ", purchasedCharacters)}]");
    }
}