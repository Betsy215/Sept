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
    
    [Header("Food Tray Settings")]
    public int maxItemsPerTray = 5;
    
    [Header("Serve Plate Settings")]
    public int plateMaxCapacity = 4;
    
    [Header("Scoring")]
    public int basePointsPerOrder = 100;
    public int perfectOrderBonus = 50;
    public int timeBonus = 10; // Points per second remaining
    
    [Header("Visual Elements")]
    public Color backgroundColor = Color.white;
    public Sprite backgroundSprite;
}