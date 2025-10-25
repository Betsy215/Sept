using UnityEngine;
using System.Collections.Generic;

public class TableLayer : MonoBehaviour
{
    [Header("Table Visual")] [Tooltip("The cloth/table sprite that will scale with screen size")]
    public SpriteRenderer tableClothSprite;

    [Header("Table Bounds")] [Tooltip("Collider that defines the table area (for drag bounds)")]
    public BoxCollider2D tableBounds;

    [Tooltip("Padding between table bounds and tablecloth edges (in world units)")] [Range(0f, 1f)]
    public float tableBoundsPadding = 0.3f;

    [Header("Table Coverage Settings")]
    [Tooltip("What percentage of the screen height should the tablecloth cover from bottom")]
    [Range(0.3f, 1f)]
    public float screenCoveragePercent = 0.45f; // Cover 45% of screen from bottom

    [Header("References")] [Tooltip("Reference to LevelManager to get active serveable items")]
    public LevelManager levelManager;

    [Tooltip("Main camera for world space conversion")]
    public Camera mainCamera;

    [Header("Debug")] public bool showDebugInfo = true;

    // Private variables
    private Vector3 initialTableScale;
    private Vector3 initialTablePosition;

    // Store initial scales for reference only - NO SCALING APPLIED
    private Dictionary<Transform, Vector3> initialItemScales = new();

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        // Auto-find main camera if not assigned
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Validation
        if (tableClothSprite == null)
        {
            Debug.LogError("TableLayer: tableClothSprite is not assigned!");
            return;
        }

        if (mainCamera == null)
        {
            Debug.LogError("TableLayer: mainCamera not found!");
            return;
        }

        // Store initial values
        initialTableScale = tableClothSprite.transform.localScale;
        initialTablePosition = tableClothSprite.transform.position;

        // Setup table layout
        SetupTableLayout();

        // Register food items (NO SCALING)
        RegisterFoodItems();

