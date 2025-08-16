using System;
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
    public TextMeshProUGUI feedbackText; // For showing "+10 points!" etc.
    
    [Header("UI Overlay Elements")]
    public TextMeshProUGUI overlayScoreText;  // Your new overlay score text
    
    [Header("Game References")]
    public OrderSystem orderSystem; // Reference to order system
    public LevelManager levelManager; // Reference to level manager
    public StarProgressBar starProgressBar;
    
    [Header("Per-Item Scoring Settings")]
    public int pointsPerItem = 10; // Points for each correct item served
    public int timeBonusMultiplier = 5; // Points per second remaining when order completed
    
    [Header("Item-Specific Points")]
    public ItemPointValues[] itemPoints; // Specific points for different items
    
    [Header("Score Popup Settings")]
    public bool enableScorePopups = true; // Set to false temporarily if animation still delayed
    public Canvas gameCanvas; // Assign your main game Canvas
    public TMP_FontAsset popupFont; // FIXED: Assign Beachday SDF font here
    public Sprite popupBackgroundSprite;
    
    [Header("Popup Management")]
    public bool clearPopupOnLevelEnd = true; // Clear popup when level ends
    public bool clearPopupOnLevelStart = true; // Clear popup when level starts
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pointsSound;
    public AudioClip bonusSound;
    public AudioClip penaltySound;
    
    // Score tracking
    private float currentScore = 0; // Level score (for star progress)
    public float currentOrderItemPoints = 0; // Track points for current order items
    
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
        
        // Subscribe to session events
        SetupSessionEvents();
        
        // Update total score display
        UpdateTotalScoreUI();
        
      
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
    
    // UPDATED: Reset score method with popup clearing
    public void ResetScore()
    {
        currentScore = 0;
        currentOrderItemPoints = 0; // Reset current order tracking
        
        // Clear popup when starting new level
        if (clearPopupOnLevelStart)
        {
            ClearScorePopup();
        }
        
        UpdateScoreUI();
        starProgressBar.UpdateDisplay(0);
        Debug.Log("Score reset for new level");
    }
    
    
    // UPDATED: Called when an individual item is served correctly - NO POPUP
    public void AwardItemPoints(string itemType)
    {
        int points = GetPointsForItem(itemType);
        
        // Add to running totals
        AddScore(points);
        currentOrderItemPoints += points; // Track for this order
        
        PlayPointsSound();
    }
    
    // UPDATED: Called when an order is completed - shows combined popup
    public void AwardOrderCompletionBonus(float remainingTime, float basepoints)
    {
        // Calculate time bonus
        float timeBonusPoints = remainingTime * timeBonusMultiplier;
        timeBonusPoints = Mathf.Round(timeBonusPoints * 100f) / 100f;
        float tip = basepoints * timeBonusPoints / 100;
        Debug.Log($"remaining time: {remainingTime}");
        Debug.Log($"bonus :  {timeBonusPoints}");
        Debug.Log($"base : { basepoints}");
        Debug.Log($"tips: : { tip}");
        // Add time bonus to score if any
        if (tip > 0)
        {
            AddScore(tip);
        }
        
        // Show ONE combined popup with item points + time bonus
        StartCoroutine(DelayedCombinedPopup(currentOrderItemPoints, tip));
        
        PlayBonusSound();
        
        // Reset for next order
        currentOrderItemPoints = 0;
    }
    
    // NEW: Show one combined popup after order completion
    IEnumerator DelayedCombinedPopup(float itemPoints, float timeBonus)
    {
        // Wait for served item animation to finish
        yield return new WaitForSeconds(0.2f);
        
        // Show combined popup
        ShowScorePopup(itemPoints, timeBonus);
    }
    
    // SIMPLIFIED: Called when an order expires - no penalties, just feedback
    public void ApplyOrderExpiredPenalty()
    {
        if (currentOrderItemPoints > 0)
        {
            // Use existing delayed popup method with 0 bonus
            StartCoroutine(DelayedCombinedPopup(currentOrderItemPoints, 0f)); // 0f = no bonus
        }
        // Only provide feedback - no score penalties
        ShowFeedback("Order Expired!", Color.red);
        PlayPenaltySound();
        
        // Reset current order tracking since order is over
        currentOrderItemPoints = 0;
    
        Debug.Log("Order expired - no score penalty applied");
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
    
    // Update total score UI
    void UpdateTotalScoreUI()
    {
        if (totalScoreText != null && SessionManager.Instance != null)
        {
            float totalScore = SessionManager.Instance.GetTotalScore();
            totalScoreText.text = "Total Score: " + totalScore;
        }
    }
    
    // UPDATED: Callback for session total score changes
    void UpdateTotalScoreDisplay(float f)
    {
        // Update dedicated total score text if you have one
        if (totalScoreText != null)
        {
            totalScoreText.text = "Total Score: " + f;
        }
        
        Debug.Log($"ScoreManager: Total score updated to {f}");
    }
    
    // UPDATED: Now shows combined popup only
    void ShowScorePopup(float basePoints, float bonusPoints = 0)
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
        
        Debug.Log($"Creating COMBINED popup: itemPoints={basePoints}, timeBonus={bonusPoints}");
        
        // Create one popup with combined points
        SimpleScorePopup popup = SimpleScorePopup.CreateCombinedPopupWithFont(
            gameCanvas.transform,
            basePoints,
            bonusPoints,
            popupFont,
            popupBackgroundSprite
        );
    }
    
    // NEW: Method to manually clear current popup
    public void ClearScorePopup()
    {
        SimpleScorePopup.ClearCurrentPopup();
       
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
    void AddScore(float points)
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
    public float GetCurrentScore()
    {
        return currentScore;
    }
}