using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FoodTray : MonoBehaviour
{
    [Header("Tray Settings")]
    public string foodType = "Burger"; // Name of the food type
    public int maxItems = 6; // Maximum items in tray
    
    [Header("Visual Elements")]
    public GameObject itemPrefab; // Prefab for individual food items
    public Transform itemContainer; // Parent object to hold items
    public Button refillButton; // Button to trigger refill
    public Text itemCountText; // Text to display current item count
    
    [Header("Layout Settings")]
    public Vector2 gridSize = new Vector2(5, 1); // Grid layout (width x height)
    public float itemSpacing = 1f; // Space between items
    
    [Header("Serve Plate Reference")]
    public ServePlate servePlate; // Reference to the serve plate
    
    // Private variables
    private int currentItems;
    private GameObject[] itemObjects;
    public GameObject popupCanvas;

    void Start()
    {
        InitializeTray();
        SetupTrayCollider();
        
        // Set up refill button click event
        if (refillButton != null)
        {
            refillButton.onClick.AddListener(CompleteRefill);
        }
    }
    
    void SetupTrayCollider()
    {
        // Add a collider to the tray itself for click detection
        Collider trayCollider = GetComponent<Collider>();
        if (trayCollider == null)
        {
            // Add a box collider that covers the entire tray area
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            
            // Calculate tray size based on grid layout (reduced padding to prevent overlap)
            float trayWidth = gridSize.x * itemSpacing; // Removed extra padding
            float trayHeight = gridSize.y * itemSpacing; // Removed extra padding
            
            boxCollider.size = new Vector3(trayWidth, trayHeight, 0.5f);
            boxCollider.isTrigger = false; // Make it solid for clicking
        }
    }
    
    void InitializeTray()
    {
        currentItems = maxItems;
        itemObjects = new GameObject[maxItems];
        
        CreateItems();
        UpdateUI();
    }
    
    void CreateItems()
    {
        // Clear existing items
        foreach (Transform child in itemContainer)
        {
            DestroyImmediate(child.gameObject);
        }
        
        // Create new items in grid layout
        for (int i = 0; i < currentItems; i++)
        {
            Vector3 position = CalculateItemPosition(i);
            GameObject newItem = Instantiate(itemPrefab, itemContainer);
            newItem.transform.localPosition = position;
            
            // Add click functionality to each item
            FoodItem foodItemScript = newItem.GetComponent<FoodItem>();
            if (foodItemScript == null)
                foodItemScript = newItem.AddComponent<FoodItem>();
            
            // Individual items no longer need click functionality
            // since we're handling clicks on the tray level
            foodItemScript.Initialize(this, foodType);
            itemObjects[i] = newItem;
        }
    }
    
    Vector3 CalculateItemPosition(int index)
    {
        int row = Mathf.FloorToInt(index / gridSize.x);
        int col = index % (int)gridSize.x;
        
        float x = (col - (gridSize.x - 1) / 2f) * itemSpacing;
        float y = (row - (gridSize.y - 1) / 2f) * itemSpacing;
        
        return new Vector3(x, y, 0);
    }
    
    // Handle clicks on the tray itself
    void OnMouseDown()
    {
        bool overUI = EventSystem.current.IsPointerOverGameObject();
        bool popupActive = popupCanvas != null && popupCanvas.activeInHierarchy;

        Debug.Log($"Tray clicked! Mouse over UI: {overUI}, Popup active: {popupActive}");

        // Only block if we're over UI AND the popup canvas is active
        if (overUI && popupActive) 
        {
            Debug.Log("Blocked by popup - tray click ignored");
            return;
        }

        Debug.Log("Tray click proceeding...");
        OnItemClicked();
    }
    
    public void OnItemClicked()
    {
        if (currentItems > 0)
        {
            // Try to add item to serve plate first
            if (servePlate != null && servePlate.AddItem(itemPrefab, foodType))
            {
                // Only remove from tray if successfully added to serve plate
                RemoveItem();
            }
            else if (servePlate == null)
            {
                // If no serve plate reference, just remove item (original behavior)
                RemoveItem();
                
            }
            // If serve plate is full, item stays in tray (no removal)
        }
    }
    
    void RemoveItem()
    {
        currentItems--;
        
        // Destroy the last item
        if (itemObjects[currentItems] != null)
        {
            Destroy(itemObjects[currentItems]);
            itemObjects[currentItems] = null;
        }
        
        UpdateUI();
    }
    
    public void CompleteRefill()
    {
        currentItems = maxItems;
    
        // CRITICAL FIX: Resize itemObjects array if maxItems changed
        if (itemObjects == null || itemObjects.Length != maxItems)
        {
            itemObjects = new GameObject[maxItems];
        }
    
        CreateItems();
        UpdateUI();
    }
    
    void UpdateUI()
    {
        if (itemCountText != null)
            itemCountText.text = currentItems + "/" + maxItems;
    }
    
    // Public method to get current item count
    public int GetCurrentItemCount()
    {
        return currentItems;
    }
    
    // Public method to check if tray has items
    public bool HasItems()
    {
        return currentItems > 0;
    }
}

// Separate script for individual food items
public class FoodItem : MonoBehaviour
{
    private FoodTray parentTray;
    private string itemType;
    
    public void Initialize(FoodTray tray, string type)
    {
        parentTray = tray;
        itemType = type;
        
        // Items no longer need individual colliders since 
        // clicks are handled at the tray level
    }
}