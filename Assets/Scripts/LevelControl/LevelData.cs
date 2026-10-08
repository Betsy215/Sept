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
    
    public Sprite backgroundSprite;
    
    [Header("Advanced Item Control")]
    public bool useSpecificFoodTypes = false;
    public float difficultyMultiplier = 1.0f;
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        ordersPerLevel = Mathf.Max(1, ordersPerLevel);
        orderDisplayTime = Mathf.Max(0.5f, orderDisplayTime);
        minOrderItems = Mathf.Max(1, minOrderItems);
        maxOrderItems = Mathf.Max(minOrderItems, maxOrderItems);

        if (starThreshold1 > starThreshold2 || starThreshold2 > starThreshold3)
            Debug.LogWarning($"{name}: star thresholds should rise ({starThreshold1}, {starThreshold2}, {starThreshold3})", this);
    }
#endif

    public int GetStarsEarned(float currentScore)
    {
        if (currentScore >= starThreshold3) return 3;
        if (currentScore >= starThreshold2) return 2;
        if (currentScore >= starThreshold1) return 1;
        return 0;
    }
    
    
    public int GetNextStarThreshold(float currentScore)
    {
        if (currentScore < starThreshold1) return starThreshold1;
        if (currentScore < starThreshold2) return starThreshold2;
        if (currentScore < starThreshold3) return starThreshold3;
        return starThreshold3;
    }
    
    public float GetScoreToNextStar(float currentScore)
    {
        int nextThreshold = GetNextStarThreshold(currentScore);
        return Mathf.Max(0, nextThreshold - currentScore);
    }
    
    public bool HasEarnedStar(float currentScore, int starLevel)
    {
        switch (starLevel)
        {
            case 1: return currentScore >= starThreshold1;
            case 2: return currentScore >= starThreshold2;
            case 3: return currentScore >= starThreshold3;
            default: return false;
        }
    }
    
    public string GetPerformanceDescription(float currentScore)
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
}