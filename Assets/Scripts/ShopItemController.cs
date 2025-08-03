using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemController : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName = "Bread";
    public Sprite itemIcon;           // ADD THIS BACK
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
        if (shopManager != null)
        {
            shopManager.ShowPurchasePopup(this);
        }
    }
    
    public void MarkAsPurchased()
    {
        isPurchased = true;
    }
}