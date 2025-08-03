using UnityEngine;

[System.Serializable]
public class ShopItem
{
    [Header("Item Info")]
    public string itemName;
    public string description;        // Keep this for internal use
    public Sprite itemIcon;
    public int price;
    
    [Header("Popup Display")]
    public string popupInfoText;      // Custom text for popup
    public string purchaseButtonText; // Custom button text with price
    
    [Header("Game Integration")]
    public ItemType itemType;
    public GameObject itemPrefab;
    
    [Header("Shop State")]
    public bool isPurchased;
    public bool isAvailable;
    
    public ShopItem(string name, int cost, Sprite icon, ItemType type, GameObject prefab)
    {
        itemName = name;
        price = cost;
        itemIcon = icon;
        itemType = type;
        itemPrefab = prefab;
        isPurchased = false;
        isAvailable = true;
    }
}

public enum ItemType
{
    Food,
    Customer
}