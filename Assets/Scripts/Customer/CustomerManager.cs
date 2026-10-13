using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// Brings customers to the counter. Since 12 October 2026 two can wait at once (slots 0 and 1): they still arrive
/// one by one, but the next one sets off a little after the previous reached the counter, so the second arrives
/// while the first is being served. Each customer stands under a spot parent; the walk-in clip animates the
/// customer's local position from (-3,1) to (1,1), so the spot's x offset decides where each slot stands.
/// With OrderSystem.maxConcurrentOrders = 1 (the tutorial) there is one slot at the scene's place, as before.
public class CustomerManager : MonoBehaviour
{
    [Header("Customer Management")] public CustomerController[] customerPrefabs;
    public Transform spawnPoint;

    [Header("References")] public OrderSystem orderSystem;
    public ScoreManager scoreManager;
    public LevelManager levelManager;

    [Header("Timing Settings")] [Tooltip("Delay between customer exit and spawning next customer")]
    public float nextCustomerSpawnDelay = 2.0f;
    [Tooltip("Not used since 13 October: customers set off on the level's beat (LevelData.timeBetweenOrders) while the day clock runs")]
    public float secondCustomerDelay = 1.0f;

    [Header("Two customers (12 October 2026)")]
    [Tooltip("X offset of each slot's spot when two customers can wait (slot 0 right, slot 1 left); the walk-in ends at x = offset + 1")]
    public float[] twoSlotCustomerX = { 0.2f, -1.5f };   // customers at x 1.2 and -0.5, 1.7 apart (owner 13 October: closer together)
    [Tooltip("The spots sit this much lower when two can wait: the customers stand behind the table, knees hidden by the cloth, so the bubbles fit under the corner buttons")]
    public float twoSlotCustomerY = -0.3f;
    [Tooltip("Customers are drawn this much smaller when two can wait")]
    public float twoSlotCustomerScale = 0.9f;

    [Header("Debug")] public bool enableDebugLogs = true;

    // Current state
    private readonly CustomerController[] slots = new CustomerController[2];
    private Transform[] spots;
    private int spawnedCount;                 // customers started this day (never more than ordersPerLevel)
    private int currentLevelIndex = 0;

    // Baseline minimum order size from LevelData (applied to OrderSystem by LevelManager before OnLevelLoaded).
    // Customer-specific minimums raise this per customer. -1 = not captured yet.
    private int levelMinOrderItems = -1;
    private float levelOrderDisplayTime = -1f; // LevelData value, before any customer multiplier

    // Tracked coroutines so disabling the manager can stop future spawns/orders
    // (disabling a component does NOT stop its running coroutines)
    private Coroutine nextCustomerSpawnCoroutine;
    private Coroutine spawnLoop;
    private readonly List<Coroutine> orderDelayCoroutines = new List<Coroutine>();

    // Events for integration
    public System.Action<CustomerController> OnCustomerSpawned;
    public System.Action<CustomerController> OnCustomerCompleted;

    private int MaxSlots => orderSystem != null ? Mathf.Clamp(orderSystem.maxConcurrentOrders, 1, slots.Length) : 1;

    private void Start()
    {
        ValidateSetup();

        // Get current level
        if (SessionManager.Instance != null && SessionManager.Instance.HasActiveSession())
        {
            currentLevelIndex = SessionManager.Instance.GetCurrentLevelIndex();
        }
        else if (levelManager != null && levelManager.GetCurrentLevelData() != null)
        {
            currentLevelIndex = levelManager.GetCurrentLevelData().levelNumber - 1;
        }
        else
        {
            currentLevelIndex = 0;
            DebugLog("No level information found, defaulting to level 1");
        }

        DebugLog($"CustomerManager initialized for level {currentLevelIndex + 1}");
    }

    private void OnDisable()
    {
        // Stop future spawns / pending order generation. Customers in flight are intentionally NOT destroyed here:
        // LevelManager disables this component synchronously inside the final CompleteOrder, and the customer must
        // still finish its reaction and walk-out (it destroys itself via OnReachedExit -> OnCustomerExited).
        StopSpawnCoroutines();
    }

