using UnityEngine;

[CreateAssetMenu(fileName = "New Level", menuName = "Food Truck/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public int levelNumber = 1;
    public string levelName = "Level 1";
    
    [Header("Order Settings")]
    public int ordersPerLevel = 3;
    public float orderDisplayTime = 5f;
    public float timeBetweenOrders = 2f;
    public int minOrderItems = 1;
    public int maxOrderItems = 4;
    
    [Header("Serveable Item Settings")]
    [Tooltip("Number of serveable items to activate for this level")]
    public int activeItemCount = 4; // ADDED: This replaces activeTrayCount
    [Tooltip("Which specific food items are available this level (e.g., Bread, Apple, Juice)")]
    public string[] availableFoodTypes = { "Bread", "Apple", "Juice", "Burger" };
    [Tooltip("Maximum number of different items available in this level")]
    public int maxAvailableItems = 4;
    
    // REMOVED: Old tray-specific settings
    // [Header("Food Tray Settings")]
    // public int maxItemsPerTray = 5;
    // public int activeTrayCount = 4;
    
    // REMOVED: Serve plate settings (no longer needed)
    // [Header("Serve Plate Settings")]
    // public int plateMaxCapacity = 4;
    
    [Header("Scoring")]
    public int basePointsPerOrder = 100;
    public int perfectOrderBonus = 50;
    public int timeBonus = 10; // Points per second remaining
    
    [Header("Visual Elements")]
    public Color backgroundColor = Color.white;
    public Sprite backgroundSprite;
    
    [Header("Advanced Item Control (Optional)")]
    [Tooltip("If true, only items in availableFoodTypes will be active")]
    public bool useSpecificFoodTypes = false;
    [Tooltip("Custom difficulty modifier for this level")]
    public float difficultyMultiplier = 1.0f;
}

/* 
🎯 CHANGES MADE TO LEVELDATA:

✅ ADDED: Serveable Item Settings
- availableFoodTypes: Array of food types available this level
- maxAvailableItems: Control how many different items are active
- useSpecificFoodTypes: Toggle for specific vs. all items

✅ REMOVED: Tray-Specific Settings
- maxItemsPerTray: No longer needed (unlimited items)
- activeTrayCount: Replaced with item-based control

✅ REMOVED: Serve Plate Settings  
- plateMaxCapacity: No longer relevant

✅ ADDED: Advanced Controls
- difficultyMultiplier: For future expansion
- More granular control over item availability

🔧 HOW TO USE THE NEW SYSTEM:

SIMPLE APPROACH (Current):
- Leave useSpecificFoodTypes = false
- All ServeableItems in scene will be active
- Easy to set up and test

ADVANCED APPROACH (Future):
- Set useSpecificFoodTypes = true
- Specify availableFoodTypes = ["Bread", "Apple", "Juice"]
- Only those items will be active for that level

📝 MIGRATION FROM OLD LEVELDATA:

Old Settings → New Settings:
- activeTrayCount: 3 → maxAvailableItems: 3
- maxItemsPerTray: 6 → (not needed - items are unlimited)
- availableFoodTypes: ["Red", "Blue", "Green"] (if specific control wanted)

🚀 BACKWARD COMPATIBILITY:

The old tray settings are commented out but preserved in case you want to reference them during migration. You can safely delete them once everything is working.
*/