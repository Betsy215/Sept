using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
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
    [Tooltip("Delay for deferred order system initialization")]
    public float systemInitializationDelay = 0.1f;
    private int ordersCompleted = 0;
    
    [Header("Order Display UI")]
    public Text orderProgressText; // "Orders: 2/3"
    public Transform orderContainer; // Parent object to hold order items
    public Text orderTitleText; // Text showing "Order:" or similar
    public Text orderTimerText; // Text showing remaining time
    
    [Header("Available Food Items")]
    public OrderItem[] availableFoods = new OrderItem[4]; // Burger, Fries, Drink, Dessert
    
    [Header("Layout Settings")]
    public float itemSpacing = 1.5f; // Space between order items
    public Vector3 startPosition = Vector3.zero; // Starting position for first item
    
    [Header("References")]
    public ScoreManager scoreManager; // Reference to get the final score
    public LevelManager levelManager; // Reference to level manager
    
    [Header("Customer Integration")]
    public CustomerManager customerManager; // Reference to customer manager
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip orderCompleteSound;
    public AudioClip itemServedSound;
    
    [Header("Debug Settings")]
    public bool enableDebugLogs = true;
    
    // Private variables - NEW SYSTEM: Individual item tracking
    private List<OrderItemInstance> currentOrderItems = new List<OrderItemInstance>();
    private List<GameObject> orderDisplayObjects = new List<GameObject>();
    private bool orderActive = false;
    private float orderTimer = 0f;
    private Coroutine orderCycleCoroutine;
    private Coroutine orderTimerCoroutine;
    
    // ADD THESE LINES:
    [Header("Speech Bubbles - Different Sizes")]
    [Tooltip("Speech bubble for 1 item orders")]
    public GameObject speechBubble1;
    [Tooltip("Speech bubble for 2 item orders")]
    public GameObject speechBubble2;
    [Tooltip("Speech bubble for 3 item orders")]
    public GameObject speechBubble3;
    [Tooltip("Speech bubble for 4 item orders")]
    public GameObject speechBubble4;
    
    // Cache for active food types
    private List<string> activeFoodTypes = new List<string>();
    
    // Flow control variables
    private bool isUsingCustomerFlow = false;
    private bool isInitialized = false;
    private bool isProcessingCustomerOrder = false;
    
    
    // NEW: Class to track individual order items (for multiple quantities)
    [System.Serializable]
    public class OrderItemInstance
    {
        public string foodType;
        public GameObject displayObject;
        public bool isServed = false;
        
        public OrderItemInstance(string type, GameObject display)
        {
            foodType = type;
            displayObject = display;
            isServed = false;
        }
    }
    
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
    // ADD THIS NEW METHOD:
    void ActivateSpeechBubbleForOrderSize(int itemCount)
    {
        
    
        // Activate the appropriate bubble based on item count
        switch(itemCount)
        {
            case 1:
                speechBubble1.SetActive(true);
                break;
            case 2:
               speechBubble2.SetActive(true);
                break;
            case 3:
               speechBubble3.SetActive(true);
                break;
            case 4:
               speechBubble4.SetActive(true);
                break;
            default:
                // Fallback: use the largest bubble if order size exceeds 4
              speechBubble4.SetActive(true);
                Debug.LogWarning($"Order size {itemCount} exceeds available bubbles, using bubble4");
                break;
        }
    }
    void UpdateActiveFoodTypes()
    {
        activeFoodTypes.Clear();
    
        if (levelManager != null)
        {
            // CHANGED: Get individual serveable items instead of trays
            ServeableItem[] activeItems = levelManager.GetActiveServeableItems();
        
            foreach (ServeableItem item in activeItems)
            {
                if (item != null && !string.IsNullOrEmpty(item.GetFoodType()))
                {
                    // Only add if we have a matching OrderItem for this food type
                    if (HasOrderItemForFoodType(item.GetFoodType()))
                    {
                        activeFoodTypes.Add(item.GetFoodType());
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
    
    void Update()
    {
        if (orderActive)
        {
            UpdateOrderTimer();
        }
    }
    
    void UpdateOrderTimer()
    {
        orderTimer -= Time.deltaTime;
        
        if (orderTimerText != null)
        {
            orderTimerText.text = "Time: " + Mathf.Ceil(orderTimer).ToString();
        }
        
        if (orderTimer <= 0)
        {
            ExpireOrder();
        }
    }
    
    public void GenerateNewOrder()
    {
        if (orderActive) return;
        
        // Clear previous order
        ClearOrderDisplay();
        
        // Generate random order items
        int orderSize = Random.Range(minOrderItems, maxOrderItems + 1);
        currentOrderItems.Clear();
        
        // Make sure we don't exceed available food types
        orderSize = Mathf.Min(orderSize, activeFoodTypes.Count);
        
        for (int i = 0; i < orderSize; i++)
        {
            // Select random food type
            string randomFood = activeFoodTypes[Random.Range(0, activeFoodTypes.Count)];
            
            // Create order item instance
            CreateOrderItemInstance(randomFood, i);
        }
        
        DisplayOrder();
        
        Debug.Log($"New order generated with {currentOrderItems.Count} items");
    }
    
    void CreateOrderItemInstance(string foodType, int index)
    {
        // Find the matching food item
        OrderItem orderItem = System.Array.Find(availableFoods, item => 
            item != null && item.foodType == foodType);
        
        if (orderItem != null && orderItem.displayPrefab != null)
        {
            // Calculate position for this item
            Vector3 itemPosition = CalculateOrderItemPosition(index);
            
            // Create the display item
            GameObject displayItem = Instantiate(orderItem.displayPrefab, orderContainer);
            displayItem.transform.localPosition = itemPosition;
            
            // Add served item visual component for pop effect
            ServedItemVisual servedVisual = displayItem.GetComponent<ServedItemVisual>();
            if (servedVisual == null)
                servedVisual = displayItem.AddComponent<ServedItemVisual>();
            
            // Create order item instance
            OrderItemInstance orderInstance = new OrderItemInstance(foodType, displayItem);
            currentOrderItems.Add(orderInstance);
            orderDisplayObjects.Add(displayItem);
        }
    }
    
    Vector3 CalculateOrderItemPosition(int index)
    {
        // VERTICAL LAYOUT: Items are stacked vertically (downward)
        float y = startPosition.y - (index * itemSpacing); // Negative to go downward
        return new Vector3(startPosition.x, y, startPosition.z);
    }

    void DisplayOrder()
    {
        // Select and activate the appropriate speech bubble based on order size
        ActivateSpeechBubbleForOrderSize(currentOrderItems.Count);
        if (orderTitleText != null)
            orderTitleText.text = "Order:";
    
        orderActive = true;
        orderTimer = orderDisplayTime;
    
        Debug.Log($"Order displayed: {currentOrderItems.Count} items using bubble{currentOrderItems.Count}");
    }
    public bool TryServeItem(string foodType)
    {
        // Method stays exactly the same - no changes needed!
        // ServeableItem calls this method and it works perfectly
    
        if (!orderActive) return false;
    
        // Find the first unserved item of this type
        OrderItemInstance itemToServe = currentOrderItems.Find(item => 
            item.foodType == foodType && !item.isServed);
    
        if (itemToServe != null)
        {
            // Mark as served
            itemToServe.isServed = true;
        
            // Play served item visual effect and remove
            StartCoroutine(ServeItemWithEffect(itemToServe));
        
            // Award points for this item
            if (scoreManager != null)
            {
                scoreManager.AwardItemPoints(foodType);
            }
        
            // Play item served sound
            PlayItemServedSound();
        
            // Check if order is complete
            CheckOrderCompletion();
        
            Debug.Log($"Served {foodType}. Remaining items: {GetRemainingItemsCount()}");
            return true;
        }
    
        Debug.Log($"No {foodType} needed in current order");
        return false;
    }
    
    IEnumerator ServeItemWithEffect(OrderItemInstance item)
    {
        if (item.displayObject != null)
        {
            // Get the visual component and play pop effect
            ServedItemVisual visual = item.displayObject.GetComponent<ServedItemVisual>();
            if (visual != null)
            {
                yield return StartCoroutine(visual.PlayServedEffect());
            }
            
            // Destroy the display object
            Destroy(item.displayObject);
        }
    }
    
    void CheckOrderCompletion()
    {
        // Check if all items are served
        bool allServed = true;
        foreach (var item in currentOrderItems)
        {
            if (!item.isServed)
            {
                allServed = false;
                break;
            }
        }
        
        if (allServed)
        {
            CompleteOrder();
        }
    }
    
    int GetRemainingItemsCount()
    {
        int count = 0;
        foreach (var item in currentOrderItems)
        {
            if (!item.isServed) count++;
        }
        return count;
    }
    
    void CompleteOrder()
    {
        Debug.Log("Order completed!");
        
        // Award completion bonus
        if (scoreManager != null)
        {
            float remainingTime = orderTimer;
            float orderBasePoints = scoreManager.currentOrderItemPoints;
            scoreManager.AwardOrderCompletionBonus(remainingTime, orderBasePoints);
        }
        
        // Play order complete sound
        PlayOrderCompleteSound();
        
        // Count as completed
        ordersCompleted++;
        UpdateOrderProgress();
        
        // CUSTOMER INTEGRATION: Notify customer manager
        if (isUsingCustomerFlow && customerManager != null)
        {
            customerManager.HandleOrderServed(true); // Always perfect in new system
            DebugLog("Notified CustomerManager - Order completed");
    
            // CRITICAL FIX: Reset the processing flag to allow next customer orders
            isProcessingCustomerOrder = false;
            DebugLog("Reset isProcessingCustomerOrder flag for next customer");
        }
        
        // Hide order and prepare for next one
        orderActive = false;
        if (speechBubble1 != null) speechBubble1.SetActive(false);
        if (speechBubble2 != null) speechBubble2.SetActive(false);
        if (speechBubble3 != null) speechBubble3.SetActive(false);
        if (speechBubble4 != null) speechBubble4.SetActive(false);

        // Check if level is complete
        if (ordersCompleted >= ordersPerLevel)
        {
            DebugLog("All orders completed for this level!");
            
            if (levelManager != null)
            {
                levelManager.OnLevelComplete();
            }

           
        }
        else
        {
            // CUSTOMER FLOW: Wait for customer to leave, then spawn next customer
            if (isUsingCustomerFlow && customerManager != null)
            {
                DebugLog("Waiting for customer to leave before spawning next customer");
                // Customer will leave automatically, and when OnCustomerExited is called,
                // the CustomerManager will be ready for the next customer
                // We don't automatically generate the next order here
            }
            else
            {
                // ORIGINAL FLOW: Generate next order after delay
                Invoke("GenerateNewOrder", timeBetweenOrders);
            }
        }
    }
    
    void ExpireOrder()
    {
        Debug.Log("Order expired!");
    
        // CRITICAL FIX: Clear all order display objects before marking order as inactive
        ClearOrderDisplay();
    
        orderActive = false;
        if (speechBubble1 != null) speechBubble1.SetActive(false);
        if (speechBubble2 != null) speechBubble2.SetActive(false);
        if (speechBubble3 != null) speechBubble3.SetActive(false);
        if (speechBubble4 != null) speechBubble4.SetActive(false);

        // Penalize for expired order
        if (scoreManager != null)
        {
            scoreManager.ApplyOrderExpiredPenalty();
        }
    
        // Count as completed (even if expired)
        ordersCompleted++;
        UpdateOrderProgress();
    
        // CUSTOMER INTEGRATION: Notify customer manager
        if (isUsingCustomerFlow && customerManager != null)
        {
            customerManager.HandleOrderExpired();
            DebugLog("Notified CustomerManager of expired order");
        
            // CRITICAL FIX: Reset the processing flag to allow next customer orders  
            isProcessingCustomerOrder = false;
            DebugLog("Reset isProcessingCustomerOrder flag after expiry");
        }
    
        // Check if level is complete
        if (ordersCompleted >= ordersPerLevel)
        {
            DebugLog("All orders processed for this level!");
        
            if (levelManager != null)
            {
                levelManager.OnLevelComplete();
            }
        }
        else
        {
            // CUSTOMER FLOW: Wait for customer to leave, then spawn next customer
            if (isUsingCustomerFlow && customerManager != null)
            {
                DebugLog("Waiting for customer to leave before spawning next customer");
                // Customer will leave automatically after expiring
            }
            else
            {
                // ORIGINAL FLOW: Generate next order after delay
                Invoke("GenerateNewOrder", timeBetweenOrders);
            }
        }
    }
    
    void ClearOrderDisplay()
    {
        // Destroy all display objects
        foreach (GameObject displayObj in orderDisplayObjects)
        {
            if (displayObj != null)
                Destroy(displayObj);
        }
        
        orderDisplayObjects.Clear();
        currentOrderItems.Clear();
    }
    
    void UpdateOrderProgress()
    {
        if (orderProgressText != null)
            orderProgressText.text = $"{ordersCompleted}/{ordersPerLevel}";
    }
    
    void PlayItemServedSound()
    {
        if (audioSource != null && itemServedSound != null)
        {
            audioSource.PlayOneShot(itemServedSound);
        }
        else if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayItemPickup();
        }
    }
    
    void PlayOrderCompleteSound()
    {
        if (audioSource != null && orderCompleteSound != null)
        {
            audioSource.PlayOneShot(orderCompleteSound);
        }
        else if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayOrderComplete();
        }
    }
    
    // Debug logging
    void DebugLog(string message, bool isWarning = false)
    {
        if (enableDebugLogs)
        {
            if (isWarning)
                Debug.LogWarning($"[OrderSystem] {message}");
            else
                Debug.Log($"[OrderSystem] {message}");
        }
    }
    
    #region Public API - Compatibility with existing scripts
    
    // For compatibility with existing code
    public bool IsOrderActive()
    {
        return orderActive;
    }
    
    public float GetRemainingTime()
    {
        return orderTimer;
    }
    
    public List<string> GetCurrentOrderTypes()
    {
        List<string> types = new List<string>();
        foreach (var item in currentOrderItems)
        {
            if (!item.isServed)
                types.Add(item.foodType);
        }
        return types;
    }
    
    // For compatibility with ScoreManager and other existing scripts
    public List<string> GetCurrentOrder()
    {
        return GetCurrentOrderTypes();
    }
    
    // For compatibility with LevelManager
    public void CompleteLevel()
    {
        EndLevel();
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
        
        orderActive = false;
    }
    
    #endregion
    
    #region CUSTOMER FLOW METHODS - For CustomerManager integration
    
    public void GenerateCustomerOrder()
    {
        if (isProcessingCustomerOrder)
        {
            DebugLog("Already processing customer order, ignoring new request");
            return;
        }
        
        isProcessingCustomerOrder = true;
        DebugLog("Generating order for customer");
        GenerateNewOrder();
    }
    
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
        
        if (isUsingCustomerFlow)
        {
            // Customer flow: wait for customer manager to generate orders
            DebugLog("Using customer flow - waiting for CustomerManager");
        }
        else
        {
            // Original flow: generate orders automatically
            orderCycleCoroutine = StartCoroutine(OrderCycleCoroutine());
        }
    }
    
    IEnumerator DeferredStartOrderCycle()
    {
        yield return new WaitForSeconds(systemInitializationDelay);
        if (isInitialized)
        {
            StartOrderCycle();
        }
        else
        {
            DebugLog("OrderSystem still not initialized after delay", true);
        }
    }
    
    IEnumerator OrderCycleCoroutine()
    {
        DebugLog("Order cycle started");
        
        for (int i = 0; i < ordersPerLevel; i++)
        {
            DebugLog($"Generating order {i + 1}/{ordersPerLevel}");
            GenerateNewOrder();
            
            // Wait until order is completed or expired
            yield return new WaitUntil(() => !orderActive);
            
            // Wait between orders (except for the last one)
            if (i < ordersPerLevel - 1)
            {
                DebugLog($"Waiting {timeBetweenOrders}s before next order");
                yield return new WaitForSeconds(timeBetweenOrders);
            }
        }
        
        DebugLog("All orders completed! Notifying LevelManager");
        
        if (levelManager != null)
        {
            levelManager.OnLevelComplete();
        }
    }
    
    // Called by CustomerManager after customer delay to start the order
    public void StartOrderCycleForCustomer()
    {
        if (!isUsingCustomerFlow)
        {
            DebugLog("StartOrderCycleForCustomer called but not using customer flow!", true);
            return;
        }
        
        if (isProcessingCustomerOrder)
        {
            DebugLog("Already processing customer order - ignoring StartOrderCycleForCustomer");
            return;
        }
        
        DebugLog("Starting order cycle for customer");
        GenerateCustomerOrder();
    }
    
    // Called when customer spawns next customer
    void SpawnNextCustomer()
    {
        if (isUsingCustomerFlow && customerManager != null)
        {
            customerManager.SpawnCustomerForCurrentLevel();
        }
    }
    
    // Handle delayed next customer spawn
    IEnumerator DelayedNextCustomer()
    {
        DebugLog($"Waiting {timeBetweenOrders}s before next customer");
        yield return new WaitForSeconds(timeBetweenOrders);
        SpawnNextCustomer();
    }
    
    #endregion
    
   
}

// NEW: Add this component to order display items for visual effects
public class ServedItemVisual : MonoBehaviour
{
    [Header("Pop Effect Settings")]
    public float popScale = 1.3f;
    public float popDuration = 0.2f;
    public float fadeDuration = 0.3f;
    
    public IEnumerator PlayServedEffect()
    {
        Vector3 originalScale = transform.localScale;
        
        // Pop effect - scale up quickly
        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / popDuration;
            
            float currentScale = Mathf.Lerp(1f, popScale, progress);
            transform.localScale = originalScale * currentScale;
            
            yield return null;
        }
        
        // Fade out effect
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fadeDuration;
            
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);
            transform.localScale = Vector3.Lerp(originalScale * popScale, originalScale * 0.5f, progress);
            
            yield return null;
        }
    }
}