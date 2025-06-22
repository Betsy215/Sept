using UnityEngine;
using System;

[System.Serializable]
public class SessionData
{
    public int totalScore;
    public int currentLevel;
    public int levelsCompleted;
    public DateTime sessionStartTime;
    public bool isActive;
    
    public SessionData()
    {
        totalScore = 0;
        currentLevel = 0;
        levelsCompleted = 0;
        sessionStartTime = DateTime.Now;
        isActive = true;
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
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSession();
        }
        else
        {
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
    
    public void AddLevelScore(int levelScore)
    {
        if (currentSession != null && currentSession.isActive)
        {
            currentSession.totalScore += levelScore;
            SaveSession();
            
            Debug.Log($"Added {levelScore} to session total. New total: {currentSession.totalScore}");
            
            // Notify UI of score change
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
    
    void LoadSession()
    {
        if (PlayerPrefs.HasKey(SESSION_SAVE_KEY))
        {
            string jsonData = PlayerPrefs.GetString(SESSION_SAVE_KEY);
            try
            {
                currentSession = JsonUtility.FromJson<SessionData>(jsonData);
                Debug.Log("Session loaded successfully");
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