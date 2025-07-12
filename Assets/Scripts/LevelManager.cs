using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [Header("Level Data")] 
    public LevelData[] allLevels;

    [Header("Game Components")] 
    public OrderSystem orderSystem;
    public ServeableItem[] serveableItems; // CHANGED: Replace foodTrays with serveableItems
    public ScoreManager scoreManager;
    public CustomerManager customerManager; // Customer Manager integration

    [Header("Visual Elements")] 
    public SpriteRenderer backgroundRenderer;
    public Camera mainCamera;

    [Header("UI Elements")] 
    public TextMeshProUGUI levelInfoText;

    [Header("Scene Management")] 
    public string mainMenuSceneName = "MainMenu";

    [Header("Level Complete UI")] 
    public GameObject popupCanvas;
    public GameObject levelCompletePanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI totalScoreText;
    public Button nextLevelButton;
    public Button mainMenuButton;

    [Header("Audio Setup")] 
    public GameObject audioManagerPrefab; // Assign the AudioManager prefab here

    // Current level tracking
    private int currentLevelIndex = 0;
    private LevelData currentLevelData;

    void Start()
    {
        // IMPORTANT: Ensure AudioManager exists (create if missing)
        EnsureAudioManagerExists();

        // Register this LevelManager with SessionManager
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.RegisterLevelManager(this);
            SessionManager.Instance.FindGameReferences();

            Debug.Log($"SessionManager found. Has active session: {SessionManager.Instance.HasActiveSession()}");
            if (SessionManager.Instance.HasActiveSession())
            {
                Debug.Log($"Session total score: {SessionManager.Instance.GetTotalScore()}");
                Debug.Log($"Session current level: {SessionManager.Instance.GetCurrentLevelIndex() + 1}");
            }
        }
        else
        {
            Debug.LogError("SessionManager not found! Make sure it exists in the Main Menu scene.");
        }

        // Check if we should continue from a specific level
        if (SessionManager.Instance != null && SessionManager.Instance.HasActiveSession())
        {
            int sessionLevel = SessionManager.Instance.GetCurrentLevelIndex();
            Debug.Log($"Continuing from session level: {sessionLevel + 1}");
            LoadLevel(sessionLevel);
        }
        else
        {
            Debug.Log("Starting from level 1 (no active session)");
            LoadLevel(currentLevelIndex);
        }

        SetupLevelCompleteUI();
        SetupSessionEvents();

        // Start gameplay music
        StartGameplayMusic();
    }

    void EnsureAudioManagerExists()
    {
        if (AudioManager.Instance == null)
        {
            Debug.Log("LevelManager: AudioManager not found, creating one for Game Scene...");

            if (audioManagerPrefab != null)
            {
                // Use prefab with audio clips already assigned
                GameObject audioManagerGO = Instantiate(audioManagerPrefab);
                audioManagerGO.name = "AudioManager"; // Remove "(Clone)" from name
                Debug.Log("LevelManager: Created AudioManager from prefab with audio clips");
            }
            else
            {
                // Fallback: create empty AudioManager
                GameObject audioManagerGO = new GameObject("AudioManager");
                audioManagerGO.AddComponent<AudioManager>();
                Debug.LogWarning(
                    "LevelManager: Created empty AudioManager - assign audioManagerPrefab for full audio support");
            }
        }
        else
        {
            Debug.Log("LevelManager: AudioManager exists (carried over from Main Menu)");
        }

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("LevelManager: AudioManager still not found after creation attempt");
        }
    }

    void SetupSessionEvents()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnSessionCompleted += OnSessionCompleted;
        }
    }

    void SetupLevelCompleteUI()
    {
        // CRITICAL FIX: Hide level complete popup before loading next level
        if (popupCanvas != null)
            popupCanvas.SetActive(false);

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(LoadNextLevel);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }
    }

    // CRITICAL FIX: Helper method to hide level complete popup
    void HideLevelCompletePopup()
    {
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }
        
        if (popupCanvas != null)
        {
            popupCanvas.SetActive(false);
        }
    }

    public void LoadLevel(int levelIndex)
    {
        // CRITICAL FIX: Ensure popup is hidden when loading any level
        HideLevelCompletePopup();
        
        if (levelIndex >= 0 && levelIndex < allLevels.Length)
        {
            currentLevelIndex = levelIndex;
            currentLevelData = allLevels[levelIndex];
            ApplyLevelSettings();
            StartLevel();
        }
        else
        {
            Debug.Log("All levels completed!");
            OnAllLevelsComplete();
        }
    }

    public void SetCurrentLevel(int levelIndex)
    {
        currentLevelIndex = levelIndex;
    }

    void ApplyLevelSettings()
    {
        Debug.Log($"Loading {currentLevelData.levelName}");

        if (orderSystem != null)
        {
            orderSystem.ordersPerLevel = currentLevelData.ordersPerLevel;
            orderSystem.orderDisplayTime = currentLevelData.orderDisplayTime;
            orderSystem.timeBetweenOrders = currentLevelData.timeBetweenOrders;
            orderSystem.minOrderItems = currentLevelData.minOrderItems;
            orderSystem.maxOrderItems = currentLevelData.maxOrderItems;
        }

        // CHANGED: Apply individual item settings instead of tray settings
        if (serveableItems != null)
        {
            ApplyServeableItemSettings();
        }

        if (scoreManager != null)
        {
            scoreManager.SetLevelSettings(
                currentLevelData.basePointsPerOrder,
                currentLevelData.perfectOrderBonus,
                currentLevelData.timeBonus
            );
        }

        // Customer Manager Integration
        if (customerManager != null)
        {
            customerManager.OnLevelLoaded(currentLevelIndex);
            Debug.Log($"CustomerManager notified of level {currentLevelIndex + 1}");
        }

        // Apply visual settings
        ApplyVisualSettings();

        // Update level info display
        UpdateLevelInfoDisplay();
    }

    // NEW: ApplyServeableItemSettings() method
    void ApplyServeableItemSettings()
    {
        // For now, activate all items. You can expand this later to control 
        // which items are available per level using currentLevelData
        
        int activeItemCount = currentLevelData != null ? 
            Mathf.Clamp(currentLevelData.activeItemCount, 1, serveableItems.Length) : 
            serveableItems.Length;

        for (int i = 0; i < serveableItems.Length; i++)
        {
            if (serveableItems[i] != null)
            {
                bool shouldBeActive = i < activeItemCount;
                serveableItems[i].gameObject.SetActive(shouldBeActive);
                
                if (shouldBeActive)
                {
                    Debug.Log($"Activated serveable item: {serveableItems[i].GetFoodType()}");
                }
            }
        }

        Debug.Log($"Activated {activeItemCount} serveable items for level {currentLevelData.levelNumber}");
        
    }

    void ApplyVisualSettings()
    {
        // Apply background color
        if (backgroundRenderer != null)
        {
            backgroundRenderer.color = currentLevelData.backgroundColor;
        }

        // Apply background sprite if available
        if (backgroundRenderer != null && currentLevelData.backgroundSprite != null)
        {
            backgroundRenderer.sprite = currentLevelData.backgroundSprite;
        }

        // Adjust camera settings if needed
        if (mainCamera != null)
        {
            // Could add camera position/zoom adjustments per level here
        }
    }

    void UpdateLevelInfoDisplay()
    {
        if (levelInfoText != null)
        {
            levelInfoText.text = $"{currentLevelData.levelName}";
        }
    }

    void StartLevel()
    {
        Debug.Log($"Starting {currentLevelData.levelName}");

        // Reset and enable all game systems
        if (orderSystem != null)
        {
            orderSystem.gameObject.SetActive(true);

            // Determine flow type and start appropriately
            if (customerManager != null)
            {
                Debug.Log("CustomerManager present - using customer-integrated flow");
                // Initialize order system but don't start cycle yet
                orderSystem.InitializeForCustomerFlow();
                // Start the first customer spawn
                customerManager.SpawnCustomerForCurrentLevel();
            }
            else
            {
                Debug.Log("No CustomerManager - using original order flow");
                orderSystem.StartOrderCycle();
            }
        }

        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

        Debug.Log($"Level {currentLevelData.levelNumber} started!");
    }

    public void OnLevelComplete()
    {
        Debug.Log($"Level {currentLevelData.levelNumber} completed!");

        // Play level completion sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLevelWin();
        }

        if (SessionManager.Instance != null && scoreManager != null)
        {
            int levelScore = scoreManager.GetCurrentScore();
            SessionManager.Instance.AddLevelScore(levelScore);
            SessionManager.Instance.OnLevelCompleted(currentLevelIndex);
        }

        ShowLevelCompletePopup();
    }

    void ShowLevelCompletePopup()
    {
        // Play level complete music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLevelCompleteMusic();
        }

        if (popupCanvas != null)
        {
            popupCanvas.SetActive(true);
        }

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }

        if (finalScoreText != null && scoreManager != null)
        {
            int finalScore = scoreManager.GetCurrentScore();
            finalScoreText.text = $"Level Score: {finalScore}";
        }

        if (totalScoreText != null && SessionManager.Instance != null)
        {
            int totalScore = SessionManager.Instance.GetTotalScore();
            totalScoreText.text = $"Total Score: {totalScore}";
        }

        SetupLevelCompleteButtons();
    }

    void SetupLevelCompleteButtons()
    {
        if (nextLevelButton != null)
        {
            bool hasMoreLevels = (currentLevelIndex + 1) < allLevels.Length;
            nextLevelButton.gameObject.SetActive(hasMoreLevels);
            Debug.Log(
                $"Next Level Button: {(hasMoreLevels ? "Shown" : "Hidden")} - Current: {currentLevelIndex + 1}, Total: {allLevels.Length}");
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(true);
        }
    }

    public void LoadNextLevel()
    {
        // CRITICAL FIX: Hide level complete popup before loading next level
        HideLevelCompletePopup();
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
        }

        LoadLevel(currentLevelIndex + 1);
    }

    public void RestartLevel()
    {
        // CRITICAL FIX: Hide level complete popup before restarting level
        HideLevelCompletePopup();
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
        }

        LoadLevel(currentLevelIndex);
    }

    void OnAllLevelsComplete()
    {
        Debug.Log("🎉 All levels completed! Session finished!");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLevelWin();
        }

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.CompleteSession();
        }

        ShowLevelCompletePopup();
    }

    void OnSessionCompleted()
    {
        Debug.Log("Session completed event received!");
    }

    void StartGameplayMusic()
    {
        // Start gameplay background music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
            Debug.Log("LevelManager: Started gameplay music");
        }
        else
        {
            Debug.LogWarning("LevelManager: AudioManager still not found after creation attempt");
        }
    }

    void GoToMainMenu()
    {
        Debug.Log("Going to Main Menu...");

        // Stop current music - Main menu will auto-start its music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }

        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("Main Menu scene name not set in LevelManager!");
        }
    }

    void OnDestroy()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnSessionCompleted -= OnSessionCompleted;
        }
    }

    // Public getters for other scripts
    public int GetCurrentLevelIndex()
    {
        return currentLevelIndex;
    }

    public LevelData GetCurrentLevelData()
    {
        return currentLevelData;
    }

    public int GetCurrentLevelNumber()
    {
        return currentLevelIndex + 1;
    }

    // CHANGED: Replace GetActiveTrays() with GetActiveServeableItems()
    public ServeableItem[] GetActiveServeableItems()
    {
        if (currentLevelData == null || serveableItems == null) 
            return new ServeableItem[0];

        // Return all active (enabled) items
        List<ServeableItem> activeItems = new List<ServeableItem>();
        
        foreach (ServeableItem item in serveableItems)
        {
            if (item != null && item.gameObject.activeInHierarchy)
            {
                activeItems.Add(item);
            }
        }
        
        return activeItems.ToArray();
    }

    // CHANGED: Replace GetActiveTrayCount() with GetActiveItemCount()
    public int GetActiveItemCount()
    {
        return serveableItems != null ? serveableItems.Length : 0;
    }

    // NEW: Helper methods for managing serveable items
    public ServeableItem[] GetAllServeableItems()
    {
        return serveableItems != null ? serveableItems : new ServeableItem[0];
    }

    public ServeableItem FindServeableItem(string foodType)
    {
        if (serveableItems == null) return null;
        
        foreach (ServeableItem item in serveableItems)
        {
            if (item != null && item.GetFoodType() == foodType)
            {
                return item;
            }
        }
        
        return null;
    }
}