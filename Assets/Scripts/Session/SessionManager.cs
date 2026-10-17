using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[Serializable]
public class SessionData
{
    public float totalScore;
    public int currentLevel;
    public int levelsCompleted;
    public DateTime sessionStartTime;
    public bool isActive;

    public List<string> purchasedFoodItems;
    public List<string> purchasedCharacters;
    public List<string> purchasedDecor;        // table decorations (new field, 16 October; old saves get an empty list)
    // Menu book (owner, 14 October 2026): bought foods the player has switched on. A food is in play only when it is
    // bought AND on. Null in saves from before the book: LoadSession fills it with every bought food.
    public List<string> enabledFoodItems;

    // True once the player has watched (or skipped past) the Level 0 tutorial for this game
    public bool tutorialSeen;
    // JsonUtility cannot serialize a Dictionary, so upgrade levels are mirrored
    // into two parallel lists around every save and load (see Sync methods).
    [NonSerialized] public Dictionary<string, int> foodUpgradeLevels;
    public List<string> foodUpgradeKeys;
    public List<int> foodUpgradeValues;

    [Serializable]
    public class FoodItemPosition
    {
        public string foodType;
        public float x, y; // Only X and Y for 2D game

        public FoodItemPosition(string type, Vector3 position)
        {
            foodType = type;
            x = position.x;
            y = position.y;
        }

        public Vector3 ToVector3(float originalZ)
        {
            return new Vector3(x, y, originalZ);
        }
    }

    public List<FoodItemPosition> savedFoodPositions;
    // Where the player dragged each sticker on the menu paper (owner, 14 October 2026); null in older saves
    public List<FoodItemPosition> menuStickerPositions;

    // Map (10 October): best stars per day (index = day - 1) and the days whose one-time
    // three-star bonus has been paid. Missing (null) in sessions saved before the map.
    public List<int> levelStars;
    public List<int> starBonusPaidDays;

    public SessionData()
    {
        totalScore = 0;
        currentLevel = 0;
        levelsCompleted = 0;
        sessionStartTime = DateTime.Now;
        isActive = true;

        purchasedFoodItems = new List<string> { "Bread", "Coffee" };
        enabledFoodItems = new List<string> { "Bread", "Coffee" };
        foodUpgradeLevels = new Dictionary<string, int>()
        {
            { "Coffee", 1 },
            { "CoffeeMachine", 1 }
        };
        purchasedCharacters = new List<string> { "Girl", "Boy", "Kid" };
        purchasedDecor = new List<string>();
        savedFoodPositions = new List<FoodItemPosition>();
        levelStars = new List<int>();
        starBonusPaidDays = new List<int>();
        SyncUpgradesToLists();
    }

    /// Copy the upgrade dictionary into the serializable lists. Call before saving.
    public void SyncUpgradesToLists()
    {
        foodUpgradeKeys = new List<string>();
        foodUpgradeValues = new List<int>();
        if (foodUpgradeLevels == null) return;
        foreach (var kv in foodUpgradeLevels)
        {
            foodUpgradeKeys.Add(kv.Key);
            foodUpgradeValues.Add(kv.Value);
        }
    }

