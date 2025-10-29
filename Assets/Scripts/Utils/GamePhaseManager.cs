using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public enum GamePhase
{
    ARRANGEMENT, // Player can drag items around
    PLAYING // Normal gameplay - serving customers
}

public class GamePhaseManager : MonoBehaviour
{
    [Header("UI References")] public GameObject arrangmentUI;
    [Header("UI References")] public Button doneButton;

    [Header("Game References")] public LevelManager levelManager;
    public OrderSystem orderSystem;
    public CustomerManager customerManager;
    public TableLayer tableLayer;
    public RefillSystem refillSystem; // NEW: Reference to refill system

    [Header("Debug")] public bool enableDebugLogs = true;

    // Private variables
    private GamePhase currentPhase = GamePhase.ARRANGEMENT;
    private ServeableItem[] allFoodItems;

    [Header("Tutorial")] public GameObject tutorialPanel;

    private void Start()
    {
        InitializeGamePhase();
    }

    private void InitializeGamePhase()
    {
        // Get all food items from LevelManager
        if (levelManager != null)
            allFoodItems = levelManager.serveableItems;
        else
            allFoodItems = FindObjectsOfType<ServeableItem>();
        if (refillSystem == null) refillSystem = FindObjectOfType<RefillSystem>();

        doneButton.onClick.AddListener(OnDoneButtonClicked);

        // Start in arrangement phase
        StartArrangementPhase();

        DebugLog("GamePhaseManager initialized");
    }

    public void StartArrangementPhase()
    {
        currentPhase = GamePhase.ARRANGEMENT;
        DebugLog("=== ARRANGEMENT PHASE STARTED ===");

        tutorialPanel.SetActive(true);
        LoadSavedFoodPositions();
        EnableArrangementMode();
        DisableGameplaySystems();
        arrangmentUI.gameObject.SetActive(true);

        // ✅ ADD THIS LINE: Initial button state check
        StartCoroutine(DelayedButtonStateUpdate());
        if (refillSystem != null) refillSystem.NotifyPhaseChange(currentPhase);
    }

// ✅ ADD THIS METHOD: Delayed initial check
    private IEnumerator DelayedButtonStateUpdate()
    {
        // Wait for draggable components to fully initialize
        yield return new WaitForSeconds(0.2f);
        UpdateDoneButtonState();
        DebugLog("Initial done button state updated");
    }

    public void StartPlayPhase()
    {
        currentPhase = GamePhase.PLAYING;
        DebugLog("=== PLAY PHASE STARTED ===");
        if (refillSystem != null) refillSystem.NotifyPhaseChange(currentPhase);
        // Disable dragging
        DisableArrangementMode();

        // Enable gameplay systems
        EnableGameplaySystems();


        arrangmentUI.gameObject.SetActive(false);
    }

    // Add this to your GamePhaseManager.cs

    // Replace these methods in your existing GamePhaseManager.cs

    private void EnableArrangementMode()
    {
        foreach (var item in allFoodItems)
            if (item != null && item.gameObject.activeInHierarchy)
            {
                var draggable = item.GetComponent<DraggableFood>();
                if (draggable == null) draggable = item.gameObject.AddComponent<DraggableFood>();

                draggable.Initialize(tableLayer, this, allFoodItems);

                // This now automatically starts iOS-style wiggling!
                draggable.SetDraggingEnabled(true);

                // Disable serving during arrangement
                item.SetServingEnabled(false);

                DebugLog($"Enabled arrangement mode with iOS wiggle for {item.GetFoodType()}");
            }
    }

    private void DisableArrangementMode()
    {
        foreach (var item in allFoodItems)
            if (item != null)
            {
                var draggable = item.GetComponent<DraggableFood>();
                if (draggable != null)
                    // This now automatically stops wiggling!
                    draggable.SetDraggingEnabled(false);

                // Re-enable serving
                item.SetServingEnabled(true);
            }
    }

    // Add this event handler (called by DraggableFood when overlap changes)
    public void OnItemOverlapChanged()
    {
        UpdateDoneButtonState();
    }

    /// <summary>
    /// Check if any items currently overlap
    /// </summary>
    private bool AnyItemsOverlapping()
    {
        if (allFoodItems == null) return false;

        foreach (var item in allFoodItems)
        {
            if (item == null || !item.gameObject.activeInHierarchy) continue;

            var draggable = item.GetComponent<DraggableFood>();
            if (draggable != null && draggable.HasOverlap()) return true;
        }

        return false;
    }

    /// <summary>
    /// Update done button state - disable if overlaps exist
    /// </summary>
    private void UpdateDoneButtonState()
    {
        if (doneButton == null) return;

        var hasOverlaps = AnyItemsOverlapping();
        doneButton.interactable = !hasOverlaps;

        if (hasOverlaps) DebugLog("Done button disabled - overlaps detected");
    }


    private void DisableGameplaySystems()
    {
        if (orderSystem != null) orderSystem.enabled = false;

        if (customerManager != null) customerManager.enabled = false;
    }

    private void EnableGameplaySystems()
    {
        orderSystem.enabled = true;
        customerManager.enabled = true;
        levelManager.StartGamePlay();
    }

    public void OnDoneButtonClicked()
    {
        tutorialPanel.SetActive(false);
        SessionManager.Instance.UpdateFoodPositions(allFoodItems);
        StartPlayPhase();
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"GamePhaseManager: {message}");
    }

    /// <summary>
    /// Load saved food positions from SessionManager when entering arrangement phase
    /// </summary>
    private void LoadSavedFoodPositions()
    {
        if (SessionManager.Instance == null || !SessionManager.Instance.HasSavedPositions())
        {
            DebugLog("No saved positions found - using scene default positions");
            return;
        }

        var sessionData = SessionManager.Instance.GetCurrentSession();
        var savedPositions = sessionData.savedFoodPositions;


        foreach (var item in allFoodItems)
            if (item != null && item.gameObject.activeInHierarchy)
            {
                // Find saved position for this food type
                var savedPos = savedPositions.Find(p => p.foodType == item.GetFoodType());

                if (savedPos != null)
                {
                    // Preserve the original Z position for layering
                    var originalZ = item.transform.position.z;
                    item.transform.position = savedPos.ToVector3(originalZ);


                    DebugLog($"Loaded saved position for {item.GetFoodType()}: ({savedPos.x:F2}, {savedPos.y:F2})");
                }
            }
    }
}