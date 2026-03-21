using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CustomerManager : MonoBehaviour
{
    [Header("Customer Management")] public CustomerController[] customerPrefabs;
    public Transform spawnPoint;

    [Header("References")] public OrderSystem orderSystem;
    public ScoreManager scoreManager;
    public LevelManager levelManager;

    [Header("Timing Settings")] [Tooltip("Delay between customer exit and spawning next customer")]
    public float nextCustomerSpawnDelay = 2.0f;

    [Header("Debug")] public bool enableDebugLogs = true;

    // Current state
    private CustomerController currentCustomer;
    private bool isProcessingCustomer = false;
    private int currentLevelIndex = 0;

    // CRITICAL: Prevent duplicate order generation
    private bool hasOrderBeenGenerated = false;

    // Events for integration
    public System.Action<CustomerController> OnCustomerSpawned;
    public System.Action<CustomerController> OnCustomerCompleted;

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
        if (currentCustomer != null)
        {
            Destroy(currentCustomer.gameObject);
            currentCustomer = null;
        }

        // Reset state
        isProcessingCustomer = false;
        hasOrderBeenGenerated = false;
    }

    public void SpawnCustomerForCurrentLevel()
    {
        if (isProcessingCustomer)
        {
            DebugLog("Already processing a customer, skipping spawn request");
            return;
        }

        var customerPrefab = SelectCustomerForLevel(currentLevelIndex);
        if (customerPrefab != null)
            SpawnCustomer(customerPrefab);
        else
            Debug.LogError($"No customer available for level {currentLevelIndex + 1}");
    }

    public void HandleOrderServed(bool perfect)
    {
        if (currentCustomer != null)
        {
            // CLEANED: In your system, this should always be true (orders always completed correctly)
            DebugLog($"Order served perfectly - notifying customer");
            currentCustomer.OnOrderServed(true); // Always true in your system
        }
        else
        {
            Debug.LogWarning("HandleOrderServed called but no current customer!");
        }
    }

    public void HandleOrderExpired()
    {
        if (currentCustomer != null)
        {
            DebugLog("Order expired - customer leaving disappointed");
            currentCustomer.OnOrderExpired();
        }
        else
        {
            Debug.LogWarning("HandleOrderExpired called but no current customer!");
        }
    }

    public void OnLevelLoaded(int levelIndex)
    {
        currentLevelIndex = levelIndex;

        if (currentCustomer != null)
        {
            Destroy(currentCustomer.gameObject);
            currentCustomer = null;
        }

        isProcessingCustomer = false;
        hasOrderBeenGenerated = false;
        DebugLog($"CustomerManager ready for level {levelIndex + 1}");
    }

    public void OnCustomerReachedService(CustomerController customer)
    {
        if (customer == currentCustomer)
        {
            DebugLog($"{customer.name} reached service point, starting order delay");

            // Apply this customer's min order size to OrderSystem
            if (orderSystem != null)
            {
                orderSystem.minOrderItems = customer.MinOrderItems;
                DebugLog($"Set minOrderItems to {customer.MinOrderItems} for {customer.name}");
            }

            // Apply this customer's tip multiplier to ScoreManager
            if (scoreManager != null)
            {
                scoreManager.SetCurrentTipMultiplier(customer.TipMultiplier);
                DebugLog($"Set tip multiplier to {customer.TipMultiplier}x for {customer.name}");
            }

            StartCoroutine(HandleCustomerOrderDelay(customer));
        }
    }

    public void OnCustomerExited(CustomerController customer)
    {
        if (customer == currentCustomer)
        {
            DebugLog($"{customer.name} has exited");
            currentCustomer = null;
            isProcessingCustomer = false;
            hasOrderBeenGenerated = false; // Reset for next customer

            OnCustomerCompleted?.Invoke(customer);
            DebugLog("Customer exited - checking if we should spawn next customer");

            // FIXED: Check if we need to spawn the next customer
            CheckForNextCustomer();
        }
        else
        {
            DebugLog($"Customer {customer.name} exited but was not current customer");
        }
    }

    // NEW: Check if we should spawn the next customer
    private void CheckForNextCustomer()
    {
        if (orderSystem == null)
        {
            Debug.LogError("OrderSystem reference missing!");
            return;
        }

        // Wait a bit, then spawn next customer
        DebugLog("Waiting before spawning next customer");
        StartCoroutine(DelayedNextCustomerSpawn());
    }

    // NEW: Delayed spawning of next customer
    private IEnumerator DelayedNextCustomerSpawn()
    {
        // Wait 2 seconds before spawning next customer
        yield return new WaitForSeconds(nextCustomerSpawnDelay);

        // Check if we should still spawn (no current customer)
        if (currentCustomer == null)
        {
            DebugLog("Spawning next customer after delay");
            SpawnCustomerForCurrentLevel();
        }
        else
        {
            DebugLog("Not spawning next customer - customer already exists");
        }
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

    private void SpawnCustomer(CustomerController customerPrefab)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point not set!");
            return;
        }

        if (orderSystem != null) orderSystem.minOrderItems = 1;
        if (scoreManager != null) scoreManager.SetCurrentTipMultiplier(1.0f);
        currentCustomer = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
        isProcessingCustomer = true;
        hasOrderBeenGenerated = false; // Reset for new customer

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCustomerWalkIn();

        DebugLog($"Spawned {currentCustomer.name} at {spawnPoint.position}");
        OnCustomerSpawned?.Invoke(currentCustomer);
    }

    private IEnumerator HandleCustomerOrderDelay(CustomerController customer)
    {
        // CRITICAL FIX: Check if order already generated to prevent duplicates
        if (hasOrderBeenGenerated)
        {
            DebugLog($"Order already generated for {customer.name} - skipping duplicate generation", true);
            yield break;
        }

        var delay = customer.OrderDelay;
        DebugLog($"Waiting {delay}s before generating order for {customer.name}");

        yield return new WaitForSeconds(delay);

        // CRITICAL FIX: Double-check before generating order
        if (hasOrderBeenGenerated)
        {
            DebugLog($"Order was generated while waiting for {customer.name} - aborting", true);
            yield break;
        }

        // Check if customer is still current and valid
        if (customer != currentCustomer || currentCustomer == null)
        {
            DebugLog($"Customer {customer.name} is no longer current customer - aborting order generation");
            yield break;
        }

        // Set flag to prevent duplicate generation
        hasOrderBeenGenerated = true;

        if (orderSystem != null)
        {
            DebugLog("Customer delay complete - requesting order generation");
            orderSystem.StartOrderCycleForCustomer();
            customer.OnOrderGenerated();
        }
        else
        {
            Debug.LogError("OrderSystem reference missing!");
            hasOrderBeenGenerated = false; // Reset flag on error
        }
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

    // Public getters for debugging
    public CustomerController GetCurrentCustomer()
    {
        return currentCustomer;
    }

    public bool IsProcessingCustomer()
    {
        return isProcessingCustomer && currentCustomer != null;
    }

    public bool HasCustomerAtService()
    {
        return currentCustomer != null && currentCustomer.HasReachedServicePoint();
    }

    public bool HasOrderBeenGenerated()
    {
        return hasOrderBeenGenerated;
    }

    [ContextMenu("Debug Customer Manager State")]
    public void DebugCustomerManagerState()
    {
        Debug.Log($"=== CUSTOMER MANAGER STATE ===");
        Debug.Log($"Current Customer: {(currentCustomer != null ? currentCustomer.name : "None")}");
        Debug.Log($"Is Processing: {isProcessingCustomer}");
        Debug.Log($"Order Generated: {hasOrderBeenGenerated}");
        Debug.Log($"Current Level: {currentLevelIndex + 1}");
        if (currentCustomer != null)
        {
            Debug.Log($"Customer at Service: {currentCustomer.HasReachedServicePoint()}");
            Debug.Log($"Customer Waiting: {currentCustomer.IsWaitingForOrder()}");
        }

        Debug.Log($"=============================");
    }
}