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
    private int LastDayIndex => Mathf.Min(allLevels.Length, Chapter.TotalDays) - 1;

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
        CandyJar.Ensure();                                                              // the candy jar on the table's back band (owner, 26 October 2026)
        if (CandyGift.Due) CandyGift.HideJarForShow(FindObjectOfType<CandyJar>());     // day 3: it arrives when the day begins (24 November 2026)
        if (currentLevelIndex == 0 && TutorialDirector.IsDue(SessionManager.Instance))
            gameObject.AddComponent<TutorialDirector>().Begin(this, gamePhaseManager, orderSystem, customerManager);
        else if (TutorialDirector.IsDue2(SessionManager.Instance))   // chapter 2, first day: the waffle machine, the blender, serving an ice cream (owner, 26 October 2026)
            gameObject.AddComponent<TutorialDirector>().Begin2(this, gamePhaseManager, orderSystem, customerManager);

        // The menu book during the arrangement phase, on every normal day (owner, 14 October)
        if (!(currentLevelIndex == 0 && TutorialDirector.IsDue(SessionManager.Instance)) && !TutorialDirector.IsDue2(SessionManager.Instance) && gamePhaseManager != null)
            gameObject.AddComponent<MenuBook>().Begin(this, gamePhaseManager);

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
        KitchenClock.Pause();   // ovens and refills stop too (20 November 2026)

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
        Time.timeScale = 1f; KitchenClock.Resume();

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
        if (!isLevelEnding) AnalyticsLog.DayLeft(currentLevelIndex, "restart");
        if (scoreManager != null) restartPayFactor = scoreManager.payFactor;

        // Universal restart method - handles both pause and level complete cases
        Time.timeScale = 1f; KitchenClock.Resume(); KitchenDay.End();   // a restarted day starts with a clean kitchen
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
                : SessionManager.Instance != null && SessionManager.Instance.IsDayDone(levelIndex) ? ReplayPayFactor : 1f;
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
        StartCoroutine(SnapBackRow());    // every back-row bottom on the flower pot's line (owner, 9 November)
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
                    serveableItems[i].gameObject.SetActive(!SessionManager.IsHeldBack(serveableItems[i].GetFoodType()));

            // Set default upgrade levels for ALL IUpgradeable objects
            SetDefaultUpgradeLevels();
            return;
        }

        // Get purchase status AND apply upgrade levels
        var activatedCount = 0;

        // Activate only purchased ServeableItems
        for (var i = 0; i < serveableItems.Length; i++)
            if (serveableItems[i] != null)
            {
                var itemFoodType = serveableItems[i].GetFoodType();
                var shouldBeActive = SessionManager.Instance.IsFoodItemActive(itemFoodType);   // bought and switched on in the menu book
                serveableItems[i].gameObject.SetActive(shouldBeActive);

                if (shouldBeActive) activatedCount++;
            }

        // The coffee machine goes with Coffee: off in the menu book, off the table (owner, 14 October)
        var coffeeOn = SessionManager.Instance.IsFoodItemActive("Coffee");
        foreach (var machine in FindObjectsOfType<CoffeeMachine>(true))
            if (machine.gameObject.activeSelf != coffeeOn) machine.gameObject.SetActive(coffeeOn);

        // Apply upgrade levels to ALL IUpgradeable objects (unified approach)
        ApplyUpgradeLevels();

        Debug.Log(
            $"Level {currentLevelIndex + 1}: Activated {activatedCount} serveable items with upgrade levels applied");
    }

    /// The menu book switched a food on or off during the arrangement: show or hide it on the table and tell the
    /// order system and the drag cache. Saved positions stay.
    public void ReapplyServeableItems()
    {
        ApplyServeableItemSettings();
        if (gamePhaseManager != null)
        {
            gamePhaseManager.SendMessage("RefreshDraggableCache", SendMessageOptions.DontRequireReceiver);
            // a food switched on now must be draggable now, not from tomorrow (21 October): dragging is only switched on
            // when the arrangement starts, so a newly shown item had no drag, no wiggle and no overlap check
            if (gamePhaseManager.GetCurrentPhase() == GamePhase.ARRANGEMENT) gamePhaseManager.SendMessage("EnableArrangementMode", SendMessageOptions.DontRequireReceiver);
        }
        if (orderSystem != null) orderSystem.SendMessage("UpdateActiveFoodTypes", SendMessageOptions.DontRequireReceiver);
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
        // Chapter art (16 October): chapter 2 days show the beach behind the table (Resources/Chapter2/bg_day); chapter 1 keeps the scene's own
        var dayArt = Chapter.Art<Sprite>(Chapter.Of(currentLevelIndex), "bg_day");
        if (dayArt != null)
        {
            var canvas = GameObject.Find("Canvas"); var bg = canvas != null ? canvas.transform.Find("bg") : null;
            var img = bg != null ? bg.GetComponent<Image>() : null;
            if (img != null) img.sprite = dayArt;
        }
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
            levelInfoText.text = $"Day {Chapter.DayInChapter(currentLevelIndex)}";   // counted inside its chapter since 23 November 2026;   // the day by its place, like the map (26 October 2026: chapter 2's first day is Day 13; its asset is still named Day 17)
    }

    private void StartLevel()
    {
        SessionManager.Instance.SnapshotScoreBeforeLevel();
        gamePhaseManager.StartArrangementPhase();
        scoreManager.ResetScore();
        if (!gamePhaseManager.SkipsArrangement) StartArrangementMusic();   // day 1 goes straight to play and its music (owner, 31 October 2026)
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
        dayClockDisabled = (currentLevelIndex == 0 && TutorialDirector.IsDue(SessionManager.Instance)) || TutorialDirector.IsDue2(SessionManager.Instance);   // the demos have no end of day
        if (!dayClockDisabled) YogaHire.StartDay(this, orderSystem);   // the hired yoga lady, when bought in the star shop

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

        // Stars for the map, and the one-time three-star bonus (the exact count of the day, no longer the running view)
        if (scoreManager != null && scoreManager.starProgressBar != null) scoreManager.starProgressBar.CloseDay();
        var stars = scoreManager != null && scoreManager.starProgressBar != null ? scoreManager.starProgressBar.GetEarnedStarsCount() : 0;
        threeStarBonusPaid = SessionManager.Instance.RecordLevelStars(currentLevelIndex, stars, ThreeStarBonus);
        AnalyticsLog.DayFinished(currentLevelIndex, stars, levelScore);

        // Last day of the chapter: record the high score. The session stays active so every day
        // can still be replayed from the map (it used to end the game here).
        if (currentLevelIndex >= LastDayIndex || Chapter.IsLastDay(currentLevelIndex))
            finalRunIsNewRecord = SessionManager.Instance.CheckAndSaveHighScore();

        isLevelEnding = true;
        KitchenDay.End();   // the kitchen stops with the day (23 November 2026)
        var totalAfter = SessionManager.Instance.GetTotalScore();

        customerManager.enabled = false;
        orderSystem.enabled = false;
        StartCoroutine(ShowLevelCompletePopup());
    }

    private IEnumerator ShowLevelCompletePopup()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(3f);
        KitchenDay.End();   // an oven tapped in these three seconds stops with the rest (the kitchen stays open until the card shows)

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayLevelCompleteMusic();

        if (popupCanvas != null)
            popupCanvas.SetActive(true);

        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(true);
        StyleDayEnd();

        // GetCurrentScore() already includes tips, so split it: order sale + tips = level score.
        // Each amount is transferred into "Earned" exactly once.
        // on a replay the wallet got only payFactor of today's money (ScoreManager), so the board shows that share
        var pay = scoreManager.payFactor;
        var tips = scoreManager.GetTotalTipsEarned() * pay;
        var todayScore = scoreManager.GetCurrentScore() * pay;
        var orderSale = Mathf.Max(0f, todayScore - tips);
        var totalScore = SessionManager.Instance != null ? SessionManager.Instance.GetTotalScore() : todayScore;
        var earnedBeforeLevel = Mathf.Max(0f, totalScore - todayScore - threeStarBonusPaid);
        var earnedAfterSale = totalScore - tips - threeStarBonusPaid;

        SetMoneyLine(todayTip, "Tips Earned:", $"$ {tips:F2}");
        SetMoneyLine(todaySale, "Today Sale:", $"$ {orderSale:F2}");
        SetMoneyLine(totalEarned, "Earned:", $"$ {earnedBeforeLevel:F2}");

        SetupLevelCompleteButtons();
        UpdateUnlockedItemsDisplay();
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(AnimateFullTransfer(todaySale, totalEarned, "Order Sale:", "Earned:",
            orderSale, earnedAfterSale));
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(AnimateFullTransfer(todayTip, totalEarned, "Tips:", "Earned:", tips,
            totalScore - threeStarBonusPaid));
        if (threeStarBonusPaid > 0) SetMoneyLine(totalEarned, "Earned:", $"$ {totalScore:F2}");

        var isLastLevel = currentLevelIndex >= LastDayIndex || Chapter.IsLastDay(currentLevelIndex);
        if (isLastLevel)
        {
            yield return new WaitForSeconds(0.5f);
            ShowFinalCompletionMessage();
        }
    }

    private void ShowFinalCompletionMessage()
    {
        var totalScore = SessionManager.Instance.GetTotalScore();
        var isNewRecord = finalRunIsNewRecord; // decided and saved in OnLevelComplete

        if (todaySale != null)
            todaySale.text = $"\nCongrats!\nChapter {Chapter.Of(currentLevelIndex)} complete!";
        todayTip.text = "";
        UpdateUnlockedItemsDisplay();

        if (isNewRecord)
            totalEarned.text = $"New Record! ${totalScore:F2}";
        else
            totalEarned.text = $"Final Score: ${totalScore:F2}";
        // on the journal page the total is written on the pink ribbon: cream, and small enough to stay on it
        totalEarned.text = "<color=#FFF8EC>" + totalEarned.text + "</color>";
        totalEarned.enableAutoSizing = true; totalEarned.fontSizeMax = 60f; totalEarned.fontSizeMin = 30f;

        Debug.Log($"🎉 All levels completed! Total score: {totalScore}");
        // The session was already completed in OnLevelComplete.
    }

    /// The bottom of the level-complete card shows the day's stars (owner, 13 October: no unlock counts, no bonus text).
    /// Three dark stars; the earned ones pop in over them one by one (owner, 14 November 2026: "The stars on this panel
    /// is flat, I want to make it more 3d looking, and show an animation of stars popping appearing one by one").
    /// The pictures are the game's own cookie stars, the same as under the hen and on the map (owner, same evening:
    /// "just use styles we already use elsewhere, it's cuter and cookie looking"); a painted pair of mine was dropped.
    private Coroutine starsPop;

    private void UpdateUnlockedItemsDisplay()
    {
        if (unlockedItemsText == null) return;
        unlockedItemsText.text = "";
        var stars = scoreManager != null && scoreManager.starProgressBar != null ? scoreManager.starProgressBar.GetEarnedStarsCount() : 0;
        var host = unlockedItemsText.rectTransform;
        var row = host.Find("StarsRow");
        if (row == null)
        {
            var bar = scoreManager != null ? scoreManager.starProgressBar : null;
            var gold = bar != null && bar.starFilledSprite != null ? bar.starFilledSprite : Resources.Load<Sprite>("UI/star_icon");
            var empty = bar != null ? bar.starUnfilledSprite : null;
            row = new GameObject("StarsRow", typeof(RectTransform)).transform;
            row.SetParent(host, false);
            var rrt = (RectTransform)row;
            rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 0.5f); rrt.pivot = new Vector2(0.5f, 0.5f);
            rrt.anchoredPosition = Vector2.zero; rrt.sizeDelta = new Vector2(500f, 130f);
            for (var k = 0; k < 3; k++)
            {
                var size = k == 1 ? 130f : 110f;
                var s = new GameObject("Star" + k, typeof(RectTransform), typeof(UnityEngine.UI.Image));
                s.transform.SetParent(row, false);
                var srt = (RectTransform)s.transform;
                srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
                srt.anchoredPosition = new Vector2((k - 1) * 150f, k == 1 ? 10f : 0f);
                srt.sizeDelta = new Vector2(size, size);
                var img = s.GetComponent<UnityEngine.UI.Image>();
                img.sprite = empty != null ? empty : MoneyFlipClock.SharedStarSprite(); img.raycastTarget = false; img.preserveAspect = true;
                if (empty == null) img.color = new Color(0.8f, 0.76f, 0.7f);

                var g = new GameObject("Gold", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                g.transform.SetParent(s.transform, false);
                var grt = (RectTransform)g.transform;
                grt.anchorMin = Vector2.zero; grt.anchorMax = Vector2.one; grt.offsetMin = grt.offsetMax = Vector2.zero;
                var gi = g.GetComponent<UnityEngine.UI.Image>();
                gi.sprite = gold != null ? gold : MoneyFlipClock.SharedStarSprite(); gi.raycastTarget = false; gi.preserveAspect = true;
                if (gold == null) gi.color = new Color(1f, 0.78f, 0.3f);
                g.transform.localScale = Vector3.zero;   // waits for its pop
            }
        }
        if (starsPop == null) starsPop = StartCoroutine(PopStars(row, stars));
        ShowServedCountForTesters(host);
        Debug.Log($"Level Complete: {stars} stars shown on the card");
    }

    /// Test builds only (editor and TestFlight, never the store): what the star rule counted, under the stars. The owner
    /// asked for logs after a day that ended on one star with "only one order missed" (14 November 2026); the phone
    /// keeps none, so the receipt says it: customers served whole, and of the lost, how many left with part.
    private void ShowServedCountForTesters(RectTransform host)
    {
        if (!(Application.isEditor || AdsInitializer.IsTestFlightBuild()) || orderSystem == null) return;
        var t = host.Find("ServedCount");
        TMPro.TextMeshProUGUI label;
        if (t == null)
        {
            var go = new GameObject("ServedCount", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            go.transform.SetParent(host, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -565f); rt.sizeDelta = new Vector2(820f, 110f);   // on the journal page: between the ribbon and the stickers
            label = go.GetComponent<TMPro.TextMeshProUGUI>();
            label.font = unlockedItemsText.font; label.fontSize = 30f; label.alignment = TMPro.TextAlignmentOptions.Top;
            label.color = new Color(0.36f, 0.3f, 0.26f); label.raycastTarget = false; label.enableWordWrapping = true;
        }
        else label = t.GetComponent<TMPro.TextMeshProUGUI>();
        int served = orderSystem.CustomersServed, lost = orderSystem.CustomersLost, partly = orderSystem.CustomersLostPartly;
        label.text = $"Test build: served {served} of {served + lost}" + (lost > 0 ? $"\nleft with part: {partly}, with nothing: {lost - partly}" : "");
    }

    /// The earned stars pop into their sockets one after the other: grow past full size, settle, with a small turn.
    private IEnumerator PopStars(Transform row, int stars)
    {
        yield return new WaitForSeconds(0.45f);
        var pop = Resources.Load<AudioClip>("Audio/pop");
        for (var k = 0; k < stars && k < row.childCount; k++)
        {
            var g = row.GetChild(k).Find("Gold");
            if (g == null) continue;
            if (StarCelebration.Current == StarCelebration.Style.Classic)
            {
                if (pop != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(pop);
                Haptics.Tap();
            }
            else StarCelebration.OnStar(this, row.GetChild(k) as RectTransform, k, stars);   // its own sound, tap and burst (24 November 2026)
            const float grow = 0.2f, settle = 0.14f;
            for (var t = 0f; t < grow + settle; t += Time.deltaTime)
            {
                var size = t < grow ? Mathf.SmoothStep(0f, 1.28f, t / grow) : Mathf.Lerp(1.28f, 1f, (t - grow) / settle);
                g.localScale = Vector3.one * size;
                g.localRotation = Quaternion.Euler(0f, 0f, -28f * (1f - Mathf.Clamp01(t / grow)));
                yield return null;
            }
            g.localScale = Vector3.one; g.localRotation = Quaternion.identity;
            yield return new WaitForSeconds(0.16f);
        }
        yield return StarCelebration.Finale(this, row as RectTransform, Mathf.Min(stars, row.childCount));
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
        Time.timeScale = 1f; KitchenClock.Resume(); KitchenDay.End();   // nothing baked after the day's end follows into the shop
        isPaused = false;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
            // The finish paper turns over (owner, 14 October); AudioManager lives across scenes, so the sound plays out
            AudioManager.Instance.PlaySFX(Resources.Load<AudioClip>("Audio/paper_flip"));
        }

        // After a day: the finish paper, the ad, the Shop, and from the Shop the map (owner, 19 November 2026: "let's
        // show shop before the level map after a level is finished"; it was paper, map, ad, Shop since 10 October)
        MapScreen.CameFromDay = false;
        System.Action toShop = () => { if (SceneTransitionManager.Instance != null) SceneTransitionManager.Instance.TransitionToScene("Shop"); };
        toShop();   // the between-days ad moved to the day's start, after Play on the day card (owner, 24 November 2026)
    }

    public void GoToMap()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopMusic();
        Time.timeScale = 1f; KitchenClock.Resume(); KitchenDay.End();   // the pause menu's Home leaves the day: the kitchen stops too (scan, 23 November 2026)
        isPaused = false;
        MapScreen.CameFromDay = false;
        SceneTransitionManager.Instance.TransitionToScene(MapScreen.SceneName);
    }

    private void OnAllLevelsComplete()
    {
        Debug.Log("🎉 All levels completed! Session finished!");

        isLevelEnding = true; // no pause during the 3 s before the popup on this path either
        KitchenDay.End();

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
        if (!isLevelEnding) AnalyticsLog.DayLeft(currentLevelIndex, "menu");

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopMusic();

        // IMPORTANT: Ensure time is restored and pause state cleared before scene change
        Time.timeScale = 1f; KitchenClock.Resume(); KitchenDay.End();
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

    /// A money line of the day-end note, centred: "Order Sale: $ 85" on one line while it fits the label, else the
    /// money on its own line under the words, so a long number never runs off the note (owner, 16 October).

    public static void SetMoneyLine(TextMeshProUGUI label, string words, string money)
    {
        if (label == null) return;
        // The day-end panel is a journal page since 19 November 2026 (owner's pick 5): a row has its words on the left
        // in the hand font and its money on the right; the total is written in cream on the pink ribbon. (Both on one
        // line: the first part's line gets no height, the second is set to the right over it.)
        label.alignment = TextAlignmentOptions.Center; label.enableWordWrapping = false; label.overflowMode = TextOverflowModes.Overflow; label.richText = true;
        var w = (words ?? "").Trim().TrimEnd(':');
        if (label.name == "TotalEarned")
            label.text = "<color=#FFF8EC><align=left>" + w.ToUpperInvariant() + "<line-height=0>\n<align=right>" + money + "<line-height=1em></color>";
        else
        {
            var shown = w.StartsWith("Tips") ? "Tips" : w.EndsWith("Sale") ? "Order sale" : w;
            label.text = "<align=left><font=\"Delius SDF\">" + shown + "</font><line-height=0>\n<align=right>" + money + "<line-height=1em>";
        }
    }

    private static readonly Color DayEndInk = new Color(0.36f, 0.20f, 0.09f);
    private bool dayEndStyled;

    /// The day-end panel laid out as the journal page of Assets/Images/paper.png (Store/ui/dayend_2026-11-19/build5.py):
    /// a title, the stars under it, the two rows, the total on the ribbon painted in the paper. Once per day.
    private void StyleDayEnd()
    {
        if (dayEndStyled || levelCompletePanel == null) return;
        dayEndStyled = true;
        var plain = Resources.Load<Material>("Fonts & Materials/beachday SDF - Plain");
        System.Action<TextMeshProUGUI, float, float, float> row = (l, y, size, width) =>
        {
            if (l == null) return;
            var rt = l.rectTransform; rt.anchoredPosition = new Vector2(-14f, y); rt.sizeDelta = new Vector2(width, 120f);
            l.fontSize = size; l.enableAutoSizing = false; l.color = DayEndInk; l.margin = Vector4.zero;
            if (plain != null) l.fontSharedMaterial = plain;   // brown ink, no rim
        };
        row(todaySale, 352f, 58f, 640f); row(todayTip, 242f, 58f, 640f); row(totalEarned, 60f, 60f, 540f);
        if (totalEarned != null) totalEarned.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -2f);   // the ribbon's tilt
        if (unlockedItemsText != null) unlockedItemsText.rectTransform.anchoredPosition = new Vector2(-14f, 540f);   // the stars, under the title
        var go = new GameObject("DayTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(levelCompletePanel.transform, false);
        var trt = (RectTransform)go.transform; trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f); trt.anchoredPosition = new Vector2(-14f, 706f); trt.sizeDelta = new Vector2(780f, 110f);
        var title = go.GetComponent<TextMeshProUGUI>();
        if (totalEarned != null) title.font = totalEarned.font; if (plain != null) title.fontSharedMaterial = plain;
        title.fontSize = 80f; title.color = DayEndInk; title.alignment = TextAlignmentOptions.Center; title.enableWordWrapping = false; title.raycastTarget = false;
        title.text = "DAY " + Chapter.DayInChapter(currentLevelIndex) + " DONE!";   // the day inside its chapter (23 November 2026)
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
        SetMoneyLine(sourceText, sourcePrefix, $"$ {sourceStart:F0}");
        SetMoneyLine(targetText, targetPrefix, $"$ {targetStart:F0}");

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
            SetMoneyLine(sourceText, sourcePrefix, $"$ {currentSource:F0}");
            SetMoneyLine(targetText, targetPrefix, $"$ {currentTarget:F0}");

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
        SetMoneyLine(sourceText, sourcePrefix, "$ 0");
        if (targetText != null)
        {
            SetMoneyLine(targetText, targetPrefix, $"$ {targetFinalAmount:F0}");
            targetText.color = targetOriginalColor;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMoneyTransferComplete();
    }

    /// The back row is measured from real renderers and the kitchen button's canvas rect; at Start the canvas scaler has
    /// not run yet, so the first snap landed the truck 20 % of its height too low (round 1, 10 November). Snap after the
    /// first frame and once more half a second later, when every builder has settled.
    private System.Collections.IEnumerator SnapBackRow()
    {
        yield return new WaitForEndOfFrame();
        TableBackRowAlign.Snap();
        yield return new WaitForSeconds(0.5f);
        TableBackRowAlign.Snap();
    }
}
