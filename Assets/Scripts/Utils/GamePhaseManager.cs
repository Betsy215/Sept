using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public enum GamePhase
{
    ARRANGEMENT, // Player can drag items around
    PLAYING // Normal gameplay - serving customers
}

public class GamePhaseManager : MonoBehaviour
{
    [Header("UI References")] public GameObject arrangmentUI;
    public Button doneButton;

    [Header("Game References")] public LevelManager levelManager;
    public OrderSystem orderSystem;
    public CustomerManager customerManager;
    public TableLayer tableLayer;
    public RefillSystem refillSystem;

    [Header("Tutorial")] public GameObject tutorialPanel;

    [Header("Level-Specific Tutorial Text")] [Tooltip("Text component inside tutorial panel to display messages")]
    public TextMeshProUGUI tutorialText;

    [Tooltip("Tutorial messages that rotate based on level (flexible array length)")]
    public string[] levelTutorialMessages = new string[]
    {
        "Drag to arrange table",
        "Hold on item to refill",
        "The fast you serve, the more tips you earn!"
    };

    [Header("Debug")] public bool enableDebugLogs = true;

    // Private variables
    private GamePhase currentPhase = GamePhase.ARRANGEMENT;
    private ServeableItem[] allFoodItems;

    // Draggable objects in the scene (ServeableItem + CoffeeMachine). Rebuilt on each phase change
    // instead of scanning every MonoBehaviour in the scene on every drag/overlap callback.
    private readonly List<IDraggable> draggableItems = new();

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

        if (refillSystem == null)
            refillSystem = FindObjectOfType<RefillSystem>();

        doneButton.onClick.AddListener(OnDoneButtonClicked);

        // Start in arrangement phase
        StartArrangementPhase();

        DebugLog("GamePhaseManager initialized");
    }

    public void StartArrangementPhase()
    {
        currentPhase = GamePhase.ARRANGEMENT;
        DebugLog("=== ARRANGEMENT PHASE STARTED ===");

        RefreshDraggableCache();

        tutorialPanel.SetActive(true);

        // Set level-specific tutorial text with rotation
        SetTutorialMessageForCurrentLevel();

        LoadSavedFoodPositions();
        EnableArrangementMode();
        DisableGameplaySystems();
        arrangmentUI.gameObject.SetActive(true);

        // Initial button state check
        StartCoroutine(DelayedButtonStateUpdate());

        if (refillSystem != null)
            refillSystem.NotifyPhaseChange(currentPhase);
    }

    /// <summary>
    /// Sets the tutorial message based on current level with rotation
    /// </summary>
    private void SetTutorialMessageForCurrentLevel()
    {
        if (tutorialText == null || levelTutorialMessages.Length == 0)
        {
            DebugLog("Tutorial text component or messages not configured");
            return;
        }

        var currentLevel = GetCurrentLevel();

        // Calculate message index with rotation based on array length
        var messageIndex = (currentLevel - 1) % levelTutorialMessages.Length;

        // Get the message and format it with the current level number
        var tutorialMessage = string.Format(levelTutorialMessages[messageIndex], currentLevel);

        tutorialText.text = tutorialMessage;

        DebugLog($"Set tutorial message for Level {currentLevel} (Message Index: {messageIndex}): {tutorialMessage}");
    }

    /// <summary>
    /// Gets the current level number from SessionManager or defaults to 1
    /// </summary>
    private int GetCurrentLevel()
    {
        if (SessionManager.Instance != null)
            return SessionManager.Instance.GetCurrentLevelIndex() + 1; // +1 because SessionManager is 0-based

        DebugLog("Could not determine current level, defaulting to Level 1");
        return 1;
    }

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

        if (refillSystem != null)
            refillSystem.NotifyPhaseChange(currentPhase);

        // Disable dragging
        DisableArrangementMode();

        // Enable gameplay systems
        EnableGameplaySystems();

        arrangmentUI.gameObject.SetActive(false);
    }

    /// <summary>
    /// Collect the draggable components (the concrete IDraggable implementers) once per phase change.
    /// </summary>
    private void RefreshDraggableCache()
    {
        draggableItems.Clear();

        var serveables = allFoodItems != null && allFoodItems.Length > 0
            ? allFoodItems
            : FindObjectsOfType<ServeableItem>();
        foreach (var item in serveables)
            if (item != null) draggableItems.Add(item);

        foreach (var machine in FindObjectsOfType<CoffeeMachine>())
            if (machine != null) draggableItems.Add(machine);

        DebugLog($"Cached {draggableItems.Count} draggable items");
    }

    private static bool IsLive(IDraggable item)
    {
        var behaviour = item as MonoBehaviour;
        return behaviour != null && behaviour.gameObject.activeInHierarchy;
    }

    private void EnableArrangementMode()
    {
        foreach (var item in draggableItems)
        {
            if (!IsLive(item)) continue;

            item.InitializeDragging(tableLayer, this, allFoodItems);
            item.SetDraggingEnabled(true);

            DebugLog($"Enabled arrangement mode for {((MonoBehaviour)item).gameObject.name}");
        }
    }

    private void DisableArrangementMode()
    {
        foreach (var item in draggableItems)
            if (item is MonoBehaviour behaviour && behaviour != null) // skip destroyed objects
                item.SetDraggingEnabled(false);
    }

    private bool AnyItemsOverlapping()
    {
        foreach (var item in draggableItems)
            if (IsLive(item) && item.HasOverlap())
                return true;

        return false;
    }

    public void OnItemOverlapChanged()
    {
        UpdateDoneButtonState();
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

    public GamePhase GetCurrentPhase()
    {
        return currentPhase;
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

    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"GamePhaseManager: {message}");
    }

    #region Debug Methods

    [ContextMenu("Test Tutorial Message Rotation")]
    public void TestTutorialMessageRotation()
    {
        DebugLog("=== TESTING TUTORIAL MESSAGE ROTATION ===");

        for (var testLevel = 1; testLevel <= levelTutorialMessages.Length + 3; testLevel++)
        {
            var messageIndex = (testLevel - 1) % levelTutorialMessages.Length;
            var message = string.Format(levelTutorialMessages[messageIndex], testLevel);
            DebugLog($"Level {testLevel} → Message Index {messageIndex}: {message}");
        }
    }

    #endregion
}