using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    [Header("UI References")]
    public Text scoreText; // Reference to your "Score: 0" text
    
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
        // This method will be called by ServePlate when serve button is pressed
        // Compare served items vs current order
        // Award points for correct matches
        // Update display
    }
    
    void UpdateScoreDisplay()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + currentScore;
    }
}