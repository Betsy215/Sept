using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelManager : MonoBehaviour
{
    [Header("Level Data")] public LevelData[] allLevels;

    [Header("Game Components")] public OrderSystem orderSystem;
    public FoodTray[] foodTrays;
    public ServePlate servePlate;
    public ScoreManager scoreManager;
    public CustomerManager customerManager; // NEW: Customer Manager integration

    [Header("Visual Elements")] public SpriteRenderer backgroundRenderer;
    public Camera mainCamera;

    [Header("UI Elements")] public TextMeshProUGUI levelInfoText;

    [Header("Scene Management")] public string mainMenuSceneName = "MainMenu";

    [Header("Level Complete UI")] public GameObject popupCanvas;
    public GameObject levelCompletePanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI totalScoreText;
    public Button nextLevelButton;
    public Button mainMenuButton;

    [Header("Audio Setup")] public GameObject audioManagerPrefab; // Assign the AudioManager prefab here

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

        if (foodTrays != null)
        {
            ApplyTraySettings();
        }

        if (servePlate != null)
        {
            servePlate.maxCapacity = currentLevelData.plateMaxCapacity;
        }

        if (scoreManager != null)
        {
            scoreManager.SetLevelSettings(
                currentLevelData.basePointsPerOrder,
                currentLevelData.perfectOrderBonus,
                currentLevelData.timeBonus
            );
        }

        // NEW: Customer Manager Integration
        if (customerManager != null)
        {
            customerManager.OnLevelLoaded(currentLevelIndex);
            Debug.Log($"CustomerManager notified of level {currentLevelIndex + 1}");
        }

        // Visual settings
        ApplyVisualSettings();

        // Update level info display
        UpdateLevelInfoDisplay();
    }

    void ApplyTraySettings()
    {
        // Activate only the required number of trays for this level
        int activeTrayCount = Mathf.Clamp(currentLevelData.activeTrayCount, 1, foodTrays.Length);

        for (int i = 0; i < foodTrays.Length; i++)
        {
            if (foodTrays[i] != null)
            {
                bool shouldBeActive = i < activeTrayCount;
                foodTrays[i].gameObject.SetActive(shouldBeActive);

                if (shouldBeActive)
                {
                    // Apply tray-specific settings from level data
                    foodTrays[i].maxItems = currentLevelData.maxItemsPerTray;
                    foodTrays[i].CompleteRefill(); // Fill the tray
                }
            }
        }

        Debug.Log($"Activated {activeTrayCount} trays for level {currentLevelData.levelNumber}");
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

            // Only start order cycle if no customer manager, otherwise customer manager controls this
            if (customerManager == null)
            {
                Debug.Log("No CustomerManager - starting original order flow");
                orderSystem.StartOrderCycle();
            }
            else
            {
                Debug.Log("CustomerManager present - order system will be controlled by customer flow");
            }
        }

        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }

        // Activate food trays based on level (already done in ApplyLevelSettings)

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
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
        }

        LoadLevel(currentLevelIndex + 1);
    }

    public void RestartLevel()
    {
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

  
    /// <summary>
    /// NEW: Public method for CustomerManager to get current level index
    /// </summary>
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
    