        if (showDebugInfo)
            Debug.Log($"TableLayer initialized. Screen: {Screen.width}x{Screen.height}");
    }

    private void SetupTableLayout()
    {
        // Scale and position tablecloth to exact screen dimensions
        UpdateTableCloth();

        // Update bounds collider to match table size exactly
        UpdateTableBounds();
    }

    // Simple version that was working - no safe area calculations
    private void UpdateTableCloth()
    {
        if (tableClothSprite == null || mainCamera == null)
            return;

        // Get screen dimensions in world space
        var screenHeight = mainCamera.orthographicSize * 2f;
        var screenWidth = screenHeight * mainCamera.aspect;

        // Target dimensions: exact screen width, percentage of screen height
        var targetWidth = screenWidth;
        var targetHeight = screenHeight * screenCoveragePercent;

        // Calculate required scale based on original sprite size
        var spriteBounds = tableClothSprite.sprite.bounds;
        var originalSpriteHeight = spriteBounds.size.y;
        var originalSpriteWidth = spriteBounds.size.x;

        // Calculate scales to achieve target dimensions
        var requiredScaleY = targetHeight / originalSpriteHeight;
        var requiredScaleX = targetWidth / originalSpriteWidth;

        // Apply the scale
        var newScale = new Vector3(requiredScaleX, requiredScaleY, 1f);
        tableClothSprite.transform.localScale = newScale;

        // Position tablecloth so bottom edge touches screen bottom
        var screenBottom = mainCamera.transform.position.y - mainCamera.orthographicSize;
        var scaledSpriteHeight = originalSpriteHeight * requiredScaleY;
        var spriteHalfHeight = scaledSpriteHeight / 2f;

        // Position sprite center so bottom edge is at screen bottom
        var newPosition = tableClothSprite.transform.position;
        newPosition.x = mainCamera.transform.position.x; // Center horizontally
        newPosition.y = screenBottom + spriteHalfHeight;
        tableClothSprite.transform.position = newPosition;

        if (showDebugInfo)
        {
            Debug.Log($"Screen dimensions: {screenWidth:F2} x {screenHeight:F2}");
            Debug.Log($"Target dimensions: {targetWidth:F2} x {targetHeight:F2}");
            Debug.Log($"Coverage: {screenCoveragePercent * 100}% height");
            Debug.Log($"TableCloth scale: {newScale}");
            Debug.Log($"TableCloth bottom edge: {newPosition.y - spriteHalfHeight:F2}");
        }
    }

    private void RegisterFoodItems()
    {
        // Clear existing data
        initialItemScales.Clear();

        // Get ALL serveable items from LevelManager
        if (levelManager != null)
        {
            var allItems = levelManager.serveableItems;

            foreach (var item in allItems)
                if (item != null)
                {
                    var foodTransform = item.transform;

                    // Store initial scale for REFERENCE ONLY - DON'T SCALE
                    initialItemScales[foodTransform] = foodTransform.localScale;

                    if (showDebugInfo)
                        Debug.Log($"Registered {item.GetFoodType()} - NO SCALING APPLIED");
                }
        }
        else
        {
            Debug.LogWarning("TableLayer: LevelManager reference not set!");
        }
    }

    private void UpdateTableBounds()
    {
        if (tableBounds == null || tableClothSprite == null || mainCamera == null) return;

        // Calculate tablecloth dimensions using SAME logic as tablecloth
        var screenHeight = mainCamera.orthographicSize * 2f;
        var screenWidth = screenHeight * mainCamera.aspect;

        var tableclothWidth = screenWidth;
        var tableclothHeight = screenHeight * screenCoveragePercent;

        // Apply padding to create smaller bounds (safe zone)
        var boundsWidth = tableclothWidth - tableBoundsPadding * 2f; // Subtract padding from both sides
        var boundsHeight = tableclothHeight - tableBoundsPadding * 2f; // Subtract padding from top and bottom

        // Position bounds at same location as tablecloth
        tableBounds.transform.position = tableClothSprite.transform.position;
        tableBounds.size = new Vector2(boundsWidth, boundsHeight);
    }

    #region Public Methods for Food Item Management

    /// <summary>
    /// Add a new food item to the table system (NO SCALING)
    /// </summary>
    public void AddFoodItem(Transform foodItem)
    {
        if (foodItem == null) return;

        // Store initial scale for reference only - NO SCALING
        initialItemScales[foodItem] = foodItem.localScale;

        if (showDebugInfo)
            Debug.Log($"Added food item: {foodItem.name} - NO SCALING APPLIED");
    }

    /// <summary>
    /// Remove a food item from the table system
    /// </summary>
    public void RemoveFoodItem(Transform foodItem)
    {
        if (foodItem != null && initialItemScales.ContainsKey(foodItem))
        {
            initialItemScales.Remove(foodItem);

            if (showDebugInfo)
                Debug.Log($"Removed food item: {foodItem.name}");
        }
    }

    /// <summary>
    /// Check if a world position is within the table bounds
    /// </summary>
    public bool IsPositionOnTable(Vector3 worldPosition)
    {
        if (tableBounds == null) return false;
        return tableBounds.bounds.Contains(worldPosition);
    }

    /// <summary>
    /// Clamp a position to stay within table bounds
    /// </summary>
    public Vector3 ClampToTableBounds(Vector3 worldPosition)
    {
        if (tableBounds == null) return worldPosition;

        var bounds = tableBounds.bounds;
        return new Vector3(
            Mathf.Clamp(worldPosition.x, bounds.min.x, bounds.max.x),
            Mathf.Clamp(worldPosition.y, bounds.min.y, bounds.max.y),
            worldPosition.z
        );
    }

    /// <summary>
    /// Force recalculate table layout
    /// </summary>
    public void RecalculateScaling()
    {
        SetupTableLayout();

        if (showDebugInfo)
            Debug.Log("TableLayer recalculated");
    }

    #endregion

    private void OnDrawGizmos()
    {
        if (showDebugInfo && mainCamera != null)
        {
            // Draw screen bounds for reference (blue)
            Gizmos.color = Color.blue;
            var height = mainCamera.orthographicSize * 2f;
            var width = height * mainCamera.aspect;
            var cameraPos = mainCamera.transform.position;
            Gizmos.DrawWireCube(cameraPos, new Vector3(width, height, 0));

            // Draw tablecloth (yellow)
            if (tableClothSprite != null)
            {
                Gizmos.color = Color.yellow;
                var tableBounds = tableClothSprite.bounds;
                Gizmos.DrawWireCube(tableBounds.center, tableBounds.size);
            }

            // Draw table bounds (green) - should match tablecloth exactly
            if (tableBounds != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(tableBounds.bounds.center, tableBounds.bounds.size);
            }

            // Draw food item positions (red)
            Gizmos.color = Color.red;
            foreach (var kvp in initialItemScales)
            {
                var foodItem = kvp.Key;
                if (foodItem != null)
                    Gizmos.DrawWireSphere(foodItem.position, 0.1f);
            }
        }
    }
}