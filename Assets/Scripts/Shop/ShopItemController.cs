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

    [Header("State")] 
    public bool isPurchased = false;

    private ShopManager shopManager;

    private void Start()
    {
        shopManager = FindObjectOfType<ShopManager>();
        SetupClickHandler();
        UpdateVisualState(true); // Default to affordable on start
    }

    private void SetupClickHandler()
    {
        var iconButton = transform.Find("ItemIcon").GetComponent<Button>();
        if (iconButton != null)
        {
            iconButton.onClick.RemoveAllListeners();
            iconButton.onClick.AddListener(OnItemClicked);
        }
    }

    private void OnItemClicked()
    {
        if (shopManager != null && !isPurchased)
        {
            shopManager.ShowPurchasePopup(this);
        }
    }

    public void MarkAsPurchased()
    {
        isPurchased = true;
        UpdateVisualState(true);
    }

    // New simplified method - only takes canAfford parameter
    public void UpdateAffordability(bool canAfford)
    {
        UpdateVisualState(canAfford);
    }

    private void UpdateVisualState(bool canAfford)
    {
        var iconButton = transform.Find("ItemIcon")?.GetComponent<Button>();
        var iconImage = iconButton?.GetComponent<Image>();

        if (isPurchased)
        {
            // Item is purchased - show as owned (green and non-clickable)
            if (iconButton != null) 
                iconButton.interactable = false;
            if (iconImage != null) 
                iconImage.color = Color.green;
        }
        else if (canAfford)
        {
            // Item is affordable - show as available (white and clickable)
            if (iconButton != null) 
                iconButton.interactable = true;
            if (iconImage != null) 
                iconImage.color = Color.white;
        }
        else
        {
            // Item is not affordable - show as transparent (grey and non-clickable)
            if (iconButton != null) 
                iconButton.interactable = false;
            if (iconImage != null) 
                iconImage.color = new Color(0f, 0f, 0f, 0.8f);
        }
    }
}