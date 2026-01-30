using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RefillSystem : MonoBehaviour
{
    [Header("Refill Settings")] [Tooltip("Default maximum count for refillable items")]
    public int defaultMaxCount = 5;

    [Tooltip("Time in seconds to refill one count")]
    public float refillTimePerCount = 1f;

    [Header("Visual Settings")] [Tooltip("Color when item is out of stock")]
    public Color outOfStockColor = new(1f, 1f, 1f, 0.5f);

    [Header("References")] [Tooltip("Reference to GamePhaseManager")]
    public GamePhaseManager gamePhaseManager;

    [Header("Debug")] public bool enableDebugLogs = true;

    public LevelManager levelManager;

    // Private variables
    private List<RefillableItem> refillableItems = new();
    private bool isGameplayMode = false;

    private void Start()
    {
        InitializeSystem();

        // Also scan again after a delay to catch any items that weren't ready yet
        Invoke("RescanForItems", 0.5f);
    }

    private void RescanForItems()
    {
        var items = FindObjectsOfType<RefillableItem>();
        foreach (var item in items)
            RegisterRefillableItem(item);

        DebugLog($"Rescan complete: {refillableItems.Count} total refillable items");
    }

    private void InitializeSystem()
    {
        // Find GamePhaseManager if not assigned
        if (gamePhaseManager == null)
            gamePhaseManager = FindObjectOfType<GamePhaseManager>();

        if (levelManager == null)
            levelManager = FindObjectOfType<LevelManager>();

        // Find all refillable items in scene
        var items = FindObjectsOfType<RefillableItem>();
        foreach (var item in items)
            RegisterRefillableItem(item);

        DebugLog($"RefillSystem initialized with {refillableItems.Count} refillable items");
    }

    public void RegisterRefillableItem(RefillableItem item)
    {
        if (item == null) return;

        if (!refillableItems.Contains(item))
        {
            refillableItems.Add(item);
            item.Initialize(this);
            DebugLog($"Registered refillable item: {item.name}");
        }
    }

    public void UnregisterRefillableItem(RefillableItem item)
    {
        if (item != null && refillableItems.Contains(item))
        {
            refillableItems.Remove(item);
            DebugLog($"Unregistered refillable item: {item.name}");
        }
    }

    public void OnGamePhaseChanged(GamePhase newPhase)
    {
        isGameplayMode = newPhase == GamePhase.PLAYING;

        foreach (var item in refillableItems)
            if (item != null)
                item.SetGameplayMode(isGameplayMode);

        DebugLog($"Game phase changed to {newPhase}. Refill system {(isGameplayMode ? "enabled" : "disabled")}");
    }

    public void OnItemServed(string foodType, bool wasCorrect)
    {
        if (!isGameplayMode) return;

        foreach (var item in refillableItems)
            if (item != null && item.GetFoodType() == foodType)
            {
                item.OnItemServed(wasCorrect);
                break;
            }
    }

    #region Settings Getters

    public float GetRefillTimePerCount()
    {
        return refillTimePerCount;
    }

    public int GetDefaultMaxCount()
    {
        return defaultMaxCount;
    }

    #endregion

    #region Debug Methods

    [ContextMenu("Register All RefillableItems")]
    public void RegisterAllRefillableItems()
    {
        var items = FindObjectsOfType<RefillableItem>();

        DebugLog($"Found {items.Length} RefillableItems in scene");

        foreach (var item in items)
            RegisterRefillableItem(item);

        DebugLog($"Registration complete: {refillableItems.Count} items registered");
    }

    [ContextMenu("Show All Refillable Items")]
    public void ShowAllRefillableItems()
    {
        DebugLog($"=== REGISTERED REFILLABLE ITEMS ({refillableItems.Count}) ===");

        for (var i = 0; i < refillableItems.Count; i++)
        {
            var item = refillableItems[i];
            if (item != null)
                DebugLog(
                    $"{i + 1}. {item.name} - Type: {item.GetFoodType()}, Count: {item.GetCurrentCount()}/{item.GetMaxCount()}, Refill Enabled: {item.enableRefill}");
            else
                DebugLog($"{i + 1}. NULL ITEM (should be cleaned up)");
        }
    }

    [ContextMenu("Force Initialize All Items")]
    public void ForceInitializeAllItems()
    {
        DebugLog("Force initializing all registered items...");

        foreach (var item in refillableItems)
            if (item != null)
            {
                item.Initialize(this);
                DebugLog($"Force initialized: {item.name}");
            }

        DebugLog("Force initialization complete");
    }

    #endregion

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"RefillSystem: {message}");
    }

    // Method to be called by GamePhaseManager when phase changes
    public void NotifyPhaseChange(GamePhase newPhase)
    {
        OnGamePhaseChanged(newPhase);
    }

    private void OnValidate()
    {
        // Validate settings in editor
        if (defaultMaxCount <= 0)
            defaultMaxCount = 5;

        if (refillTimePerCount <= 0)
            refillTimePerCount = 1f;
    }
}