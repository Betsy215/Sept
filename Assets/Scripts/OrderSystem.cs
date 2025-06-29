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
    
    // Private variables
    private List<string> currentOrder = new List<string>();
    private List<GameObject> orderDisplayItems = new List<GameObject>();
    private bool orderActive = false;
    private float orderTimer = 0f;
    private Coroutine orderCoroutine;
    private Coroutine orderTimerCoroutine;
    
    // Cache for active food types
    private List<string> activeFoodTypes = new List<string>();
    
    void Start()
    {
        InitializeOrderSystem();
    }
    
    void OnEnable()
    {
        // Reset when re-enabled by LevelManager
        ordersCompleted = 0;
        InitializeOrderSystem();
    }
    
    void InitializeOrderSystem()
    {
        // Update active food types based on current level
        UpdateActiveFoodTypes();
        
        // Hide order panel initially
        HideOrder();
        
        // Update order progress display
        UpdateOrderProgress();
        
        Debug.Log($"OrderSystem initialized. Orders per level: {ordersPerLevel}");
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
            Debug.LogWarning("No active food types found! Using all available foods as fallback.");
            foreach (OrderItem item in availableFoods)
            {
                if (item != null && !string.IsNullOrEmpty(item.foodType))
                {
                    activeFoodTypes.Add(item.foodType);
                }
            }
        }
        
        Debug.Log($"Active food types for orders: {string.Join(", ", activeFoodTypes)}");
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
    
    #region Original Order Cycle (Modified for Customer Integration)
    
    public void StartOrderCycle()
    {
        if (orderCoroutine != null)
            StopCoroutine(orderCoroutine);
            
        orderCoroutine = StartCoroutine(OrderCycleCoroutine());
    }
    
    IEnumerator OrderCycleCoroutine()
    {
        while (ordersCompleted < ordersPerLevel)
        {
            // Update active food types each time (in case trays change mid-level)
            UpdateActiveFoodTypes();
            
            // CUSTOMER INTEGRATION: Spawn customer first
            if (customerManager != null)
            {
                Debug.Log("Spawning customer for new order");
                customerManager.SpawnCustomerForCurrentLevel();
                
                // Wait for customer to reach service point and complete their delay
                // The customer manager will call StartOrderCycleForCustomer() after delay
                // So we yield break here to avoid double-processing
                yield break;
            }
            else
            {
                // Fallback to original behavior if no customer manager
                Debug.LogWarning("No CustomerManager found - using original order flow");
                
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
                
                    // Check if order was served
                    if (CheckIfOrderServed())
                    {
                        OnOrderServed();
                        break;
                    }
                
                    yield return null;
                }
            
                // Order expired or was served
                if (orderActive)
                {
                    OnOrderExpired();
                }
            
                // Hide order and wait before next one
                HideOrder();
            
                // Check if level complete before waiting
                if (ordersCompleted >= ordersPerLevel)
                {
                    break; // Exit the loop
                }
            
                yield return new WaitForSeconds(timeBetweenOrders);
            }
        }
    
        // Level is complete
        EndLevel();
    }
    
    #endregion
    
    #region Customer Integration Methods
    
    /// <summary>
    /// Called by CustomerManager after customer delay to actually start the order
    /// </summary>
    public void StartOrderCycleForCustomer()
    {
        Debug.Log("Starting order cycle for customer");
        
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
        // Update timer every frame
        while (orderTimer > 0 && orderActive)
        {
            orderTimer -= Time.deltaTime;
            UpdateTimerDisplay();
        
            // Check if order was served
            if (CheckIfOrderServed())
            {
                OnOrderServed();
                yield break;
            }
        
            yield return null;
        }

        // Order expired if we get here
        if (orderActive)
        {
            OnOrderExpired();
        }

        // Hide order 
        HideOrder();

        // Check if level complete
        if (ordersCompleted >= ordersPerLevel)
        {
            EndLevel();
        }
        else
        {
            // Wait before allowing next customer
            yield return new WaitForSeconds(timeBetweenOrders);
            
            // Ready for next order cycle - spawn next customer
            if (customerManager != null)
            {
                Debug.Log("Ready for next customer");
                customerManager.SpawnCustomerForCurrentLevel();
            }
        }
    }
    
    #endregion
    
    void GenerateNewOrder()
    {
        currentOrder.Clear();
        
        // Make sure we have active food types
        if (activeFoodTypes.Count == 0)
        {
            Debug.LogError("No active food types available for order generation!");
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
            // Comment out the next line if you want to allow duplicate items in orders
            availableForSelection.RemoveAt(randomIndex);
        }
        
        Debug.Log($"Generated order: {string.Join(", ", currentOrder)}");
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
            Debug.LogWarning($"No display prefab found for food type: {foodType}");
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
    
    bool CheckIfOrderServed()
    {
        // This will be called when serve button is pressed via ServePlate
        return false;
    }
    
    #region Event Handlers (Modified for Customer Integration)
    
    void OnOrderServed()
    {
        orderActive = false;
        
        // CUSTOMER INTEGRATION: Notify customer manager
        if (customerManager != null)
        {
            // Check if order was perfect
            bool isPerfect = CheckIfOrderPerfect();
            customerManager.HandleOrderServed(isPerfect);
            Debug.Log($"Order served - Perfect: {isPerfect}");
        }
        
        // Scoring is handled by ScoreManager
    }
    
    void OnOrderExpired()
    {
        orderActive = false;
        
        // CUSTOMER INTEGRATION: Notify customer manager
        if (customerManager != null)
        {
            customerManager.HandleOrderExpired();
            Debug.Log("Order expired - customer disappointed");
        }
        
        Debug.Log("Order expired!");
    }
    
    #endregion
    
    #region Order Validation
    
    bool CheckIfOrderPerfect()
    {
        if (servePlate == null)
            return false;
            
        List<string> servedItems = servePlate.GetServedItemTypes();
        
        // Check if served items exactly match current order
        if (servedItems.Count != currentOrder.Count)
            return false;
            
        foreach (string orderItem in currentOrder)
        {
            if (!servedItems.Contains(orderItem))
                return false;
        }
        
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
    }
    
    public void CompleteCurrentOrder()
    {
        if (orderActive)
        {
            ordersCompleted++;
            UpdateOrderProgress();
            OnOrderServed(); // This will now notify customer manager
        
            if (ordersCompleted >= ordersPerLevel)
            {
                EndLevel();
            }
        }
    }

    void EndLevel()
    {
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
        if (orderCoroutine != null)
        {
            StopCoroutine(orderCoroutine);
            orderCoroutine = null;
        }
        
        if (orderTimerCoroutine != null)
        {
            StopCoroutine(orderTimerCoroutine);
            orderTimerCoroutine = null;
        }
        
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
    
    #endregion
}