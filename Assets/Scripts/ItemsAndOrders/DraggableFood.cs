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

    [Header("iOS-Style Wiggle (Built-in)")] [Tooltip("Enable wiggle effect when dragging is enabled")]
    public bool enableWiggle = true;

    [Header("Collision Settings")] [Tooltip("Minimum distance between food items")]
    public float minDistanceBetweenItems = 1f;

    [Header("Debug")] public bool enableDebugLogs = false;

    // Authentic iOS wiggle constants (reverse-engineered from iOS SpringBoard)
    private const float WIGGLE_ROTATION = 1f; // ±1 degree (iOS standard)  
    private const float WIGGLE_SPEED = 20f; // ~0.1s per cycle (fast like iOS)
    private const float WIGGLE_POSITION = 0.01f; // ~1 pixel equivalent in Unity units

    // Private variables
    private bool isDraggingEnabled = false;
    private bool isDragging = false;
    private bool isTouched = false;
    private bool isWiggling = false;

    // Transform values
    private Vector3 originalScale;
    private Color originalColor;
    private float originalZ;
    private Vector3 originalPosition;
    private Vector3 originalRotation;

    // Wiggle variables
    private float wigglePhaseOffset;
    private Coroutine wiggleCoroutine;

    // References
    private TableLayer tableLayer;
    private ServeableItem[] allFoodItems;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private Vector3 offset;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        originalScale = transform.localScale;
        originalZ = transform.position.z;

        // Store original transform for wiggle
        originalPosition = transform.localPosition;
        originalRotation = transform.localEulerAngles;

        // iOS-authentic random phase offset (prevents sync between items)
        wigglePhaseOffset = Random.Range(0f, 0.1f); // Random 0-100ms offset like iOS

        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    public void Initialize(TableLayer tableLayerRef, GamePhaseManager phaseManagerRef, ServeableItem[] foodItemsArray)
    {
        tableLayer = tableLayerRef;
        allFoodItems = foodItemsArray;
        Debug.Log($"DraggableFood: Initialized {gameObject.name}");
    }

    public void SetDraggingEnabled(bool enabled)
    {
        isDraggingEnabled = enabled;

        if (enabled)
        {
            // Start wiggling when dragging is enabled
            if (enableWiggle) StartWiggle();
        }
        else
        {
            // Stop wiggling when dragging is disabled
            StopWiggle();

            // Stop any active dragging
            if (isDragging) StopDragging();
        }
    }

    // === WIGGLE FUNCTIONALITY ===

    private void StartWiggle()
    {
        if (isWiggling || !enableWiggle) return;

        isWiggling = true;
        wiggleCoroutine = StartCoroutine(WiggleCoroutine());

        if (enableDebugLogs)
            Debug.Log($"Started iOS wiggle for {gameObject.name}");
    }

    private void StopWiggle()
    {
        if (!isWiggling) return;

        isWiggling = false;

        if (wiggleCoroutine != null)
        {
            StopCoroutine(wiggleCoroutine);
            wiggleCoroutine = null;
        }

        // Restore original transform
        if (!isDragging)
        {
            transform.localRotation = Quaternion.Euler(originalRotation);
            transform.localPosition = originalPosition;
        }

        if (enableDebugLogs)
            Debug.Log($"Stopped iOS wiggle for {gameObject.name}");
    }

    private void PauseWiggle()
    {
        if (wiggleCoroutine != null)
        {
            StopCoroutine(wiggleCoroutine);
            wiggleCoroutine = null;
        }

        isWiggling = false;
    }

    private void ResumeWiggle()
    {
        if (isDraggingEnabled && enableWiggle && !isDragging) StartWiggle();
    }

    private IEnumerator WiggleCoroutine()
    {
        // iOS-authentic timing: wait for random offset before starting
        yield return new WaitForSeconds(wigglePhaseOffset);

        while (isWiggling)
        {
            // iOS SpringBoard timing: 0.1 second cycles
            var time = Time.time * WIGGLE_SPEED;

            // Authentic iOS rotation: ±1 degree
            var rotationZ = Mathf.Sin(time) * WIGGLE_ROTATION;
            var newRotation = originalRotation;
            newRotation.z += rotationZ;

            // Very subtle position wiggle (like iOS)
            var positionOffset = Vector3.zero;
            positionOffset.x = Mathf.Sin(time * 1.1f) * WIGGLE_POSITION;
            positionOffset.y = Mathf.Cos(time * 0.9f) * WIGGLE_POSITION;

            // Apply transformations
            transform.localRotation = Quaternion.Euler(newRotation);
            transform.localPosition = originalPosition + positionOffset;

            yield return null;
        }
    }

    private void UpdateOriginalPosition()
    {
        originalPosition = transform.localPosition;
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

        if (tableLayer != null) targetPosition = tableLayer.ClampToTableBounds(targetPosition);

        var smoothPosition = Vector3.Lerp(transform.position, targetPosition, dragSmoothness);
        smoothPosition.z = originalZ + dragZOffset;
        transform.position = smoothPosition;
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

        // Pause wiggle while dragging
        PauseWiggle();
    }

    private void StopDragging()
    {
        isDragging = false;
        transform.localScale = originalScale;
        var finalPosition = transform.position;
        finalPosition.z = originalZ;
        transform.position = finalPosition;

        // Update original position for wiggle
        UpdateOriginalPosition();

        // Resume wiggle if still enabled
        ResumeWiggle();

        if (!isTouched) RestoreOriginalVisuals();
    }

    private void ApplyTouchedVisuals()
    {
        if (spriteRenderer != null) spriteRenderer.color = touchedColor;
    }

    private void RestoreOriginalVisuals()
    {
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    private void OnDisable()
    {
        // Clean up wiggle coroutine if object is disabled
        StopWiggle();
    }
}