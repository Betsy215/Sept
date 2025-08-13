using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI inGameScoreText; // For in-game score display
    public TextMeshProUGUI finalScoreText; // For level complete panel
    public TextMeshProUGUI totalScoreText; // Session total score
    public TextMeshProUGUI comboText; // For combo display
    public TextMeshProUGUI feedbackText; // For showing "+10 points!" etc.
    
    [Header("UI Overlay Elements")]
    public TextMeshProUGUI overlayScoreText;  // Your new overlay score text
    public TextMeshProUGUI overlayComboText;  // For combo display in overlay
    
    [Header("Game References")]
    public OrderSystem orderSystem; // Reference to order system
    public LevelManager levelManager; // Reference to level manager
    public StarProgressBar starProgressBar;
    
    [Header("Level Settings - Set by LevelManager")]
    [SerializeField] private int basePointsPerOrder = 100;
    [SerializeField] private int perfectOrderBonus = 50;
    [SerializeField] private int timeBonus = 10;
    
    [Header("Per-Item Scoring Settings")]
    public int pointsPerItem = 10; // Points for each correct item served
    public int orderCompletionBonus = 50; // Bonus for completing an order
    public int timeBonusMultiplier = 5; // Points per second remaining when order completed
    
    [Header("Item-Specific Points")]
    public ItemPointValues[] itemPoints; // Specific points for different items
    
    [Header("Score Popup Settings")]
    public bool enableScorePopups = true; // Set to false temporarily if animation still delayed
    public Canvas gameCanvas; // Assign your main game Canvas
    public TMP_FontAsset popupFont; // FIXED: Assign Beachday SDF font here
    
    [Header("Popup Management")]
    public bool clearPopupOnLevelEnd = true; // Clear popup when level ends
    public bool clearPopupOnLevelStart = true; // Clear popup when level starts
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pointsSound;
    public AudioClip bonusSound;
    public AudioClip penaltySound;
    
    // Score tracking
    private int currentScore = 0; // Level score (for star progress)
    private int consecutiveCorrectOrders = 0;
    
    [System.Serializable]
    public class ItemPointValues
    {
        public string itemType;
        public int points;
    }
    
    void Start()
    {
        // Try to auto-find the in-game score text if not assigned
        if (inGameScoreText == null)
        {
            // Look for common names for in-game score display
            GameObject[] possibleObjects = {
                GameObject.Find("scoreText"),
                GameObject.Find("ScoreText"),
                GameObject.Find("InGameScore"),
                GameObject.Find("CurrentScore"),
                GameObject.Find("LevelScore")
            };
            
            foreach (GameObject obj in possibleObjects)
            {
                if (obj != null)
                {
                    TextMeshProUGUI textComponent = obj.GetComponent<TextMeshProUGUI>();
                    if (textComponent != null && obj != finalScoreText?.gameObject)
                    {
                        inGameScoreText = textComponent;
                        Debug.Log($"ScoreManager: Auto-found in-game score text: {obj.name}");
                        break;
                    }
                }
            }
        }
        
        // Auto-find game canvas if not assigned
        if (gameCanvas == null)
        {
            gameCanvas = FindObjectOfType<Canvas>();
            if (gameCanvas != null)
            {
                Debug.Log("ScoreManager: Auto-found Canvas for popups");
            }
            else
            {
                Debug.LogWarning("ScoreManager: No Canvas found! Score popups will be disabled.");
                enableScorePopups = false;
            }
        }
        
        UpdateScoreUI();
        UpdateComboUI();
        
        // Subscribe to session events
        SetupSessionEvents();
        
        // Update total score display
        UpdateTotalScoreUI();
        
        Debug.Log($"ScoreManager: Initialized - InGame: {(inGameScoreText != null ? "Found" : "Missing")}, Final: {(finalScoreText != null ? "Found" : "Missing")}, Canvas: {(gameCanvas != null ? "Found" : "Missing")}");
    }
    
    // UPDATED: Enhanced session event setup
    void SetupSessionEvents()
    {
        if (SessionManager.Instance != null)
        {
            // Remove old subscription to avoid duplicates
            SessionManager.Instance.OnTotalScoreChanged -= UpdateTotalScoreDisplay;
            // Subscribe to session total score changes
            SessionManager.Instance.OnTotalScoreChanged += UpdateTotalScoreDisplay;
            
            Debug.Log("ScoreManager: Subscribed to session total score changes");
        }
    }
    
    // Called by LevelManager when level loads
    public void SetLevelSettings(int basePoints, int perfectBonus, int timeBonusPoints)
    {
        basePointsPerOrder = basePoints;
        perfectOrderBonus = perfectBonus;
        timeBonus = timeBonusPoints;
        
        LevelData currentLevelData = levelManager.GetCurrentLevelData();
        if (currentLevelData != null)
        {
            starProgressBar.Initialize(currentLevelData);
            Debug.Log($"StarProgressBar initialized with {currentLevelData.levelName} data");
        }
        
        Debug.Log($"Score settings updated: Base={basePoints}, Perfect={perfectBonus}, Time={timeBonusPoints}");
    }
    
    // UPDATED: Reset score method with popup clearing
    public void ResetScore()
    {
        currentScore = 0;
        consecutiveCorrectOrders = 0;
        
        // Clear popup when starting new level
        if (clearPopupOnLevelStart)
        {
            ClearScorePopup();
        }
        
        UpdateScoreUI();
        UpdateComboUI();
        starProgressBar.UpdateDisplay(0);
        Debug.Log("Score reset for new level");
    }
    
    // Called when an individual item is served correctly
    public void AwardItemPoints(string itemType)
    {
        int points = GetPointsForItem(itemType);
        
        AddScore(points);
        ShowFeedback($"+{points} points!", Color.green);
        
        // DELAY the popup creation to allow served item animation to play first
        StartCoroutine(DelayedItemPopup(points));
        
        PlayPointsSound();
        
        Debug.Log($"Awarded {points} points for serving {itemType}");
    }
    
    // NEW: Delay item popup creation to allow served item animation to play first
    IEnumerator DelayedItemPopup(int points)
    {
        // Wait a tiny bit for the served item animation to start
        yield return new WaitForSeconds(0.05f); // Very small delay
        
        // Now show the item points popup
        ShowScorePopup(points, 0);
    }
    
    // Called when an order is completed successfully - FIXED TO MATCH ORDERSYSTEM
    public void AwardOrderCompletionBonus(float remainingTime)
    {
        // Base completion bonus
        int bonus = orderCompletionBonus;
        
        // Time bonus based on remaining time
        int timeBonusPoints = Mathf.RoundToInt(remainingTime * timeBonusMultiplier);
        
        // Combo multiplier
        int comboMultiplier = Mathf.Min(consecutiveCorrectOrders + 1, 5); // Max 5x combo
        
        int totalBonus = (bonus + timeBonusPoints) * comboMultiplier;
        
        AddScore(totalBonus);
        consecutiveCorrectOrders++;
        
        // DELAY the popup creation to allow served item animation to play first
        StartCoroutine(DelayedBonusPopup(totalBonus));
        
        UpdateComboUI();
        
        ShowFeedback($"Order Complete! +{totalBonus} bonus!", Color.yellow);
        PlayBonusSound();
        
        Debug.Log($"Order completion bonus: {bonus} + time bonus: {timeBonusPoints} x combo: {comboMultiplier} = {totalBonus}");
    }
    
    // NEW: Delay popup creation to allow served item animation to play first
    IEnumerator DelayedBonusPopup(int bonusPoints)
    {
        // Wait for served item animation to start playing
        yield return new WaitForSeconds(0.1f); // Small delay to let animation start
        
        // Now show the bonus popup
        ShowScorePopup(0, bonusPoints);
    }
    
    // Called when an order expires - FIXED TO MATCH ORDERSYSTEM
    public void ApplyOrderExpiredPenalty()
    {
        // Keep all feedback effects
        ShowFeedback("Order Expired!", Color.red); // Red warning text
        PlayPenaltySound(); // Penalty sound effect
    
        // Consequence: Reset combo streak
        consecutiveCorrectOrders = 0;
        UpdateComboUI();
    
        Debug.Log("Order expired - combo reset but no points lost");
    }
    
    // UPDATED: Show level score instead of session total score
    void UpdateScoreUI()
    {
        // Show current level score instead of session total score
        string scoreDisplayText = "$ " + currentScore;
        
        // Update in-game score display (now shows level score only)
        if (inGameScoreText != null)
        {
            inGameScoreText.text = scoreDisplayText;
            Debug.Log($"ScoreManager: Updated in-game score to '{scoreDisplayText}' (Level Score Only)");
        }
        else
        {
            Debug.LogWarning("ScoreManager: inGameScoreText is null! Please assign it in the inspector.");
        }
        
        // Update final score display (for level complete panel - keep as level score)
        if (finalScoreText != null)
        {
            finalScoreText.text = "Level Score: " + currentScore;
        }
        
        // Update overlay score text if you added one
        if (overlayScoreText != null)
        {
            overlayScoreText.text = scoreDisplayText;
        }
    }
    
    // UPDATED: Enhanced combo UI with overlay support
    void UpdateComboUI()
    {
        string comboDisplay = consecutiveCorrectOrders > 0 ? $"Combo x{consecutiveCorrectOrders + 1}" : "";
        
        // Update existing combo text
        if (comboText != null)
        {
            comboText.text = comboDisplay;
            comboText.color = consecutiveCorrectOrders > 0 ? Color.yellow : Color.white;
        }
        
        // Update overlay combo text
        if (overlayComboText != null)
        {
            overlayComboText.text = comboDisplay;
            overlayComboText.color = consecutiveCorrectOrders > 0 ? Color.yellow : Color.white;
        }
    }
    
    // Update total score UI
    void UpdateTotalScoreUI()
    {
        if (totalScoreText != null && SessionManager.Instance != null)
        {
            int totalScore = SessionManager.Instance.GetTotalScore();
            totalScoreText.text = "Total Score: " + totalScore;
        }
    }
    
    // UPDATED: Callback for session total score changes
    void UpdateTotalScoreDisplay(int newTotalScore)
    {
        // Update dedicated total score text if you have one
        if (totalScoreText != null)
        {
            totalScoreText.text = "Total Score: " + newTotalScore;
        }
        
        Debug.Log($"ScoreManager: Total score updated to {newTotalScore}");
    }
    
    // FIXED: Now properly passes the font from ScoreManager to SimpleScorePopup
    void ShowScorePopup(int basePoints, int bonusPoints = 0)
    {
        if (!enableScorePopups)
        {
            Debug.Log("Score popups are disabled in settings");
            return;
        }
        
        if (gameCanvas == null)
        {
            Debug.LogError("gameCanvas is null! Cannot create popup.");
            return;
        }
        
        Debug.Log($"Creating popup: basePoints={basePoints}, bonusPoints={bonusPoints}, font={popupFont?.name ?? "default"}");
        
        // FIXED: Now properly passes the font to the popup
        SimpleScorePopup popup = SimpleScorePopup.CreateCombinedPopupWithFont(
            gameCanvas.transform,
            basePoints,
            bonusPoints,
            popupFont  // ← THIS IS WHERE THE FONT IS PASSED
        );
        
        if (popup != null)
        {
            Debug.Log($"Popup created successfully with font: {(popupFont != null ? popupFont.name : "default")}");
        }
        else
        {
            Debug.LogError("Failed to create popup!");
        }
    }
    
    // For backwards compatibility, keep the old method but use the new combined one:
    void ShowScorePopup(int points, bool isBonus)
    {
        if (isBonus)
        {
            ShowScorePopup(0, points); // Show as bonus only
        }
        else
        {
            ShowScorePopup(points, 0); // Show as base only
        }
    }
    
    // When you want to show combined points (call this instead of separate calls):
    public void ShowCombinedPoints(int basePoints, int bonusPoints)
    {
        ShowScorePopup(basePoints, bonusPoints);
    }
    
    // NEW: Method to manually clear current popup
    public void ClearScorePopup()
    {
        SimpleScorePopup.ClearCurrentPopup();
        Debug.Log("ScoreManager: Manually cleared current score popup");
    }
    
    // NEW: Method called when level ends (call this from LevelManager)
    public void OnLevelEnd()
    {
        // Clear popup when level ends if enabled
        if (clearPopupOnLevelEnd)
        {
            ClearScorePopup();
        }
        
        Debug.Log("ScoreManager: Level ended");
    }
    
    // NEW: Method to check if popup is currently visible
    public bool IsPopupVisible()
    {
        return SimpleScorePopup.HasActivePopup();
    }
    
    // Helper method to get points for specific item types
    int GetPointsForItem(string itemType)
    {
        // Check if there are specific points for this item type
        foreach (var itemPoint in itemPoints)
        {
            if (itemPoint.itemType == itemType)
            {
                return itemPoint.points;
            }
        }
        
        // Return default points if no specific value found
        return pointsPerItem;
    }
    
    // UPDATED: Helper method to add score and update session total immediately
    void AddScore(int points)
    {
        // Add to level score (for star progress and level completion)
        currentScore += points;
        
        // IMMEDIATELY add to session total score
        if (SessionManager.Instance != null && SessionManager.Instance.HasActiveSession())
        {
            SessionManager.Instance.AddScoreImmediately(points);
        }
        
        UpdateScoreUI(); // This will now show the updated level score
        starProgressBar.UpdateDisplay(currentScore); // Star progress uses level score
    }
    
    // Helper method to show feedback text
    void ShowFeedback(string message, Color? color = null)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = color ?? Color.green;
            
            // Auto-hide feedback after a delay
            CancelInvoke("HideFeedback");
            Invoke("HideFeedback", 2f);
        }
        else
        {
            // If no feedback text component, just log it
            Debug.Log($"Score Feedback: {message}");
        }
    }
    
    void HideFeedback()
    {
        if (feedbackText != null)
            feedbackText.text = "";
    }
    
    // Audio methods
    void PlayPointsSound()
    {
        if (audioSource != null && pointsSound != null)
        {
            audioSource.PlayOneShot(pointsSound);
        }
        else if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayItemPickup();
        }
    }
    
    void PlayBonusSound()
    {
        if (audioSource != null && bonusSound != null)
        {
            audioSource.PlayOneShot(bonusSound);
        }
        else if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayOrderComplete();
        }
    }
    
    void PlayPenaltySound()
    {
        if (audioSource != null && penaltySound != null)
        {
            audioSource.PlayOneShot(penaltySound);
        }
    }
    
    // Clean up events when destroyed
    void OnDestroy()
    {
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnTotalScoreChanged -= UpdateTotalScoreDisplay;
        }
    }
    
    // Public getters
    public int GetCurrentScore()
    {
        return currentScore;
    }
    
    public int GetComboCount()
    {
        return consecutiveCorrectOrders;
    }
    
    // Additional helper methods
    public void AddBonusPoints(int points)
    {
        AddScore(points); // Uses the updated AddScore method
        Debug.Log($"Bonus points added: {points}");
    }
    
    public void ResetCombo()
    {
        consecutiveCorrectOrders = 0;
        UpdateComboUI();
    }
    
    // UPDATED: Test method for popup (for debugging)
    [ContextMenu("Test Score Popup")]
    public void TestScorePopup()
    {
        Debug.Log("Testing score popup...");
        if (enableScorePopups)
        {
            ShowCombinedPoints(50, 25);  // Test combined popup: +50\n+25 BONUS!
        }
        else
        {
            Debug.Log("Score popups disabled!");
        }
    }
    
    // NEW: Simple test for just showing a basic popup
    [ContextMenu("Test Simple Popup")]
    public void TestSimplePopup()
    {
        Debug.Log("Testing simple popup...");
        ShowScorePopup(100, 0); // Just show +100
    }
    
    // NEW: Test animation timing
    [ContextMenu("Test Animation Timing")]
    public void TestAnimationTiming()
    {
        Debug.Log("Testing animation timing - this should show popup immediately");
        ShowScorePopup(50, 0);
        Debug.Log("Popup creation completed");
    }
    
    // NEW: Context menu method to clear popup manually (for testing)
    [ContextMenu("Clear Current Popup")]
    public void TestClearPopup()
    {
        ClearScorePopup();
    }
    
    // Test method to manually update score (for debugging)
    [ContextMenu("Test Score Update")]
    public void TestScoreUpdate()
    {
        AddScore(100); // Uses the updated AddScore method
        Debug.Log("Test score update - added 100 points");
    }
}