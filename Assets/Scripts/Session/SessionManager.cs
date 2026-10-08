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

    public SessionData()
    {
        totalScore = 0;
        currentLevel = 0;
        levelsCompleted = 0;
        sessionStartTime = DateTime.Now;
        isActive = true;

        purchasedFoodItems = new List<string> { "Bread", "Coffee" };
        foodUpgradeLevels = new Dictionary<string, int>()
        {
            { "Coffee", 1 },
            { "CoffeeMachine", 1 }
        };
        purchasedCharacters = new List<string> { "Girl" };
        savedFoodPositions = new List<FoodItemPosition>();
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
    public int totalFoodItems = 4;

    [Tooltip("Total number of characters available in the shop")]
    public int totalCharacters = 2;

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

    public bool IsFoodItemPurchased(string foodType)
    {
        if (string.IsNullOrEmpty(foodType)) return true;
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
            currentSession.levelsCompleted = levelIndex + 1; // Levels actually completed
            SaveSession();

            Debug.Log($"Level {levelIndex + 1} completed. Next level: {currentSession.currentLevel + 1}");
        }
    }

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
                    currentSession.purchasedCharacters = new List<string> { "Girl" };

                currentSession.SyncUpgradesFromLists();
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
            SaveSession();
            OnFoodItemPurchased?.Invoke(foodType);
            Debug.Log($"Purchased food item: {foodType}");
            return true;
        }

        Debug.Log($"Food item already purchased: {foodType}");
        return false;
    }

    // Purchase a character
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