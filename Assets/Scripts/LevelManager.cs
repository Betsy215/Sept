using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelManager : MonoBehaviour
{
    [Header("Level Data")]
    public LevelData[] allLevels;
    
    [Header("Game Components")]
    public OrderSystem orderSystem;
    public FoodTray[] foodTrays; // Array of all food trays
    public ServePlate servePlate;
    public ScoreManager scoreManager;
    
    [Header("Visual Elements")]
    public SpriteRenderer backgroundRenderer;
    public Camera mainCamera;
    
    [Header("UI Elements")]
    public Text levelNameText;
    
    [Header("Scene Management")]
    public string mainMenuSceneName = "MainMenu"; // Name of your main menu scene
    
    [Header("Level Complete UI")]
    public GameObject popupCanvas;
    public GameObject levelCompletePanel;
    public TextMeshProUGUI finalScoreText;
    public Button nextLevelButton;
    public Button mainMenuButton;
    
    // Current level tracking
    private int currentLevelIndex = 0;
    private LevelData currentLevelData;
    
    void Start()
    {
        LoadLevel(currentLevelIndex);
        SetupLevelCompleteUI();
    }
    
    void SetupLevelCompleteUI()
    {
        // Hide popup initially
        if (popupCanvas != null)
            popupCanvas.SetActive(false);
        
        // Set up next level button
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(LoadNextLevel);
        }
        
        // Set up main menu button (you can implement this later)
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
    
    void ApplyLevelSettings()
    {
        Debug.Log($"Loading {currentLevelData.levelName}");
        
        // Apply Order System settings
        if (orderSystem != null)
        {
            orderSystem.ordersPerLevel = currentLevelData.ordersPerLevel;
            orderSystem.orderDisplayTime = currentLevelData.orderDisplayTime;
            orderSystem.timeBetweenOrders = currentLevelData.timeBetweenOrders;
            orderSystem.minOrderItems = currentLevelData.minOrderItems;
            orderSystem.maxOrderItems = currentLevelData.maxOrderItems;
        }
        
        // Apply Food Tray settings with tray count management
        if (foodTrays != null)
        {
            ApplyTraySettings();
        }
        
        // Apply Serve Plate settings
        if (servePlate != null)
        {
            servePlate.maxCapacity = currentLevelData.plateMaxCapacity;
        }
        
        // Apply Score Manager settings
        if (scoreManager != null)
        {
            scoreManager.SetLevelSettings(
                currentLevelData.basePointsPerOrder,
                currentLevelData.perfectOrderBonus,
                currentLevelData.timeBonus
            );
        }
        
        // Apply visual settings
        ApplyVisualSettings();
        
        // Update UI
        if (levelNameText != null)
        {
            levelNameText.text = currentLevelData.levelName;
        }
    }
    
    void ApplyTraySettings()
    {
        int activeTrayCount = Mathf.Clamp(currentLevelData.activeTrayCount, 1, foodTrays.Length);
        
        Debug.Log($"Setting {activeTrayCount} active trays out of {foodTrays.Length} total trays");
        
        for (int i = 0; i < foodTrays.Length; i++)
        {
            if (foodTrays[i] != null)
            {
                // Determine if this tray should be active
                bool shouldBeActive = i < activeTrayCount;
                
                // Activate/deactivate the entire tray GameObject
                foodTrays[i].gameObject.SetActive(shouldBeActive);
                
                if (shouldBeActive)
                {
                    // Apply level settings to active trays
                    foodTrays[i].maxItems = currentLevelData.maxItemsPerTray;
                    
                    // Refill the tray for the new level
                    foodTrays[i].CompleteRefill();
                    
                    Debug.Log($"Tray {i} ({foodTrays[i].foodType}): ACTIVE");
                }
                else
                {
                    Debug.Log($"Tray {i}: INACTIVE");
                }
            }
        }
    }
    
    void ApplyVisualSettings()
    {
        // Apply background sprite
        if (backgroundRenderer != null && currentLevelData.backgroundSprite != null)
        {
            backgroundRenderer.sprite = currentLevelData.backgroundSprite;
        }
        
        // Apply background color
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = currentLevelData.backgroundColor;
        }
    }
    
    void StartLevel()
    {
        // Hide level complete popup
        if (popupCanvas != null)
        {
            popupCanvas.SetActive(false);
        }
        
        // Reset score for new level
        if (scoreManager != null)
        {
            scoreManager.ResetScore();
        }
        
        // Start the order system (it should restart automatically)
        if (orderSystem != null)
        {
            orderSystem.StopOrderSystem(); // Stop current cycle
            orderSystem.enabled = false;   // Disable and re-enable to restart
            orderSystem.enabled = true;
        }
    }
    
    // Called when current level is completed
    public void OnLevelComplete()
    {
        Debug.Log($"{currentLevelData.levelName} completed!");
        ShowLevelCompletePopup();
    }
    
    void ShowLevelCompletePopup()
    {
        // Show popup canvas
        if (popupCanvas != null)
        {
            popupCanvas.SetActive(true);
        }
        
        // Show level complete panel
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }
        
        // Update final score
        if (finalScoreText != null && scoreManager != null)
        {
            int finalScore = scoreManager.GetCurrentScore();
            finalScoreText.text = $"Final Score: {finalScore}";
        }
        
        // Setup buttons based on available levels
        SetupLevelCompleteButtons();
    }
    
    void SetupLevelCompleteButtons()
    {
        // Setup Next Level Button
        if (nextLevelButton != null)
        {
            bool hasMoreLevels = (currentLevelIndex + 1) < allLevels.Length;
            nextLevelButton.gameObject.SetActive(hasMoreLevels);
            
            Debug.Log($"Next Level Button: {(hasMoreLevels ? "Shown" : "Hidden")} - Current: {currentLevelIndex + 1}, Total: {allLevels.Length}");
        }
        
        // Main Menu button is always available
        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(true);
        }
    }
    
    public void LoadNextLevel()
    {
        LoadLevel(currentLevelIndex + 1);
    }
    
    public void RestartLevel()
    {
        LoadLevel(currentLevelIndex);
    }
    
    void OnAllLevelsComplete()
    {
        Debug.Log("🎉 All levels completed! Game finished!");
        ShowLevelCompletePopup(); // Still show the popup, but Next Level button will be hidden
    }
    
    void GoToMainMenu()
    {
        Debug.Log("Going to Main Menu...");
        
        // Load the main menu scene
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("Main Menu scene name not set in LevelManager!");
        }
    }
    
    // Public getters
    public LevelData GetCurrentLevelData()
    {
        return currentLevelData;
    }
    
    public int GetCurrentLevelNumber()
    {
        return currentLevelIndex + 1;
    }
    
    // NEW: Public method to get active tray count for current level
    public int GetActiveTrayCount()
    {
        return currentLevelData != null ? currentLevelData.activeTrayCount : foodTrays.Length;
    }
    
    // NEW: Public method to get active trays only
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