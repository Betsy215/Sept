using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Linq;

[System.Serializable]
public class OrderItem
{
    public string foodType;
    public GameObject displayPrefab; // Prefab to show in the order display
}

public class OrderSystem : MonoBehaviour
{
    [Header("Level Settings - Updated by LevelManager")]
    public int ordersPerLevel = 3; // Configurable orders for current level
    public float orderDisplayTime = 5f; // How long each order is shown
    public float timeBetweenOrders = 2f; // Time between orders
    public int minOrderItems = 1; // Minimum items in an order
    public int maxOrderItems = 4; // Maximum items in an order
    
    private int ordersCompleted = 0;
    
    [Header("Order Display UI")]
    public Text orderProgressText; // "Orders: 2/3"
    public Transform orderContainer; // Parent object to hold order items
    public Text orderTitleText; // Text showing "Order:" or similar
    public Text orderTimerText; // Text showing remaining time
    public GameObject orderPanel; // Panel containing the entire order display
    
    [Header("Available Food Items")]
    public OrderItem[] availableFoods = new OrderItem[4]; // Burger, Fries, Drink, Dessert
    
    [Header("Layout Settings")]
    public float itemSpacing = 1.5f; // Space between order items
    public Vector3 startPosition = Vector3.zero; // Starting position for first item
    
    [Header("References")]
    public ServePlate servePlate; // Reference to check served items
    public ScoreManager scoreManager; // Reference to get the final score
    public LevelManager levelManager; // Reference to level manager
    
    [Header("Customer Integration")]
    public CustomerManager customerManager; // Reference to customer manager
    
    [Header("Debug Settings")]
    public bool enableDebugLogs = true;
    
    // Private variables
    private List<string> currentOrder = new List<string>();
    private List<GameObject> orderDisplayItems = new List<GameObject>();
    private bool orderActive = false;
    private float orderTimer = 0f;
    private Coroutine orderCycleCoroutine;
    private Coroutine orderTimerCoroutine;
    
    // Cache for active food types
    private List<string> activeFoodTypes = new List<string>();
    
    // NEW: Flow control variables
    private bool isUsingCustomerFlow = false;
    private bool isInitialized = false;
    
    // CRITICAL: Prevent duplicate order generation
    private bool isProcessingCustomerOrder = false;
    
    void Start()
    {
        InitializeOrderSystem();
    }
    
    void OnEnable()
    {
        // Reset when re-enabled by LevelManager
        ordersCompleted = 0;
        isProcessingCustomerOrder = false;
        InitializeOrderSystem();
    }
    
    void InitializeOrderSystem()
    {
        // Determine which flow to use
        isUsingCustomerFlow = (customerManager != null);
        
        // Update active food types based on current level
        UpdateActiveFoodTypes();
        
        // Hide order panel initially
        HideOrder();
        
        // Update order progress display
        UpdateOrderProgress();
        
        isInitialized = true;
        
        DebugLog($"OrderSystem initialized. Flow: {(isUsingCustomerFlow ? "Customer-Integrated" : "Original")}, Orders per level: {ordersPerLevel}");
    }
    
    // NEW: Initialize for customer flow without starting cycle
    public void InitializeForCustomerFlow()
    {
        DebugLog("Initializing OrderSystem for customer flow");
        InitializeOrderSystem();
    }
    
    void UpdateActiveFoodTypes()
    {
        activeFoodTypes.Clear();
        
        if (levelManager != null)
        {
            // Get only the active trays from LevelManager
            FoodTray[] activeTrays = levelManager.GetActiveTrays();
            
            foreach (FoodTray tray in activeTrays)
            {
                if (tray != null && !string.IsNullOrEmpty(tray.foodType))
                {
                    // Only add if we have a matching OrderItem for this food type
                    if (HasOrderItemForFoodType(tray.foodType))
                    {
                        activeFoodTypes.Add(tray.foodType);
                    }
                }
            }
        }
        
        // Fallback: if no active food types found, use all available foods
        if (activeFoodTypes.Count == 0)
        {
            DebugLog("No active food types found! Using all available foods as fallback.", true);
            foreach (OrderItem item in availableFoods)
            {
                if (item != null && !string.IsNullOrEmpty(item.foodType))
                {
                    activeFoodTypes.Add(item.foodType);
                }
            }
        }
        
        DebugLog($"Active food types for orders: {string.Join(", ", activeFoodTypes)}");
    }
    
