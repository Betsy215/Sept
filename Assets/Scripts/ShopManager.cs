using System.Collections;
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
    
        // Load previously purchased items
        LoadShopData();
    
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
        // Check if player has enough score
        if (playerScore >= item.price && !item.isPurchased)
        {
            // Deduct score
            playerScore -= item.price;
            
            // Mark as purchased
            item.MarkAsPurchased();
            
            // Update score display
            UpdateScoreDisplay();
            
            Debug.Log($"Purchased {item.itemName} for {item.price} points!");
        }
        else
        {
            Debug.Log("Not enough points or item already purchased!");
        }
    }
    
    public void OnNextLevelClicked()
    {
        // TODO: Save shop data and load next level
    }
    
    public void OnMainMenuClicked()
    {
        // TODO: Save shop data and return to main menu
    }
}