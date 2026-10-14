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

/// Orders of the customers at the counter. Since 12 October 2026 up to two customers can wait at once, each with
/// an order of their own (slot 0 and slot 1), so a second customer may arrive while the first is being served.
/// A tap on a food serves the oldest open order that still needs that food. Every order keeps its own timer,
/// tip multiplier and points. Slot 0 uses the scene's bubble objects and OrderContainer; slot 1 uses clones of them.
/// With maxConcurrentOrders = 1 (the tutorial) everything behaves and looks as before.
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

    [Header("Two customers (12 October 2026)")]
    [Tooltip("How many customers can wait with an order at the same time: 2 in play, 1 in the tutorial")]
    public int maxConcurrentOrders = 2;
    [Tooltip("Where slot 0's bubbles and icons move when two customers can wait (the scene places them for one customer at the right)")]
    // The bubble's centre sits at root + scale * (-1.11, 2.14): these roots put the bubbles over the customers
    // (x 1.2 and -0.5, CustomerManager.twoSlotCustomerX + 1) with their centres at y 2.95, under the corner buttons (owner 13 October, layout D)
    public Vector2 slot0BubbleOffset = new Vector2(1.92f, 1.5f);    // y 1.5: bubbles just over the people's heads (owner, 13 October: closer to the people, people a little higher)
    public Vector2 slot1BubbleOffset = new Vector2(0.22f, 1.5f);
    [Tooltip("Scale of each bubble set when two customers can wait")]
    public float twoSlotBubbleScale = 0.65f;   // 0.7 bubbles 1.7 apart would touch; at 0.85 they reached the top buttons

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

    // ADD THESE LINES:
    [Header("Speech Bubbles - Different Sizes")] [Tooltip("Speech bubble for 1 item orders")]
    public GameObject speechBubble1;

    [Tooltip("Speech bubble for 2 item orders")]
    public GameObject speechBubble2;

    [Tooltip("Speech bubble for 3 item orders")]
    public GameObject speechBubble3;

    [Tooltip("Speech bubble for 4 item orders")]
    public GameObject speechBubble4;

    /// Patience read by PatienceMeter: an order's seconds left and full time (per order since 12 October).
    public float RemainingTime => orders.Count > 0 ? orders[0].timer : 0f;
    public float TotalTime => orders.Count > 0 ? orders[0].displayTime : orderDisplayTime;
    public bool OrderActive => orders.Count > 0;
    /// Petals on the daisy for the next customer's order (set by CustomerManager; kept for the single-customer path).
    [System.NonSerialized] public int patiencePetals;

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

    /// One customer's open order
    public class ActiveOrder
    {
        public int slot;
        public CustomerController customer;
        public readonly List<OrderItemInstance> items = new List<OrderItemInstance>();
        public readonly List<GameObject> displayObjects = new List<GameObject>();
        public readonly List<Coroutine> serveEffects = new List<Coroutine>();
        public float timer, displayTime, tipMultiplier = 1f, basePoints, startedAt;
        public int petals;
        public bool active;
        public bool hadCustomer;                 // opened for a customer (false on the original no-customer flow)
        public bool AllServed => items.TrueForAll(i => i.isServed);
        public bool Needs(string food) => items.Exists(i => !i.isServed && i.foodType == food);
    }

    /// The bubble objects and icon container of one slot
    private class SlotVisuals
    {
        public Transform root;
        public GameObject bubble1, bubble2, bubble4;
        public Transform container;
    }

    private readonly List<ActiveOrder> orders = new List<ActiveOrder>();   // open orders, oldest first
    private SlotVisuals[] slots;
    private bool layoutForTwo;

    private Coroutine orderCycleCoroutine;

    // Cache for active food types
    private List<string> activeFoodTypes = new();

    // Flow control variables
    private bool isUsingCustomerFlow = false;
    private bool isInitialized = false;

    // Guard so Start/OnEnable/InitializeForCustomerFlow only run initialization once per enable cycle
    private bool hasInitializedSinceEnable = false;

    private void Start()
    {
        InitializeOrderSystem();
    }

    private void OnEnable()
    {
        // Reset when re-enabled by LevelManager: no order survives a level change
        ordersCompleted = 0;
        foreach (var o in orders.ToArray()) { PatienceMeter.Hide(o); ClearOrderDisplay(o); if (slots != null) SetBubble(slots[o.slot], 0); }
        orders.Clear();
        hasInitializedSinceEnable = false;
        InitializeOrderSystem();
    }

    private void OnDisable()
    {
        // Allow a fresh initialization when re-enabled (level settings may have changed)
        hasInitializedSinceEnable = false;
    }

    private void OnDestroy()
    {
        PatienceMeter.HideAll();   // no meter entry survives the scene
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

        BuildSlots();
        ApplyLayout();

        // Update order progress display
        UpdateOrderProgress();

        isInitialized = true;

        DebugLog(
            $"OrderSystem initialized. Flow: {(isUsingCustomerFlow ? "Customer-Integrated" : "Original")}, Orders per level: {ordersPerLevel}, slots: {maxConcurrentOrders}");
    }

    // NEW: Initialize for customer flow without starting cycle
    public void InitializeForCustomerFlow()
    {
        DebugLog("Initializing OrderSystem for customer flow");
        InitializeOrderSystem();
        ApplyLayout();   // the tutorial may have set maxConcurrentOrders after the first initialization
    }

    /// Slot 0 = the scene's bubbles and container, gathered under one root; slot 1 = clones of them under a second root.
    private void BuildSlots()
    {
        if (slots != null) return;
        var s0 = new SlotVisuals { root = new GameObject("OrderSlot0").transform };
        s0.root.SetParent(transform.parent, false);
        s0.root.position = Vector3.zero;
        if (speechBubble1 != null) speechBubble1.transform.SetParent(s0.root, true);
        if (speechBubble2 != null) speechBubble2.transform.SetParent(s0.root, true);
        if (speechBubble4 != null) speechBubble4.transform.SetParent(s0.root, true);
        if (orderContainer != null) orderContainer.SetParent(s0.root, true);
        s0.bubble1 = speechBubble1; s0.bubble2 = speechBubble2; s0.bubble4 = speechBubble4; s0.container = orderContainer;

        var s1 = new SlotVisuals { root = new GameObject("OrderSlot1").transform };
        s1.root.SetParent(transform.parent, false);
        s1.root.position = Vector3.zero;
        s1.bubble1 = CloneUnder(speechBubble1, s1.root);
        s1.bubble2 = CloneUnder(speechBubble2, s1.root);
        s1.bubble4 = CloneUnder(speechBubble4, s1.root);
        if (orderContainer != null)
        {
            var c = new GameObject("OrderContainer1").transform;
            c.SetParent(s1.root, false);
            c.position = orderContainer.position;
            c.localScale = orderContainer.lossyScale;
            s1.container = c;
        }
        slots = new[] { s0, s1 };
    }

    private static GameObject CloneUnder(GameObject original, Transform root)
    {
        if (original == null) return null;
        var clone = Instantiate(original, root);          // same local position under a root at the origin = same place
        clone.name = original.name + "_1";
        clone.SetActive(false);
        return clone;
    }

    /// One customer: the bubbles stay where the scene puts them. Two: each slot's set moves up beside its customer.
    private void ApplyLayout()
    {
        if (slots == null) return;
        var two = maxConcurrentOrders > 1;
        if (two == layoutForTwo && slots[0].root.position == (two ? (Vector3)slot0BubbleOffset : Vector3.zero)) return;
        layoutForTwo = two;
        slots[0].root.position = two ? (Vector3)slot0BubbleOffset : Vector3.zero;
        slots[1].root.position = two ? (Vector3)slot1BubbleOffset : Vector3.zero;
        var k = two ? twoSlotBubbleScale : 1f;
        slots[0].root.localScale = Vector3.one * k;
        slots[1].root.localScale = Vector3.one * k;
    }

    /// The bubble sits over its customer: centred on the customer, its bottom just above the head, whatever the
    /// customer's height or the bubble's size (owner, 13 October). With two slots the whole set (bubble, icons) moves.
    [Tooltip("Gap between the customer's head and the bubble's lowest point (its tail), world units; negative overlaps")]
    public float bubbleGapAboveHead = 0f;
    [Tooltip("Transparent rows above the head in every customer picture (Store/artkit/rim0.py pads 40 px), so the bubble can sit on the real head")]
    public float customerCanvasPadPx = 40f;
    private void PlaceBubbleOverCustomer(SlotVisuals s, CustomerController customer)
    {
        if (customer == null || maxConcurrentOrders <= 1) return;
        var b = s.bubble1 != null && s.bubble1.activeSelf ? s.bubble1 : s.bubble2 != null && s.bubble2.activeSelf ? s.bubble2 : s.bubble4;
        var cr = customer.GetComponentInChildren<SpriteRenderer>();
        if (b == null || cr == null) return;
        // the whole bubble: body, tail dots and icons together
        var rs = b.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        var bb = rs[0].bounds;
        foreach (var r in rs) bb.Encapsulate(r.bounds);
        var cb = cr.bounds;
        var headTop = cb.max.y - (cr.sprite != null ? customerCanvasPadPx * cb.size.y / cr.sprite.rect.height : 0f);
        var delta = new Vector3(cb.center.x - bb.center.x, headTop + bubbleGapAboveHead - bb.min.y, 0f);
        s.root.position += delta;
    }

    /// Width of the biggest bubble (four items) at the two-slot scale, world units. CustomerManager spaces the two
    /// customers from it so the margins left, between and right of two big bubbles are equal (owner, 13 October).
    public float TwoSlotBigBubbleWidth()
    {
        var b = speechBubble4 != null ? speechBubble4 : speechBubble2 != null ? speechBubble2 : speechBubble1;
        var sr = b != null ? b.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (sr == null || sr.sprite == null) return 2f;
        var w = sr.sprite.bounds.size.x * sr.transform.lossyScale.x;
        if (slots != null && slots[0].root != null) w /= Mathf.Max(0.01f, slots[0].root.localScale.x);   // without the root's current scale
        return w * twoSlotBubbleScale;
    }

    /// The patience flowers sit centred over whichever bubble is showing, on its top edge, whatever its size
    /// (owner, 13 October). Falls back to the fixed offset when the bubble has no renderer.
    private Vector3 MeterPosition(SlotVisuals s)
    {
        var b = s.bubble1 != null && s.bubble1.activeSelf ? s.bubble1 : s.bubble2 != null && s.bubble2.activeSelf ? s.bubble2 : s.bubble4;
        var r = b != null ? b.GetComponentInChildren<Renderer>() : null;
        if (r == null) return s.root.TransformPoint(PatienceMeter.WorldPosition);
        var bounds = r.bounds;
        return new Vector3(bounds.center.x, bounds.max.y - 0.04f, 0f);
    }

    private void SetBubble(SlotVisuals s, int itemCount)
    {
        if (s.bubble1 != null) s.bubble1.SetActive(false);
        if (s.bubble2 != null) s.bubble2.SetActive(false);
        if (s.bubble4 != null) s.bubble4.SetActive(false);
        if (itemCount <= 0) return;
        var b = itemCount == 1 ? s.bubble1 : itemCount == 2 ? s.bubble2 : s.bubble4;
        if (b == null) b = s.bubble4;
        if (b != null) b.SetActive(true);
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

    private bool HasOrderItemForFoodType(string foodType)
    {
        return System.Array.Exists(availableFoods, item =>
            item != null && item.foodType == foodType);
    }

    private void Update()
    {
        if (orders.Count == 0 || freezeTimer) return;
        // oldest first; expiring one may remove it from the list, so walk a copy
        foreach (var o in orders.ToArray())
        {
            if (!o.active) continue;
            o.timer -= Time.deltaTime;
            if (o.timer <= 0f)
            {
                Debug.Log($"Order timer is over, expired (slot {o.slot})");
                ExpireOrder(o);
            }
        }
    }

    /// When set, the next generated order uses exactly these food types (in this order) instead of a
    /// random pick, then clears itself. Used by the Level 0 tutorial to show a coffee and a bread.
    [System.NonSerialized] public List<string> forcedNextOrder;

    /// While true the order countdown does not run (the tutorial holds the order open while it
    /// explains). The tip is still computed from the remaining time, so it stays realistic.
    [System.NonSerialized] public bool freezeTimer;

    /// Opens an order for a customer in its slot. Called by CustomerManager after the customer's order delay.
    public void StartOrderForCustomer(CustomerController customer, int slot, int minItems, float displayTime, float tipMultiplier, int petals)
    {
        if (slots == null) BuildSlots();
        slot = Mathf.Clamp(slot, 0, slots.Length - 1);
        var stale = orders.Find(o => o.slot == slot && o.active);
        if (stale != null)
        {
            // should not happen; if it ever does, the old order is closed so the new customer is never left waiting
            DebugLog($"Slot {slot} already had an open order - expiring it first", true);
            ExpireOrder(stale);
        }
        var order = new ActiveOrder
        {
            slot = slot, customer = customer, displayTime = Mathf.Max(0.1f, displayTime), timer = Mathf.Max(0.1f, displayTime),
            tipMultiplier = tipMultiplier, petals = petals, startedAt = Time.time, active = true, hadCustomer = !ReferenceEquals(customer, null)
        };
        var s = slots[slot];

        if (forcedNextOrder != null && forcedNextOrder.Count > 0)
        {
            var forced = forcedNextOrder;
            forcedNextOrder = null;
            for (var i = 0; i < forced.Count; i++) CreateOrderItemInstance(order, s, forced[i], i, forced.Count);
        }
        else
        {
            // Random order items (clamp so min can never exceed max, e.g. a customer-specific minimum on a small level)
            var safeMax = Mathf.Max(1, maxOrderItems);
            var safeMin = Mathf.Clamp(minItems, 1, safeMax);
            var orderSize = Random.Range(safeMin, safeMax + 1);
            for (var i = 0; i < orderSize; i++)
            {
                var randomFood = activeFoodTypes[Random.Range(0, activeFoodTypes.Count)];   // duplicates allowed
                CreateOrderItemInstance(order, s, randomFood, i, orderSize);
            }
        }

        orders.Add(order);
        SetBubble(s, order.items.Count);
        PlaceBubbleOverCustomer(s, customer);
        if (orderTitleText != null) orderTitleText.text = "Order:";
        PatienceMeter.Show(order, MeterPosition(s));
        Debug.Log($"New order in slot {slot} with {order.items.Count} items, timer {order.timer:F1}s, tip x{tipMultiplier}");
    }

    /// Single-customer path kept for the original flow and old callers: slot 0 with the current level values.
    public void GenerateNewOrder()
    {
        StartOrderForCustomer(null, 0, minOrderItems, orderDisplayTime, 1f, patiencePetals);
    }

    private void CreateOrderItemInstance(ActiveOrder order, SlotVisuals s, string foodType, int index, int totalOrderSize)
    {
        // Find the matching food item
        var orderItem = System.Array.Find(availableFoods, item =>
            item != null && item.foodType == foodType);

        if (orderItem != null && orderItem.displayPrefab != null && s.container != null)
        {
            // FIXED: Pass the intended total order size instead of using incremental count
            var itemPosition = CalculateOrderItemPosition(index, totalOrderSize);

            // Create the display item
            var displayItem = Instantiate(orderItem.displayPrefab, s.container);
            displayItem.transform.localPosition = itemPosition;

            // Add served item visual component for pop effect
            var servedVisual = displayItem.GetComponent<ServedItemVisual>();
            if (servedVisual == null)
                servedVisual = displayItem.AddComponent<ServedItemVisual>();

            order.items.Add(new OrderItemInstance(foodType, displayItem));
            order.displayObjects.Add(displayItem);
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

    /// A tap on a food: serves the oldest open order that still needs it (the customer who came first).
    public bool TryServeItem(string foodType)
    {
        if (orders.Count == 0) return false;

        ActiveOrder target = null;
        foreach (var o in orders)
            if (o.active && o.Needs(foodType) && (target == null || o.startedAt < target.startedAt)) target = o;

        if (target == null)
        {
            Debug.Log($"No {foodType} needed in the open orders");
            return false;
        }

        var itemToServe = target.items.Find(item => item.foodType == foodType && !item.isServed);
        itemToServe.isServed = true;

        // Play served item visual effect and remove (tracked so the order's clean-up can stop it)
        Coroutine effectHandle = null;
        effectHandle = StartCoroutine(ServeItemWithEffect(itemToServe, () => target.serveEffects.Remove(effectHandle)));
        if (effectHandle != null) target.serveEffects.Add(effectHandle);

        // Award points for this item (kept on the order for its tip)
        if (scoreManager != null) target.basePoints += scoreManager.AwardItemPointsValue(foodType);

        // Patience: the flower that was draining fills up again (owner, 13 October: the hearts mechanic)
        RefillPatience(target);

        // Play item served sound
        PlayItemServedSound();

        if (target.AllServed) CompleteOrder(target);
        Debug.Log($"Served {foodType} to slot {target.slot}. Remaining items: {target.items.Count(i => !i.isServed)}");
        return true;
    }

    /// Gives the order back the time of the flower that was draining: its timer moves up to the next flower
    /// boundary (never above the full time), and the meter pops that flower back to full
    private void RefillPatience(ActiveOrder order)
    {
        if (!order.active || order.displayTime <= 0f) return;
        var per = order.displayTime / PatienceMeter.Flowers;
        var fullFlowers = Mathf.FloorToInt(order.timer / per + 1e-4f);
        order.timer = Mathf.Min(order.displayTime, (fullFlowers + 1) * per);
        PatienceMeter.OnItemServed(order);
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

    private void CompleteOrder(ActiveOrder order)
    {
        Debug.Log($"Order completed! (slot {order.slot})");
        PatienceMeter.Hide(order);
        order.active = false;

        // Award completion bonus: the tip from this order's time left, points and customer
        if (scoreManager != null)
            scoreManager.AwardOrderCompletionBonus(order.timer, order.basePoints, order.tipMultiplier);

        // Perfect = finished with at least perfectTimeFraction of the display time still remaining
        var isPerfect = order.timer >= order.displayTime * perfectTimeFraction;

        // Count as completed
        ordersCompleted++;
        UpdateOrderProgress();

        // CUSTOMER INTEGRATION: Notify customer manager (once, null-guarded)
        // (a customer that was opened for but is gone is not swapped for the other slot's customer)
        if (isUsingCustomerFlow && customerManager != null && (order.customer != null || !order.hadCustomer))
        {
            customerManager.HandleOrderServed(order.customer, isPerfect);
            DebugLog($"Notified CustomerManager - Order completed (perfect: {isPerfect}, time left: {order.timer:F2}s)");
        }

        SetBubble(slots[order.slot], 0);
        orders.Remove(order);
        AfterOrderClosed();
    }

    /// Customer flow: the day ends on the clock (LevelManager checks). Original flow: by the order count.
    private void AfterOrderClosed()
    {
        if (isUsingCustomerFlow && customerManager != null)
        {
            if (levelManager != null) levelManager.OnOrderClosed();
        }
        else if (ordersCompleted >= ordersPerLevel)
        {
            DebugLog("All orders processed for this level!");
            if (levelManager != null) levelManager.OnLevelComplete();
        }
        else Invoke("GenerateNewOrder", timeBetweenOrders);
    }

    private void ExpireOrder(ActiveOrder order)
    {
        Debug.Log($"Order expired! (slot {order.slot})");
        PatienceMeter.Hide(order);
        order.active = false;

        // Clear this order's display objects before telling anyone
        ClearOrderDisplay(order);
        SetBubble(slots[order.slot], 0);

        // Penalize for expired order: the points of what was served are still paid, no tip
        if (scoreManager != null) scoreManager.ApplyOrderExpiredPenalty(order.basePoints);

        // Count as completed (even if expired)
        ordersCompleted++;
        UpdateOrderProgress();

        // CUSTOMER INTEGRATION: Notify customer manager
        if (isUsingCustomerFlow && customerManager != null && (order.customer != null || !order.hadCustomer))
        {
            customerManager.HandleOrderExpired(order.customer);
            DebugLog("Notified CustomerManager of expired order");
        }

        orders.Remove(order);
        AfterOrderClosed();
    }

    private void ClearOrderDisplay(ActiveOrder order)
    {
        // Stop any in-flight serve effects first so they never touch a destroyed display object.
        // Snapshot + clear before stopping: stopping disposes the coroutine, whose finally block removes itself from the list.
        var effectsToStop = order.serveEffects.ToArray();
        order.serveEffects.Clear();
        foreach (var effect in effectsToStop)
            if (effect != null)
                StopCoroutine(effect);

        // Destroy all display objects
        foreach (var displayObj in order.displayObjects)
            if (displayObj != null)
                Destroy(displayObj);

        order.displayObjects.Clear();
        order.items.Clear();
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
    /// True while any customer has an open order
    public bool IsOrderActive()
    {
        return orders.Exists(o => o.active);
    }

    /// Open orders right now (LevelManager ends the day only when this is 0)
    public int OpenOrders => orders.Count;

    /// Whether a slot has an open order (CustomerManager spawns into free slots)
    public bool IsSlotBusy(int slot) => orders.Exists(o => o.active && o.slot == slot);

    /// Unserved food types across the open orders, oldest order first (used by the editor auto-play)
    public List<string> GetCurrentOrderTypes()
    {
        var types = new List<string>();
        foreach (var o in orders.OrderBy(o => o.startedAt))
            foreach (var item in o.items)
                if (!item.isServed)
                    types.Add(item.foodType);
        return types;
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
            yield return new WaitUntil(() => !IsOrderActive());

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

    /// Old entry point (one customer, slot 0, the level's values). CustomerManager now calls StartOrderForCustomer.
    public void StartOrderCycleForCustomer()
    {
        if (!isUsingCustomerFlow)
        {
            DebugLog("StartOrderCycleForCustomer called but not using customer flow!", true);
            return;
        }
        GenerateNewOrder();
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