    // Helper method to check if we have an OrderItem for a given food type
    bool HasOrderItemForFoodType(string foodType)
    {
        return System.Array.Exists(availableFoods, item => 
            item != null && item.foodType == foodType);
    }
    
    void UpdateOrderProgress()
    {
        if (orderProgressText != null)
            orderProgressText.text = $"{ordersCompleted}/{ordersPerLevel}";
    }
    
    #region FIXED: Order Cycle Management
    
    public void StartOrderCycle()
    {
        if (!isInitialized)
        {
            DebugLog("OrderSystem not initialized yet, deferring start", true);
            StartCoroutine(DeferredStartOrderCycle());
            return;
        }
        
        if (orderCycleCoroutine != null)
        {
            StopCoroutine(orderCycleCoroutine);
        }
        
        DebugLog($"Starting order cycle. Flow type: {(isUsingCustomerFlow ? "Customer-Integrated" : "Original")}");
        orderCycleCoroutine = StartCoroutine(OrderCycleCoroutine());
    }
    
    IEnumerator DeferredStartOrderCycle()
    {
        yield return new WaitForEndOfFrame();
        StartOrderCycle();
    }
    
    IEnumerator OrderCycleCoroutine()
    {
        DebugLog($"Order cycle started - Target orders: {ordersPerLevel}");
        
        if (isUsingCustomerFlow)
        {
            // FIXED: Customer-integrated flow - don't spawn customers here
            // LevelManager will spawn the first customer directly
            DebugLog("Using customer-integrated flow - waiting for LevelManager to spawn first customer");
        }
        else
        {
            // Original flow for backwards compatibility (no customer manager)
            DebugLog("Using original order flow (no customer manager)");
            
            while (ordersCompleted < ordersPerLevel)
            {
                // Update active food types each time
                UpdateActiveFoodTypes();
                
                // Generate and display new order
                GenerateNewOrder();
                DisplayOrder();
            
                // Wait for order duration
                orderTimer = orderDisplayTime;
                orderActive = true;
            
                // Update timer every frame
                while (orderTimer > 0 && orderActive)
                {
                    orderTimer -= Time.deltaTime;
                    UpdateTimerDisplay();
                
                    // Order completion is handled by ServePlate calling CompleteCurrentOrder()
                    yield return null;
                }
            
                // Order expired if we get here
                if (orderActive)
                {
                    OnOrderExpired();
                }
            
                // Hide order and wait before next one
                HideOrder();
            
                // Check if level complete before waiting
                if (ordersCompleted >= ordersPerLevel)
                {
                    break;
                }
            
                yield return new WaitForSeconds(timeBetweenOrders);
            }
            
            // Level is complete
            EndLevel();
        }
    }
    
    #endregion
    
    #region FIXED: Customer Integration Methods
    
    /// <summary>
    /// FIXED: Called by CustomerManager after customer delay to start the order
    /// Added protection against duplicate calls
    /// </summary>
    public void StartOrderCycleForCustomer()
    {
        if (!isUsingCustomerFlow)
        {
            DebugLog("StartOrderCycleForCustomer called but not using customer flow!", true);
            return;
        }
        
        // CRITICAL FIX: Prevent duplicate order generation
        if (isProcessingCustomerOrder)
        {
            DebugLog("Already processing customer order - ignoring duplicate call", true);
            return;
        }
        
        if (orderActive)
        {
            DebugLog("Order already active - ignoring duplicate call", true);
            return;
        }
        
        // Set flag to prevent duplicate processing
        isProcessingCustomerOrder = true;
        
        DebugLog($"Starting order cycle for customer #{ordersCompleted + 1}");
        
        // Update active food types
        UpdateActiveFoodTypes();
        
        // Generate and display new order
        GenerateNewOrder();
        DisplayOrder();

        // Start order timer
        orderTimer = orderDisplayTime;
        orderActive = true;
        
        // Start timer coroutine
        if (orderTimerCoroutine != null)
            StopCoroutine(orderTimerCoroutine);
            
        orderTimerCoroutine = StartCoroutine(OrderTimerCoroutine());
    }
    
