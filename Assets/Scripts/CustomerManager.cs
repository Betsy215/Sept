using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CustomerManager : MonoBehaviour
{
    [Header("Customer Management")]
    public CustomerController[] customerPrefabs;
    public Transform spawnPoint;
    
    [Header("Level-Based Customer Selection")]
    [Tooltip("Level index when each customer type unlocks (0-based)")]
    public int[] customerUnlockLevels = { 0, 2, 4 };
    
    [Header("References")]
    public OrderSystem orderSystem;
    public LevelManager levelManager;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    // Current state
    private CustomerController currentCustomer;
    private bool isProcessingCustomer = false;
    private int currentLevelIndex = 0;
    
    // CRITICAL: Prevent duplicate order generation
    private bool hasOrderBeenGenerated = false;
    
    // Events for integration
    public System.Action<CustomerController> OnCustomerSpawned;
    public System.Action<CustomerController> OnCustomerCompleted;
    
    void Start()
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
    
    void OnDisable()
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
        
        CustomerController customerPrefab = SelectCustomerForLevel(currentLevelIndex);
        if (customerPrefab != null)
        {
            SpawnCustomer(customerPrefab);
        }
        else
        {
            Debug.LogError($"No customer available for level {currentLevelIndex + 1}");
        }
    }
    
    public void HandleOrderServed(bool perfect)
    {
        if (currentCustomer != null)
        {
            DebugLog($"Order served - Perfect: {perfect}");
            currentCustomer.OnOrderServed(perfect);
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
            StartCoroutine(HandleCustomerOrderDelay(customer));
        }
        else
        {
            DebugLog($"Customer {customer.name} reached service but is not current customer - ignoring");
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
            DebugLog("Ready for next customer");
        }
        else
        {
            DebugLog($"Customer {customer.name} exited but was not current customer");
        }
    }
    
    private CustomerController SelectCustomerForLevel(int levelIndex)
    {
        List<CustomerController> availableCustomers = new List<CustomerController>();
        
        for (int i = 0; i < customerPrefabs.Length && i < customerUnlockLevels.Length; i++)
        {
            if (levelIndex >= customerUnlockLevels[i])
            {
                availableCustomers.Add(customerPrefabs[i]);
            }
        }
        
        if (availableCustomers.Count == 0)
        {
            Debug.LogWarning($"No customers unlocked for level {levelIndex + 1}");
            return null;
        }
        
        int randomIndex = Random.Range(0, availableCustomers.Count);
        CustomerController selected = availableCustomers[randomIndex];
        
        DebugLog($"Selected {selected.name} for level {levelIndex + 1} (from {availableCustomers.Count} available)");
        return selected;
    }
    
    private void SpawnCustomer(CustomerController customerPrefab)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point not set!");
            return;
        }
        
        currentCustomer = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
        isProcessingCustomer = true;
        hasOrderBeenGenerated = false; // Reset for new customer
        
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
        
        float delay = customer.OrderDelay;
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
        bool isValid = true;
        
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
        {
            Debug.LogError("CustomerManager setup is incomplete!");
        }
        else
        {
            DebugLog("CustomerManager setup validated successfully");
        }
    }
    
    private void DebugLog(string message, bool isWarning = false)
    {
        if (enableDebugLogs)
        {
            string formattedMessage = $"[CustomerManager] {message}";
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
    
    // FIXED: Additional state validation
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
    
    // NEW: Check if order has been generated for current customer
    public bool HasOrderBeenGenerated()
    {
        return hasOrderBeenGenerated;
    }
    
    // NEW: Debug method to check state
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