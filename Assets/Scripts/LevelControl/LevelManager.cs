using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    [Header("Level Data")] public LevelData[] allLevels;

    [Header("Game Components")] public OrderSystem orderSystem;
    public ServeableItem[] serveableItems;
    public ScoreManager scoreManager;
    public CustomerManager customerManager;

    [Header("Visual Elements")] public SpriteRenderer backgroundRenderer;
    public Camera mainCamera;

    [Header("UI Elements")] public TextMeshProUGUI levelInfoText;

    [Header("Scene Management")] public string mainMenuSceneName = "MainMenu";

    [Header("Level Complete UI")] public GameObject popupCanvas;
    public GameObject levelCompletePanel;
    public TextMeshProUGUI totalEarned;
    public TextMeshProUGUI todaySale;
    public TextMeshProUGUI todayTip;
    public Button nextLevelButton;
    public Button levelCompleteRestartButton;
    public Button levelCompleteMainMenuButton;

    [Header("Score Animation Settings")] public float scoreTransferDuration = 2f;
    public AnimationCurve scoreTransferCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Pause UI")] public GameObject pausePanel;
    public Button resumeButton;
    public Button pauseRestartButton;
    public Button pauseMainMenuButton;
    public TextMeshProUGUI unlockedItemsText;

    [Header("Audio Setup")] public GameObject audioManagerPrefab;

    public GamePhaseManager gamePhaseManager;

    // Current level tracking
    private int currentLevelIndex = 0;
    private LevelData currentLevelData;

    // Pause state
    private bool isPaused = false;

    private void Start()
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
                var savedLevel = SessionManager.Instance.GetCurrentLevelIndex();
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

        // REMOVED: StartGameplayMusic(); - Music now handled by phase transitions
    }

    private void EnsureAudioManagerExists()
    {
        if (AudioManager.Instance == null)
        {
            Debug.Log("LevelManager: AudioManager not found, creating one for Game Scene...");

            if (audioManagerPrefab != null)
            {
                var audioManagerGO = Instantiate(audioManagerPrefab);
                audioManagerGO.name = "AudioManager";
                Debug.Log("LevelManager: Created AudioManager from prefab with audio clips");
            }
            else
            {
                var audioManagerGO = new GameObject("AudioManager");
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
            Debug.LogWarning("LevelManager: AudioManager still not found after creation attempt");
    }

    private void SetupSessionEvents()
    {
        if (SessionManager.Instance != null)
            SessionManager.Instance.OnSessionCompleted += OnSessionCompleted;
    }

    private void SetupLevelCompleteUI()
    {
        if (popupCanvas != null)
            popupCanvas.SetActive(false);

        // Setup Next Level button
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(LoadNextLevel);
            Debug.Log("LevelManager: Next Level button setup complete");
        }

        // Setup Level Complete Restart button
        if (levelCompleteRestartButton != null)
        {
            levelCompleteRestartButton.onClick.RemoveAllListeners();
            levelCompleteRestartButton.onClick.AddListener(RestartLevel);
            Debug.Log("LevelManager: Level Complete Restart button setup complete");
        }
        else
        {
            Debug.LogWarning("LevelManager: Level Complete Restart button is not assigned!");
        }

        // Setup Level Complete Main Menu button
        if (levelCompleteMainMenuButton != null)
        {
            levelCompleteMainMenuButton.onClick.RemoveAllListeners();
            levelCompleteMainMenuButton.onClick.AddListener(GoToMainMenu);
            Debug.Log("LevelManager: Level Complete Main Menu button setup complete");
        }
        else
        {
            Debug.LogWarning("LevelManager: Level Complete Main Menu button is not assigned!");
        }
    }

    private void SetupPauseUI()
    {
        // Setup Resume button
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
            Debug.Log("LevelManager: Resume button setup complete");
        }

        // Setup Pause Restart button
        if (pauseRestartButton != null)
        {
            pauseRestartButton.onClick.RemoveAllListeners();
            pauseRestartButton.onClick.AddListener(RestartLevel);
            Debug.Log("LevelManager: Pause Restart button setup complete");
        }
        else
        {
            Debug.LogWarning("LevelManager: Pause Restart button is not assigned!");
        }

        // Setup Pause Main Menu button
        if (pauseMainMenuButton != null)
        {
            pauseMainMenuButton.onClick.RemoveAllListeners();
            pauseMainMenuButton.onClick.AddListener(GoToMainMenu);
            Debug.Log("LevelManager: Pause Main Menu button setup complete");
        }
        else
        {
            Debug.LogWarning("LevelManager: Pause Main Menu button is not assigned!");
        }

        if (pausePanel != null)
            pausePanel.SetActive(false);

        Debug.Log("LevelManager: Pause UI setup complete");
    }

    #region PAUSE FUNCTIONALITY

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        if (isPaused) return;

        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
        {
            if (popupCanvas != null)
                popupCanvas.SetActive(true);
            pausePanel.SetActive(true);
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopMusic();

        Debug.Log("Game paused");
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (popupCanvas != null && !IsAnyPopupActive())
            popupCanvas.SetActive(false);

        // UPDATED: Resume appropriate music based on current game phase
        if (AudioManager.Instance != null)
        {
            // Check if orders are active to determine if we're in gameplay phase
            if (orderSystem != null && orderSystem.IsOrderActive())
                StartGameplayMusic();
            else
                StartArrangementMusic();
        }

        Debug.Log("Game resumed");
    }

    private bool IsAnyPopupActive()
    {
        if (levelCompletePanel != null && levelCompletePanel.activeSelf)
            return true;
        return false;
    }

    public void RestartLevel()
    {
        Debug.Log("RestartLevel called");

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

        if (SessionManager.Instance != null && SessionManager.Instance.HasActiveSession())
        {
            SessionManager.Instance.SetCurrentLevel(currentLevelIndex);
            Debug.Log($"RestartLevel: Reset session to level {currentLevelIndex}");
        }

        // UPDATED: Stop existing music - let the restart handle music properly
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopMusic();

        SceneTransitionManager.Instance.TransitionToScene(SceneManager.GetActiveScene().name);
    }

    public bool IsPaused()
    {
        return isPaused;
    }

    #endregion

    #region LEVEL MANAGEMENT

    private void HideLevelCompletePopup()
    {
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        if (popupCanvas != null)
            popupCanvas.SetActive(false);
    }

    public void LoadLevel(int levelIndex)
    {
        HideLevelCompletePopup();

        if (levelIndex >= 0 && levelIndex < allLevels.Length)
        {
            currentLevelIndex = levelIndex;
            currentLevelData = allLevels[levelIndex];
            scoreManager.starProgressBar.Initialize(currentLevelData);
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

    private void ApplyLevelSettings()
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
            ApplyServeableItemSettings();

        if (customerManager != null)
        {
            customerManager.OnLevelLoaded(currentLevelIndex);
            Debug.Log($"CustomerManager notified of level {currentLevelIndex + 1}");
        }

        ApplyVisualSettings();
        UpdateLevelInfoDisplay();
    }

    private void ApplyServeableItemSettings()
    {
        // Check if SessionManager is available
        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession())
        {
            // Fallback: activate all items if no session
            Debug.LogWarning("No active session found, activating all serveable items as fallback");
            for (var i = 0; i < serveableItems.Length; i++)
                if (serveableItems[i] != null)
                    serveableItems[i].gameObject.SetActive(true);

            return;
        }

        // Get purchased food items from session
        var purchasedItems = SessionManager.Instance.GetCurrentSession().purchasedFoodItems;
        var activatedCount = 0;

        // Activate only purchased items
        for (var i = 0; i < serveableItems.Length; i++)
            if (serveableItems[i] != null)
            {
                var itemFoodType = serveableItems[i].GetFoodType();
                var shouldBeActive = purchasedItems.Contains(itemFoodType);
                serveableItems[i].gameObject.SetActive(shouldBeActive);

                if (shouldBeActive)
                    activatedCount++;
            }

        Debug.Log(
            $"Level {currentLevelIndex + 1}: Activated {activatedCount} out of {serveableItems.Length} serveable items based on purchased items: [{string.Join(", ", purchasedItems)}]");
    }

    private void ApplyVisualSettings()
    {
        if (currentLevelData != null && backgroundRenderer != null)
            if (currentLevelData.backgroundSprite != null)
                backgroundRenderer.sprite = currentLevelData.backgroundSprite;
    }

    private void UpdateLevelInfoDisplay()
    {
        if (levelInfoText != null && currentLevelData != null)
            levelInfoText.text = $"{currentLevelData.levelName}";
    }

    private void StartLevel()
    {
        gamePhaseManager.StartArrangementPhase();
        scoreManager.ResetScore();
        StartArrangementMusic(); // NEW: Start arrangement music when level starts
    }

    // NEW: Method to start arrangement music
    private void StartArrangementMusic()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayArrangementMusic();
    }

    // EXISTING: Method to start gameplay music (now uses playlist)
    private void StartGameplayMusic()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGameplayMusic();
    }

    public void StartGamePlay()
    {
        StartGameplayMusic(); // NEW: Transition to gameplay music when gameplay starts

        if (orderSystem != null)
        {
            // Determine flow type and start appropriately
            if (customerManager != null)
            {
                // Initialize order system but don't start cycle yet
                orderSystem.InitializeForCustomerFlow();
                // Start the first customer spawn
                customerManager.SpawnCustomerForCurrentLevel();
            }
            else
            {
                orderSystem.StartOrderCycle();
            }
        }
    }

    public void OnLevelComplete()
    {
        Debug.Log($"Level {currentLevelData.levelNumber} completed!");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayLevelWin();

        Debug.Log("=== LEVEL COMPLETE DEBUG ===");

        // Notify ScoreManager that level is ending (clears popup if enabled)
        if (scoreManager != null)
            scoreManager.OnLevelEnd();

        var levelScore = scoreManager.GetCurrentScore();
        var totalBefore = SessionManager.Instance.GetTotalScore();

        //    SessionManager.Instance.AddLevelScore(levelScore);
        SessionManager.Instance.OnLevelCompleted(currentLevelIndex);

        var totalAfter = SessionManager.Instance.GetTotalScore();

        customerManager.enabled = false;
        orderSystem.enabled = false;
        StartCoroutine(ShowLevelCompletePopup());
    }

    private IEnumerator ShowLevelCompletePopup()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(3f);

        AudioManager.Instance.PlayLevelCompleteMusic();

        if (popupCanvas != null)
            popupCanvas.SetActive(true);

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);

        var tips = scoreManager.GetTotalTipsEarned();
        todayTip.text = $"Tips Earned: $ {tips:F2}";
        var todayScore = scoreManager.GetCurrentScore();
        todaySale.text = $"Today Sale: $ {todayScore:F2}";
        var totalScore = SessionManager.Instance.GetTotalScore();
        totalEarned.text = $"Earned: $ 0.00";

        SetupLevelCompleteButtons();
        UpdateUnlockedItemsDisplay();
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(AnimateFullTransfer(todaySale, totalEarned, "Order Sale: $ ", "Earned: $ ",
            todayScore, totalScore));
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(AnimateFullTransfer(todayTip, totalEarned, "Tips: $ ", "Earned: $ ", tips,
            totalScore));

        var isLastLevel = currentLevelIndex + 1 >= allLevels.Length;
        if (isLastLevel)
        {
            yield return new WaitForSeconds(0.5f); // Small pause after animations
            ShowFinalCompletionMessage();
        }
    }

    private void ShowFinalCompletionMessage()
    {
        // Get total score from session
        var totalScore = SessionManager.Instance.GetTotalScore();

        // Override the text elements to show completion message
        if (todaySale != null)
            todaySale.text = "\nCongrats!\nYou finished all levels!";

        if (todayTip != null)
            todayTip.text = ""; // Clear the tips text

        if (totalEarned != null)
            totalEarned.text = $"Unlocked items : ";
        UpdateUnlockedItemsDisplay();
        Debug.Log($"🎉 All levels completed! Total score: {totalScore}");

        // FIX: Mark session as completed when all levels are finished
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.CompleteSession();
            Debug.Log("Session marked as completed - Continue button should now be disabled");
        }
    }

    private void UpdateUnlockedItemsDisplay()
    {
        if (unlockedItemsText == null) return;

        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession())
        {
            // No session, show zeros
            unlockedItemsText.text = "";
            return;
        }

        // Get REMAINING items to unlock (total - purchased)
        var (remainingFood, remainingCharacters) = SessionManager.Instance.GetUnpurchasedItemsCount();

        // Update UI to show remaining items
        unlockedItemsText.text = $"Unlocked Food: {remainingFood}\nUnlocked Customers: {remainingCharacters}";

        Debug.Log($"Level Complete: Remaining to unlock - Food: {remainingFood}, Customers: {remainingCharacters}");
    }

    private void SetupLevelCompleteButtons()
    {
        var isLastLevel = currentLevelIndex + 1 >= allLevels.Length;

        if (nextLevelButton != null && !isLastLevel) nextLevelButton.gameObject.SetActive(true);
        if (levelCompleteMainMenuButton != null && isLastLevel) levelCompleteMainMenuButton.gameObject.SetActive(true);
        if (levelCompleteRestartButton != null && !isLastLevel) levelCompleteRestartButton.gameObject.SetActive(true);
    }

    public void LoadNextLevel()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopMusic();

        SceneTransitionManager.Instance.TransitionToScene("Shop");
    }

    private void OnAllLevelsComplete()
    {
        Debug.Log("🎉 All levels completed! Session finished!");

        if (SessionManager.Instance != null)
            SessionManager.Instance.CompleteSession();

        StartCoroutine(ShowLevelCompletePopup());
    }

    public void GoToMainMenu()
    {
        Debug.Log("GoToMainMenu called");

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopMusic();

        // IMPORTANT: Ensure time is restored and pause state cleared before scene change
        Time.timeScale = 1f;
        isPaused = false;

        SceneTransitionManager.Instance.TransitionToScene(mainMenuSceneName);
    }

    private void OnSessionCompleted()
    {
        Debug.Log("Session completed event received");
    }

    public void ClearScorePopup()
    {
        if (scoreManager != null)
            scoreManager.ClearScorePopup();
    }

    #endregion

    public LevelData GetCurrentLevelData()
    {
        return currentLevelData;
    }

    public ServeableItem[] GetActiveServeableItems()
    {
        if (currentLevelData == null || serveableItems == null)
            return new ServeableItem[0];

        // Return all active (enabled) items
        var activeItems = new List<ServeableItem>();

        foreach (var item in serveableItems)
            if (item != null && item.gameObject.activeInHierarchy)
                activeItems.Add(item);

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
        var duration = amountToTransfer / 100f;
        duration = Mathf.Clamp(duration, 0.5f, 10f);

        // Source starts with full amount, target starts without it
        var sourceStart = amountToTransfer;
        var targetStart = targetFinalAmount - amountToTransfer;

        // Set initial values
        if (sourceText != null)
            sourceText.text = $"{sourcePrefix}{sourceStart:F0}";
        if (targetText != null)
            targetText.text = $"{targetPrefix}{targetStart:F0}";

        var elapsedTime = 0f;
        var nextSoundTime = 1f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            var progress = elapsedTime / duration;
            var easedProgress = AnimationCurve.EaseInOut(0, 0, 1, 1).Evaluate(progress);

            // Source counts down to 0, target counts up to final
            var currentSource = Mathf.Lerp(sourceStart, 0f, easedProgress);
            var currentTarget = Mathf.Lerp(targetStart, targetFinalAmount, easedProgress);

            // Update UI
            if (sourceText != null)
                sourceText.text = $"{sourcePrefix}{currentSource:F0}";
            if (targetText != null)
                targetText.text = $"{targetPrefix}{currentTarget:F0}";
            targetText.color = Color.red;

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