    IEnumerator OrderTimerCoroutine()
    {
        DebugLog($"Order timer started - Duration: {orderDisplayTime}s");
        
        // Update timer every frame
        while (orderTimer > 0 && orderActive)
        {
            orderTimer -= Time.deltaTime;
            UpdateTimerDisplay();
        
            // Order completion is handled by ServePlate calling CompleteCurrentOrder()
            yield return null;
        }

        // Order expired if we get here and still active
        if (orderActive)
        {
            DebugLog("Order timer expired");
            OnOrderExpired();
        }

        // Hide order 
        HideOrder();

        // Reset processing flag
        isProcessingCustomerOrder = false;

        // FIXED: Proper level completion and next customer handling
        if (ordersCompleted >= ordersPerLevel)
        {
            DebugLog("All orders completed - ending level");
            EndLevel();
        }
        else
        {
            // Wait before allowing next customer
            DebugLog($"Waiting {timeBetweenOrders}s before next customer");
            yield return new WaitForSeconds(timeBetweenOrders);
            
            // Spawn next customer
            SpawnNextCustomer();
        }
    }
    
    /// <summary>
    /// FIXED: Centralized method to spawn next customer
    /// </summary>
    void SpawnNextCustomer()
    {
        if (customerManager != null)
        {
            DebugLog($"Spawning customer for order {ordersCompleted + 1}/{ordersPerLevel}");
            customerManager.SpawnCustomerForCurrentLevel();
        }
        else
        {
            DebugLog("Cannot spawn customer - CustomerManager is null!", true);
        }
    }
    
    #endregion
    
    void GenerateNewOrder()
    {
        currentOrder.Clear();
        
        // Make sure we have active food types
        if (activeFoodTypes.Count == 0)
        {
            DebugLog("No active food types available for order generation!", true);
            return;
        }
        
        // Clamp order size to not exceed available food types
        int maxPossibleItems = Mathf.Min(maxOrderItems, activeFoodTypes.Count);
        int adjustedMinItems = Mathf.Min(minOrderItems, maxPossibleItems);
        
        // Random number of items based on level settings and available foods
        int orderSize = Random.Range(adjustedMinItems, maxPossibleItems + 1);
        
        // Create a list of available foods for this order (to avoid duplicates)
        List<string> availableForSelection = new List<string>(activeFoodTypes);
        
        // Generate random food items from active food types
        for (int i = 0; i < orderSize && availableForSelection.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availableForSelection.Count);
            string selectedFood = availableForSelection[randomIndex];
            
            currentOrder.Add(selectedFood);
            
            // Remove from available list to prevent duplicates in same order
            availableForSelection.RemoveAt(randomIndex);
        }
        
