using UnityEngine;

[System.Serializable]
public class ShopItem
{
    [Header("Item Info")]
    public string itemName;           // e.g., "Bread"
    public string description;        // e.g., "Unlock bread for your kitchen"
    public Sprite itemIcon;           // The visual icon for this item
    public int price;                 // Cost in score points
    
    [Header("Game Integration")]
    public ItemType itemType;         // Food or Customer
    public GameObject itemPrefab;     // The actual food tray prefab or customer prefab
    
    [Header("Shop State")]
    public bool isPurchased;          // Has player bought this item?
    public bool isAvailable;          // Is this item available for purchase?
    
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