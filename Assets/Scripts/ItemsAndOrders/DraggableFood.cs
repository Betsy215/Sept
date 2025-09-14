using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableFood : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Drag Settings")]
    [Tooltip("How smoothly the item follows the drag")]
    [Range(0.1f, 1f)]
    public float dragSmoothness = 0.8f;
    
    [Tooltip("Z-offset when dragging (brings item to front)")]
    public float dragZOffset = -1f;
    
    [Header("Visual Feedback")]
    [Tooltip("Scale multiplier when dragging")]
    [Range(0.8f, 1.5f)]
    public float dragScale = 1.05f;
    
    [Tooltip("Color when touched/dragging (darker)")]
    public Color touchedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    
    [Header("Collision Settings")]
    [Tooltip("Minimum distance between food items")]
    public float minDistanceBetweenItems = 1f;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    // Private variables
    private bool isDraggingEnabled = false;
    private bool isDragging = false;
    private bool isTouched = false;
    private Vector3 originalScale;
    private Color originalColor;
    private float originalZ;
    
    // References
    private TableLayer tableLayer;
    private ServeableItem[] allFoodItems;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private Vector3 offset;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        originalScale = transform.localScale;
        originalZ = transform.position.z;
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
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
        if (!enabled && isDragging)
        {
            StopDragging();
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isDraggingEnabled) return;
        isTouched = true;
        ApplyTouchedVisuals();
        
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(eventData.position);
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
        
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = originalZ;
        Vector3 targetPosition = worldPos + offset;
        
        if (tableLayer != null)
        {
            targetPosition = tableLayer.ClampToTableBounds(targetPosition);
        }
        
        Vector3 smoothPosition = Vector3.Lerp(transform.position, targetPosition, dragSmoothness);
        smoothPosition.z = originalZ + dragZOffset;
        transform.position = smoothPosition;
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDraggingEnabled) return;
        StopDragging();
    }
    
    void StartDragging()
    {
        isDragging = true;
        transform.localScale = originalScale * dragScale;
    }
    
    void StopDragging()
    {
        isDragging = false;
        transform.localScale = originalScale;
        Vector3 finalPosition = transform.position;
        finalPosition.z = originalZ;
        transform.position = finalPosition;
        
        if (!isTouched) RestoreOriginalVisuals();
    }
    
    void ApplyTouchedVisuals()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = touchedColor;
        }
    }
    
    void RestoreOriginalVisuals()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
}