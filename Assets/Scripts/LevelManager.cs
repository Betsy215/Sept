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
    public ServeableItem[] serveableItems;
    public ScoreManager scoreManager;
    public CustomerManager customerManager;

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

    [Header("Pause UI")] 
    public GameObject pausePanel;
    public Button resumeButton;

    [Header("Shared UI Buttons - Can be reused across different panels")]
    public Button restartButton; // Used by both level complete and pause panels
    public Button mainMenuButton; // Used by both level complete and pause panels

    [Header("Audio Setup")] 
    public GameObject audioManagerPrefab;

    // Current level tracking
    private int currentLevelIndex = 0;
    private LevelData currentLevelData;
    
    // Pause state
    private bool isPaused = false;

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
        SetupPauseUI();
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
                GameObject audioManagerGO = Instantiate(audioManagerPrefab);
                audioManagerGO.name = "AudioManager";
                Debug.Log("LevelManager: Created AudioManager from prefab with audio clips");
            }
            else
            {
                GameObject audioManagerGO = new GameObject("AudioManager");
                audioManagerGO.AddComponent<AudioManager>();
                Debug.LogWarning("LevelManager: Created empty AudioManager - assign audioManagerPrefab for full audio support");
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
        if (popupCanvas != null)
            popupCanvas.SetActive(false);

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(LoadNextLevel);
        }

        // Set up shared buttons for level complete context
        SetupSharedButtons();
    }

    void SetupPauseUI()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeGame);
        }
        
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        
        Debug.Log("LevelManager: Pause UI setup complete");
    }

    void SetupSharedButtons()
    {
        // These buttons are shared across different UI panels
        // Universal methods handle both pause and level complete cases
        
        if (restartButton != null)
        {
            // Clear existing listeners to avoid duplicates
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartLevel);
        }

        if (mainMenuButton != null)
        {
            // Clear existing listeners to avoid duplicates
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }
        
        Debug.Log("LevelManager: Shared buttons setup complete");
    }

    #region PAUSE FUNCTIONALITY

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused) return;
        
        isPaused = true;
        Time.timeScale = 0f;
        
        if (pausePanel != null)
        {
            if (popupCanvas != null)
            {
                popupCanvas.SetActive(true);
            }
            pausePanel.SetActive(true);
        }
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }
        
        Debug.Log("Game paused");
    }

    public void ResumeGame()
    {
        if (!isPaused) return;
        
        isPaused = false;
        Time.timeScale = 1f;
        
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        
        if (popupCanvas != null && !IsAnyPopupActive())
        {
            popupCanvas.SetActive(false);
        }
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
        }
        
        Debug.Log("Game resumed");
    }

    private bool IsAnyPopupActive()
    {
        if (levelCompletePanel != null && levelCompletePanel.activeSelf)
        {
            return true;
        }
        return false;
    }

    public void RestartLevel()
    {
        // Universal restart method - handles both pause and level complete cases
        Time.timeScale = 1f;
        isPaused = false;
        
        // Hide ALL popups (both pause and level complete)
        if (pausePanel != null) 
            pausePanel.SetActive(false);
        if (levelCompletePanel != null) 
            levelCompletePanel.SetActive(false);
        if (popupCanvas != null) 
            popupCanvas.SetActive(false);
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
        }
        
        LoadLevel(currentLevelIndex);
        
        Debug.Log("Level restarted");
    }

    public bool IsPaused()
    {
        return isPaused;
    }

    #endregion

    #region LEVEL MANAGEMENT

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

        if (customerManager != null)
        {
            customerManager.OnLevelLoaded(currentLevelIndex);
            Debug.Log($"CustomerManager notified of level {currentLevelIndex + 1}");
        }

        ApplyVisualSettings();
        UpdateLevelInfoDisplay();
    }

    void ApplyServeableItemSettings()
    {
        // Check if SessionManager is available
        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession())
        {
            // Fallback: activate all items if no session
            Debug.LogWarning("No active session found, activating all serveable items as fallback");
            for (int i = 0; i < serveableItems.Length; i++)
            {
                if (serveableItems[i] != null)
                {
                    serveableItems[i].gameObject.SetActive(true);
                }
            }
            return;
        }
    
        // Get purchased food items from session
        List<string> purchasedItems = SessionManager.Instance.GetCurrentSession().purchasedFoodItems;
        int activatedCount = 0;
    
        // Activate only purchased items
        for (int i = 0; i < serveableItems.Length; i++)
        {
            if (serveableItems[i] != null)
            {
                string itemFoodType = serveableItems[i].GetFoodType();
                bool shouldBeActive = purchasedItems.Contains(itemFoodType);
                serveableItems[i].gameObject.SetActive(shouldBeActive);
            
                if (shouldBeActive) activatedCount++;
            }
        }
    
        Debug.Log($"Level {currentLevelIndex + 1}: Activated {activatedCount} out of {serveableItems.Length} serveable items based on purchased items: [{string.Join(", ", purchasedItems)}]");
    }

    void ApplyVisualSettings()
    {
        if (currentLevelData != null && backgroundRenderer != null)
        {
            if (currentLevelData.backgroundSprite != null)
            {
                backgroundRenderer.sprite = currentLevelData.backgroundSprite;
            }
        }
    }

    void UpdateLevelInfoDisplay()
    {
        if (levelInfoText != null && currentLevelData != null)
        {
            levelInfoText.text = $"{currentLevelData.levelName}";
        }
    }

    void StartLevel()
    {
        Debug.Log($"Starting level: {currentLevelData.levelName}");
        
        if (orderSystem != null)
        {
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
    }

    public void OnLevelComplete()
    {
        Debug.Log($"Level {currentLevelData.levelNumber} completed!");

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
            Debug.Log($"Next Level Button: {(hasMoreLevels ? "Shown" : "Hidden")} - Current: {currentLevelIndex + 1}, Total: {allLevels.Length}");
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(true);
        }
    }

    public void LoadNextLevel()
    {
        HideLevelCompletePopup();
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
        }

        LoadLevel(currentLevelIndex + 1);
    }

    void OnAllLevelsComplete()
    {
        Debug.Log("🎉 All levels completed! Session finished!");

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.CompleteSession();
        }

        ShowLevelCompletePopup();
    }

    public void GoToMainMenu()
    {
        Debug.Log("Returning to main menu...");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }

        // IMPORTANT: Ensure time is restored and pause state cleared before scene change
        Time.timeScale = 1f;
        isPaused = false;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    void StartGameplayMusic()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
        }
    }

    void OnSessionCompleted()
    {
        Debug.Log("Session completed event received");
    }

    #endregion

    #region UTILITY METHODS

    public LevelData GetCurrentLevelData()
    {
        return currentLevelData;
    }

    public int GetCurrentLevelIndex()
    {
        return currentLevelIndex;
    }

    public int GetCurrentLevelNumber()
    {
        return currentLevelIndex + 1;
    }

    public ServeableItem[] GetAllServeableItems()
    {
        return serveableItems != null ? serveableItems : new ServeableItem[0];
    }

    // CRITICAL: This method is used by OrderSystem.UpdateActiveFoodTypes()
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

    public int GetActiveItemCount()
    {
        return GetActiveServeableItems().Length;
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

    #endregion
}