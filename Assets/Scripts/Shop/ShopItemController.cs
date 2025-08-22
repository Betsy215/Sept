using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemController : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName = "Bread";
    public int price = 50;
    public ItemType itemType = ItemType.Food;
    
    [Header("Popup Display")]
    [TextArea(2, 4)]
    public string popupInfoText = "Fresh bread attracts more customers!";
    public string purchaseButtonText = "Buy for 50 Points";
    
    [Header("Game Integration")]
    public GameObject itemPrefab;
    
    [Header("State")]
    public bool isPurchased = false;
    public bool isAvailable = true;
    
    private ShopManager shopManager;
    
    void Start()
    {
        shopManager = FindObjectOfType<ShopManager>();
        SetupClickHandler();
        UpdateVisualState();
    }
    
    void SetupClickHandler()
    {
        Button iconButton = transform.Find("ItemIcon").GetComponent<Button>();
        if (iconButton != null)
        {
            iconButton.onClick.RemoveAllListeners();
            iconButton.onClick.AddListener(OnItemClicked);
        }
    }
    
    void OnItemClicked()
    {
        if (shopManager != null && isAvailable && !isPurchased)
        {
            shopManager.ShowPurchasePopup(this);
        }
        else if (!isAvailable && !isPurchased)
        {
            Debug.Log($"{itemName} is not yet available for purchase!");
            // Could add visual feedback here (shake, sound, etc.)
        }
    }
    
    public void MarkAsPurchased()
    {
        isPurchased = true;
        UpdateVisualState();
    }
    
    public void SetAvailable(bool available, bool canAfford = true)
    {
        if (!isPurchased)
        {
            isAvailable = available;
        }
        UpdateVisualState(canAfford);
    }
    
    void UpdateVisualState(bool canAfford = true)
    {
        // Get UI components
        Button iconButton = transform.Find("ItemIcon")?.GetComponent<Button>();
        Image iconImage = iconButton?.GetComponent<Image>();
    
        if (isPurchased)
        {
            // Item is purchased - show as owned (green and non-clickable)
            if (iconButton != null) iconButton.interactable = false;
            if (iconImage != null) iconImage.color = Color.green;
        }
        else if (isAvailable && canAfford)
        {
            // Item is available for purchase (white and clickable)
            if (iconButton != null) iconButton.interactable = true;
            if (iconImage != null) iconImage.color = Color.white;
        }
        else if (!canAfford)
        {
            // Item is not affordable - show as very transparent (only shape visible)
            if (iconButton != null) iconButton.interactable = false;
            if (iconImage != null) iconImage.color = new Color(0f, 0f, 0f, 0.8f); 
        }
        else
        {
            // Item is not yet available - show as locked (grey and non-clickable)
            if (iconButton != null) iconButton.interactable = false;
            if (iconImage != null) iconImage.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Grey
        }
    }
    // Public method for ShopManager to update visual state
    public void RefreshVisualState()
    {
        UpdateVisualState();
    }
}