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

        // Show kitchen button tutorial (session flag inside guards against re-showing)
        TryShowKitchenButtonTutorial();
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

    private void EnableArrangementMode()
    {
        // Find all IDraggable objects
        var draggableItems = FindObjectsOfType<MonoBehaviour>().OfType<IDraggable>();

        foreach (var item in draggableItems)
        {
            var gameObject = ((MonoBehaviour)item).gameObject;

            if (gameObject != null && gameObject.activeInHierarchy)
            {
                item.InitializeDragging(tableLayer, this, allFoodItems);
                item.SetDraggingEnabled(true);

                DebugLog($"Enabled arrangement mode for {gameObject.name}");
            }
        }
    }

    private void DisableArrangementMode()
    {
        var draggableItems = FindObjectsOfType<MonoBehaviour>().OfType<IDraggable>();

        foreach (var item in draggableItems) item.SetDraggingEnabled(false);
    }

    private bool AnyItemsOverlapping()
    {
        var draggableItems = FindObjectsOfType<MonoBehaviour>().OfType<IDraggable>();
        return draggableItems.Any(item => item.HasOverlap());
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

    /// <summary>
    /// Triggers the kitchen button tutorial via LevelManager.
    /// No level-number check here — the session flag inside
    /// ShowKitchenButtonTutorial() is the sole guard, so this
    /// is safe to call on every arrangement phase start.
    /// </summary>
    private void TryShowKitchenButtonTutorial()
    {
        if (levelManager != null)
            StartCoroutine(ShowKitchenTutorialAfterDelay(0.35f));
    }

    private IEnumerator ShowKitchenTutorialAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        levelManager.ShowKitchenButtonTutorial();
        DebugLog("Kitchen button tutorial check triggered");
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