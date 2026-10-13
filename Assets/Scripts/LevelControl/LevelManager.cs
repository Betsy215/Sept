using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Linq;

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

    public Button closeButton;

    // Current level tracking
    private int currentLevelIndex = 0;
    private LevelData currentLevelData;

    [Header("--- DEBUG ONLY ---")] public bool debugMode = false;

    public int debugStartLevel = 13;

    // Pause state
    private bool isPaused = false;

    // Set once the level's last order is done. Blocks pausing during the 3 s before the popup,
    // where Main Menu would skip the Shop (and, on the final day, leave the save half-finished).
    private bool isLevelEnding = false;

    // Final day: the high-score result is decided when the level ends, then only displayed later.
    private bool finalRunIsNewRecord = false;

    // Map (10 October): a chapter is 16 days; days past it stay in allLevels for chapter 2.
    // The day clock (13 October 2026): a day is dayDurationSeconds long. Customers keep coming on the level's beat
    // while it runs; when it reaches zero no more come, and the day ends once the last order is closed and the last
    // customer has left. The tutorial has no clock. The clock is not shown: the evening tint (DayNightTint) tells
    // the player the day is ending (owner, 13 October).
    private float dayTimeLeft;
    private bool dayStarted, dayClockDisabled;
    public float DayDuration => currentLevelData != null ? currentLevelData.dayDurationSeconds : 0f;
    public float DayTimeLeft => dayTimeLeft;
    public bool DayStarted => dayStarted;
    public bool DayOver => dayStarted && !dayClockDisabled && dayTimeLeft <= 0f;

    public const float ReplayPayFactor = 0.5f;   // share of order money paid when a finished day is replayed (12 October 2026)
    public const int ThreeStarBonus = 100;   // paid once per day, the first time it gets three stars
    private int threeStarBonusPaid;
    private int LastDayIndex => Mathf.Min(allLevels.Length, MapScreen.ChapterDays) - 1;

    private void Start()
    {
        // IMPORTANT: Ensure AudioManager exists (create if missing)
        EnsureAudioManagerExists();

        // DEBUG: bypass session level and jump to specific level
        if (debugMode)
        {
            Debug.Log($"[DEBUG] Jumping directly to level {debugStartLevel + 1}");
            if (SessionManager.Instance != null)
                SessionManager.Instance.SetCurrentLevel(debugStartLevel);
            LoadLevel(debugStartLevel);
            return;
        }

        // Register this LevelManager with SessionManager
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.RegisterLevelManager(this);
            SessionManager.Instance.FindGameReferences();

            // Check if continuing from saved session
            if (SessionManager.Instance.HasActiveSession())
            {
                var savedLevel = Mathf.Min(SessionManager.Instance.GetCurrentLevelIndex(), LastDayIndex);
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

        // Level 0: a self-playing tutorial before the first real day (or a requested replay)
        if (currentLevelIndex == 0 && TutorialDirector.IsDue(SessionManager.Instance))
            gameObject.AddComponent<TutorialDirector>().Begin(this, gamePhaseManager, orderSystem, customerManager);

        // Evening and night over the last two customers; draws only on the background image
        if (orderSystem != null) gameObject.AddComponent<DayNightTint>().Begin(orderSystem);

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
        {
            // Remove first so a re-entry never double-subscribes
            SessionManager.Instance.OnSessionCompleted -= OnSessionCompleted;
            SessionManager.Instance.OnSessionCompleted += OnSessionCompleted;
        }
    }

    private void OnDestroy()
    {
        // SessionManager is DontDestroyOnLoad; drop our delegate so it does not leak per level
        if (SessionManager.Instance != null)
            SessionManager.Instance.OnSessionCompleted -= OnSessionCompleted;
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
            pauseMainMenuButton.onClick.AddListener(GoToMap); // the pause menu's home opens the map (owner, 10 October)
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

        // Ignore pause once the level-complete popup is up; otherwise a paused
        // timeScale can leak into the Shop scene through paths other than LoadNextLevel
        if (IsAnyPopupActive() || isLevelEnding)
        {
            Debug.Log("PauseGame ignored: level is ending or the level complete popup is showing");
            return;
        }

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
            if (gamePhaseManager.GetCurrentPhase() == GamePhase.PLAYING)
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

    // A Restart keeps the pay rate of the run it restarts: a first play restarted from the finish board is still a
    // first play (levelsCompleted was already bumped by then). Set by RestartLevel, used once by LoadLevel.
    private static float restartPayFactor = -1f;

    public void RestartLevel()
    {
        Debug.Log("RestartLevel called");
        if (scoreManager != null) restartPayFactor = scoreManager.payFactor;

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
            SessionManager.Instance.RestoreScoreToSnapshot();
            // The three-star bonus is paid once and recorded; Restart must not take it back
            if (threeStarBonusPaid > 0) SessionManager.Instance.AddScoreImmediately(threeStarBonusPaid);
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
            // A day already finished pays half its order money on a replay (stars and the three-star bonus are unchanged)
            scoreManager.payFactor = restartPayFactor > 0f ? restartPayFactor
                : SessionManager.Instance != null && levelIndex < SessionManager.Instance.GetLevelsCompleted() ? ReplayPayFactor : 1f;
            restartPayFactor = -1f;
            scoreManager.starProgressBar.Initialize(currentLevelData);
            ApplyLevelSettings();
            StartLevel();
        }
        else
        {
            Debug.Log("All levels completed!");
            // Saves from before the final-day fix can point one past the last level.
            // Treat that as the final day so the popup shows the finished state and Main Menu.
            currentLevelIndex = allLevels.Length - 1;
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

        KitchenButtonOverTable.Setup();   // the truck button a third onto the cloth (12 October)
        // Money and stars live on the table now (13 October): the flip clock next to the truck; the sign at the top goes
        MoneyFlipClock.Setup(scoreManager);
        if (levelInfoText != null && levelInfoText.transform.parent != null) levelInfoText.transform.parent.gameObject.SetActive(false);

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
            // Fallback: activate all items and set default upgrade levels
            for (var i = 0; i < serveableItems.Length; i++)
                if (serveableItems[i] != null)
                    serveableItems[i].gameObject.SetActive(true);

            // Set default upgrade levels for ALL IUpgradeable objects
            SetDefaultUpgradeLevels();
            return;
        }

        // Get purchase status AND apply upgrade levels
        var sessionData = SessionManager.Instance.GetCurrentSession();
        var purchasedItems = sessionData.purchasedFoodItems;
        var activatedCount = 0;

        // Activate only purchased ServeableItems
        for (var i = 0; i < serveableItems.Length; i++)
            if (serveableItems[i] != null)
            {
                var itemFoodType = serveableItems[i].GetFoodType();
                var shouldBeActive = purchasedItems.Contains(itemFoodType);
                serveableItems[i].gameObject.SetActive(shouldBeActive);

                if (shouldBeActive) activatedCount++;
            }

        // Apply upgrade levels to ALL IUpgradeable objects (unified approach)
        ApplyUpgradeLevels();

        Debug.Log(
            $"Level {currentLevelIndex + 1}: Activated {activatedCount} serveable items with upgrade levels applied");
    }

    private void ApplyUpgradeLevels()
    {
        var upgradeableItems = FindObjectsOfType<MonoBehaviour>().OfType<IUpgradeable>();

        foreach (var item in upgradeableItems)
        {
            var gameObject = ((MonoBehaviour)item).gameObject;

            // Get upgrade level based on object type
            var upgradeLevel = GetUpgradeLevelForObject(gameObject);
            item.SetUpgradeLevel(upgradeLevel);

            Debug.Log($"Applied upgrade level {upgradeLevel} to {gameObject.name}");
        }
    }

    private int GetUpgradeLevelForObject(GameObject obj)
    {
        // For ServeableItems, use food type
        var serveableItem = obj.GetComponent<ServeableItem>();
        if (serveableItem != null) return SessionManager.Instance.GetFoodUpgradeLevel(serveableItem.GetFoodType());

        // For CoffeeMachine, use "CoffeeMachine" key
        var coffeeMachine = obj.GetComponent<CoffeeMachine>();
        if (coffeeMachine != null) return SessionManager.Instance.GetFoodUpgradeLevel("CoffeeMachine");

        // For other objects, use GameObject name as key
        return SessionManager.Instance.GetFoodUpgradeLevel(obj.name);
    }

    private void SetDefaultUpgradeLevels()
    {
        var upgradeableItems = FindObjectsOfType<MonoBehaviour>().OfType<IUpgradeable>();

        foreach (var item in upgradeableItems) item.SetUpgradeLevel(1); // Default level 1
    }

    private void ApplyVisualSettings()
    {
        if (currentLevelData != null && backgroundRenderer != null)
            if (currentLevelData.backgroundSprite != null)
                backgroundRenderer.sprite = currentLevelData.backgroundSprite;
    }

    private void Update()
    {
        if (!dayStarted || dayClockDisabled || isLevelEnding) return;
        if (dayTimeLeft > 0f)
        {
            if (orderSystem == null || !orderSystem.freezeTimer) dayTimeLeft = Mathf.Max(0f, dayTimeLeft - Time.deltaTime);
            if (dayTimeLeft <= 0f) TryEndDay();
        }
    }

    /// Called by OrderSystem when an order closes and by CustomerManager when a customer has left
    public void OnOrderClosed() => TryEndDay();
    public void OnCustomerLeft() => TryEndDay();

    private void TryEndDay()
    {
        if (!DayOver || isLevelEnding) return;
        if (orderSystem != null && orderSystem.OpenOrders > 0) return;
        if (customerManager != null && customerManager.ActiveCustomers > 0) return;
        OnLevelComplete();
    }

    private void UpdateLevelInfoDisplay()
    {
        if (levelInfoText != null && currentLevelData != null)
            levelInfoText.text = $"{currentLevelData.levelName}";
    }

    private void StartLevel()
    {
        SessionManager.Instance.SnapshotScoreBeforeLevel();
        gamePhaseManager.StartArrangementPhase();
        scoreManager.ResetScore();
        StartArrangementMusic();
    }

    private void StartArrangementMusic()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayArrangementMusic();
    }

    private void StartGameplayMusic()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGameplayMusic();
    }

    public void StartGamePlay()
    {
        StartGameplayMusic();
        dayStarted = true;
        dayTimeLeft = DayDuration;
        dayClockDisabled = currentLevelIndex == 0 && TutorialDirector.IsDue(SessionManager.Instance);   // the demo has no end of day

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

        // Stars for the map, and the one-time three-star bonus
        var stars = scoreManager != null && scoreManager.starProgressBar != null ? scoreManager.starProgressBar.GetEarnedStarsCount() : 0;
        threeStarBonusPaid = SessionManager.Instance.RecordLevelStars(currentLevelIndex, stars, ThreeStarBonus);

        // Last day of the chapter: record the high score. The session stays active so every day
        // can still be replayed from the map (it used to end the game here).
        if (currentLevelIndex >= LastDayIndex)
            finalRunIsNewRecord = SessionManager.Instance.CheckAndSaveHighScore();

        isLevelEnding = true;
        var totalAfter = SessionManager.Instance.GetTotalScore();

        customerManager.enabled = false;
        orderSystem.enabled = false;
        StartCoroutine(ShowLevelCompletePopup());
    }

    private IEnumerator ShowLevelCompletePopup()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(3f);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayLevelCompleteMusic();

        if (popupCanvas != null)
            popupCanvas.SetActive(true);

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);

        // GetCurrentScore() already includes tips, so split it: order sale + tips = level score.
        // Each amount is transferred into "Earned" exactly once.
        var tips = scoreManager.GetTotalTipsEarned();
        var todayScore = scoreManager.GetCurrentScore();
        var orderSale = Mathf.Max(0f, todayScore - tips);
        var totalScore = SessionManager.Instance != null ? SessionManager.Instance.GetTotalScore() : todayScore;
        var earnedBeforeLevel = Mathf.Max(0f, totalScore - todayScore - threeStarBonusPaid);
        var earnedAfterSale = totalScore - tips - threeStarBonusPaid;

        if (todayTip != null)
            todayTip.text = $"Tips Earned: $ {tips:F2}";
        if (todaySale != null)
            todaySale.text = $"Today Sale: $ {orderSale:F2}";
        if (totalEarned != null)
            totalEarned.text = $"Earned: $ {earnedBeforeLevel:F2}";

        SetupLevelCompleteButtons();
        UpdateUnlockedItemsDisplay();
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(AnimateFullTransfer(todaySale, totalEarned, "Order Sale: $ ", "Earned: $ ",
            orderSale, earnedAfterSale));
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(AnimateFullTransfer(todayTip, totalEarned, "Tips: $ ", "Earned: $ ", tips,
            totalScore - threeStarBonusPaid));
        if (threeStarBonusPaid > 0 && totalEarned != null) totalEarned.text = $"Earned: $ {totalScore:F2}";

        var isLastLevel = currentLevelIndex >= LastDayIndex;
        if (isLastLevel)
        {
            yield return new WaitForSeconds(0.5f);
            ShowFinalCompletionMessage();
        }
        if (threeStarBonusPaid > 0 && unlockedItemsText != null)
            unlockedItemsText.text = $"Three stars! Bonus $ {threeStarBonusPaid}\n" + unlockedItemsText.text;
    }

    private void ShowFinalCompletionMessage()
    {
        var totalScore = SessionManager.Instance.GetTotalScore();
        var isNewRecord = finalRunIsNewRecord; // decided and saved in OnLevelComplete

        if (todaySale != null)
            todaySale.text = "\nCongrats!\nChapter 1 complete!";
        todayTip.text = "";
        UpdateUnlockedItemsDisplay();

        if (isNewRecord)
            totalEarned.text = $"New Record! ${totalScore:F2}";
        else
            totalEarned.text = $"Final Score: ${totalScore:F2}";

        Debug.Log($"🎉 All levels completed! Total score: {totalScore}");
        // The session was already completed in OnLevelComplete.
    }

    private void UpdateUnlockedItemsDisplay()
    {
        if (unlockedItemsText == null) return;

        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession())
        {
            unlockedItemsText.text = "";
            return;
        }

        // Get REMAINING items to unlock (total - purchased)
        var (remainingFood, remainingCharacters) = SessionManager.Instance.GetUnpurchasedItemsCount();

        unlockedItemsText.text = $"Unlocked Food: {remainingFood}\nUnlocked Customers: {remainingCharacters}";

        Debug.Log($"Level Complete: Remaining to unlock - Food: {remainingFood}, Customers: {remainingCharacters}");
    }

    private void SetupLevelCompleteButtons()
    {
        // Every day, the last one included, ends with Next (to the map) and Restart
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(true);
        if (levelCompleteRestartButton != null) levelCompleteRestartButton.gameObject.SetActive(true);
    }

    public void LoadNextLevel()
    {
        // Never carry a paused time scale into the Shop scene
        Time.timeScale = 1f;
        isPaused = false;

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopMusic();

        // After a day: finish board, then the map; the map's Play shows the ad, then the Shop (owner, 10 October)
        MapScreen.CameFromDay = true;
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionToScene(MapScreen.SceneName);
    }

    public void GoToMap()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopMusic();
        Time.timeScale = 1f;
        isPaused = false;
        MapScreen.CameFromDay = false;
        SceneTransitionManager.Instance.TransitionToScene(MapScreen.SceneName);
    }

    private void OnAllLevelsComplete()
    {
        Debug.Log("🎉 All levels completed! Session finished!");

        isLevelEnding = true; // no pause during the 3 s before the popup on this path either

        if (SessionManager.Instance != null)
        {
            finalRunIsNewRecord = SessionManager.Instance.CheckAndSaveHighScore();
            SessionManager.Instance.CompleteSession();
        }

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

        // Highlight the target while counting, then restore its original color
        var targetOriginalColor = targetText != null ? targetText.color : Color.white;
        if (targetText != null)
            targetText.color = Color.red;

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

            // Play sound every second
            if (elapsedTime >= nextSoundTime)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayMoneyCount();
                nextSoundTime += 1f;
            }

            yield return null;
        }

        // Final values
        if (sourceText != null)
            sourceText.text = $"{sourcePrefix}0";
        if (targetText != null)
        {
            targetText.text = $"{targetPrefix}{targetFinalAmount:F0}";
            targetText.color = targetOriginalColor;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMoneyTransferComplete();
    }
}