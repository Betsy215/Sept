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
    
    [Header("NEW: Order System Reference")]
    public OrderSystem orderSystem; // Reference to the order system for direct serving
    
    [Header("Visual Feedback")]
    public GameObject popupCanvas; // For UI popup detection
    
    // Private variables
    private int currentItems;
    private GameObject[] itemObjects;

    void Start()
    {
        // Find order system if not assigned
        if (orderSystem == null)
            orderSystem = FindObjectOfType<OrderSystem>();
            
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
            
            // Calculate tray size based on grid layout
            float trayWidth = gridSize.x * itemSpacing;
            float trayHeight = gridSize.y * itemSpacing;
            
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
            Vector3 itemPosition = CalculateItemPosition(i);
            GameObject newItem = Instantiate(itemPrefab, itemContainer);
            newItem.transform.localPosition = itemPosition;
            
            // Store reference
            itemObjects[i] = newItem;
            
            // Initialize food item component
            FoodItem foodItem = newItem.GetComponent<FoodItem>();
            if (foodItem == null)
                foodItem = newItem.AddComponent<FoodItem>();
            
            foodItem.Initialize(this, foodType);
        }
    }
    
    Vector3 CalculateItemPosition(int index)
    {
        int row = index / (int)gridSize.x;
        int col = index % (int)gridSize.x;
        
        float x = col * itemSpacing;
        float y = -row * itemSpacing; // Negative for downward layout
        
        return new Vector3(x, y, 0);
    }
    
    // Handle clicks on the tray itself - NEW DIRECT SERVING SYSTEM
    void OnMouseDown()
    {
        // Check if we're over UI elements
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool popupActive = popupCanvas != null && popupCanvas.activeInHierarchy;
        
        Debug.Log($"Tray {foodType} clicked. Current items: {currentItems}");
        Debug.Log($"Mouse over UI: {overUI}, Popup active: {popupActive}");

        // Only block if we're over UI AND the popup canvas is active
        if (overUI && popupActive) 
        {
            Debug.Log("Blocked by popup - tray click ignored");
            return;
        }

        Debug.Log("Tray click proceeding...");
        OnItemClicked();
    }
    
    // NEW: Direct serving to customer orders
    public void OnItemClicked()
    {
        // Check if tray has items
        if (currentItems <= 0)
        {
            Debug.Log($"{foodType} tray is empty!");
            return;
        }
        
        // Check if there's an active order
        if (orderSystem == null || !orderSystem.IsOrderActive())
        {
            Debug.Log("No active order to serve!");
            return;
        }
        
        // Try to serve this item to the current order
        bool itemServed = orderSystem.TryServeItem(foodType);
        
        if (itemServed)
        {
            // Remove item from tray
            RemoveItem();
            Debug.Log($"Successfully served {foodType}. Remaining in tray: {currentItems}");
        }
        else
        {
            Debug.Log($"{foodType} is not needed in the current order!");
            // Show visual feedback for wrong item
            StartCoroutine(ShowWrongItemFeedback());
        }
    }
    
    void RemoveItem()
    {
        if (currentItems > 0)
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
    }
    
    // Visual feedback for wrong item selection
    System.Collections.IEnumerator ShowWrongItemFeedback()
    {
        // Simple shake effect for wrong item
        Vector3 originalPosition = transform.position;
        float shakeIntensity = 0.1f;
        float shakeDuration = 0.3f;
        
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            
            float x = originalPosition.x + Random.Range(-shakeIntensity, shakeIntensity);
            float y = originalPosition.y + Random.Range(-shakeIntensity, shakeIntensity);
            
            transform.position = new Vector3(x, y, originalPosition.z);
            
            yield return null;
        }
        
        transform.position = originalPosition;
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
        
        Debug.Log($"{foodType} tray refilled to {maxItems} items");
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
    
    // Public method to get food type - NEEDED FOR LEVEL MANAGER INTEGRATION
    public string GetFoodType()
    {
        return foodType;
    }
    
    // Public method to check if tray can serve (has items and order is active)
    public bool CanServe()
    {
        return HasItems() && orderSystem != null && orderSystem.IsOrderActive();
    }
}

// Separate script for individual food items (simplified for new system)
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