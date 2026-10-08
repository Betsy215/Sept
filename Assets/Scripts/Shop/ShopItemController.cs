using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemController : MonoBehaviour
{
    [Header("Item Info")] public string itemName;
    public int price;
    public ItemType itemType;

    [Header("Upgrade")]
    [Tooltip("SessionManager key to upgrade (e.g. 'Coffee', 'CoffeeMachine'). Only used when ItemType = Upgrade.")]
    public string upgradeFoodType;

    [Header("Popup Display")] [TextArea(2, 4)]
    public string popupInfoText = "Fresh bread attracts more customers!";

    public string purchaseButtonText = "Buy for 50 Points";

    [Header("State")] public bool isPurchased = false;

    private ShopManager shopManager;

    private void Awake()
    {
        // These are matched by exact string against SessionManager keys ("Bread", "CoffeeMachine");
        // a stray space typed in the inspector would silently make the purchase never register.
        itemName = itemName?.Trim();
        upgradeFoodType = upgradeFoodType?.Trim();
    }

    private void Start()
    {
        shopManager = FindObjectOfType<ShopManager>();
        SetupClickHandler();

        // FIX: Start with unaffordable state instead of affordable
        UpdateVisualState(false); // ← Changed from true to false
    }

    private void SetupClickHandler()
    {
        var iconButton = transform.Find("ItemIcon")?.GetComponent<Button>();
        if (iconButton != null)
        {
            iconButton.onClick.RemoveAllListeners();
            iconButton.onClick.AddListener(OnItemClicked);
        }
    }

    private void OnItemClicked()
    {
        if (shopManager != null && !isPurchased) shopManager.ShowPurchasePopup(this);
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