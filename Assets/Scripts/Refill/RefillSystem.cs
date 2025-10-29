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
    }

    private void InitializeSystem()
    {
        // Find GamePhaseManager if not assigned
        if (gamePhaseManager == null) gamePhaseManager = FindObjectOfType<GamePhaseManager>();

        // Find UI Canvas if not assigned
        if (uiCanvas == null) uiCanvas = FindObjectOfType<Canvas>();

        // Find all refillable items in scene
        RefillableItem[] items = FindObjectsOfType<RefillableItem>();
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
        if (countUIPrefab == null || uiCanvas == null) return null;

        var countUI = Instantiate(countUIPrefab, uiCanvas.transform);
        return countUI;
    }

    public GameObject CreateStatusBar(Transform parent)
    {
        if (statusBarPrefab == null || uiCanvas == null) return null;

        var statusBar = Instantiate(statusBarPrefab, uiCanvas.transform);
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