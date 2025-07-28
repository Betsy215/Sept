using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI inGameScoreText; // For in-game score display
    public TextMeshProUGUI finalScoreText; // For level complete panel
    public TextMeshProUGUI totalScoreText; // Session total score
    public TextMeshProUGUI comboText; // For combo display
    public TextMeshProUGUI feedbackText; // For showing "+10 points!" etc.
    
    [Header("Game References")]
    public OrderSystem orderSystem; // Reference to order system
    public LevelManager levelManager; // Reference to level manager
    
    [Header("Level Settings - Set by LevelManager")]
    [SerializeField] private int basePointsPerOrder = 100;
    [SerializeField] private int perfectOrderBonus = 50;
    [SerializeField] private int timeBonus = 10;
    
    [Header("NEW: Per-Item Scoring Settings")]
    public int pointsPerItem = 10; // Points for each correct item served
    public int orderCompletionBonus = 50; // Bonus for completing an order
    public int timeBonusMultiplier = 5; // Points per second remaining when order completed
    
    [Header("Item-Specific Points")]
    public ItemPointValues[] itemPoints; // Specific points for different items
    
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip pointsSound;
    public AudioClip bonusSound;
    public AudioClip penaltySound;
    
    // Score tracking
    private int currentScore = 0;
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
        
        UpdateScoreUI();
        UpdateComboUI();
        
        // Subscribe to session events
        SetupSessionEvents();
        
        // Update total score display
        UpdateTotalScoreUI();
        
        Debug.Log($"ScoreManager: Initialized - InGame: {(inGameScoreText != null ? "Found" : "Missing")}, Final: {(finalScoreText != null ? "Found" : "Missing")}");
    }
    
    void SetupSessionEvents()
    {
        // Subscribe to session total score changes
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.OnTotalScoreChanged += UpdateTotalScoreDisplay;
        }
    }
    
    // Called by LevelManager when level loads
    public void SetLevelSettings(int basePoints, int perfectBonus, int timeBonusPoints)
    {
        basePointsPerOrder = basePoints;
        perfectOrderBonus = perfectBonus;
        timeBonus = timeBonusPoints;
        
        Debug.Log($"Score settings updated: Base={basePoints}, Perfect={perfectBonus}, Time={timeBonusPoints}");
    }
    
    public void ResetScore()
    {
        currentScore = 0;
        consecutiveCorrectOrders = 0;
        UpdateScoreUI();
        UpdateComboUI();
        Debug.Log("Score reset for new level");
    }
    
    // NEW: Called when an individual item is served correctly
    public void AwardItemPoints(string itemType)
    {
        int points = GetPointsForItem(itemType);
        
        AddScore(points);
        ShowFeedback($"+{points} points!");
        PlayPointsSound();
        
        Debug.Log($"Awarded {points} points for serving {itemType}");
    }
    
    // NEW: Called when an order is completed (all items served)
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
        ShowFeedback($"Order Complete! +{totalBonus} bonus!");
        PlayBonusSound();
        
        // Increase combo counter
        consecutiveCorrectOrders++;
        UpdateComboUI();
        
        Debug.Log($"Order completion bonus: {bonus} + time bonus: {timeBonusPoints} x combo: {comboMultiplier} = {totalBonus}");
    }
    
    // NEW: Called when an order expires
    public void ApplyOrderExpiredPenalty()
    {

        // ✅ KEEP: All feedback effects
        ShowFeedback("Order Expired!", Color.red); // Red warning text
        PlayPenaltySound(); // Penalty sound effect
    
        // ✅ CONSEQUENCE: Reset combo streak
        consecutiveCorrectOrders = 0;
        UpdateComboUI();
    
        Debug.Log("Order expired - combo reset but no points lost");
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
    
    // Helper method to add score and update UI
    void AddScore(int points)
    {
        currentScore += points;
        
        UpdateScoreUI();
        
        // Note: Session total score is updated by LevelManager at level completion
        // using SessionManager.AddLevelScore() with the final level score
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
    
    void UpdateScoreUI()
    {
        string scoreDisplayText = "Level Score: " + currentScore;
        
        // Update in-game score display
        if (inGameScoreText != null)
        {
            inGameScoreText.text = scoreDisplayText;
            Debug.Log($"ScoreManager: Updated in-game score to '{scoreDisplayText}'");
        }
        else
        {
            Debug.LogWarning("ScoreManager: inGameScoreText is null! Please assign it in the inspector.");
        }
        
        // Update final score display (for level complete panel)
        if (finalScoreText != null)
        {
            finalScoreText.text = "Final Score: " + currentScore;
        }
    }
    
    void UpdateComboUI()
    {
        if (comboText != null)
        {
            if (consecutiveCorrectOrders > 0)
            {
                comboText.text = "Combo: " + consecutiveCorrectOrders + "x";
                comboText.color = Color.yellow;
            }
            else
            {
                comboText.text = "";
            }
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
    
    // Callback for session total score changes
    void UpdateTotalScoreDisplay(int newTotalScore)
    {
        if (totalScoreText != null)
        {
            totalScoreText.text = "Total Score: " + newTotalScore;
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
        currentScore += points;
        UpdateScoreUI();
        Debug.Log($"Bonus points added: {points}");
    }
    
    public void ResetCombo()
    {
        consecutiveCorrectOrders = 0;
        UpdateComboUI();
    }
    
    // Test method to manually update score (for debugging)
    [ContextMenu("Test Score Update")]
    public void TestScoreUpdate()
    {
        currentScore += 100;
        UpdateScoreUI();
        Debug.Log("Test score update - added 100 points");
    }
}