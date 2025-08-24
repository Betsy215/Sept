using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages proportional scaling of tablecloth and manually-positioned food items across different screen resolutions
/// Preserves the relative positions you set manually in the Unity editor
/// </summary>
public class TableClothScaler : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The tablecloth sprite renderer")]
    public SpriteRenderer tableclothRenderer;
    
    [Tooltip("List of food items to scale with the tablecloth - can be auto-populated")]
    public List<Transform> foodItems = new List<Transform>();
    
    [Header("Auto-Detection")]
    [Tooltip("Automatically find all ServeableItem objects in the scene")]
    public bool autoDetectFoodItems = true;
    
    [Tooltip("Tag to filter food items (optional, leave empty to find all ServeableItems)")]
    public string foodItemTag = "";
    
    [Header("Reference Design")]
    [Tooltip("The reference screen width your design is based on")]
    public float referenceScreenWidth = 1080f;
    
    [Tooltip("The reference screen height your design is based on")]
    public float referenceScreenHeight = 1920f;
    
    [Tooltip("Reference orthographic size the game was designed with")]
    public float referenceOrthographicSize = 5f;
    
    [Header("Scaling Settings")]
    [Tooltip("How much to consider width vs height (0 = height only, 1 = width only, 0.5 = balanced)")]
    [Range(0f, 1f)]
    public float widthHeightMatch = 0.5f;
    
    [Tooltip("Minimum scale to prevent content becoming too small")]
    public float minScaleFactor = 0.7f;
    
    [Tooltip("Maximum scale to prevent content becoming too large")]
    public float maxScaleFactor = 1.3f;
    
    [Tooltip("Scale food items with tablecloth")]
    public bool scaleFoodItems = true;
    
    [Tooltip("Reposition food items to maintain relative positions")]
    public bool repositionFoodItems = true;
    
    [Header("Fine-Tuning")]
    [Tooltip("Additional scale multiplier for food items only")]
    public float foodItemScaleMultiplier = 1f;
    
    [Tooltip("Vertical offset for different aspect ratios")]
    public float verticalOffset = 0f;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool drawGizmos = true;
    
    // Store original transforms
    private Vector3 tableclothOriginalPosition;
    private Vector3 tableclothOriginalScale;
    private Dictionary<Transform, TransformData> foodItemsOriginalData = new Dictionary<Transform, TransformData>();
    
    private Camera mainCamera;
    private float currentScaleFactor = 1f;
    
    [System.Serializable]
    private class TransformData
    {
        public Vector3 position;
        public Vector3 localScale;
        public Vector3 relativePositionToTablecloth; // Position relative to tablecloth center
        
        public TransformData(Vector3 pos, Vector3 scl, Vector3 relPos)
        {
            position = pos;
            localScale = scl;
            relativePositionToTablecloth = relPos;
        }
    }
    
    void Awake()
    {
        InitializeScaler();
    }
    
    void Start()
    {
        // Re-apply scaling after all objects are initialized
        ApplyScaling();
    }
    
    void InitializeScaler()
    {
        // Get camera reference
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("TableClothScaler: Main camera not found!");
            return;
        }
        
        // Find tablecloth if not assigned
        if (tableclothRenderer == null)
        {
            GameObject clothObj = GameObject.Find("Cloth");
            if (clothObj != null)
            {
                tableclothRenderer = clothObj.GetComponent<SpriteRenderer>();
            }
        }
        
        // Auto-detect food items if enabled
        if (autoDetectFoodItems)
        {
            DetectFoodItems();
        }
        
        // Store original transforms
        StoreOriginalTransforms();
        
        // Apply initial scaling
        ApplyScaling();
    }
    
    void DetectFoodItems()
    {
        foodItems.Clear();
        
        // Find all ServeableItem objects
        ServeableItem[] serveableItems = FindObjectsOfType<ServeableItem>();
        
        foreach (ServeableItem item in serveableItems)
        {
            // Filter by tag if specified
            if (string.IsNullOrEmpty(foodItemTag) || item.CompareTag(foodItemTag))
            {
                foodItems.Add(item.transform);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"TableClothScaler: Auto-detected {foodItems.Count} food items");
        }
    }
    
    void StoreOriginalTransforms()
    {
        // Store tablecloth original data
        if (tableclothRenderer != null)
        {
            tableclothOriginalPosition = tableclothRenderer.transform.position;
            tableclothOriginalScale = tableclothRenderer.transform.localScale;
        }
        
        // Store food items original data with positions relative to tablecloth
        Vector3 tableclothCenter = tableclothOriginalPosition;
        
        foreach (Transform item in foodItems)
        {
            if (item != null)
            {
                Vector3 relativePos = item.position - tableclothCenter;
                foodItemsOriginalData[item] = new TransformData(
                    item.position, 
                    item.localScale,
                    relativePos
                );
            }
        }
    }
    
    public void ApplyScaling()
    {
        if (mainCamera == null || !mainCamera.orthographic)
        {
            Debug.LogWarning("TableClothScaler: Camera is not orthographic or not found!");
            return;
        }
        
        // Calculate the current scale factor
        currentScaleFactor = CalculateScaleFactor();
        
        // Apply scaling to tablecloth
        if (tableclothRenderer != null)
        {
            // Scale the tablecloth
            tableclothRenderer.transform.localScale = tableclothOriginalScale * currentScaleFactor;
            
            // Apply vertical offset if needed
            Vector3 newPosition = tableclothOriginalPosition;
            newPosition.y += verticalOffset * (currentScaleFactor - 1f);
            tableclothRenderer.transform.position = newPosition;
        }
        
        // Apply scaling to food items
        ApplyScalingToFoodItems();
        
        if (showDebugInfo)
        {
            Debug.Log($"TableClothScaler: Applied scale factor {currentScaleFactor:F3} " +
                     $"(Screen: {Screen.width}x{Screen.height}, Aspect: {(float)Screen.width / Screen.height:F3})");
        }
    }
    
    float CalculateScaleFactor()
    {
        // Get current screen aspect ratio
        float screenAspect = (float)Screen.width / Screen.height;
        float referenceAspect = referenceScreenWidth / referenceScreenHeight;
        
        // Calculate how much wider/narrower the screen is compared to reference
        float aspectRatioFactor = screenAspect / referenceAspect;
        
        // Calculate orthographic camera scaling
        float orthoScaleFactor = mainCamera.orthographicSize / referenceOrthographicSize;
        
        // For width-based scaling
        float widthBasedScale = aspectRatioFactor;
        
        // For height-based scaling (orthographic size already handles this)
        float heightBasedScale = 1f;
        
        // Combine based on widthHeightMatch
        // 0 = purely height-based (content scales with orthographic size)
        // 1 = purely width-based (content scales to fill width)
        // 0.5 = balanced between both
        float combinedAspectScale = Mathf.Lerp(heightBasedScale, widthBasedScale, widthHeightMatch);
        
        // Final scale combines orthographic scaling with aspect ratio adjustment
        float finalScale = orthoScaleFactor * combinedAspectScale;
        
        // Apply constraints
        return Mathf.Clamp(finalScale, minScaleFactor, maxScaleFactor);
    }
    
    void ApplyScalingToFoodItems()
    {
        if (!scaleFoodItems && !repositionFoodItems) return;
        
        Vector3 currentTableclothCenter = tableclothRenderer != null ? 
            tableclothRenderer.transform.position : tableclothOriginalPosition;
        
        foreach (var kvp in foodItemsOriginalData)
        {
            Transform item = kvp.Key;
            TransformData originalData = kvp.Value;
            
            if (item != null)
            {
                // Scale the food item if enabled
                if (scaleFoodItems)
                {
                    float itemScale = currentScaleFactor * foodItemScaleMultiplier;
                    item.localScale = originalData.localScale * itemScale;
                }
                
                // Reposition to maintain relative position to tablecloth
                if (repositionFoodItems)
                {
                    // Scale the relative position by the same factor as the tablecloth
                    Vector3 scaledRelativePos = originalData.relativePositionToTablecloth * currentScaleFactor;
                    item.position = currentTableclothCenter + scaledRelativePos;
                }
            }
        }
    }
    
    // Public methods for runtime management
    
    /// <summary>
    /// Manually refresh the food items list and recalculate scaling
    /// </summary>
    public void RefreshFoodItems()
    {
        if (autoDetectFoodItems)
        {
            DetectFoodItems();
        }
        StoreOriginalTransforms();
        ApplyScaling();
    }
    
    /// <summary>
    /// Add a new food item at runtime
    /// </summary>
    public void AddFoodItem(Transform newItem)
    {
        if (newItem != null && !foodItems.Contains(newItem))
        {
            foodItems.Add(newItem);
            
            // Calculate and store relative position
            Vector3 tableclothCenter = tableclothRenderer != null ? 
                tableclothRenderer.transform.position : tableclothOriginalPosition;
            Vector3 relativePos = newItem.position - tableclothCenter;
            
            foodItemsOriginalData[newItem] = new TransformData(
                newItem.position,
                newItem.localScale,
                relativePos
            );
            
            // Apply current scaling to the new item
            ApplyScalingToSingleItem(newItem);
        }
    }
    
    /// <summary>
    /// Remove a food item from the scaling system
    /// </summary>
    public void RemoveFoodItem(Transform item)
    {
        if (item != null)
        {
            foodItems.Remove(item);
            foodItemsOriginalData.Remove(item);
        }
    }
    
    void ApplyScalingToSingleItem(Transform item)
    {
        if (!foodItemsOriginalData.TryGetValue(item, out TransformData originalData)) return;
        
        Vector3 currentTableclothCenter = tableclothRenderer != null ? 
            tableclothRenderer.transform.position : tableclothOriginalPosition;
        
        if (scaleFoodItems)
        {
            float itemScale = currentScaleFactor * foodItemScaleMultiplier;
            item.localScale = originalData.localScale * itemScale;
        }
        
        if (repositionFoodItems)
        {
            Vector3 scaledRelativePos = originalData.relativePositionToTablecloth * currentScaleFactor;
            item.position = currentTableclothCenter + scaledRelativePos;
        }
    }
    
    // Handle screen resolution changes
    void OnRectTransformDimensionsChange()
    {
        if (Application.isPlaying)
        {
            ApplyScaling();
        }
    }
    
    // Editor helpers
    
    [ContextMenu("Store Current Positions")]
    public void StoreCurrentPositions()
    {
        StoreOriginalTransforms();
        Debug.Log("Stored current positions as original transforms");
    }
    
    [ContextMenu("Reset to Original Positions")]
    public void ResetToOriginalPositions()
    {
        if (tableclothRenderer != null)
        {
            tableclothRenderer.transform.position = tableclothOriginalPosition;
            tableclothRenderer.transform.localScale = tableclothOriginalScale;
        }
        
        foreach (var kvp in foodItemsOriginalData)
        {
            if (kvp.Key != null)
            {
                kvp.Key.position = kvp.Value.position;
                kvp.Key.localScale = kvp.Value.localScale;
            }
        }
        
        currentScaleFactor = 1f;
    }
    
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        
        // Draw tablecloth bounds
        if (tableclothRenderer != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(tableclothRenderer.bounds.center, tableclothRenderer.bounds.size);
            
            // Draw center point
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(tableclothRenderer.transform.position, 0.1f);
        }
        
        // Draw food item positions and connections to tablecloth
        if (tableclothRenderer != null)
        {
            Vector3 tableclothCenter = tableclothRenderer.transform.position;
            
            foreach (Transform item in foodItems)
            {
                if (item != null)
                {
                    // Draw food item position
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(item.position, 0.15f);
                    
                    // Draw line from tablecloth center to food item
                    Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                    Gizmos.DrawLine(tableclothCenter, item.position);
                }
            }
        }
        
        // Display current scale factor
        if (Application.isPlaying && showDebugInfo)
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"Scale Factor: {currentScaleFactor:F3}"
            );
        }
    }
#endif
}