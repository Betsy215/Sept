using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelManager : MonoBehaviour
{
    [Header("Level Data")]
    public LevelData[] allLevels;
    
    [Header("Game Components")]
    public OrderSystem orderSystem;
    public FoodTray[] foodTrays; // Array of all food trays
    public ServePlate servePlate;
    public ScoreManager scoreManager;
    
    [Header("Visual Elements")]
    public SpriteRenderer backgroundRenderer;
    public Camera mainCamera;
    
    [Header("UI Elements")]
    public TextMeshProUGUI levelInfoText;
    
    [Header("Scene Management")]
    public string mainMenuSceneName = "MainMenu"; // Name of your main menu scene
    
    [Header("Level Complete UI")]
    public GameObject popupCanvas;
    public GameObject levelCompletePanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI totalScoreText; // NEW: Display session total score
    public Button nextLevelButton;
    public Button mainMenuButton;
    
    // Current level tracking
    private int currentLevelIndex = 0;
    private LevelData currentLevelData;
    
    void Start()
    {
        // Initialize AudioManager in game scene
        InitializeAudioManager();
        
        // Register this LevelManager with SessionManager
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.RegisterLevelManager(this);
            // Also try to find other references
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
    
        // Check if we should continue from a specific level (for session continuation)
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
    
    void InitializeAudioManager()
    {
        // Ensure AudioManager exists in game scene
        if (AudioManager.Instance == null)
        {
            Debug.Log("LevelManager: Creating AudioManager for Game Scene...");
            GameObject audioManagerGO = new GameObject("AudioManager");
            audioManagerGO.AddComponent<AudioManager>();
        }
        else
        {
            Debug.Log("LevelManager: AudioManager already exists");
        }
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
            Debug.LogWarning("LevelManager: AudioManager not found, cannot start gameplay music");
        }
    }
    
    void SetupSessionEvents()
    {
        // Subscribe to session events if SessionManager exists
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnSessionCompleted += OnSessionCompleted;
        }
    }
    
    void SetupLevelCompleteUI()
    {
        // Hide popup initially
        if (popupCanvas != null)
            popupCanvas.SetActive(false);
        
        // Set up next level button
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(LoadNextLevel);
        }
        
        // Set up main menu button
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }
    }
    
    public void LoadLevel(int levelIndex)
    {
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
    
    // NEW: Public method to set current level (used by SessionManager)
    public void SetCurrentLevel(int levelIndex)
    {
        currentLevelIndex = levelIndex;
    }
    
    void ApplyLevelSettings()
    {
        Debug.Log($"Loading {currentLevelData.levelName}");
        
        // Apply Order System settings
        if (orderSystem != null)
        {
            orderSystem.ordersPerLevel = currentLevelData.ordersPerLevel;
            orderSystem.orderDisplayTime = currentLevelData.orderDisplayTime;
            orderSystem.timeBetweenOrders = currentLevelData.timeBetweenOrders;
            orderSystem.minOrderItems = currentLevelData.minOrderItems;
            orderSystem.maxOrderItems = currentLevelData.maxOrderItems;
        }
        
        // Apply Food Tray settings with tray count management
        if (foodTrays != null)
        {
            ApplyTraySettings();
        }
        
        // Apply Serve Plate settings
        if (servePlate != null)
        {
            servePlate.maxCapacity = currentLevelData.plateMaxCapacity;
        }
        
        // Apply Score Manager settings
        if (scoreManager != null)
        {
            scoreManager.SetLevelSettings(
                currentLevelData.basePointsPerOrder,
                currentLevelData.perfectOrderBonus,
                currentLevelData.timeBonus
            );
        }
        
        // Apply visual settings
        ApplyVisualSettings();
        
        // Update UI
        if (levelInfoText != null)
        {
            levelInfoText.text = currentLevelData.levelName;
        }
    }
    
    void ApplyTraySettings()
    {
        int activeTrayCount = Mathf.Clamp(currentLevelData.activeTrayCount, 1, foodTrays.Length);
        
        Debug.Log($"Setting {activeTrayCount} active trays out of {foodTrays.Length} total trays");
        
        for (int i = 0; i < foodTrays.Length; i++)
        {
            if (foodTrays[i] != null)
            {
                // Determine if this tray should be active
                bool shouldBeActive = i < activeTrayCount;
                
                // Activate/deactivate the entire tray GameObject
                foodTrays[i].gameObject.SetActive(shouldBeActive);
                
                if (shouldBeActive)
                {
                    // Apply level settings to active trays
                    foodTrays[i].maxItems = currentLevelData.maxItemsPerTray;
                    
                    // Refill the tray for the new level
                    foodTrays[i].CompleteRefill();
                    
                    Debug.Log($"Tray {i} ({foodTrays[i].foodType}): ACTIVE");
                }
                else
                {
                    Debug.Log($"Tray {i}: INACTIVE");
                }
            }
        }
    }
    
    void ApplyVisualSettings()
    {
        // Apply background sprite
        if (backgroundRenderer != null && currentLevelData.backgroundSprite != null)
        {
            backgroundRenderer.sprite = currentLevelData.backgroundSprite;
        }
        
        // Apply background color
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = currentLevelData.backgroundColor;
        }
    }
    
    void StartLevel()
    {
        // Hide level complete popup
        if (popupCanvas != null)
        {
            popupCanvas.SetActive(false);
        }
        
        // Reset score for new level
        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }
        
        // Start the order system (it should restart automatically)
        if (orderSystem != null)
        {
            orderSystem.StopOrderSystem(); // Stop current cycle
            orderSystem.enabled = false;   // Disable and re-enable to restart
            orderSystem.enabled = true;
        }
        
        // Ensure gameplay music is playing
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
        }
    }
    
    // Called when current level is completed
    public void OnLevelComplete()
    {
        Debug.Log($"{currentLevelData.levelName} completed!");
        
        // Play level completion sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLevelWin();
        }
        
        // Add level score to session total
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
        
        // Show popup canvas
        if (popupCanvas != null)
        {
            popupCanvas.SetActive(true);
        }
        
        // Show level complete panel
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }
        
        // Update final score (level score)
        if (finalScoreText != null && scoreManager != null)
        {
            int finalScore = scoreManager.GetCurrentScore();
            finalScoreText.text = $"Level Score: {finalScore}";
        }
        
        // NEW: Update total session score
        if (totalScoreText != null && SessionManager.Instance != null)
        {
            int totalScore = SessionManager.Instance.GetTotalScore();
            totalScoreText.text = $"Total Score: {totalScore}";
        }
        
        // Setup buttons based on available levels
        SetupLevelCompleteButtons();
    }
    
    void SetupLevelCompleteButtons()
    {
        // Setup Next Level Button
        if (nextLevelButton != null)
        {
            bool hasMoreLevels = (currentLevelIndex + 1) < allLevels.Length;
            nextLevelButton.gameObject.SetActive(hasMoreLevels);
            
            Debug.Log($"Next Level Button: {(hasMoreLevels ? "Shown" : "Hidden")} - Current: {currentLevelIndex + 1}, Total: {allLevels.Length}");
        }
        
        // Main Menu button is always available
        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(true);
        }
    }
    
    public void LoadNextLevel()
    {
        // Resume gameplay music when continuing to next level
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
        }
        
        LoadLevel(currentLevelIndex + 1);
    }
    
    public void RestartLevel()
    {
        // Resume gameplay music when restarting level
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
        }
        
        LoadLevel(currentLevelIndex);
    }
    
    void OnAllLevelsComplete()
    {
        Debug.Log("🎉 All levels completed! Session finished!");
        
        // Play special completion sound/music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLevelWin();
            // You could add a special "game complete" music here if you have one
        }
        
        // Complete the session
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.CompleteSession();
        }
        
        ShowLevelCompletePopup(); // Still show the popup, but Next Level button will be hidden
    }
    
    void OnSessionCompleted()
    {
        Debug.Log("Session completed event received!");
        // You can add additional session completion logic here
    }
    
    void GoToMainMenu()
    {
        Debug.Log("Going to Main Menu...");
        
        // Stop current music before returning to main menu
        // Main menu will start its own music automatically
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }
        
        // Load the main menu scene
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("Main Menu scene name not set in LevelManager!");
        }
    }
    
    // Clean up events when destroyed
    void OnDestroy()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnSessionCompleted -= OnSessionCompleted;
        }
    }
    
    // Public getters
    public LevelData GetCurrentLevelData()
    {
        return currentLevelData;
    }
    
    public int GetCurrentLevelNumber()
    {
        return currentLevelIndex + 1;
    }
    
    public int GetActiveTrayCount()
    {
        return currentLevelData != null ? currentLevelData.activeTrayCount : foodTrays.Length;
    }
    
    public FoodTray[] GetActiveTrays()
    {
        if (currentLevelData == null || foodTrays == null) return new FoodTray[0];
        
        int activeTrayCount = Mathf.Clamp(currentLevelData.activeTrayCount, 1, foodTrays.Length);
        FoodTray[] activeTrays = new FoodTray[activeTrayCount];
        
        for (int i = 0; i < activeTrayCount; i++)
        {
            activeTrays[i] = foodTrays[i];
        }
        
        return activeTrays;
    }
}