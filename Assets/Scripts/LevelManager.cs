using System.Collections;
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
    public TextMeshProUGUI totalEarned;
    public TextMeshProUGUI todaySale;
    public TextMeshProUGUI todayTip;
    public Button nextLevelButton;

    [Header("Score Animation Settings")]
    public float scoreTransferDuration = 2f;    // How long the animation takes
    public AnimationCurve scoreTransferCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Animation curve for smoothness
    
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

            // Check if continuing from saved session
            if (SessionManager.Instance.HasActiveSession())
            {
                int savedLevel = SessionManager.Instance.GetCurrentLevelIndex();
                Debug.Log($"🔄 Continuing from saved session - Level {savedLevel + 1}");
                LoadLevel(savedLevel);
            }
            else
            {
                Debug.Log("🆕 Starting new gameplay from Level 1");
                LoadLevel(0);
            }
        }
        else
        {
            Debug.LogError("SessionManager not found! Make sure it exists in the Main Menu scene.");
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

    // UPDATED: Restart level method with popup clearing
    public void RestartLevel()
    {
        // Universal restart method - handles both pause and level complete cases
        Time.timeScale = 1f;
        isPaused = false;
        
        // Clear score popup when restarting
        ClearScorePopup();
        
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

    // UPDATED: StartLevel method with popup clearing
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
            scoreManager.ResetScore(); // This will clear popup if enabled
        }
    }

    // UPDATED: OnLevelComplete method with popup management
    public void OnLevelComplete()
    {
        Debug.Log($"Level {currentLevelData.levelNumber} completed!");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLevelWin();
        }

        Debug.Log("=== LEVEL COMPLETE DEBUG ===");
        
        // Notify ScoreManager that level is ending (clears popup if enabled)
        if (scoreManager != null)
        {
            scoreManager.OnLevelEnd();
        }

      



        float levelScore = scoreManager.GetCurrentScore();
        float totalBefore = SessionManager.Instance.GetTotalScore();

        SessionManager.Instance.AddLevelScore(levelScore);
        SessionManager.Instance.OnLevelCompleted(currentLevelIndex);

        float totalAfter = SessionManager.Instance.GetTotalScore();

        customerManager.enabled = false; 
        orderSystem.enabled = false;
        StartCoroutine(ShowLevelCompletePopup());
    }

    IEnumerator ShowLevelCompletePopup()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(3f);
        
        AudioManager.Instance.PlayLevelCompleteMusic();

        if (popupCanvas != null)
        {
            popupCanvas.SetActive(true);
        }

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }
      
       
            float tips = scoreManager.GetTotalTipsEarned();
            todayTip.text = $"Tips Earned: {tips}";
            float todayScore = scoreManager.GetCurrentScore();
            todaySale.text = $"Today Sale: {todayScore}";
            float totalScore = SessionManager.Instance.GetTotalScore();
            totalEarned.text = $"Earned: {totalScore}";
            
        SetupLevelCompleteButtons();
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(AnimateFullTransfer(todaySale, totalEarned, "Order Sale: ","Earned: ", todayScore,totalScore));
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(AnimateFullTransfer(todayTip, totalEarned, "Tips: ","Earned: ", tips,totalScore));
        
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
            AudioManager.Instance.StopMusic();
        }

        SceneManager.LoadScene("Shop");
    }

    void OnAllLevelsComplete()
    {
        Debug.Log("🎉 All levels completed! Session finished!");

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.CompleteSession();
        }

        StartCoroutine(ShowLevelCompletePopup());
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

    // NEW: Method to manually clear score popup (useful for pause/resume)
    public void ClearScorePopup()
    {
        if (scoreManager != null)
        {
            scoreManager.ClearScorePopup();
        }
    }

    #endregion

    public LevelData GetCurrentLevelData()
    {
        return currentLevelData;
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
    public IEnumerator AnimateFullTransfer(
        TextMeshProUGUI sourceText, 
        TextMeshProUGUI targetText,
        string sourcePrefix,
        string targetPrefix,
        float amountToTransfer, 
        float targetFinalAmount)
    {
        // Calculate dynamic duration
        float duration =  (amountToTransfer / 100f) ;
        duration = Mathf.Clamp(duration, 0.5f, 10f);
    
        // Source starts with full amount, target starts without it
        float sourceStart = amountToTransfer;
        float targetStart = targetFinalAmount - amountToTransfer;
    
        // Set initial values
        if (sourceText != null)
            sourceText.text = $"{sourcePrefix}{sourceStart:F0}";
        if (targetText != null)
            targetText.text = $"{targetPrefix}{targetStart:F0}";
    
        float elapsedTime = 0f;
        float nextSoundTime = 1f;
    
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float easedProgress = AnimationCurve.EaseInOut(0, 0, 1, 1).Evaluate(progress);
        
            // Source counts down to 0, target counts up to final
            float currentSource = Mathf.Lerp(sourceStart, 0f, easedProgress);
            float currentTarget = Mathf.Lerp(targetStart, targetFinalAmount, easedProgress);
        
            // Update UI
            if (sourceText != null)
                sourceText.text = $"{sourcePrefix}{currentSource:F0}";
            if (targetText != null)
                targetText.text = $"{targetPrefix}{currentTarget:F0}";
        
            // Play sound every second
            if (elapsedTime >= nextSoundTime)
            {
               AudioManager.Instance.PlayMoneyCount();
                nextSoundTime += 1f;
            }
        
            yield return null;
        }
    
        // Final values
        if (sourceText != null)
            sourceText.text = $"{sourcePrefix}0";
        if (targetText != null)
            targetText.text = $"{targetPrefix}{targetFinalAmount:F0}";
    
        AudioManager.Instance.PlayMoneyTransferComplete();
    }

  
}