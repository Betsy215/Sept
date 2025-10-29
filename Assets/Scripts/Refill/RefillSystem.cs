using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RefillSystem : MonoBehaviour
{
    [Header("Refill Settings")] [Tooltip("Default maximum count for refillable items")]
    public int defaultMaxCount = 5;

    [Tooltip("Time in seconds to refill one count")]
    public float refillTimePerCount = 1f;

    [Header("UI Prefabs")] [Tooltip("Prefab for count display UI above items")]
    public GameObject countUIPrefab;

    [Tooltip("Prefab for refill status bar under items")]
    public GameObject statusBarPrefab;

    [Header("Visual Settings")] [Tooltip("Color when item is out of stock")]
    public Color outOfStockColor = new(1f, 1f, 1f, 0.5f);

    [Tooltip("Vertical offset for count UI above items")]
    public float countUIOffset = 0.8f;

    [Tooltip("Vertical offset for status bar below items")]
    public float statusBarOffset = -0.6f;

    [Header("References")] [Tooltip("Canvas for UI elements")]
    public Canvas uiCanvas;

    [Tooltip("Reference to GamePhaseManager")]
    public GamePhaseManager gamePhaseManager;

    [Header("Debug")] public bool enableDebugLogs = true;

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
        foreach (var item in items) RegisterRefillableItem(item);

        DebugLog($"Rescan complete: {refillableItems.Count} total refillable items");
    }

    private void InitializeSystem()
    {
        // Find GamePhaseManager if not assigned
        if (gamePhaseManager == null) gamePhaseManager = FindObjectOfType<GamePhaseManager>();

        // Find UI Canvas if not assigned
        if (uiCanvas == null) uiCanvas = FindObjectOfType<Canvas>();

        // Find all refillable items in scene
        var items = FindObjectsOfType<RefillableItem>();
        foreach (var item in items) RegisterRefillableItem(item);

        DebugLog($"RefillSystem initialized with {refillableItems.Count} refillable items");
    }

    public void RegisterRefillableItem(RefillableItem item)
    {
        if (!refillableItems.Contains(item))
        {
            refillableItems.Add(item);
            item.Initialize(this);
            DebugLog($"Registered refillable item: {item.name}");
        }
    }

    public void UnregisterRefillableItem(RefillableItem item)
    {
        if (refillableItems.Contains(item))
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

    public GameObject CreateCountUI(Transform parent)
    {
        DebugLog($"CreateCountUI called");
        DebugLog($"countUIPrefab assigned: {countUIPrefab != null}");
        DebugLog($"uiCanvas assigned: {uiCanvas != null}");

        if (countUIPrefab == null)
        {
            DebugLog("ERROR: countUIPrefab is null! Please assign the CountUIPrefab in RefillSystem Inspector");
            return null;
        }

        if (uiCanvas == null)
        {
            DebugLog("ERROR: uiCanvas is null! Please assign your Canvas in RefillSystem Inspector");
            return null;
        }

        DebugLog($"About to instantiate countUIPrefab: {countUIPrefab.name}");
        var countUI = Instantiate(countUIPrefab, uiCanvas.transform);

        // IMPORTANT: Make sure the count UI GameObject is active
        countUI.SetActive(true);
        DebugLog($"Count UI GameObject set to active: {countUI.activeInHierarchy}");

        // Name the UI object to identify which food item it belongs to
        if (parent != null)
        {
            countUI.name = $"CountUI_{parent.name}";
            DebugLog($"Named count UI: {countUI.name}");
        }

        DebugLog($"Count UI instantiated: {countUI != null}");

        if (countUI != null)
        {
            DebugLog($"Created count UI GameObject: {countUI.name}");

            // Check if it has the RefillCountUI component
            var countUIComponent = countUI.GetComponent<RefillCountUI>();
            DebugLog($"RefillCountUI component found: {countUIComponent != null}");
        }

        return countUI;
    }

    public GameObject CreateStatusBar(Transform parent)
    {
        DebugLog($"CreateStatusBar called");
        DebugLog($"statusBarPrefab assigned: {statusBarPrefab != null}");
        DebugLog($"uiCanvas assigned: {uiCanvas != null}");

        if (statusBarPrefab == null)
        {
            DebugLog("ERROR: statusBarPrefab is null! Please assign the StatusBarPrefab in RefillSystem Inspector");
            return null;
        }

        if (uiCanvas == null)
        {
            DebugLog("ERROR: uiCanvas is null! Please assign your Canvas in RefillSystem Inspector");
            return null;
        }

        var statusBar = Instantiate(statusBarPrefab, uiCanvas.transform);

        // Name the UI object to identify which food item it belongs to
        if (parent != null)
        {
            statusBar.name = $"StatusBar_{parent.name}";
            DebugLog($"Named status bar: {statusBar.name}");
        }

        DebugLog($"Status bar instantiated: {statusBar != null}");

        return statusBar;
    }

    public float GetCountUIOffset()
    {
        return countUIOffset;
    }

    public float GetStatusBarOffset()
    {
        return statusBarOffset;
    }

    public Color GetOutOfStockColor()
    {
        return outOfStockColor;
    }

    public float GetRefillTimePerCount()
    {
        return refillTimePerCount;
    }

    public int GetDefaultMaxCount()
    {
        return defaultMaxCount;
    }

    [ContextMenu("Register All RefillableItems")]
    public void RegisterAllRefillableItems()
    {
        var items = FindObjectsOfType<RefillableItem>();

        DebugLog($"Found {items.Length} RefillableItems in scene");

        foreach (var item in items) RegisterRefillableItem(item);

        DebugLog($"Registration complete: {refillableItems.Count} items registered");
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"RefillSystem: {message}");
    }

    // Method to be called by GamePhaseManager when phase changes
    public void NotifyPhaseChange(GamePhase newPhase)
    {
        OnGamePhaseChanged(newPhase);
    }
}