    private void OnDestroy()
    {
        // Scene reload / teardown: make sure nothing is left behind
        StopSpawnCoroutines();
        for (var i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null) Destroy(slots[i].gameObject);
            slots[i] = null;
        }
    }

    private void StopSpawnCoroutines()
    {
        if (nextCustomerSpawnCoroutine != null)
        {
            StopCoroutine(nextCustomerSpawnCoroutine);
            nextCustomerSpawnCoroutine = null;
        }
        if (spawnLoop != null)
        {
            StopCoroutine(spawnLoop);
            spawnLoop = null;
        }
        foreach (var c in orderDelayCoroutines) if (c != null) StopCoroutine(c);
        orderDelayCoroutines.Clear();
    }

    // Clamp a requested minimum order size so Random.Range in OrderSystem never gets min > max
    private int ClampMinOrderItems(int requestedMin)
    {
        var max = orderSystem != null ? Mathf.Max(1, orderSystem.maxOrderItems) : int.MaxValue;
        return Mathf.Clamp(requestedMin, 1, max);
    }

    private int FreeSlot()
    {
        for (var i = 0; i < MaxSlots; i++) if (slots[i] == null) return i;
        return -1;
    }

    private bool AnyWalkingIn()
    {
        foreach (var c in slots) if (c != null && !c.HasReachedServicePoint()) return true;
        return false;
    }

    private Transform Spot(int slot)
    {
        if (spots == null)
        {
            var root = new GameObject("CustomerSpots").transform;
            spots = new Transform[slots.Length];
            for (var i = 0; i < spots.Length; i++)
            {
                spots[i] = new GameObject("Spot" + i).transform;
                spots[i].SetParent(root, false);
            }
        }
        var two = MaxSlots > 1;
        var x = two && slot < twoSlotCustomerX.Length ? twoSlotCustomerX[slot] : 0f;
        spots[slot].position = new Vector3(x, two ? twoSlotCustomerY : 0f, 0f);
        return spots[slot];
    }

    /// Starts the day's arrivals: one customer now, then one every timeBetweenOrders seconds while the day clock
    /// runs, each waiting for a free slot (two at most) and for the previous one to have reached the counter.
    public void SpawnCustomerForCurrentLevel()
    {
        if (!enabled) return;
        if (spawnLoop == null) spawnLoop = StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (enabled)
        {
            yield return new WaitUntil(() => !enabled || (levelManager != null && levelManager.DayOver) || (FreeSlot() >= 0 && !AnyWalkingIn()));
            if (!enabled || (levelManager != null && levelManager.DayOver)) break;
            SpawnOne();
            var interval = orderSystem != null ? Mathf.Max(0.5f, orderSystem.timeBetweenOrders) : 5f;
            yield return new WaitForSeconds(interval);
        }
        spawnLoop = null;
        DebugLog("No more customers today");
    }

    private void SpawnOne()
    {
        var slot = FreeSlot();
        if (slot < 0) return;
        var customerPrefab = SelectCustomerForLevel(currentLevelIndex);
        if (customerPrefab != null)
            SpawnCustomer(customerPrefab, slot);
        else
            Debug.LogError($"No customer available for level {currentLevelIndex + 1}");
    }

    /// Customers on screen (walking in, waiting or walking out); the day ends only when this is 0
    public int ActiveCustomers
    {
        get { var n = 0; foreach (var c in slots) if (c != null) n++; return n; }
    }

    public void HandleOrderServed(CustomerController customer, bool perfect)
    {
        if (customer == null) customer = FirstCustomer();
        if (customer != null)
        {
            DebugLog($"Order served - notifying {customer.name}");
            customer.OnOrderServed(perfect);
        }
        else Debug.LogWarning("HandleOrderServed called but no customer!");
    }

    public void HandleOrderExpired(CustomerController customer)
    {
        if (customer == null) customer = FirstCustomer();
        if (customer != null)
        {
            DebugLog($"Order expired - {customer.name} leaving disappointed");
            customer.OnOrderExpired();
        }
        else Debug.LogWarning("HandleOrderExpired called but no customer!");
    }

    // Old single-customer entry points
    public void HandleOrderServed(bool perfect) => HandleOrderServed(null, perfect);
    public void HandleOrderExpired() => HandleOrderExpired(null);

    private CustomerController FirstCustomer()
    {
        foreach (var c in slots) if (c != null) return c;
        return null;
    }

    public void OnLevelLoaded(int levelIndex)
    {
        currentLevelIndex = levelIndex;

        StopSpawnCoroutines();
        for (var i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null) Destroy(slots[i].gameObject);
            slots[i] = null;
        }
        spawnedCount = 0;

        // LevelManager applies LevelData.minOrderItems to OrderSystem before calling this - capture it as the baseline
        if (orderSystem != null)
        {
            levelMinOrderItems = orderSystem.minOrderItems;
            levelOrderDisplayTime = orderSystem.orderDisplayTime;
            DebugLog($"Captured level baseline minOrderItems = {levelMinOrderItems}, orderDisplayTime = {levelOrderDisplayTime}");
        }

        DebugLog($"CustomerManager ready for level {levelIndex + 1}");
    }

    public void OnCustomerReachedService(CustomerController customer)
    {
        if (customer == null || System.Array.IndexOf(slots, customer) < 0) return;
        DebugLog($"{customer.name} reached service point (slot {customer.Slot}), starting order delay");

        if (levelMinOrderItems < 0 && orderSystem != null) levelMinOrderItems = orderSystem.minOrderItems;
        if (levelOrderDisplayTime < 0f && orderSystem != null) levelOrderDisplayTime = orderSystem.orderDisplayTime;

        // This customer's order: never below the level's own minimum, clamped to the level's maximum
        var minItems = ClampMinOrderItems(Mathf.Max(levelMinOrderItems + customer.ExtraOrderItems, customer.MinOrderItems));
        // Patience: the level's time; the meter shows it falling at the customer's own pace (patient or hurried)
        var patience = levelOrderDisplayTime;
        var displayTime = patience * customer.OrderTimeMultiplier;
        var petals = Mathf.Max(1, Mathf.RoundToInt(patience));
        DebugLog($"{customer.name}: min items {minItems}, time {displayTime:F1}s, tip x{customer.TipMultiplier}");

        orderDelayCoroutines.Add(StartCoroutine(HandleCustomerOrderDelay(customer, minItems, displayTime, customer.TipMultiplier, petals)));
    }

    public void OnCustomerExited(CustomerController customer)
    {
        var slot = System.Array.IndexOf(slots, customer);
        if (slot < 0)
        {
            DebugLog($"Customer {customer.name} exited but was not in a slot");
            return;
        }
        DebugLog($"{customer.name} has exited (slot {slot})");
        slots[slot] = null;
        OnCustomerCompleted?.Invoke(customer);
        if (levelManager != null) levelManager.OnCustomerLeft();   // the day may end now
    }

    private CustomerController SelectCustomerForLevel(int levelIndex)
    {
        // Check if SessionManager is available
        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession())
        {
            // Fallback: return first customer if no session
            Debug.LogWarning("No active session found, using first customer as fallback");
            return customerPrefabs.Length > 0 ? customerPrefabs[0] : null;
        }

        // Get purchased characters from session
        var purchasedCharacters = SessionManager.Instance.GetCurrentSession().purchasedCharacters;
        var availableCustomers = new List<CustomerController>();

        // Check each customer prefab against purchased characters
        for (var i = 0; i < customerPrefabs.Length; i++)
            if (customerPrefabs[i] != null)
            {
                // Extract character name from prefab name (e.g., "CustomerGirl" -> "Girl")
                var customerName = customerPrefabs[i].name.Replace("Customer", "").Replace("Prefab", "").Trim();

                if (purchasedCharacters.Contains(customerName)) availableCustomers.Add(customerPrefabs[i]);
            }

        if (availableCustomers.Count == 0)
        {
            Debug.LogWarning(
                $"No purchased customers available. Purchased: [{string.Join(", ", purchasedCharacters)}]");
            return null;
        }

        var randomIndex = Random.Range(0, availableCustomers.Count);
        var selected = availableCustomers[randomIndex];

        DebugLog(
            $"Selected {selected.name} from {availableCustomers.Count} purchased customers: [{string.Join(", ", purchasedCharacters)}]");
        return selected;
    }

    private void SpawnCustomer(CustomerController customerPrefab, int slot)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point not set!");
            return;
        }

        if (scoreManager != null) scoreManager.SetCurrentTipMultiplier(1.0f);
        var spot = Spot(slot);
        var customer = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation, spot);
        customer.Slot = slot;
        if (MaxSlots > 1)
        {
            customer.transform.localScale *= twoSlotCustomerScale;
            // The walk-in clip starts at local x -3; with the right slot's offset that is on screen. The spot starts
            // further left and slides to its place during the walk, so every customer enters from off screen
            var home = spot.position;
            spot.position = home + Vector3.left * 2.5f;
            StartCoroutine(SlideSpot(spot, home, 3f));
            // Draw order between the two: the right slot in front of the left, so an overlap never flickers
            foreach (var sr in customer.GetComponentsInChildren<SpriteRenderer>(true)) sr.sortingOrder += slot == 0 ? 2 : 0;
        }
        slots[slot] = customer;
        spawnedCount++;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCustomerWalkIn();

        DebugLog($"Spawned {customer.name} in slot {slot} (number {spawnedCount} today)");
        OnCustomerSpawned?.Invoke(customer);
    }

    private IEnumerator SlideSpot(Transform spot, Vector3 home, float seconds)
    {
        var from = spot.position; var t = 0f;
        while (t < seconds && spot != null)
        {
            t += Time.deltaTime;
            spot.position = Vector3.Lerp(from, home, Mathf.SmoothStep(0f, 1f, t / seconds));
            yield return null;
        }
        if (spot != null) spot.position = home;
    }

    private IEnumerator HandleCustomerOrderDelay(CustomerController customer, int minItems, float displayTime, float tipMultiplier, int petals)
    {
        var delay = customer.OrderDelay;
        DebugLog($"Waiting {delay}s before generating order for {customer.name}");
        yield return new WaitForSeconds(delay);

        // Still here (not destroyed by a level change) and still in its slot
        if (customer == null || System.Array.IndexOf(slots, customer) < 0)
        {
            DebugLog("Customer gone before its order - aborting order generation");
            yield break;
        }

        if (orderSystem != null)
        {
            DebugLog("Customer delay complete - opening the order");
            orderSystem.StartOrderForCustomer(customer, customer.Slot, minItems, displayTime, tipMultiplier, petals);
            customer.OnOrderGenerated();
        }
        else Debug.LogError("OrderSystem reference missing!");
    }

    private void ValidateSetup()
    {
        var isValid = true;

        if (customerPrefabs == null || customerPrefabs.Length == 0)
        {
            Debug.LogError("No customer prefabs assigned!");
            isValid = false;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point not assigned!");
            isValid = false;
        }

        if (orderSystem == null)
        {
            Debug.LogError("OrderSystem reference missing!");
            isValid = false;
        }

        if (!isValid)
            Debug.LogError("CustomerManager setup is incomplete!");
        else
            DebugLog("CustomerManager setup validated successfully");
    }

    private void DebugLog(string message, bool isWarning = false)
    {
        if (enableDebugLogs)
        {
            var formattedMessage = $"[CustomerManager] {message}";
            if (isWarning)
                Debug.LogWarning(formattedMessage);
            else
                Debug.Log(formattedMessage);
        }
    }

    // Public getters for debugging and the tutorial
    public CustomerController GetCurrentCustomer() => FirstCustomer();

    public bool IsProcessingCustomer() => FirstCustomer() != null;

    /// True when any customer stands at the counter
    public bool HasCustomerAtService()
    {
        foreach (var c in slots) if (c != null && c.HasReachedServicePoint()) return true;
        return false;
    }

    public bool HasOrderBeenGenerated() => orderSystem != null && orderSystem.IsOrderActive();

    [ContextMenu("Debug Customer Manager State")]
    public void DebugCustomerManagerState()
    {
        Debug.Log($"=== CUSTOMER MANAGER STATE ===");
        for (var i = 0; i < slots.Length; i++)
            Debug.Log($"Slot {i}: {(slots[i] != null ? slots[i].name : "None")}{(slots[i] != null && slots[i].HasReachedServicePoint() ? " at service" : "")}");
        Debug.Log($"Spawned: {spawnedCount}, Current Level: {currentLevelIndex + 1}");
        Debug.Log($"=============================");
    }
}
