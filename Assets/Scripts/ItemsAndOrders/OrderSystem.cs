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

    [Tooltip("Fraction of orderDisplayTime that must still be remaining when an order is completed for it to count as 'perfect' (plays the perfect-order sound). 0.5 = at least half the time left. Orders finished later play the normal order-done sound.")]
    [Range(0f, 1f)]
    public float perfectTimeFraction = 0.5f;

    public float timeBetweenOrders = 2f; // Time between orders
    public int minOrderItems = 1; // Minimum items in an order
    public int maxOrderItems = 4; // Maximum items in an order

    [Tooltip("Delay for deferred order system initialization")]
    public float systemInitializationDelay = 0.1f;

    private int ordersCompleted = 0;

    /// Orders finished or expired so far this day (read by DayNightTint).
    public int OrdersCompleted => ordersCompleted;

    [Header("Order Display UI")] public Text orderProgressText; // "Orders: 2/3"
    public Transform orderContainer; // Parent object to hold order items
    public Text orderTitleText; // Text showing "Order:" or similar
    public Text orderTimerText; // Text showing remaining time

    [Header("Available Food Items")]
    public OrderItem[] availableFoods = new OrderItem[4]; // Burger, Fries, Drink, Dessert

    [Header("Layout Settings")] public float itemSpacing = 1.5f; // Space between order items (used for both layouts)
    public Vector3 singleColumnStartPosition = Vector3.zero; // Starting position for 1-2 item orders
    public Vector3 twoColumnStartPosition = Vector3.zero;

    [Header("References")] public ScoreManager scoreManager; // Reference to get the final score
    public LevelManager levelManager; // Reference to level manager

    [Header("Customer Integration")] public CustomerManager customerManager; // Reference to customer manager

    [Header("Audio")] public AudioSource audioSource;
    public AudioClip itemServedSound;

    [Header("Debug Settings")] public bool enableDebugLogs = true;

    // Private variables - NEW SYSTEM: Individual item tracking
    private List<OrderItemInstance> currentOrderItems = new();
    private List<GameObject> orderDisplayObjects = new();
    private bool orderActive = false;
    private float orderTimer = 0f;
    private Coroutine orderCycleCoroutine;
    private Coroutine orderTimerCoroutine;

    // Running serve-item effect coroutines, so ClearOrderDisplay can stop them before destroying their targets
    private readonly List<Coroutine> activeServeEffects = new();

    // ADD THESE LINES:
    [Header("Speech Bubbles - Different Sizes")] [Tooltip("Speech bubble for 1 item orders")]
    public GameObject speechBubble1;

    [Tooltip("Speech bubble for 2 item orders")]
    public GameObject speechBubble2;

    [Tooltip("Speech bubble for 3 item orders")]
    public GameObject speechBubble3;

    [Tooltip("Speech bubble for 4 item orders")]
    public GameObject speechBubble4;

    // Cache for active food types
    private List<string> activeFoodTypes = new();

    // Flow control variables
    private bool isUsingCustomerFlow = false;
    private bool isInitialized = false;
    private bool isProcessingCustomerOrder = false;

    // Guard so Start/OnEnable/InitializeForCustomerFlow only run initialization once per enable cycle
    private bool hasInitializedSinceEnable = false;


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

    private void Start()
    {
        InitializeOrderSystem();
    }

    private void OnEnable()
    {
        // Reset when re-enabled by LevelManager
        ordersCompleted = 0;
        isProcessingCustomerOrder = false;
        hasInitializedSinceEnable = false;
        InitializeOrderSystem();
    }

    private void OnDisable()
    {
        // Allow a fresh initialization when re-enabled (level settings may have changed)
        hasInitializedSinceEnable = false;
    }

    private void InitializeOrderSystem()
    {
        // Idempotent: Start, OnEnable and InitializeForCustomerFlow may all call this in the same enable cycle
        if (hasInitializedSinceEnable)
        {
            DebugLog("OrderSystem already initialized for this enable cycle - skipping duplicate initialization");
            return;
        }

        hasInitializedSinceEnable = true;

        // Determine which flow to use
        isUsingCustomerFlow = customerManager != null;

        // Update active food types based on current level
        UpdateActiveFoodTypes();


        // Update order progress display
        UpdateOrderProgress();

        isInitialized = true;

        DebugLog(
            $"OrderSystem initialized. Flow: {(isUsingCustomerFlow ? "Customer-Integrated" : "Original")}, Orders per level: {ordersPerLevel}");
    }

    // NEW: Initialize for customer flow without starting cycle
    public void InitializeForCustomerFlow()
    {
        DebugLog("Initializing OrderSystem for customer flow");
        InitializeOrderSystem();
    }

    // ADD THIS NEW METHOD:
    private void ActivateSpeechBubbleForOrderSize(int itemCount)
    {
        // Deactivate all bubbles first
        if (speechBubble1 != null) speechBubble1.SetActive(false);
        if (speechBubble2 != null) speechBubble2.SetActive(false);
        // speechBubble3 removed - don't reference it
        if (speechBubble4 != null) speechBubble4.SetActive(false);

        // Activate the appropriate bubble based on item count
        switch (itemCount)
        {
            case 1:
                if (speechBubble1 != null) speechBubble1.SetActive(true);
                break;
            case 2:
                if (speechBubble2 != null) speechBubble2.SetActive(true);
                break;
            case 3:
            case 4:
                // Both 3 and 4 item orders use speechBubble4
                if (speechBubble4 != null) speechBubble4.SetActive(true);
                break;
            default:
                // Fallback: use speechBubble4
                if (speechBubble4 != null) speechBubble4.SetActive(true);
                Debug.LogWarning($"Order size {itemCount} exceeds available bubbles, using speechBubble4");
                break;
        }
    }

    private void UpdateActiveFoodTypes()
    {
        activeFoodTypes.Clear();

        if (levelManager != null)
        {
            // CHANGED: Get individual serveable items instead of trays
            var activeItems = levelManager.GetActiveServeableItems();

            foreach (var item in activeItems)
                if (item != null && !string.IsNullOrEmpty(item.GetFoodType()))
                    // Only add if we have a matching OrderItem for this food type
                    if (HasOrderItemForFoodType(item.GetFoodType()))
                        activeFoodTypes.Add(item.GetFoodType());
        }

        // Fallback: if no active food types found, use all available foods
        if (activeFoodTypes.Count == 0)
        {
            DebugLog("No active food types found! Using all available foods as fallback.", true);
            foreach (var item in availableFoods)
                if (item != null && !string.IsNullOrEmpty(item.foodType))
                    activeFoodTypes.Add(item.foodType);
        }

        DebugLog($"Active food types for orders: {string.Join(", ", activeFoodTypes)}");
    }

    // Helper method to check if we have an OrderItem for a given food type
    private bool HasOrderItemForFoodType(string foodType)
    {
        return System.Array.Exists(availableFoods, item =>
            item != null && item.foodType == foodType);
    }

    private void Update()
    {
        if (orderActive) UpdateOrderTimer();
    }

    private void UpdateOrderTimer()
    {
        if (!freezeTimer) orderTimer -= Time.deltaTime;

        if (orderTimer <= 0)
        {
            Debug.Log("Order timer is over, expired");
            ExpireOrder();
        }
    }

    /// When set, the next generated order uses exactly these food types (in this order) instead of a
    /// random pick, then clears itself. Used by the Level 0 tutorial to show a coffee and a bread.
    [System.NonSerialized] public List<string> forcedNextOrder;

    /// While true the order countdown does not run (the tutorial holds the order open while it
    /// explains). The tip is still computed from the remaining time, so it stays realistic.
    [System.NonSerialized] public bool freezeTimer;

    public void GenerateNewOrder()
    {
        if (orderActive) return;

        // Clear previous order
        ClearOrderDisplay();

        currentOrderItems.Clear();

        if (forcedNextOrder != null && forcedNextOrder.Count > 0)
        {
            var forced = forcedNextOrder;
            forcedNextOrder = null;
            for (var i = 0; i < forced.Count; i++)
                CreateOrderItemInstance(forced[i], i, forced.Count);
            DisplayOrder();
            Debug.Log($"New forced order generated with {currentOrderItems.Count} items");
            return;
        }

        // Generate random order items (clamp so min can never exceed max, e.g. a customer-specific minimum on a small level)
        var safeMax = Mathf.Max(1, maxOrderItems);
        var safeMin = Mathf.Clamp(minOrderItems, 1, safeMax);
        var orderSize = Random.Range(safeMin, safeMax + 1);

        // REMOVED: Allow duplicate food items in orders
        // orderSize = Mathf.Min(orderSize, activeFoodTypes.Count);

        for (var i = 0; i < orderSize; i++)
        {
            // Select random food type (can be duplicate)
            var randomFood = activeFoodTypes[Random.Range(0, activeFoodTypes.Count)];

            // FIXED: Pass the total orderSize to prevent incremental count bug
            CreateOrderItemInstance(randomFood, i, orderSize);
        }

        DisplayOrder();

        Debug.Log($"New order generated with {currentOrderItems.Count} items");
    }


    private void CreateOrderItemInstance(string foodType, int index, int totalOrderSize)
    {
        // Find the matching food item
        var orderItem = System.Array.Find(availableFoods, item =>
            item != null && item.foodType == foodType);

        if (orderItem != null && orderItem.displayPrefab != null)
        {
            // FIXED: Pass the intended total order size instead of using incremental count
            var itemPosition = CalculateOrderItemPosition(index, totalOrderSize);

            // Create the display item
            var displayItem = Instantiate(orderItem.displayPrefab, orderContainer);
            displayItem.transform.localPosition = itemPosition;

            // Add served item visual component for pop effect
            var servedVisual = displayItem.GetComponent<ServedItemVisual>();
            if (servedVisual == null)
                servedVisual = displayItem.AddComponent<ServedItemVisual>();

            // Create order item instance
            var orderInstance = new OrderItemInstance(foodType, displayItem);
            currentOrderItems.Add(orderInstance);
            orderDisplayObjects.Add(displayItem);
        }
    }

    private Vector3 CalculateOrderItemPosition(int index, int totalItems)
    {
        // For 1-2 items: Use single column layout
        if (totalItems <= 2)
        {
            // SINGLE COLUMN LAYOUT: Items stacked vertically
            var y = singleColumnStartPosition.y - index * itemSpacing;
            return new Vector3(singleColumnStartPosition.x, y, singleColumnStartPosition.z);
        }
        // For 3-4 items: Use 2x2 grid layout
        else
        {
            // TWO COLUMN LAYOUT: Calculate 2x2 grid positions using itemSpacing
            // Grid positions:
            // [0] [2]
            // [1] [3]

            var row = index % 2; // Row: 0 for items 0,2 and 1 for items 1,3
            var col = index / 2; // Column: 0 for items 0,1 and 1 for items 2,3

            // Calculate position using itemSpacing for both horizontal and vertical spacing
            var x = twoColumnStartPosition.x + col * itemSpacing;
            var y = twoColumnStartPosition.y - row * itemSpacing;

            return new Vector3(x, y, twoColumnStartPosition.z);
        }
    }

    private void DisplayOrder()
    {
        // Select and activate the appropriate speech bubble based on order size
        ActivateSpeechBubbleForOrderSize(currentOrderItems.Count);
        if (orderTitleText != null)
            orderTitleText.text = "Order:";

        orderActive = true;

        orderTimer = orderDisplayTime;

        Debug.Log($"Order active: {orderActive} , timer: {orderTimer}");
    }

    public bool TryServeItem(string foodType)
    {
        // Method stays exactly the same - no changes needed!
        // ServeableItem calls this method and it works perfectly

        if (!orderActive) return false;

        // Find the first unserved item of this type
        var itemToServe = currentOrderItems.Find(item =>
            item.foodType == foodType && !item.isServed);

        if (itemToServe != null)
        {
            // Mark as served
            itemToServe.isServed = true;

            // Play served item visual effect and remove (tracked so ClearOrderDisplay can stop it)
            Coroutine effectHandle = null;
            effectHandle = StartCoroutine(ServeItemWithEffect(itemToServe, () => activeServeEffects.Remove(effectHandle)));
            if (effectHandle != null) activeServeEffects.Add(effectHandle);

            // Award points for this item
            if (scoreManager != null) scoreManager.AwardItemPoints(foodType);

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

    private IEnumerator ServeItemWithEffect(OrderItemInstance item, System.Action onFinished = null)
    {
        try
        {
            if (item.displayObject != null)
            {
                // Get the visual component and play pop effect.
                // Yield the enumerator directly (not a nested StartCoroutine) so stopping this coroutine stops the effect too.
                var visual = item.displayObject.GetComponent<ServedItemVisual>();
                if (visual != null) yield return visual.PlayServedEffect();

                // Destroy the display object if it still exists (ClearOrderDisplay may have destroyed it mid-animation)
                if (item.displayObject != null) Destroy(item.displayObject);
            }
        }
        finally
        {
            onFinished?.Invoke();
        }
    }

    private void CheckOrderCompletion()
    {
        // Check if all items are served
        var allServed = true;
        foreach (var item in currentOrderItems)
            if (!item.isServed)
            {
                allServed = false;
                break;
            }

        if (allServed) CompleteOrder();
    }

    private int GetRemainingItemsCount()
    {
        var count = 0;
        foreach (var item in currentOrderItems)
            if (!item.isServed)
                count++;
        return count;
    }

    private void CompleteOrder()
    {
        Debug.Log("Order completed!");

        // Award completion bonus
        if (scoreManager != null)
        {
            var remainingTime = orderTimer;
            var orderBasePoints = scoreManager.currentOrderItemPoints;
            scoreManager.AwardOrderCompletionBonus(remainingTime, orderBasePoints);
        }

        // Perfect = finished with at least perfectTimeFraction of the display time still remaining
        // (expiry fires at <= 0, so "orderTimer > 0" would always be true here)
        var isPerfect = orderTimer >= orderDisplayTime * perfectTimeFraction;

        // Count as completed
        ordersCompleted++;
        UpdateOrderProgress();

        // CUSTOMER INTEGRATION: Notify customer manager (once, null-guarded)
        if (isUsingCustomerFlow && customerManager != null)
        {
            customerManager.HandleOrderServed(isPerfect);
            DebugLog($"Notified CustomerManager - Order completed (perfect: {isPerfect}, time left: {orderTimer:F2}s)");

            // Reset the processing flag to allow next customer orders
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

            if (levelManager != null) levelManager.OnLevelComplete();
        }
        else
        {
            // CUSTOMER FLOW: Wait for customer to leave, then spawn next customer
            if (isUsingCustomerFlow && customerManager != null)
                DebugLog("Waiting for customer to leave before spawning next customer");
            // Customer will leave automatically, and CustomerManager will handle next spawn
            else
                // ORIGINAL FLOW: Generate next order after delay
                Invoke("GenerateNewOrder", timeBetweenOrders);
        }
    }

    private void ExpireOrder()
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
        if (scoreManager != null) scoreManager.ApplyOrderExpiredPenalty();

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

            if (levelManager != null) levelManager.OnLevelComplete();
        }
        else
        {
            // CUSTOMER FLOW: Wait for customer to leave, then spawn next customer
            if (isUsingCustomerFlow && customerManager != null)
                DebugLog("Waiting for customer to leave before spawning next customer");
            // Customer will leave automatically after expiring
            else
                // ORIGINAL FLOW: Generate next order after delay
                Invoke("GenerateNewOrder", timeBetweenOrders);
        }
    }

    private void ClearOrderDisplay()
    {
        // Stop any in-flight serve effects first so they never touch a destroyed display object.
        // Snapshot + clear before stopping: stopping disposes the coroutine, whose finally block removes itself from the list.
        var effectsToStop = activeServeEffects.ToArray();
        activeServeEffects.Clear();
        foreach (var effect in effectsToStop)
            if (effect != null)
                StopCoroutine(effect);

        // Destroy all display objects
        foreach (var displayObj in orderDisplayObjects)
            if (displayObj != null)
                Destroy(displayObj);

        orderDisplayObjects.Clear();
        currentOrderItems.Clear();
    }

    private void UpdateOrderProgress()
    {
        if (orderProgressText != null)
            orderProgressText.text = $"{ordersCompleted}/{ordersPerLevel}";
    }

    private void PlayItemServedSound()
    {
        if (audioSource != null && itemServedSound != null)
            audioSource.PlayOneShot(itemServedSound);
        else if (AudioManager.Instance != null) AudioManager.Instance.PlayItemPickup();
    }

    private void PlayOrderCompleteSound()
    {
        AudioManager.Instance.PlayOrderComplete();
    }

    // Debug logging
    private void DebugLog(string message, bool isWarning = false)
    {
        if (enableDebugLogs)
        {
            if (isWarning)
                Debug.LogWarning($"[OrderSystem] {message}");
            else
                Debug.Log($"[OrderSystem] {message}");
        }
    }


    // For compatibility with existing code
    public bool IsOrderActive()
    {
        return orderActive;
    }


    public List<string> GetCurrentOrderTypes()
    {
        var types = new List<string>();
        foreach (var item in currentOrderItems)
            if (!item.isServed)
                types.Add(item.foodType);
        return types;
    }


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

        if (orderCycleCoroutine != null) StopCoroutine(orderCycleCoroutine);

        DebugLog($"Starting order cycle. Flow type: {(isUsingCustomerFlow ? "Customer-Integrated" : "Original")}");

        if (isUsingCustomerFlow)
            // Customer flow: wait for customer manager to generate orders
            DebugLog("Using customer flow - waiting for CustomerManager");
        else
            // Original flow: generate orders automatically
            orderCycleCoroutine = StartCoroutine(OrderCycleCoroutine());
    }

    private IEnumerator DeferredStartOrderCycle()
    {
        yield return new WaitForSeconds(systemInitializationDelay);
        if (isInitialized)
            StartOrderCycle();
        else
            DebugLog("OrderSystem still not initialized after delay", true);
    }

    private IEnumerator OrderCycleCoroutine()
    {
        DebugLog("Order cycle started");

        for (var i = 0; i < ordersPerLevel; i++)
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

        if (levelManager != null) levelManager.OnLevelComplete();
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
}

