using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI inGameScoreText; // NEW: For in-game score display
    public TextMeshProUGUI finalScoreText; // For level complete panel (your current scoreText)
    public TextMeshProUGUI totalScoreText; // Session total score
    public TextMeshProUGUI comboText;
    
    [Header("Game References")]
    public ServePlate servePlate;
    public OrderSystem orderSystem;
    public LevelManager levelManager;
    
    [Header("Level Settings - Set by LevelManager")]
    [SerializeField] private int basePointsPerOrder = 100;
    [SerializeField] private int perfectOrderBonus = 50;
    [SerializeField] private int timeBonus = 10;
    
    // Score tracking
    private int currentScore = 0;
    private int consecutiveCorrectOrders = 0;
    
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
    
    public void CalculateAndAwardScore()
    {
        if (servePlate == null || orderSystem == null)
        {
            Debug.LogWarning("ServePlate or OrderSystem reference missing!");
            return;
        }
        
        // Get served items and current order
        List<string> servedItems = servePlate.GetServedItemTypes();
        List<string> currentOrder = orderSystem.GetCurrentOrder();
        
        // Calculate score based on order accuracy
        int orderScore = CalculateOrderScore(servedItems, currentOrder);
        
        // Add time bonus
        float remainingTime = orderSystem.GetRemainingTime();
        int timeBonusPoints = Mathf.RoundToInt(remainingTime * timeBonus);
        
        // Apply combo multiplier for consecutive correct orders
        int comboMultiplier = Mathf.Min(consecutiveCorrectOrders, 5); // Max 5x combo
        int totalScore = (orderScore + timeBonusPoints) * (1 + comboMultiplier);
        
        // Update score
        currentScore += totalScore;
        
        // Update combo counter
        bool perfectOrder = IsOrderPerfect(servedItems, currentOrder);
        if (perfectOrder)
        {
            consecutiveCorrectOrders++;
        }
        else
        {
            consecutiveCorrectOrders = 0;
        }
        
        // Update UI immediately after scoring
        UpdateScoreUI();
        UpdateComboUI();
        
        Debug.Log($"Order Score: {orderScore}, Time Bonus: {timeBonusPoints}, Combo: {comboMultiplier}x, Total: {totalScore}");
        Debug.Log($"Current Level Score: {currentScore}");
    }
    
    int CalculateOrderScore(List<string> served, List<string> ordered)
    {
        if (ordered.Count == 0) return 0;
        
        // Check if order is perfect
        if (IsOrderPerfect(served, ordered))
        {
            return basePointsPerOrder + perfectOrderBonus;
        }
        
        // Calculate partial score based on correct items
        int correctItems = 0;
        List<string> orderedCopy = new List<string>(ordered);
        
        foreach (string servedItem in served)
        {
            if (orderedCopy.Contains(servedItem))
            {
                orderedCopy.Remove(servedItem);
                correctItems++;
            }
        }
        
        // Partial score: base points * (correct items / total ordered items)
        float accuracy = (float)correctItems / ordered.Count;
        return Mathf.RoundToInt(basePointsPerOrder * accuracy);
    }
    
    bool IsOrderPerfect(List<string> served, List<string> ordered)
    {
        if (served.Count != ordered.Count) return false;
        
        // Create copies to avoid modifying original lists
        List<string> servedCopy = new List<string>(served);
        List<string> orderedCopy = new List<string>(ordered);
        
        // Sort both lists to compare regardless of order
        servedCopy.Sort();
        orderedCopy.Sort();
        
        // Compare sorted lists
        for (int i = 0; i < servedCopy.Count; i++)
        {
            if (servedCopy[i] != orderedCopy[i])
                return false;
        }
        
        return true;
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
            finalScoreText.text = scoreDisplayText;
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