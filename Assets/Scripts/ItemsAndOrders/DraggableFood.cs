using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class DraggableFood : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Drag Settings")] [Tooltip("How smoothly the item follows the drag")] [Range(0.1f, 1f)]
    public float dragSmoothness = 0.8f;

    [Tooltip("Z-offset when dragging (brings item to front)")]
    public float dragZOffset = -1f;

    [Header("Visual Feedback")] [Tooltip("Scale multiplier when dragging")] [Range(0.8f, 1.5f)]
    public float dragScale = 1.05f;

    [Tooltip("Color when touched/dragging (darker)")]
    public Color touchedColor = new(0.7f, 0.7f, 0.7f, 1f);

    [Tooltip("Color when overlap is detected (reddish)")]
    public Color overlapColor = new(1f, 0.5f, 0.5f, 1f);

    [Header("iOS-Style Wiggle (Built-in)")] [Tooltip("Enable wiggle effect when dragging is enabled")]
    public bool enableWiggle = true;

    [Header("Collision Settings")] [Tooltip("Enable overlap prevention between items")]
    public bool preventOverlap = true;

    [Tooltip("Minimum distance between food items (based on sprite bounds)")] [Range(0f, 2f)]
    public float minDistanceBetweenItems = 0f;

    [Tooltip("Use sprite bounds for accurate collision detection")]
    public bool useSpriteBounds = true;

    [Header("Debug")] public bool enableDebugLogs = false;

    [Tooltip("Show visual gizmos for collision detection")]
    public bool showCollisionGizmos = false;

    // Authentic iOS wiggle constants (reverse-engineered from iOS SpringBoard)
    private const float WIGGLE_ROTATION = 1f; // ±1 degree (iOS standard)  
    private const float WIGGLE_SPEED = 20f; // ~0.1s per cycle (fast like iOS)
    private const float WIGGLE_POSITION = 0.01f; // ~1 pixel equivalent in Unity units

    // Private variables
    private bool isDraggingEnabled = false;
    private bool isDragging = false;
    private bool isTouched = false;
    private bool isWiggling = false;
    private bool isWigglePaused = false; // Paused for a drag; resumes when the drag ends
    private bool hasOverlap = false;

    // Transform values
    private Vector3 originalScale;
    private Color originalColor;
    private float originalZ;
    private Vector3 originalPosition;
    private Vector3 originalRotation;
    private Vector3 lastValidPosition;

    // Wiggle variables
    private float wigglePhaseOffset;
    private Coroutine wiggleCoroutine;

    // References
    private TableLayer tableLayer;
    private GamePhaseManager gamePhaseManager;
    private ServeableItem[] allFoodItems;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private Vector3 offset;

    // Collision detection
    private float itemRadius;
    private Coroutine overlapCheckCoroutine;

    // NEW: Refill system integration
    private RefillableItem refillableItem;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        originalScale = transform.localScale;
        originalZ = transform.position.z;

        // Store original transform for wiggle
        originalPosition = transform.localPosition;
        originalRotation = transform.localEulerAngles;
        lastValidPosition = transform.position;

        // iOS-authentic random phase offset (prevents sync between items)
        wigglePhaseOffset = Random.Range(0f, 0.1f); // Random 0-100ms offset like iOS

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            CalculateItemRadius();
        }

        // NEW: Get RefillableItem component
        refillableItem = GetComponent<RefillableItem>();
    }

    public void Initialize(TableLayer tableLayerRef, GamePhaseManager phaseManagerRef, ServeableItem[] foodItemsArray)
    {
        tableLayer = tableLayerRef;
        gamePhaseManager = phaseManagerRef;
        allFoodItems = foodItemsArray;

        // Recalculate radius in case sprite was loaded after Awake
        CalculateItemRadius();

        DebugLog($"Initialized {gameObject.name} with radius: {itemRadius}");
    }

    private void CalculateItemRadius()
    {
        if (useSpriteBounds)
        {
            // NEW: Use RefillableItem bounds if available (includes padding)
            Bounds bounds;
            if (refillableItem != null)
            {
                bounds = refillableItem.GetBoundsWithPadding();
                DebugLog($"Using RefillableItem bounds with padding for {gameObject.name}");
            }
            else
            {
                // Try BoxCollider (3D) first
                var boxCollider3D = GetComponent<BoxCollider>();
                if (boxCollider3D != null)
                {
                    bounds = boxCollider3D.bounds;
                    DebugLog($"Using BoxCollider (3D) bounds for {gameObject.name}");
                }
                else if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    // Use actual sprite bounds for accurate collision
                    bounds = spriteRenderer.bounds;
                    DebugLog($"Using SpriteRenderer bounds for {gameObject.name}");
                }
                else
                {
                    // Fallback to default size
                    bounds = new Bounds(transform.position, Vector3.one * 0.5f);
                    DebugLog($"Using fallback bounds for {gameObject.name}");
                }
            }

            // Use the larger of width/height and divide by 2 for radius
            itemRadius = Mathf.Max(bounds.size.x, bounds.size.y) / 2f;
        }
        else
        {
            // Simple approximation based on transform scale
            itemRadius = Mathf.Max(transform.localScale.x, transform.localScale.y) * 0.5f;
        }

        DebugLog($"Calculated radius for {gameObject.name}: {itemRadius}");
    }

    public void SetDraggingEnabled(bool enabled)
    {
        isDraggingEnabled = enabled;

        if (isDraggingEnabled)
        {
            // ✅ Check immediately when enabling
            CheckAndUpdateOverlapState();

            // ✅ Start periodic checking coroutine
            if (overlapCheckCoroutine == null)
                overlapCheckCoroutine = StartCoroutine(OverlapCheckLoop());

            if (enableWiggle && !isWiggling)
                StartWiggle();
        }
        else
        {
            // ✅ Stop checking coroutine when disabled
            if (overlapCheckCoroutine != null)
            {
                StopCoroutine(overlapCheckCoroutine);
                overlapCheckCoroutine = null;
            }

            if (isWiggling)
                StopWiggle();

            // ✅ Clear overlap state when disabling
            hasOverlap = false;
            RestoreOriginalVisuals();
        }
    }

    // === WIGGLE FUNCTIONALITY ===

    private void StartWiggle()
    {
        if (isWiggling || !enableWiggle) return;

        isWiggling = true;
        wiggleCoroutine = StartCoroutine(WiggleCoroutine());

        DebugLog($"Started iOS wiggle for {gameObject.name}");
    }

    /// <summary>
    /// Coroutine that checks for overlaps periodically.
    /// Only runs when dragging is enabled, automatically stops when disabled.
    /// Replaces the Update() method with a more efficient approach.
    /// </summary>
    private IEnumerator OverlapCheckLoop()
    {
        while (isDraggingEnabled)
        {
            CheckAndUpdateOverlapState();
            yield return new WaitForSeconds(0.1f); // Check every 100ms
        }
    }

    /// <summary>
    /// Check current overlap state and update visuals if needed
    /// </summary>
    private void CheckAndUpdateOverlapState()
    {
        var currentOverlap = CheckOverlapAtPosition(transform.position);

        if (currentOverlap != hasOverlap)
        {
            hasOverlap = currentOverlap;

            // Update visuals based on current state
            if (hasOverlap)
                ApplyOverlapVisuals();
            else if (isTouched)
                ApplyTouchedVisuals();
            else
                RestoreOriginalVisuals();

            // Notify GamePhaseManager about overlap state change
            if (gamePhaseManager != null) gamePhaseManager.OnItemOverlapChanged();

            DebugLog($"Overlap state changed to: {hasOverlap}");
        }
    }

    private void StopWiggle()
    {
        if (!isWiggling && !isWigglePaused) return;

        isWiggling = false;
        isWigglePaused = false;

        if (wiggleCoroutine != null)
        {
            StopCoroutine(wiggleCoroutine);
            wiggleCoroutine = null;
        }

        // Reset transform to original state
        transform.localPosition = originalPosition;
        transform.localEulerAngles = originalRotation;

        DebugLog($"Stopped iOS wiggle for {gameObject.name}");
    }

    private void PauseWiggle()
    {
        if (!isWiggling) return;

        // Clear the running flag so ResumeWiggle/StartWiggle can restart the coroutine later.
        isWiggling = false;
        isWigglePaused = true;

        if (wiggleCoroutine != null)
        {
            StopCoroutine(wiggleCoroutine);
            wiggleCoroutine = null;
        }

        // Don't reset transform - keep current position
        DebugLog($"Paused wiggle for {gameObject.name}");
    }

    private void ResumeWiggle()
    {
        isWigglePaused = false;

        if (isDraggingEnabled && enableWiggle && !isWiggling)
            StartWiggle();
    }

    private IEnumerator WiggleCoroutine()
    {
        while (isWiggling && isDraggingEnabled && !isDragging)
        {
            var time = Time.time + wigglePhaseOffset;

            // iOS-authentic wiggle motion: rotation + subtle position
            var rotationWiggle = Mathf.Sin(time * WIGGLE_SPEED) * WIGGLE_ROTATION;
            var positionWiggleX = Mathf.Sin(time * WIGGLE_SPEED * 0.7f) * WIGGLE_POSITION;
            var positionWiggleY = Mathf.Cos(time * WIGGLE_SPEED * 0.9f) * WIGGLE_POSITION;

            // Apply wiggle
            transform.localEulerAngles = originalRotation + Vector3.forward * rotationWiggle;
            transform.localPosition = originalPosition + new Vector3(positionWiggleX, positionWiggleY, 0);

            yield return null;
        }
    }

    // === COLLISION DETECTION ===

    private bool CheckOverlapAtPosition(Vector3 testPosition)
    {
        if (!preventOverlap || allFoodItems == null) return false;

        foreach (var otherItem in allFoodItems)
        {
            if (otherItem == null || otherItem.gameObject == gameObject || !otherItem.gameObject.activeInHierarchy)
                continue;

            var otherPosition = otherItem.transform.position;

            // Calculate 2D distance (ignore Z)
            var distance = Vector2.Distance(
                new Vector2(testPosition.x, testPosition.y),
                new Vector2(otherPosition.x, otherPosition.y)
            );

            // Get other item's radius
            var otherRadius = GetItemRadius(otherItem);

            // Calculate minimum allowed distance
            var minDistance = itemRadius + otherRadius + minDistanceBetweenItems;

            // Check if overlap would occur
            if (distance < minDistance)
            {
                DebugLog(
                    $"Overlap detected with {otherItem.GetFoodType()} - Distance: {distance:F2}, MinDistance: {minDistance:F2}");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Public method to check if this item currently has overlap
    /// </summary>
    public bool HasOverlap()
    {
        return CheckOverlapAtPosition(transform.position);
    }

    private float GetItemRadius(ServeableItem item)
    {
        // Try to get DraggableFood component to use its calculated radius
        var draggable = item.GetComponent<DraggableFood>();
        if (draggable != null) return draggable.itemRadius;

        // NEW: Try to get RefillableItem bounds (now uses BoxCollider 3D)
        var refillable = item.GetComponent<RefillableItem>();
        if (refillable != null)
        {
            var bounds = refillable.GetBoundsWithPadding();
            return Mathf.Max(bounds.size.x, bounds.size.y) / 2f;
        }

        // Try BoxCollider (3D) first
        var boxCollider3D = item.GetComponent<BoxCollider>();
        if (boxCollider3D != null)
        {
            var bounds = boxCollider3D.bounds;
            return Mathf.Max(bounds.size.x, bounds.size.y) / 2f;
        }

        // Fallback: calculate from sprite renderer
        var sr = item.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null && useSpriteBounds)
        {
            var bounds = sr.bounds;
            return Mathf.Max(bounds.size.x, bounds.size.y) / 2f;
        }

        // Default fallback
        return 0.5f;
    }

    // === DRAG FUNCTIONALITY ===

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isDraggingEnabled) return;
        isTouched = true;
        ApplyTouchedVisuals();

        var worldPos = mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = originalZ;
        offset = transform.position - worldPos;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDraggingEnabled) return;
        isTouched = false;
        if (!isDragging) RestoreOriginalVisuals();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isDraggingEnabled) return;
        StartDragging();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggingEnabled || !isDragging) return;

        var worldPos = mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = originalZ;
        var targetPosition = worldPos + offset;

        // Clamp to table bounds
        if (tableLayer != null)
            targetPosition = tableLayer.ClampToTableBounds(targetPosition);

        // Check for overlap at target position
        var wouldOverlap = CheckOverlapAtPosition(targetPosition);
        var overlapStateChanged = wouldOverlap != hasOverlap;

        // Update visual feedback
        if (wouldOverlap)
        {
            if (!hasOverlap)
            {
                hasOverlap = true;
                ApplyOverlapVisuals();
                DebugLog("Overlap detected - showing red but allowing movement");
            }
        }
        else
        {
            if (hasOverlap)
            {
                hasOverlap = false;
                ApplyTouchedVisuals();
            }
        }

        // ALWAYS allow movement (no blocking)
        var smoothPosition = Vector3.Lerp(transform.position, targetPosition, dragSmoothness);
        smoothPosition.z = originalZ + dragZOffset;
        transform.position = smoothPosition;

        // Update last valid position
        lastValidPosition = smoothPosition;
        lastValidPosition.z = originalZ;

        // Notify GamePhaseManager when overlap state changes
        if (overlapStateChanged && gamePhaseManager != null) gamePhaseManager.OnItemOverlapChanged();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggingEnabled) return;
        StopDragging();
    }

    private void StartDragging()
    {
        isDragging = true;
        transform.localScale = originalScale * dragScale;
        lastValidPosition = transform.position;
        lastValidPosition.z = originalZ;

        // Pause wiggle while dragging
        PauseWiggle();
    }

    private void StopDragging()
    {
        isDragging = false;
        hasOverlap = false;
        transform.localScale = originalScale;

        // Snap to last valid position
        var finalPosition = lastValidPosition;
        finalPosition.z = originalZ;
        transform.position = finalPosition;

        // Update original position for wiggle
        UpdateOriginalPosition();

        // Resume wiggle if still enabled
        ResumeWiggle();

        if (!isTouched) RestoreOriginalVisuals();

        // Final check after drag ends
        if (gamePhaseManager != null) gamePhaseManager.OnItemOverlapChanged();
    }

    private void UpdateOriginalPosition()
    {
        originalPosition = transform.localPosition;
    }

    private void ApplyTouchedVisuals()
    {
        if (spriteRenderer != null) spriteRenderer.color = touchedColor;
    }

    private void ApplyOverlapVisuals()
    {
        if (spriteRenderer != null) spriteRenderer.color = overlapColor;
    }

    private void RestoreOriginalVisuals()
    {
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    private void OnDisable()
    {
        // ✅ Stop overlap check coroutine if running
        if (overlapCheckCoroutine != null)
        {
            StopCoroutine(overlapCheckCoroutine);
            overlapCheckCoroutine = null;
        }

        // Clean up wiggle coroutine if object is disabled
        StopWiggle();
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"DraggableFood ({gameObject.name}): {message}");
    }

    // === DEBUG VISUALIZATION ===

    private void OnDrawGizmos()
    {
        if (!showCollisionGizmos || !isDraggingEnabled) return;

        // Draw this item's collision radius
        Gizmos.color = hasOverlap ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, itemRadius);

        // Draw minimum distance circle
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, itemRadius + minDistanceBetweenItems);

        // NEW: Draw refill padding if available
        if (refillableItem != null)
        {
            Gizmos.color = Color.blue;
            var bounds = refillableItem.GetBoundsWithPadding();
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}