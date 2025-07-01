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
        DebugLog($"CustomerManager ready for level {levelIndex + 1}");
    }
    
    public void OnCustomerReachedService(CustomerController customer)
    {
        if (customer == currentCustomer)
        {
            DebugLog($"{customer.name} reached service point, starting order delay");
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
            
            OnCustomerCompleted?.Invoke(customer);
            DebugLog("Ready for next customer");
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
        
        DebugLog($"Spawned {currentCustomer.name} at {spawnPoint.position}");
        OnCustomerSpawned?.Invoke(currentCustomer);
    }
    
    private IEnumerator HandleCustomerOrderDelay(CustomerController customer)
    {
        float delay = customer.OrderDelay;
        DebugLog($"Waiting {delay}s before generating order for {customer.name}");
        
        yield return new WaitForSeconds(delay);
        
        if (orderSystem != null)
        {
            DebugLog("Customer delay complete - requesting order generation");
            orderSystem.StartOrderCycleForCustomer();
            customer.OnOrderGenerated();
        }
        else
        {
            Debug.LogError("OrderSystem reference missing!");
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
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[CustomerManager] {message}");
        }
    }
    
    public CustomerController GetCurrentCustomer()
    {
        return currentCustomer;
    }
    
    public bool IsProcessingCustomer()
    {
        return isProcessingCustomer;
    }
    
    public bool HasCustomerAtService()
    {
        return currentCustomer != null && currentCustomer.HasReachedServicePoint();
    }
}