        DebugLog($"Generated order #{ordersCompleted + 1}: [{string.Join(", ", currentOrder)}]");
    }
    
    void DisplayOrder()
    {
        // Clear previous order display
        ClearOrderDisplay();
        
        // Show order panel
        if (orderPanel != null)
            orderPanel.SetActive(true);
        
        // Update order title
        if (orderTitleText != null)
            orderTitleText.text = "Order:";
        
        // Create visual items for the order
        for (int i = 0; i < currentOrder.Count; i++)
        {
            CreateOrderDisplayItem(currentOrder[i], i);
        }
        
        orderActive = true;
        orderTimer = orderDisplayTime;
        
        DebugLog($"Order displayed: {currentOrder.Count} items");
    }
    
    void CreateOrderDisplayItem(string foodType, int index)
    {
        // Find the matching food item from our OrderItem array
        OrderItem orderItem = System.Array.Find(availableFoods, item => 
            item != null && item.foodType == foodType);
        
        if (orderItem != null && orderItem.displayPrefab != null)
        {
            // Calculate position for this item
            Vector3 itemPosition = CalculateOrderItemPosition(index);
            
            // Create the display item
            GameObject displayItem = Instantiate(orderItem.displayPrefab, orderContainer);
            displayItem.transform.localPosition = itemPosition;
            
            // Add to tracking list
            orderDisplayItems.Add(displayItem);
        }
        else
        {
            DebugLog($"No display prefab found for food type: {foodType}", true);
        }
    }
    
    Vector3 CalculateOrderItemPosition(int index)
    {
        // Arrange items left to right, similar to serve plate
        float x = startPosition.x + (index * itemSpacing);
        return new Vector3(x, startPosition.y, startPosition.z);
    }
    
    void ClearOrderDisplay()
    {
        // Destroy all current order display items
        foreach (GameObject item in orderDisplayItems)
        {
            if (item != null)
                Destroy(item);
        }
        orderDisplayItems.Clear();
    }
    
    void UpdateTimerDisplay()
    {
        if (orderTimerText != null)
        {
            int seconds = Mathf.CeilToInt(orderTimer);
            orderTimerText.text = $"Time: {seconds}s";
        }
    }
    
    #region Event Handlers
    
    void OnOrderServed()
    {
        if (!orderActive)
        {
            DebugLog("OnOrderServed called but order not active", true);
            return;
        }
        
        orderActive = false;
        
        DebugLog($"Order #{ordersCompleted + 1} served successfully");
        
        // CUSTOMER INTEGRATION: Notify customer manager
        if (isUsingCustomerFlow && customerManager != null)
        {
            bool isPerfect = CheckIfOrderPerfect();
            customerManager.HandleOrderServed(isPerfect);
            DebugLog($"Notified CustomerManager - Perfect order: {isPerfect}");
        }
        
        // Scoring is handled by ScoreManager when ServePlate.Serve() is called
    }
    
    void OnOrderExpired()
    {
        if (!orderActive)
        {
            DebugLog("OnOrderExpired called but order not active");
            return;
        }
        
        orderActive = false;
        
        DebugLog($"Order #{ordersCompleted + 1} expired!");
        
        // CUSTOMER INTEGRATION: Notify customer manager
        if (isUsingCustomerFlow && customerManager != null)
        {
            customerManager.HandleOrderExpired();
            DebugLog("Notified CustomerManager of expired order");
        }
        
        // Count expired orders as completed
        ordersCompleted++;
        UpdateOrderProgress();
    }
    
    #endregion
    
    #region Order Validation
    
    bool CheckIfOrderPerfect()
    {
        if (servePlate == null)
        {
            DebugLog("Cannot check order - ServePlate is null", true);
            return false;
        }
            
        List<string> servedItems = servePlate.GetServedItemTypes();
        
        // Check if served items exactly match current order
        if (servedItems.Count != currentOrder.Count)
        {
            DebugLog($"Order not perfect - Count mismatch. Served: {servedItems.Count}, Ordered: {currentOrder.Count}");
            return false;
        }
            
        // Create sorted copies for comparison
        List<string> sortedServed = new List<string>(servedItems);
        List<string> sortedOrder = new List<string>(currentOrder);
        sortedServed.Sort();
        sortedOrder.Sort();
        
        for (int i = 0; i < sortedServed.Count; i++)
        {
            if (sortedServed[i] != sortedOrder[i])
            {
                DebugLog($"Order not perfect - Item mismatch at index {i}. Served: {sortedServed[i]}, Ordered: {sortedOrder[i]}");
                return false;
            }
        }
        
        DebugLog("Order is perfect!");
        return true;
    }
    
    #endregion
    
    void HideOrder()
    {
        // Hide order panel
        if (orderPanel != null)
            orderPanel.SetActive(false);
        
        // Clear display items
        ClearOrderDisplay();
        
        orderActive = false;
        DebugLog("Order hidden");
    }
    
    /// <summary>
    /// FIXED: Called by ServePlate when serve button is pressed
    /// </summary>
    public void CompleteCurrentOrder()
    {
        if (!orderActive)
        {
            DebugLog("CompleteCurrentOrder called but no active order");
            return;
        }
        
        DebugLog($"Completing order #{ordersCompleted + 1}");
        
        ordersCompleted++;
        UpdateOrderProgress();
        OnOrderServed();
        
        // Stop the timer coroutine since order was manually completed
        if (orderTimerCoroutine != null)
        {
            StopCoroutine(orderTimerCoroutine);
            orderTimerCoroutine = null;
        }
        
        // Reset processing flag
        isProcessingCustomerOrder = false;
        
        // Hide the order immediately
        HideOrder();
        
        // Check for level completion
        if (ordersCompleted >= ordersPerLevel)
        {
            DebugLog("All orders completed via serve button - ending level");
            EndLevel();
        }
        else if (isUsingCustomerFlow)
        {
            // In customer flow, wait then spawn next customer
            StartCoroutine(DelayedNextCustomer());
        }
        // In original flow, the main cycle will handle the next order
    }
    
    /// <summary>
    /// NEW: Handle delayed next customer spawn
    /// </summary>
    IEnumerator DelayedNextCustomer()
    {
        DebugLog($"Waiting {timeBetweenOrders}s before next customer");
        yield return new WaitForSeconds(timeBetweenOrders);
        SpawnNextCustomer();
    }

    void EndLevel()
    {
        DebugLog("Ending level - stopping order system");
        
        // Stop the order system
        StopOrderSystem();
        
        // Notify LevelManager that level is complete
        if (levelManager != null)
        {
            levelManager.OnLevelComplete();
        }
    }
    
    #region Public API
    
    // Public methods for external access
    public List<string> GetCurrentOrder()
    {
        return new List<string>(currentOrder); // Return a copy
    }
    
    public bool IsOrderActive()
    {
        return orderActive;
    }
    
    public float GetRemainingTime()
    {
        return orderTimer;
    }
    
    public void StopOrderSystem()
    {
        DebugLog("Stopping order system");
        
        if (orderCycleCoroutine != null)
        {
            StopCoroutine(orderCycleCoroutine);
            orderCycleCoroutine = null;
        }
        
        if (orderTimerCoroutine != null)
        {
            StopCoroutine(orderTimerCoroutine);
            orderTimerCoroutine = null;
        }
        
        // Reset processing flag
        isProcessingCustomerOrder = false;
        
        HideOrder();
    }
    
    // Public method to get currently active food types
    public List<string> GetActiveFoodTypes()
    {
        return new List<string>(activeFoodTypes);
    }
    
    // Public method to manually refresh active food types (useful for debugging)
    public void RefreshActiveFoodTypes()
    {
        UpdateActiveFoodTypes();
    }
    
    /// <summary>
    /// NEW: Get current order progress info
    /// </summary>
    public void GetOrderProgress(out int completed, out int total)
    {
        completed = ordersCompleted;
        total = ordersPerLevel;
    }
    
    /// <summary>
    /// NEW: Check if using customer flow
    /// </summary>
    public bool IsUsingCustomerFlow()
    {
        return isUsingCustomerFlow;
    }
    
    #endregion
    
    #region Debug Utilities
    
    void DebugLog(string message, bool isWarning = false)
    {
        if (enableDebugLogs)
        {
            string formattedMessage = $"[OrderSystem] {message}";
            if (isWarning)
            {
                Debug.LogWarning(formattedMessage);
            }
            else
            {
                Debug.Log(formattedMessage);
            }
        }
    }
    
    /// <summary>
    /// NEW: Debug method to validate current state
    /// </summary>
    [ContextMenu("Debug Order State")]
    public void DebugOrderState()
    {
        Debug.Log($"=== ORDER SYSTEM STATE ===");
        Debug.Log($"Flow Type: {(isUsingCustomerFlow ? "Customer-Integrated" : "Original")}");
        Debug.Log($"Orders: {ordersCompleted}/{ordersPerLevel}");
        Debug.Log($"Order Active: {orderActive}");
        Debug.Log($"Processing Customer Order: {isProcessingCustomerOrder}");
        Debug.Log($"Current Order: [{string.Join(", ", currentOrder)}]");
        Debug.Log($"Active Food Types: [{string.Join(", ", activeFoodTypes)}]");
        Debug.Log($"Timer Remaining: {orderTimer:F1}s");
        Debug.Log($"Customer Manager: {(customerManager != null ? "Present" : "Missing")}");
        if (customerManager != null)
        {
            Debug.Log($"Customer Processing: {customerManager.IsProcessingCustomer()}");
        }
        Debug.Log($"=========================");
    }
    
    #endregion
}