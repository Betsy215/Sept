using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CustomerManager : MonoBehaviour
{
    [Header("Customer Management")]
    public CustomerController[] customerPrefabs; // Toad, Frog, etc.
    public Transform spawnPoint; // Right side of screen
    public Transform servicePoint; // Where customer waits (center window)
    public Transform exitPoint; // Right side of screen
    
    [Header("Level-Based Customer Selection")]
    [Tooltip("Level index when each customer type unlocks (0-based)")]
    public int[] customerUnlockLevels = { 0, 2, 4 }; // Toad at level 1, others later
    
    [Header("Customer Waypoints")]
    [Tooltip("These will be assigned to spawned customers for movement")]
    public Transform[] movementWaypoints; // 0: spawn, 1: service, 2: exit
    
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
        // Validate setup
        ValidateSetup();
        
        // Get current level - try SessionManager first, then fallback
        if (SessionManager.Instance != null && SessionManager.Instance.HasActiveSession())
        {
            currentLevelIndex = SessionManager.Instance.GetCurrentLevelIndex();
        }
        else if (levelManager != null && levelManager.GetCurrentLevelData() != null)
        {
            // Fallback: try to get level number from current level data
            currentLevelIndex = levelManager.GetCurrentLevelData().levelNumber - 1; // Convert to 0-based index
        }
        else
        {
            // Final fallback: start at level 0
            currentLevelIndex = 0;
            DebugLog("No level information found, defaulting to level 1");
        }
        
        DebugLog($"CustomerManager initialized for level {currentLevelIndex + 1}");
    }
    
    void OnEnable()
    {
        // Subscribe to OrderSystem events
        if (orderSystem != null)
        {
            // We'll handle these through public methods called by OrderSystem
            DebugLog("CustomerManager enabled and ready for integration");
        }
    }
    
    void OnDisable()
    {
        // Clean up any active customers
        if (currentCustomer != null)
        {
            Destroy(currentCustomer.gameObject);
            currentCustomer = null;
        }
    }
    
    #region Public API - Called by OrderSystem and LevelManager
    
    /// <summary>
    /// Called by OrderSystem to spawn a customer for the current order cycle
    /// </summary>
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
    
    /// <summary>
    /// Called by OrderSystem when an order is served correctly or incorrectly
    /// </summary>
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
    
    /// <summary>
    /// Called by OrderSystem when an order expires
    /// </summary>
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
    
    /// <summary>
    /// Called by LevelManager when level changes
    /// </summary>
    public void SetCurrentLevel(int levelIndex)
    {
        currentLevelIndex = levelIndex;
        DebugLog($"Level changed to {levelIndex + 1}");
    }
    
    /// <summary>
    /// Called by LevelManager when a new level loads
    /// </summary>
    public void OnLevelLoaded(int levelIndex)
    {
        SetCurrentLevel(levelIndex);
        
        // Clean up any existing customer
        if (currentCustomer != null)
        {
            Destroy(currentCustomer.gameObject);
            currentCustomer = null;
        }
        
        isProcessingCustomer = false;
        DebugLog($"CustomerManager ready for level {levelIndex + 1}");
    }
    
    #endregion
    
    #region Customer Lifecycle Management
    
    private CustomerController SelectCustomerForLevel(int levelIndex)
    {
        List<CustomerController> availableCustomers = new List<CustomerController>();
        
        // Check which customers are unlocked for this level
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
        
        // For now, select randomly from available customers
        // Later this could be weighted based on level progression, customer rarity, etc.
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
        
        // Instantiate customer at spawn point
        currentCustomer = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // Assign waypoints to the customer
        AssignWaypointsToCustomer(currentCustomer);
        
        isProcessingCustomer = true;
        
        DebugLog($"Spawned {currentCustomer.name} at {spawnPoint.position}");
        
        // Notify systems
        OnCustomerSpawned?.Invoke(currentCustomer);
    }
    
    private void AssignWaypointsToCustomer(CustomerController customer)
    {
        if (customer != null && movementWaypoints != null && movementWaypoints.Length >= 3)
        {
            customer.movementWaypoints = movementWaypoints;
            DebugLog($"Assigned waypoints to {customer.name}");
        }
        else
        {
            Debug.LogError("Cannot assign waypoints - missing customer or insufficient waypoints");
        }
    }
    
    #endregion
    
    #region Customer Event Handlers - Called by CustomerController
    
    /// <summary>
    /// Called by CustomerController when they reach the service point
    /// </summary>
    public void OnCustomerReachedService(CustomerController customer)
    {
        if (customer == currentCustomer)
        {
            DebugLog($"{customer.name} reached service point, starting order delay");
            StartCoroutine(HandleCustomerOrderDelay(customer));
        }
    }
    
    /// <summary>
    /// Called by CustomerController when they exit the scene
    /// </summary>
    public void OnCustomerExited(CustomerController customer)
    {
        if (customer == currentCustomer)
        {
            DebugLog($"{customer.name} has exited");
            currentCustomer = null;
            isProcessingCustomer = false;
            
            // Notify systems
            OnCustomerCompleted?.Invoke(customer);
            
            // Ready for next customer
            DebugLog("Ready for next customer");
        }
    }
    
    private IEnumerator HandleCustomerOrderDelay(CustomerController customer)
    {
        float delay = customer.OrderDelay;
        DebugLog($"Waiting {delay}s before generating order for {customer.name}");
        
        yield return new WaitForSeconds(delay);
        
        // Tell OrderSystem to generate the order
        if (orderSystem != null)
        {
            DebugLog("Customer delay complete - requesting order generation");
            orderSystem.StartOrderCycle(); // This should generate and display the order
            
            // Notify customer that order appeared
            customer.OnOrderGenerated();
        }
        else
        {
            Debug.LogError("OrderSystem reference missing!");
        }
    }
    
    #endregion
    
    #region Utility and Debug
    
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
        
        if (servicePoint == null)
        {
            Debug.LogError("Service point not assigned!");
            isValid = false;
        }
        
        if (exitPoint == null)
        {
            Debug.LogError("Exit point not assigned!");
            isValid = false;
        }
        
        if (movementWaypoints == null || movementWaypoints.Length < 3)
        {
            Debug.LogError("Need at least 3 movement waypoints (spawn, service, exit)!");
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
    
    #endregion
    
    #region Public Getters
    
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
    
    #endregion
}