    /// Rebuild the upgrade dictionary from the serialized lists. Call after loading.
    public void SyncUpgradesFromLists()
    {
        foodUpgradeLevels = new Dictionary<string, int>();
        if (foodUpgradeKeys != null && foodUpgradeValues != null)
        {
            var n = Math.Min(foodUpgradeKeys.Count, foodUpgradeValues.Count);
            for (var i = 0; i < n; i++) foodUpgradeLevels[foodUpgradeKeys[i]] = foodUpgradeValues[i];
        }

        // Defaults for sessions saved before upgrades were persisted
        if (!foodUpgradeLevels.ContainsKey("Coffee")) foodUpgradeLevels["Coffee"] = 1;
        if (!foodUpgradeLevels.ContainsKey("CoffeeMachine")) foodUpgradeLevels["CoffeeMachine"] = 1;
    }
}

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [Header("References (Auto-found in game scene)")] [SerializeField]
    private ScoreManager scoreManager;

    [SerializeField] private LevelManager levelManager;

    [Header("Shop Configuration")] [Tooltip("Total number of food items available in the shop")]
    public int totalFoodItems = 7;   // Bread, Coffee, Juice, Apple, Choux, Cake, Cookies (back on 14 October)

    [Tooltip("Total number of characters available in the shop")]
    public int totalCharacters = 7; // Girl, Boy, Kid (default) + Grandma, Chef, Yoga, Businessman (shop)

    // Session data
    private SessionData currentSession;
    private const string SESSION_SAVE_KEY = "FoodTruckSession";
    private const string HIGH_SCORE_KEY = "FoodTruckHighScore";

    // Events for UI updates
    public Action<float> OnTotalScoreChanged;
    public Action OnSessionCompleted;
    public Action<string> OnFoodItemPurchased;

    private void Awake()
    {
        Debug.Log($"=== SessionManager Awake() in scene: {SceneManager.GetActiveScene().name} ===");

        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("✅ SessionManager created and set to DontDestroyOnLoad");

            // Auto-load any existing session
            InitializeSession();

            // If we loaded an active session, log it
            if (HasActiveSession())
                Debug.Log(
                    $"📱 Loaded existing session - Score: {GetTotalScore()}, Level: {GetCurrentLevelIndex() + 1}");
            else
                Debug.Log("📱 No existing session found - ready for new session");
        }
        else
        {
            Debug.Log("SessionManager already exists, destroying duplicate");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Try to find references in current scene (useful when loading into game scene)
        FindGameReferences();
    }

    private float scoreAtLevelStart = 0f;

    public void SnapshotScoreBeforeLevel()
    {
        scoreAtLevelStart = currentSession.totalScore;
        Debug.Log($"Score snapshot taken: {scoreAtLevelStart}");
    }

    public void RestoreScoreToSnapshot()
    {
        if (currentSession != null)
        {
            currentSession.totalScore = scoreAtLevelStart;
            SaveSession();
            OnTotalScoreChanged?.Invoke(currentSession.totalScore);
            Debug.Log($"Score restored to: {scoreAtLevelStart}");
        }
    }

    // NEW: Method to find game components when entering game scene
    public void FindGameReferences()
    {
        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager>();
            if (scoreManager != null)
                Debug.Log("ScoreManager reference found");
        }

        if (levelManager == null)
        {
            levelManager = FindObjectOfType<LevelManager>();
            if (levelManager != null)
                Debug.Log("LevelManager reference found");
        }
    }

    /// Chapter-only foods (owner, 16 October): a food listed here belongs to one chapter. It is in the shop only in
    /// that chapter and on the table only on that chapter's days; on another chapter's days it waits greyed in the
    /// menu's carousel and comes back by itself. A food not listed is common to every chapter. The apple is chapter
    /// 1's own, and so is the coffee (owner, 16 October); the coconut drink and the ice cream are chapter 2's. The sticker paper's colour says which
    /// (MenuBook.PaperColour). Cookies were held back on 13 October and are a plain chapter 1 buy since 14 October.
    public static readonly Dictionary<string, int> FoodChapter = new Dictionary<string, int>
    {
        { "Apple", 1 }, { "Coffee", 1 }, { "IceCream", 2 }   // Coconut is a chapter 2 unlock but plays in any chapter (UnlockChapter)
    };
    public static readonly string[] HeldBackFoods = { };   // kept for older callers; FoodChapter decides
    /// 0 for a common food, else the one chapter it belongs to.
    public static int ChapterOfFood(string foodType) =>
        foodType != null && FoodChapter.TryGetValue(foodType, out var chapter) ? chapter : 0;
    /// May this food be sold and played today? Common foods always; a chapter-only food on its own chapter's days.
    public static bool IsFoodInSeason(string foodType) { var c = ChapterOfFood(foodType); return c == 0 || c == CurrentChapter; }
    /// Not for sale and not on the table today (the shop and the kitchen hide it).
    public static bool IsHeldBack(string foodType) => foodType != null && !IsFoodInSeason(foodType);

    /// Foods that appear in the shop with a lock until the player has started their chapter (owner, 16 October):
    /// Cookies and Coconut open with chapter 2, and once bought they play in any chapter.
    public static readonly Dictionary<string, int> UnlockChapter = new Dictionary<string, int> { { "Cookies", 2 }, { "Coconut", 2 } };
    /// Chapters the player has reached: 1 plus one per 16 finished days, never past Chapter.Count
    public static int ChaptersStarted => Mathf.Clamp(1 + (Instance != null ? Instance.GetLevelsCompleted() : 0) / Chapter.Days, 1, Chapter.Count);
    public static bool IsLockedInShop(string foodType) => foodType != null && UnlockChapter.TryGetValue(foodType, out var c) && c > ChaptersStarted;

    /// Chapter of the day being played (always 1 while Chapter.Count is 1).
    public static int CurrentChapter =>
        Mathf.Clamp(Chapter.Of(Instance != null ? Instance.GetCurrentLevelIndex() : 0), 1, Chapter.Count);

    #region MENU BOOK (owner, 14 October 2026)

    /// Each food's station in the kitchen, and how many foods a station can have switched on at once.
    public static readonly Dictionary<string, string> FoodStation = new Dictionary<string, string>
    {
        { "Bread", "Oven" }, { "Cake", "Oven" }, { "Choux", "Oven" }, { "Cookies", "Oven" },
        { "Coffee", "Machine" }, { "Juice", "Counter" }, { "Apple", "Counter" }, { "Coconut", "Counter" }, { "IceCream", "Machine" }
    };
    public static readonly Dictionary<string, int> StationCap = new Dictionary<string, int> { { "Oven", 3 }, { "Machine", 1 }, { "Counter", 2 } };
    public const int MinEnabledFoods = 2;
    public const int MaxEnabledFoods = 9;   // the menu paper holds nine stickers (owner, 14 October)
    public static string StationOf(string foodType) => foodType != null && FoodStation.TryGetValue(foodType, out var st) ? st : "Other";
    public static int CapOf(string station) => StationCap.TryGetValue(station, out var cap) ? cap : 99;

    private List<string> EnabledList()
    {
        if (currentSession == null) return null;
        if (currentSession.enabledFoodItems == null)
            currentSession.enabledFoodItems = new List<string>(currentSession.purchasedFoodItems ?? new List<string>());
        return currentSession.enabledFoodItems;
    }

    /// Switched on in the menu book (bought or not). No session: everything is on.
    public bool IsFoodItemEnabled(string foodType)
    {
        if (IsFixedFood(foodType) && IsFoodItemPurchased(foodType)) return true;   // the chapter's fixed food is always on
        if (string.IsNullOrEmpty(foodType)) return true;
        var list = EnabledList(); return list == null || list.Contains(foodType);
    }

    /// The one rule: a food is in play (table, kitchen, orders) when it is bought and switched on.
    public bool IsFoodItemActive(string foodType) => IsFoodItemPurchased(foodType) && IsFoodItemEnabled(foodType) && IsFoodInSeason(foodType);

    /// Foods switched on at this station now (bought ones only).
    public int EnabledCountAtStation(string station)
    {
        var list = EnabledList(); if (list == null) return 0;
        var n = 0; foreach (var f in list) if (StationOf(f) == station && IsFoodItemPurchased(f) && IsFoodInSeason(f)) n++;
        return n;
    }

    /// Can this food be switched on now? reason = "" when yes, else why not (the station is full).
    public bool CanEnableFood(string foodType, out string reason)
    {
        reason = "";
        if (!IsFoodItemPurchased(foodType)) { reason = "not bought"; return false; }
        if (!IsFoodInSeason(foodType)) { reason = "not this chapter"; return false; }
        if (IsFoodItemEnabled(foodType)) return true;
        var st = StationOf(foodType);
        if (EnabledCountAtStation(st) >= CapOf(st)) { reason = $"{st} full"; return false; }
        var on = 0; foreach (var f in EnabledList() ?? new List<string>()) if (IsFoodItemPurchased(f) && IsFoodInSeason(f)) on++;
        if (on >= MaxEnabledFoods) { reason = "menu full"; return false; }
        return true;
    }

    /// Can this food be switched off now? At least MinEnabledFoods bought foods must stay on.
    /// A chapter's fixed food (owner, 16 October): always on the menu, cannot be taken off. Chapter 1 the coffee,
    /// chapter 2 the ice cream (once it exists).
    public static readonly Dictionary<int, string> FixedFood = new Dictionary<int, string> { { 1, "Coffee" }, { 2, "IceCream" } };
    public static bool IsFixedFood(string foodType) => foodType != null && FixedFood.TryGetValue(CurrentChapter, out var f) && f == foodType;

    public bool CanDisableFood(string foodType, out string reason)
    {
        reason = "";
        if (IsFixedFood(foodType)) { reason = "always on the menu this chapter"; return false; }
        if (!IsFoodItemEnabled(foodType)) return true;
        var on = 0; foreach (var f in EnabledList() ?? new List<string>()) if (IsFoodItemPurchased(f) && IsFoodInSeason(f)) on++;
        if (on <= MinEnabledFoods) { reason = $"keep at least {MinEnabledFoods} on"; return false; }
        return true;
    }

    /// Switch a bought food on or off. Returns false (and changes nothing) when the rules say no.
    public bool SetFoodEnabled(string foodType, bool on)
    {
        var list = EnabledList(); if (list == null || string.IsNullOrEmpty(foodType)) return false;
        string why;
        if (on) { if (!CanEnableFood(foodType, out why)) { Debug.Log($"Menu book: cannot switch on {foodType}: {why}"); return false; } if (!list.Contains(foodType)) list.Add(foodType); }
        else { if (!CanDisableFood(foodType, out why)) { Debug.Log($"Menu book: cannot switch off {foodType}: {why}"); return false; } list.Remove(foodType); }
        SaveSession();
        return true;
    }

    /// A sticker's spot on the menu paper, if the player has moved it.
    public bool TryGetMenuStickerSpot(string foodType, out Vector2 spot)
    {
        spot = Vector2.zero;
        var list = currentSession != null ? currentSession.menuStickerPositions : null;
        if (list == null) return false;
        foreach (var p in list) if (p.foodType == foodType) { spot = new Vector2(p.x, p.y); return true; }
        return false;
    }

    public void SetMenuStickerSpot(string foodType, Vector2 spot)
    {
        if (currentSession == null) return;
        if (currentSession.menuStickerPositions == null) currentSession.menuStickerPositions = new List<SessionData.FoodItemPosition>();
        var list = currentSession.menuStickerPositions;
        var p = list.Find(e => e.foodType == foodType);
        if (p == null) { p = new SessionData.FoodItemPosition(foodType, Vector3.zero); list.Add(p); }
        p.x = spot.x; p.y = spot.y;
        SaveSession();
    }

    #endregion

    public bool IsFoodItemPurchased(string foodType)
    {
        if (string.IsNullOrEmpty(foodType)) return true;
        if (IsHeldBack(foodType)) return false;
        if (IsFixedFood(foodType)) return true;                 // the chapter's fixed food needs no purchase (ice cream in chapter 2; 17 October)
        if (currentSession == null || currentSession.purchasedFoodItems == null) return true;
        return currentSession.purchasedFoodItems.Contains(foodType);
    }

    public void UpdateFoodPositions(ServeableItem[] foodItems)
    {
        if (currentSession == null) return;

        currentSession.savedFoodPositions.Clear();

        foreach (var item in foodItems)
            if (item != null && item.gameObject.activeInHierarchy)
            {
                var position = new SessionData.FoodItemPosition(
                    item.GetFoodType(),
                    item.transform.position
                );
                currentSession.savedFoodPositions.Add(position);
            }

        SaveSession(); // Persist to PlayerPrefs
        Debug.Log($"Updated session with {currentSession.savedFoodPositions.Count} food positions");
    }

    private void ApplySavedPositions(ServeableItem[] foodItems)
    {
        if (!HasSavedPositions())
        {
            Debug.Log("No saved positions to apply");
            return;
        }

        var appliedCount = 0;

        foreach (var item in foodItems)
            if (item != null && item.gameObject.activeInHierarchy)
            {
                var savedPos = currentSession.savedFoodPositions.Find(
                    p => p.foodType == item.GetFoodType()
                );

                if (savedPos != null)
                {
                    var originalZ = item.transform.position.z; // Preserve Z layer
                    item.transform.position = savedPos.ToVector3(originalZ);
                    appliedCount++;
                }
            }

        Debug.Log($"Applied {appliedCount} saved food positions (2D)");
    }

    public bool HasSavedPositions()
    {
        return currentSession != null &&
               currentSession.savedFoodPositions != null &&
               currentSession.savedFoodPositions.Count > 0;
    }


    private void InitializeSession()
    {
        // Try to load existing session
        LoadSession();

        // If no session exists or session is completed, current session will be null
        if (currentSession == null || !currentSession.isActive)
            Debug.Log("No active session found.");
        else
            Debug.Log(
                $"Loaded existing session - Total Score: {currentSession.totalScore}, Current Level: {currentSession.currentLevel + 1}");
    }

    public void StartNewSession()
    {
        currentSession = new SessionData();
        // Daily Treats collected on the menu start the new game off
        var pending = DailyTreats.TakePending();
        if (pending > 0) currentSession.totalScore = RoundToCents(currentSession.totalScore + pending);
        SaveSession();

        // Kitchen bakes and cooldowns live in static/persistent helpers; a new game starts them fresh
        KitchenTimerHelper.CancelAll();
        CooldownRegistry.ClearAll();

        Debug.Log("New session started!");

        // Notify UI of score change
        OnTotalScoreChanged?.Invoke(currentSession.totalScore);
    }

    public void ContinueSession()
    {
        if (HasActiveSession())
        {
            // Daily Treats collected on the menu
            var treats = DailyTreats.TakePending();
            if (treats > 0) { currentSession.totalScore = RoundToCents(currentSession.totalScore + treats); SaveSession(); }

            // Set the level manager to continue from the correct level (only if available)
            if (levelManager != null) levelManager.SetCurrentLevel(currentSession.currentLevel);

            // Notify UI of current total score
            OnTotalScoreChanged?.Invoke(currentSession.totalScore);
        }
        else
        {
            StartNewSession();
        }
    }

    public float GetHighScore()
    {
        return PlayerPrefs.GetFloat(HIGH_SCORE_KEY, 0f);
    }

    public bool IsNewHighScore(float currentScore)
    {
        return currentScore > GetHighScore();
    }

    public void SaveHighScore(float newHighScore)
    {
        PlayerPrefs.SetFloat(HIGH_SCORE_KEY, newHighScore);
        PlayerPrefs.Save();
        Debug.Log($"New high score saved: {newHighScore:F2}");
    }

    public bool CheckAndSaveHighScore()
    {
        var currentScore = GetTotalScore();
        var isNewRecord = IsNewHighScore(currentScore);

        if (isNewRecord) SaveHighScore(currentScore);

        return isNewRecord;
    }

    /// Adds coins to the active session. Returns false when there is no active
    /// session, so callers (e.g. the rewarded ad button) can tell the player.
    public bool AddScoreImmediately(float points)
    {
        if (currentSession == null || !currentSession.isActive) return false;

        currentSession.totalScore = RoundToCents(currentSession.totalScore + points);
        SaveSession();
        OnTotalScoreChanged?.Invoke(currentSession.totalScore);
        return true;
    }

    /// Money is a float; keep it at whole cents so "$50.00" really is 50.
    private static float RoundToCents(float value)
    {
        return Mathf.Round(value * 100f) / 100f;
    }

    public bool CanAfford(int amount)
    {
        return currentSession != null && currentSession.totalScore + 0.005f >= amount;
    }

    public (int unpurchasedFood, int unpurchasedCharacters) GetUnpurchasedItemsCount()
    {
        if (currentSession == null) return (0, 0);

        // Count purchased items
        var purchasedFoodCount = currentSession.purchasedFoodItems?.Count ?? 0;
        var purchasedCharacterCount = currentSession.purchasedCharacters?.Count ?? 0;

        // Calculate unpurchased counts
        var unpurchasedFood = Mathf.Max(0, totalFoodItems - purchasedFoodCount);
        var unpurchasedCharacters = Mathf.Max(0, totalCharacters - purchasedCharacterCount);

        return (unpurchasedFood, unpurchasedCharacters);
    }

    public void OnLevelCompleted(int levelIndex)
    {
        if (currentSession != null && currentSession.isActive)
        {
            currentSession.currentLevel = levelIndex + 1; // Next level to play
            // A replay of an earlier day must not move progress back
            currentSession.levelsCompleted = Mathf.Max(currentSession.levelsCompleted, levelIndex + 1);
            SaveSession();

            Debug.Log($"Level {levelIndex + 1} completed. Next level: {currentSession.currentLevel + 1}");
        }
    }

    /// Best stars earned on a day (0 when never played).
    public int GetLevelStars(int levelIndex)
    {
        var list = currentSession?.levelStars;
        return list != null && levelIndex >= 0 && levelIndex < list.Count ? list[levelIndex] : 0;
    }

    /// Records the stars of a finished day (keeps the best) and pays the one-time three-star bonus.
    /// Returns the bonus paid now (0 when not three stars, or already paid for that day).
    public int RecordLevelStars(int levelIndex, int stars, int threeStarBonus)
    {
        if (currentSession == null || !currentSession.isActive || levelIndex < 0) return 0;
        if (currentSession.levelStars == null) currentSession.levelStars = new List<int>();
        if (currentSession.starBonusPaidDays == null) currentSession.starBonusPaidDays = new List<int>();
        while (currentSession.levelStars.Count <= levelIndex) currentSession.levelStars.Add(0);
        currentSession.levelStars[levelIndex] = Mathf.Max(currentSession.levelStars[levelIndex], stars);
        var bonus = 0;
        if (stars >= 3 && !currentSession.starBonusPaidDays.Contains(levelIndex))
        {
            currentSession.starBonusPaidDays.Add(levelIndex);
            bonus = threeStarBonus;
            currentSession.totalScore = RoundToCents(currentSession.totalScore + bonus);
            OnTotalScoreChanged?.Invoke(currentSession.totalScore);
        }
        SaveSession();
        return bonus;
    }

    public int GetLevelsCompleted() => currentSession != null ? currentSession.levelsCompleted : 0;

    // Add this to SessionManager.cs
    public void SetCurrentLevel(int levelIndex)
    {
        if (currentSession != null && currentSession.isActive)
        {
            currentSession.currentLevel = levelIndex;
            SaveSession();
            Debug.Log($"Session current level set to {levelIndex}");
        }
    }

    public void CompleteSession()
    {
        if (currentSession != null)
        {
            currentSession.isActive = false;
            SaveSession();

            Debug.Log($"Session completed! Final score: {currentSession.totalScore}");

            // Notify that session is complete
            OnSessionCompleted?.Invoke();
        }
    }

    public bool HasActiveSession()
    {
        return currentSession != null && currentSession.isActive;
    }

    public void MarkTutorialSeen()
    {
        if (currentSession == null) return;
        currentSession.tutorialSeen = true;
        SaveSession();
    }

    public float GetTotalScore()
    {
        return currentSession != null ? currentSession.totalScore : 0;
    }

    public int GetCurrentLevelIndex()
    {
        return currentSession != null ? currentSession.currentLevel : 0;
    }

    public SessionData GetCurrentSession()
    {
        return currentSession;
    }

    // NEW: Public method for LevelManager to register itself
    public void RegisterLevelManager(LevelManager manager)
    {
        levelManager = manager;
        Debug.Log("LevelManager registered with SessionManager");
    }


    private void SaveSession()
    {
        if (currentSession != null)
        {
            currentSession.SyncUpgradesToLists();
            var jsonData = JsonUtility.ToJson(currentSession);
            PlayerPrefs.SetString(SESSION_SAVE_KEY, jsonData);
            PlayerPrefs.Save();

            Debug.Log("Session saved");
        }
    }

    private void LoadSession()
    {
        if (PlayerPrefs.HasKey(SESSION_SAVE_KEY))
        {
            var jsonData = PlayerPrefs.GetString(SESSION_SAVE_KEY);
            try
            {
                currentSession = JsonUtility.FromJson<SessionData>(jsonData);
                Debug.Log("Session loaded successfully");

                if (currentSession.purchasedCharacters == null)
                    currentSession.purchasedCharacters = new List<string> { "Girl", "Boy", "Kid" };
                if (currentSession.purchasedDecor == null) currentSession.purchasedDecor = new List<string>();

                currentSession.SyncUpgradesFromLists();
                if (currentSession.levelStars == null) currentSession.levelStars = new List<int>();
                if (currentSession.starBonusPaidDays == null) currentSession.starBonusPaidDays = new List<int>();
                if (currentSession.enabledFoodItems == null)   // a save from before the menu book: everything bought is on
                    currentSession.enabledFoodItems = new List<string>(currentSession.purchasedFoodItems ?? new List<string>());
                // NEW: Handle legacy sessions without saved positions
                if (currentSession.savedFoodPositions == null)
                {
                    currentSession.savedFoodPositions = new List<SessionData.FoodItemPosition>();
                    Debug.Log("Initialized savedFoodPositions for legacy session");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load session: {e.Message}");
                currentSession = null;
            }
        }
        else
        {
            currentSession = null;
        }
    }


    #region SHOP SYSTEM METHODS

    // Purchase a food item
    public bool PurchaseFoodItem(string foodType)
    {
        if (currentSession == null) return false;

        if (!currentSession.purchasedFoodItems.Contains(foodType))
        {
            currentSession.purchasedFoodItems.Add(foodType);
            var list = EnabledList(); string why;
            if (list != null && !list.Contains(foodType) && CanEnableFood(foodType, out why)) list.Add(foodType);   // on by default
            SaveSession();
            OnFoodItemPurchased?.Invoke(foodType);
            Debug.Log($"Purchased food item: {foodType}");
            return true;
        }

        Debug.Log($"Food item already purchased: {foodType}");
        return false;
    }

    // Purchase a character
    /// Decor bought in the shop (the flower pots). Each pot bought raises every customer's patience by 15 percent.
    public const float PatiencePerDecor = 0.15f;

    public bool IsDecorPurchased(string decorName)
    {
        var s = GetCurrentSession(); return s != null && s.purchasedDecor != null && s.purchasedDecor.Contains(decorName);
    }

    public bool PurchaseDecor(string decorName)
    {
        if (currentSession == null) return false;
        if (currentSession.purchasedDecor == null) currentSession.purchasedDecor = new List<string>();
        if (currentSession.purchasedDecor.Contains(decorName)) return false;
        currentSession.purchasedDecor.Add(decorName);
        SaveSession();
        Debug.Log($"Purchased decor: {decorName}");
        return true;
    }

    /// 1.0 with no decor, 1.15 with one pot, 1.30 with two: multiplies the order time of every customer
    public float PatienceMultiplier
    {
        get { var s = GetCurrentSession(); var n = s != null && s.purchasedDecor != null ? s.purchasedDecor.Count : 0; return 1f + PatiencePerDecor * n; }
    }

    public bool PurchaseCharacter(string characterName)
    {
        if (currentSession == null) return false;

        if (!currentSession.purchasedCharacters.Contains(characterName))
        {
            currentSession.purchasedCharacters.Add(characterName);
            SaveSession();
            Debug.Log($"Purchased character: {characterName}");
            return true;
        }

        Debug.Log($"Character already purchased: {characterName}");
        return false;
    }

    #endregion

    // Deduct score for purchases
    public bool DeductScore(int amount)
    {
        if (currentSession == null) return false;

        if (CanAfford(amount))
        {
            currentSession.totalScore = RoundToCents(currentSession.totalScore - amount);
            SaveSession();
            OnTotalScoreChanged?.Invoke(currentSession.totalScore);
            Debug.Log($"Deducted {amount} points. New total: {currentSession.totalScore}");
            return true;
        }

        Debug.Log($"Cannot deduct {amount} points - insufficient score ({currentSession.totalScore})");
        return false;
    }

    // ADD these methods to SessionManager class:

    public bool UpgradeFood(string foodType, int cost = 0)
    {
        if (currentSession == null) return false;


        if (cost > 0 && !DeductScore(cost)) return false;

        if (!currentSession.foodUpgradeLevels.ContainsKey(foodType))
            currentSession.foodUpgradeLevels[foodType] = 1;

        currentSession.foodUpgradeLevels[foodType]++;
        SaveSession();

        Debug.Log($"Upgraded {foodType} to level {currentSession.foodUpgradeLevels[foodType]}");
        return true;
    }

    public int GetFoodUpgradeLevel(string foodType)
    {
        if (currentSession?.foodUpgradeLevels?.ContainsKey(foodType) == true)
            return currentSession.foodUpgradeLevels[foodType];
        return 1;
    }

    public Dictionary<string, int> GetAllFoodUpgradeLevels()
    {
        return currentSession?.foodUpgradeLevels ?? new Dictionary<string, int>();
    }

    public bool CanUpgradeFood(string foodType, int maxLevel = 3)
    {
        var currentLevel = GetFoodUpgradeLevel(foodType);
        return currentLevel < maxLevel;
    }

    // Called when the application is paused/closed
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) SaveSession();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) SaveSession();
    }
}