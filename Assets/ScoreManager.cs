using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro; // Add TextMeshPro namespace

public class ScoreManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI scoreText; // Reference to your "Score: 0" TextMeshPro text
    
    [Header("Game References")]
    public OrderSystem orderSystem; // To get current order
    public ServePlate servePlate; // To get served items
    
    private int currentScore = 0;
    private const int POINTS_PER_ITEM = 20;
    
    void Start()
    {
        UpdateScoreDisplay();
    }
    
    public void CalculateAndAwardScore()
    {
        // Get current order and served items
        List<string> currentOrder = orderSystem.GetCurrentOrder();
        List<string> servedItems = servePlate.GetServedItemTypes();
        
        if (!orderSystem.IsOrderActive())
        {
            Debug.Log("No active order to score against");
            return;
        }
        
        int orderScore = 0;
        int maxItems = Mathf.Max(currentOrder.Count, servedItems.Count);
        
        // Compare each position
        for (int i = 0; i < maxItems; i++)
        {
            string orderedItem = i < currentOrder.Count ? currentOrder[i] : "";
            string servedItem = i < servedItems.Count ? servedItems[i] : "";
            
            // Check if both item type and position match
            if (!string.IsNullOrEmpty(orderedItem) && 
                !string.IsNullOrEmpty(servedItem) && 
                orderedItem == servedItem)
            {
                orderScore += POINTS_PER_ITEM;
                Debug.Log($"Position {i}: {servedItem} matches {orderedItem} - +{POINTS_PER_ITEM} points");
            }
            else
            {
                Debug.Log($"Position {i}: {servedItem} does not match {orderedItem} - 0 points");
            }
        }
        
        // Award the score
        currentScore += orderScore;
        UpdateScoreDisplay();
        
        Debug.Log($"Order completed! Earned {orderScore} points. Total score: {currentScore}");
    }
    
    void UpdateScoreDisplay()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + currentScore;
    }
    
    // Public method to get current score (for other systems if needed)
    public int GetCurrentScore()
    {
        return currentScore;
    }
    
    // Method to reset score (useful for game restart)
    public void ResetScore()
    {
        currentScore = 0;
        UpdateScoreDisplay();
        Debug.Log("Score reset to 0");
    }
}