using UnityEngine;

[CreateAssetMenu(fileName = "New Level", menuName = "Food Truck/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public int levelNumber = 1;
    public string levelName = "Level 1";
    
    [Header("Order Settings")]
    public int ordersPerLevel = 3;
    public float orderDisplayTime = 5f;
    public float timeBetweenOrders = 2f;
    public int minOrderItems = 1;
    public int maxOrderItems = 4;
    
    [Header("⭐ Star Rating System")]
    public int starThreshold1 = 10;
    public int starThreshold2 = 20;
    public int starThreshold3 = 30;
    public int maxPossibleScore = 50;
    
    [Header("Star Visualization")]
    public Color earnedStarColor = Color.yellow;
    public Color unearnedStarColor = Color.gray;
    
    [Header("Visual Elements")]
    public Color backgroundColor = Color.white;
    public Sprite backgroundSprite;
    
    [Header("Advanced Item Control")]
    public bool useSpecificFoodTypes = false;
    public float difficultyMultiplier = 1.0f;
    
    public int GetStarsEarned(int currentScore)
    {
        if (currentScore >= starThreshold3) return 3;
        if (currentScore >= starThreshold2) return 2;
        if (currentScore >= starThreshold1) return 1;
        return 0;
    }
    
    public float GetProgressPercentage(int currentScore)
    {
        return Mathf.Clamp01((float)currentScore / maxPossibleScore);
    }
    
    public int GetNextStarThreshold(int currentScore)
    {
        if (currentScore < starThreshold1) return starThreshold1;
        if (currentScore < starThreshold2) return starThreshold2;
        if (currentScore < starThreshold3) return starThreshold3;
        return starThreshold3;
    }
    
    public float GetProgressToNextStar(int currentScore)
    {
        int nextThreshold = GetNextStarThreshold(currentScore);
        int previousThreshold = 0;
        
        if (nextThreshold == starThreshold2) 
            previousThreshold = starThreshold1;
        else if (nextThreshold == starThreshold3) 
            previousThreshold = starThreshold2;
        
        if (currentScore >= starThreshold3) 
            return 1f;
        
        return Mathf.Clamp01((float)(currentScore - previousThreshold) / (nextThreshold - previousThreshold));
    }
    
    public int GetScoreToNextStar(int currentScore)
    {
        int nextThreshold = GetNextStarThreshold(currentScore);
        return Mathf.Max(0, nextThreshold - currentScore);
    }
    
    public bool HasEarnedStar(int currentScore, int starLevel)
    {
        switch (starLevel)
        {
            case 1: return currentScore >= starThreshold1;
            case 2: return currentScore >= starThreshold2;
            case 3: return currentScore >= starThreshold3;
            default: return false;
        }
    }
    
    public string GetPerformanceDescription(int currentScore)
    {
        int stars = GetStarsEarned(currentScore);
        switch (stars)
        {
            case 0: return "Keep trying!";
            case 1: return "Good job!";
            case 2: return "Great work!";
            case 3: return "Perfect!";
            default: return "Amazing!";
        }
    }
    
    public bool ValidateStarThresholds()
    {
        bool isValid = starThreshold1 > 0 && 
                      starThreshold2 > starThreshold1 && 
                      starThreshold3 > starThreshold2 &&
                      maxPossibleScore >= starThreshold3;
        
        if (!isValid)
        {
            Debug.LogWarning($"Invalid star thresholds in {levelName}!");
        }
        
        return isValid;
    }
    
    public float GetEstimatedPlayTime()
    {
        return (ordersPerLevel * orderDisplayTime) + ((ordersPerLevel - 1) * timeBetweenOrders);
    }
}