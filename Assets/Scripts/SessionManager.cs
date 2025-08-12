using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SessionData
{
    public int totalScore;
    public int currentLevel;
    public int levelsCompleted;
    public DateTime sessionStartTime;
    public bool isActive;
    
    public List<string> purchasedFoodItems;
    public List<string> purchasedCharacters;
    
    public SessionData()
    {
        totalScore = 0;
        currentLevel = 0;
        levelsCompleted = 0;
        sessionStartTime = DateTime.Now;
        isActive = true;
        
        purchasedFoodItems = new List<string> { "Apple", "Bread" };
        purchasedCharacters = new List<string> { "Girl" };
    }
}

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }
    
    [Header("References (Auto-found in game scene)")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private LevelManager levelManager;
    
    // Session data
    private SessionData currentSession;
    private const string SESSION_SAVE_KEY = "FoodTruckSession";
    
    // Events for UI updates
    public System.Action<int> OnTotalScoreChanged;
    public System.Action OnSessionCompleted;
    
    void Awake()
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
            {
                Debug.Log($"📱 Loaded existing session - Score: {GetTotalScore()}, Level: {GetCurrentLevelIndex() + 1}");
            }
            else
            {
                Debug.Log("📱 No existing session found - ready for new session");
            }
        }
        else
        {
            Debug.Log("SessionManager already exists, destroying duplicate");
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Try to find references in current scene (useful when loading into game scene)
        FindGameReferences();
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
    
    void InitializeSession()
    {
        // Try to load existing session
        LoadSession();
        
        // If no session exists or session is completed, current session will be null
        if (currentSession == null || !currentSession.isActive)
        {
            Debug.Log("No active session found.");
        }
        else
        {
            Debug.Log($"Loaded existing session - Total Score: {currentSession.totalScore}, Current Level: {currentSession.currentLevel + 1}");
        }
    }
    
    public void StartNewSession()
    {
        currentSession = new SessionData();
        SaveSession();
        
        Debug.Log("New session started!");
        
        // Notify UI of score change
        OnTotalScoreChanged?.Invoke(currentSession.totalScore);
    }
    
    public void ContinueSession()
    {
        if (HasActiveSession())
        {
            Debug.Log($"Continuing session from level {currentSession.currentLevel + 1}");
            
            // Set the level manager to continue from the correct level (only if available)
            if (levelManager != null)
            {
                levelManager.SetCurrentLevel(currentSession.currentLevel);
            }
            
            // Notify UI of current total score
            OnTotalScoreChanged?.Invoke(currentSession.totalScore);
        }
        else
        {
            Debug.Log("No active session to continue, starting new session");
            StartNewSession();
        }
    }
    
    // NEW: Method to immediately add points to session total (called during gameplay)
    public void AddScoreImmediately(int points)
    {
        if (currentSession != null && currentSession.isActive)
        {
            currentSession.totalScore += points;
            SaveSession(); // Save immediately to persist progress
            
            Debug.Log($"Added {points} points immediately. New session total: {currentSession.totalScore}");
            
            // Notify UI of score change
            OnTotalScoreChanged?.Invoke(currentSession.totalScore);
        }
    }
    
    // UPDATED: Modified to avoid double-adding scores
    public void AddLevelScore(int levelScore)
    {
        // This method is now called only at level completion for summary/logging
        // The actual score addition happens immediately during gameplay via AddScoreImmediately()
        
        if (currentSession != null && currentSession.isActive)
        {
            // Don't add to total score here anymore - it's already been added immediately
            // Just save the session to ensure persistence
            SaveSession();
            
            Debug.Log($"Level completed with score: {levelScore}. Session total: {currentSession.totalScore}");
            
            // Still notify UI in case it needs updating
            OnTotalScoreChanged?.Invoke(currentSession.totalScore);
        }
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
    
    public int GetTotalScore()
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
    
    // NEW: Public method for ScoreManager to register itself
    public void RegisterScoreManager(ScoreManager manager)
    {
        scoreManager = manager;
        Debug.Log("ScoreManager registered with SessionManager");
    }
    
    void SaveSession()
    {
        if (currentSession != null)
        {
            string jsonData = JsonUtility.ToJson(currentSession);
            PlayerPrefs.SetString(SESSION_SAVE_KEY, jsonData);
            PlayerPrefs.Save();
            
            Debug.Log("Session saved");
        }
    }
    
    // UPDATED: Fixed to handle existing sessions without Bread
    void LoadSession()
    {
        if (PlayerPrefs.HasKey(SESSION_SAVE_KEY))
        {
            string jsonData = PlayerPrefs.GetString(SESSION_SAVE_KEY);
            try
            {
                currentSession = JsonUtility.FromJson<SessionData>(jsonData);
                Debug.Log("Session loaded successfully");
                
                // Handle legacy sessions that don't have purchased items
                if (currentSession.purchasedFoodItems == null)
                {
                    currentSession.purchasedFoodItems = new List<string> { "Apple", "Bread" };
                }
                else
                {
                    // NEW: Ensure Bread is in existing sessions
                    if (!currentSession.purchasedFoodItems.Contains("Bread"))
                    {
                        currentSession.purchasedFoodItems.Add("Bread");
                        Debug.Log("Added Bread to existing session");
                        SaveSession(); // Save the updated session
                    }
                }
                
                if (currentSession.purchasedCharacters == null)
                {
                    currentSession.purchasedCharacters = new List<string> { "Girl" };
                }
            }
            catch (System.Exception e)
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
    
    public void DeleteSession()
    {
        PlayerPrefs.DeleteKey(SESSION_SAVE_KEY);
        currentSession = null;
        Debug.Log("Session deleted");
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
    
        if (currentSession.totalScore >= amount)
        {
            currentSession.totalScore -= amount;
            SaveSession();
            OnTotalScoreChanged?.Invoke(currentSession.totalScore);
            Debug.Log($"Deducted {amount} points. New total: {currentSession.totalScore}");
            return true;
        }
    
        Debug.Log($"Cannot deduct {amount} points - insufficient score ({currentSession.totalScore})");
        return false;
    }
    
    // Called when the application is paused/closed
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveSession();
        }
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveSession();
        }
    }
}