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
    [Header("UI References")] public Button doneButton;

    [Header("Game References")] public LevelManager levelManager;
    public OrderSystem orderSystem;
    public CustomerManager customerManager;
    public TableLayer tableLayer;

    [Header("Debug")] public bool enableDebugLogs = true;

    // Private variables
    private GamePhase currentPhase = GamePhase.ARRANGEMENT;
    private ServeableItem[] allFoodItems;

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


        doneButton.onClick.AddListener(OnDoneButtonClicked);

        // Start in arrangement phase
        StartArrangementPhase();

        DebugLog("GamePhaseManager initialized");
    }

    public void StartArrangementPhase()
    {
        currentPhase = GamePhase.ARRANGEMENT;
        DebugLog("=== ARRANGEMENT PHASE STARTED ===");

        LoadSavedFoodPositions();

        // Enable dragging on all food items
        EnableArrangementMode();

        // Disable gameplay systems
        DisableGameplaySystems();

        // Show done button
        if (doneButton != null) doneButton.gameObject.SetActive(true);
    }

    public void StartPlayPhase()
    {
        currentPhase = GamePhase.PLAYING;
        DebugLog("=== PLAY PHASE STARTED ===");

        // Disable dragging
        DisableArrangementMode();

        // Enable gameplay systems
        EnableGameplaySystems();

        // Hide done button
        if (doneButton != null) doneButton.gameObject.SetActive(false);
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