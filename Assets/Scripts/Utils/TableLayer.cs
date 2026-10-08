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
    public float screenCoveragePercent = 0.5f;

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

    // World-space size of the tablecloth after UpdateTableCloth (zero until computed)
    private Vector2 clothWorldSize;

    // Drag bounds in world space, computed directly so clamping never waits for the
    // physics transform sync that Collider2D.bounds depends on.
    private Bounds cachedTableBounds;
    private bool hasCachedBounds;

    // Awake (not Start) so the bounds exist before GamePhaseManager/LevelManager Start()
    // load saved food positions and clamp them to the table.
    private void Awake()
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

        if (tableClothSprite.sprite == null)
        {
            Debug.LogError("TableLayer: tableClothSprite has no sprite assigned!");
            return;
        }

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

        clothWorldSize = new Vector2(originalSpriteWidth * requiredScaleX, scaledSpriteHeight);

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

        // Screen rect in world space
        var screenHeight = mainCamera.orthographicSize * 2f;
        var screenWidth = screenHeight * mainCamera.aspect;
        var cameraPos = mainCamera.transform.position;
        var screenLeft = cameraPos.x - screenWidth / 2f;
        var screenBottom = cameraPos.y - mainCamera.orthographicSize;

        // Tablecloth rect in world space (fallback to renderer bounds if UpdateTableCloth bailed out)
        var clothCenter = tableClothSprite.transform.position;
        var clothSize = clothWorldSize.sqrMagnitude > 0f ? clothWorldSize : (Vector2)tableClothSprite.bounds.size;
        var clothMinX = clothCenter.x - clothSize.x / 2f;
        var clothMaxX = clothCenter.x + clothSize.x / 2f;
        var clothMinY = clothCenter.y - clothSize.y / 2f;
        var clothMaxY = clothCenter.y + clothSize.y / 2f;

        // Safe area converted to the same world space. Screen.safeArea is a pixel rect with its
        // origin at the bottom-left, so on a notched iPhone safeArea.y is the home-indicator inset
        // and (height - yMax) the notch inset. The old code scaled the table height by
        // safeArea.height / Screen.height, which mixed both insets into one ratio and ignored
        // where they actually are; this offsets each edge by its real inset instead.
        var safeArea = Screen.safeArea;
        var unitsPerPixelX = screenWidth / Mathf.Max(1, Screen.width);
        var unitsPerPixelY = screenHeight / Mathf.Max(1, Screen.height);
        var safeMinX = screenLeft + safeArea.xMin * unitsPerPixelX;
        var safeMaxX = screenLeft + safeArea.xMax * unitsPerPixelX;
        var safeMinY = screenBottom + safeArea.yMin * unitsPerPixelY;
        var safeMaxY = screenBottom + safeArea.yMax * unitsPerPixelY;

        // Interactive area = tablecloth ∩ safe area, inset by the comfort padding
        var minX = Mathf.Max(clothMinX, safeMinX) + tableBoundsPadding;
        var maxX = Mathf.Min(clothMaxX, safeMaxX) - tableBoundsPadding;
        var minY = Mathf.Max(clothMinY, safeMinY) + tableBoundsPadding;
        var maxY = Mathf.Min(clothMaxY, safeMaxY) - tableBoundsPadding;

        // Degenerate (padding larger than the available area): collapse to the centre line
        if (maxX < minX) minX = maxX = (minX + maxX) / 2f;
        if (maxY < minY) minY = maxY = (minY + maxY) / 2f;

        var center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, tableBounds.transform.position.z);
        var size = new Vector2(maxX - minX, maxY - minY);

        // Keep the collider in sync for anything that still reads it (gizmos, physics queries)
        tableBounds.offset = Vector2.zero;
        tableBounds.transform.position = center;
        var scale = tableBounds.transform.lossyScale;
        tableBounds.size = new Vector2(
            size.x / (Mathf.Approximately(scale.x, 0f) ? 1f : Mathf.Abs(scale.x)),
            size.y / (Mathf.Approximately(scale.y, 0f) ? 1f : Mathf.Abs(scale.y)));

        cachedTableBounds = new Bounds(center, new Vector3(size.x, size.y, 0f));
        hasCachedBounds = true;

        if (showDebugInfo)
        {
            Debug.Log($"Screen safe area: {safeArea} (screen {Screen.width}x{Screen.height})");
            Debug.Log($"Safe area world rect: x [{safeMinX:F2}, {safeMaxX:F2}] y [{safeMinY:F2}, {safeMaxY:F2}]");
            Debug.Log($"Tablecloth size: {clothSize.x:F2} x {clothSize.y:F2} (visual)");
            Debug.Log($"Bounds: center {center} size {size.x:F2} x {size.y:F2} (interactive)");
        }
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
        if (hasCachedBounds)
        {
            var b = cachedTableBounds;
            return worldPosition.x >= b.min.x && worldPosition.x <= b.max.x &&
                   worldPosition.y >= b.min.y && worldPosition.y <= b.max.y;
        }

        if (tableBounds == null) return false;
        return tableBounds.bounds.Contains(worldPosition);
    }

    /// <summary>
    /// Clamp a position to stay within table bounds
    /// </summary>
    public Vector3 ClampToTableBounds(Vector3 worldPosition)
    {
        if (!hasCachedBounds && tableBounds == null) return worldPosition;

        var bounds = hasCachedBounds ? cachedTableBounds : tableBounds.bounds;
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