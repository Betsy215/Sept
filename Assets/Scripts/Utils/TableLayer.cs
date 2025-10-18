using UnityEngine;
using System.Collections.Generic;

public class TableLayer : MonoBehaviour
{
    [Header("Table Visual")] [Tooltip("The cloth/table sprite that will scale with screen size")]
    public SpriteRenderer tableClothSprite;

    [Header("Table Bounds")] [Tooltip("Collider that defines the table area (for drag bounds)")]
    public BoxCollider2D tableBounds;

    [Header("Table Coverage Settings")]
    [Tooltip("What percentage of the screen height should the tablecloth cover from bottom")]
    [Range(0.3f, 1f)]
    public float screenCoveragePercent = 0.45f; // Cover 45% of screen from bottom

    [Header("Responsive Settings")] [Tooltip("Base reference resolution for scaling calculations")]
    public Vector2 referenceResolution = new(1920, 1080);

    [Tooltip("How much the food items should scale relative to table scaling")] [Range(0.5f, 2f)]
    public float itemScaleMultiplier = 1f;

    [Header("References")] [Tooltip("Reference to LevelManager to get active serveable items")]
    public LevelManager levelManager;

    [Tooltip("Main camera for calculating screen bounds")]
    public Camera mainCamera;

    [Header("Debug")] public bool showDebugInfo = true;

    // Private variables
    private Vector2 currentScreenSize;
    private Vector3 initialTableScale;
    private Vector3 initialTablePosition;
    private float uniformScaleFactor = 1f;

    // Store initial scales of food items (no position tracking needed for draggable items)
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
        currentScreenSize = new Vector2(Screen.width, Screen.height);
        initialTableScale = tableClothSprite.transform.localScale;
        initialTablePosition = tableClothSprite.transform.position;

        // Calculate scaling once at start
        CalculateScaling();

        // Setup table layout
        SetupTableLayout();

        // Register food items and their initial positions
        RegisterFoodItems();

        // Apply initial scaling
        ApplyScaling();

