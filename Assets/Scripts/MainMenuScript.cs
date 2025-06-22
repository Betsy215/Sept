using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuScript : MonoBehaviour
{
    [Header("UI References")]

    public Button continueGameButton;
    public Button playButton; // Original play button (optional to keep)
    
    [Header("Game Scene")]
    public string gameSceneName = "GameScene"; // Name of your main game scene
    
    void Start()
    {
        SetupButtons();
        UpdateButtonStates();
    }
    
    void SetupButtons()
    {
        // Setup New Game Button
        if (playButton != null)
        {
            playButton.onClick.AddListener(StartNewGame);
        }
        
        // Setup Continue Game Button
        if (continueGameButton != null)
        {
            continueGameButton.onClick.AddListener(ContinueGame);
        }
        
        // Keep original play button functionality (starts new game)
        if (playButton != null)
        {
            playButton.onClick.AddListener(StartNewGame);
        }
    }
    
    void UpdateButtonStates()
    {
        // Check if there's an active session to continue
        bool hasActiveSession = SessionManager.Instance != null && SessionManager.Instance.HasActiveSession();
        
        // Enable/disable continue button based on session availability
        if (continueGameButton != null)
        {
            continueGameButton.interactable = hasActiveSession;
            
            // Optional: Change button appearance when disabled
            if (!hasActiveSession)
            {
                // You can change the button's color or text here
                Text buttonText = continueGameButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.color = Color.gray;
                }
            }
        }
        
        // Show session info if available
        if (hasActiveSession && SessionManager.Instance != null)
        {
            SessionData session = SessionManager.Instance.GetCurrentSession();
            Debug.Log($"Active session found - Level: {session.currentLevel + 1}, Total Score: {session.totalScore}");
        }
    }
    
    public void StartNewGame()
    {
        Debug.Log("Starting new game session...");
        
        // Create SessionManager if it doesn't exist
        if (SessionManager.Instance == null)
        {
            CreateSessionManager();
        }
        
        // Start a new session
        SessionManager.Instance.StartNewSession();
        
        // Load the game scene
        LoadGameScene();
    }
    
    public void ContinueGame()
    {
        Debug.Log("Continuing existing game session...");
        
        // Create SessionManager if it doesn't exist
        if (SessionManager.Instance == null)
        {
            CreateSessionManager();
        }
        
        // Continue the existing session
        SessionManager.Instance.ContinueSession();
        
        // Load the game scene
        LoadGameScene();
    }
    
    void CreateSessionManager()
    {
        // Create a SessionManager GameObject if one doesn't exist
        GameObject sessionManagerGO = new GameObject("SessionManager");
        sessionManagerGO.AddComponent<SessionManager>();
        
        Debug.Log("SessionManager created");
    }
    
    void LoadGameScene()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("Game scene name not set in MainMenuScript!");
        }
    }
    
    // Keep the original GotoScene method for other buttons
    public void GotoScene(string scene)
    {
        SceneManager.LoadScene(scene);
    }
    
    // Optional: Method to delete saved session (for testing or reset functionality)
    public void DeleteSavedSession()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.DeleteSession();
        }
        else
        {
            // Delete directly from PlayerPrefs if SessionManager doesn't exist
            PlayerPrefs.DeleteKey("FoodTruckSession");
        }
        
        UpdateButtonStates();
        Debug.Log("Saved session deleted");
    }
    
    // Called when the menu becomes active (useful if returning from game)
    void OnEnable()
    {
        UpdateButtonStates();
    }
}