// NEW: Add this component to order display items for visual effects
public class ServedItemVisual : MonoBehaviour
{
    [Header("Pop Effect Settings")] public float popScale = 1.3f;
    public float popDuration = 0.2f;
    public float fadeDuration = 0.3f;

    public IEnumerator PlayServedEffect()
    {
        // This coroutine may be driven by another MonoBehaviour (OrderSystem), so it keeps running
        // after this object is destroyed. Check the target each iteration and bail out safely.
        if (this == null) yield break;

        var originalScale = transform.localScale;

        // Pop effect - scale up quickly
        var elapsed = 0f;
        while (elapsed < popDuration)
        {
            if (this == null) yield break; // target destroyed mid-animation

            elapsed += Time.deltaTime;
            var progress = elapsed / popDuration;

            var currentScale = Mathf.Lerp(1f, popScale, progress);
            transform.localScale = originalScale * currentScale;

            yield return null;
        }

        if (this == null) yield break;

        // Fade out effect
        var canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            if (this == null || canvasGroup == null) yield break; // target destroyed mid-animation

            elapsed += Time.deltaTime;
            var progress = elapsed / fadeDuration;

            canvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);
            transform.localScale = Vector3.Lerp(originalScale * popScale, originalScale * 0.5f, progress);

            yield return null;
        }
    }
}