        if (showDebugInfo)
            Debug.Log($"TableLayer initialized. Screen: {currentScreenSize}, Scale Factor: {uniformScaleFactor}");
    }

    private void CalculateScaling()
    {
        // Calculate scale factor based on screen area vs reference area
        var screenArea = currentScreenSize.x * currentScreenSize.y;
        var referenceArea = referenceResolution.x * referenceResolution.y;

        // Area ratio, then square root to get linear scale factor
        var areaRatio = screenArea / referenceArea;
        uniformScaleFactor = Mathf.Sqrt(areaRatio);

        if (showDebugInfo)
        {
            Debug.Log($"Screen: {currentScreenSize} (Area: {screenArea:F0})");
            Debug.Log($"Reference: {referenceResolution} (Area: {referenceArea:F0})");
            Debug.Log($"Area Ratio: {areaRatio:F3}, Scale Factor: {uniformScaleFactor:F3}");
        }
    }

    private void SetupTableLayout()
    {
        // Scale and position tablecloth
        UpdateTableCloth();

        // Update bounds collider to match table size
        UpdateTableBounds();
    }

    // Scale tablecloth to cover bottom percentage of screen (no padding)
    private void UpdateTableCloth()
    {
        if (tableClothSprite == null || mainCamera == null)
            return;

        // Calculate world space screen dimensions
        var cameraHeight = mainCamera.orthographicSize * 2f;
        var cameraWidth = cameraHeight * mainCamera.aspect;

        // Calculate desired coverage dimensions (no padding)
        var targetWidth = cameraWidth;
        var targetHeight = cameraHeight * screenCoveragePercent;

        // Calculate required scale based on original sprite size
        var spriteBounds = tableClothSprite.sprite.bounds;
        var originalSpriteHeight = spriteBounds.size.y;
        var originalSpriteWidth = spriteBounds.size.x;

        // Calculate scales needed to achieve target dimensions
        var requiredScaleY = targetHeight / originalSpriteHeight;
        var requiredScaleX = targetWidth / originalSpriteWidth;

        // Apply the scale
        var newScale = new Vector3(requiredScaleX, requiredScaleY, 1f);
        tableClothSprite.transform.localScale = newScale;

        // Position tablecloth at bottom of screen
        var cameraBottom = mainCamera.transform.position.y - mainCamera.orthographicSize;
        var scaledSpriteHeight = originalSpriteHeight * requiredScaleY;
        var spriteHalfHeight = scaledSpriteHeight / 2f;

        // Position so it covers from bottom up
        var newPosition = tableClothSprite.transform.position;
        newPosition.y = cameraBottom + spriteHalfHeight;
        tableClothSprite.transform.position = newPosition;

        if (showDebugInfo)
        {
            Debug.Log($"Screen dimensions: {cameraWidth:F2} x {cameraHeight:F2}");
            Debug.Log($"Target dimensions: {targetWidth:F2} x {targetHeight:F2} (no padding)");
            Debug.Log($"Coverage: {screenCoveragePercent * 100}% = {targetHeight:F2} units");
            Debug.Log($"TableCloth scale: {newScale}");
            Debug.Log($"TableCloth position Y: {newPosition.y:F2}");
        }
    }

    private void RegisterFoodItems()
    {
        // Clear existing data
        initialItemScales.Clear();

        // Get ALL serveable items from LevelManager (not just active ones)
        if (levelManager != null)
        {
            var allItems = levelManager.serveableItems;

            foreach (var item in allItems)
                if (item != null)
                {
                    var foodTransform = item.transform;

                    // Store initial scale for ALL items (regardless of active state)
                    initialItemScales[foodTransform] = foodTransform.localScale;

                    if (showDebugInfo)
                        Debug.Log($"Registered {item.GetFoodType()} with initial scale: {foodTransform.localScale}");
                }
        }
        else
        {
            Debug.LogWarning("TableLayer: LevelManager reference not set! Cannot auto-register food items.");
        }
    }

    private void ApplyScaling()
    {
        // Scale tablecloth first
        UpdateTableCloth();

        // Then scale food items
        UpdateFoodItems();

        // Finally update bounds to match scaled tablecloth
        UpdateTableBounds();
    }

    private void UpdateFoodItems()
    {
        // Only scale items, don't move them (players can drag them wherever they want)
        foreach (var kvp in initialItemScales)
        {
            var foodItem = kvp.Key;
            var originalScale = kvp.Value;

            if (foodItem == null) continue;

            // Update scale only - position is controlled by player dragging
            var newScale = originalScale * uniformScaleFactor * itemScaleMultiplier;
            foodItem.localScale = newScale;
        }
    }

    private void UpdateTableBounds()
    {
        if (tableBounds == null || tableClothSprite == null) return;

        // Make bounds exactly match tablecloth size
        var spriteBounds = tableClothSprite.bounds;

        tableBounds.transform.position = spriteBounds.center;
        tableBounds.size = new Vector2(spriteBounds.size.x, spriteBounds.size.y);

        if (showDebugInfo)
            Debug.Log($"TableBounds updated - Center: {spriteBounds.center}, Size: {spriteBounds.size}");
    }

    #region Public Methods for Food Item Management

    /// <summary>
    /// Add a new food item to the table scaling system
    /// </summary>
    public void AddFoodItem(Transform foodItem)
    {
        if (foodItem == null) return;

        // Store initial scale only
        initialItemScales[foodItem] = foodItem.localScale;

        // Apply current scaling immediately
        var newScale = foodItem.localScale * uniformScaleFactor * itemScaleMultiplier;
        foodItem.localScale = newScale;

        if (showDebugInfo)
            Debug.Log($"Added food item: {foodItem.name} with scale: {newScale}");
    }

    /// <summary>
    /// Remove a food item from the table scaling system
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
    /// Update scale for a specific food item (useful for dynamic items)
    /// </summary>
    public void UpdateFoodItemScale(Transform foodItem)
    {
        if (foodItem == null || !initialItemScales.ContainsKey(foodItem))
            return;

        var newScale = initialItemScales[foodItem] * uniformScaleFactor * itemScaleMultiplier;
        foodItem.localScale = newScale;
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
    /// Force recalculate scaling (useful when switching scenes)
    /// </summary>
    public void RecalculateScaling()
    {
        currentScreenSize = new Vector2(Screen.width, Screen.height);
        CalculateScaling();
        SetupTableLayout();
        ApplyScaling();

        if (showDebugInfo)
            Debug.Log("TableLayer scaling recalculated manually");
    }

    #endregion

    private void OnDrawGizmos()
    {
        if (showDebugInfo && mainCamera != null)
        {
            // Draw camera bounds for reference
            Gizmos.color = Color.blue;
            var height = mainCamera.orthographicSize * 2f;
            var width = height * mainCamera.aspect;
            var cameraPos = mainCamera.transform.position;
            Gizmos.DrawWireCube(cameraPos, new Vector3(width, height, 0));

            // Draw tablecloth coverage area
            if (tableClothSprite != null)
            {
                Gizmos.color = Color.yellow;
                var tableBounds = tableClothSprite.bounds;
                Gizmos.DrawWireCube(tableBounds.center, tableBounds.size);
            }

            // Draw table bounds (should match tablecloth)
            if (tableBounds != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(tableBounds.bounds.center, tableBounds.bounds.size);
            }

            // Draw food item positions
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