using UnityEngine;
using UnityEngine.EventSystems;

public class ServeableItem : MonoBehaviour
{
    [Header("Item Settings")]
    [Tooltip("Type of food this item represents (must match OrderSystem food types)")]
    public string foodType = "Bread"; // e.g., "Bread", "Apple", "Juice", etc.
    
    [Header("References")]
    [Tooltip("Reference to OrderSystem - will auto-find if not assigned")]
    public OrderSystem orderSystem;
    
    [Header("Visual Feedback")]
    [Tooltip("Canvas used for UI popup detection")]
    public GameObject popupCanvas;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;

    void Start()
    {
        // Auto-find OrderSystem if not assigned
        if (orderSystem == null)
        {
            orderSystem = FindObjectOfType<OrderSystem>();
            if (orderSystem == null)
            {
                Debug.LogError($"ServeableItem {foodType}: OrderSystem not found in scene!");
            }
        }

        // Ensure we have a collider for click detection
        SetupCollider();
        
        DebugLog($"ServeableItem {foodType} initialized successfully");
    }

    void SetupCollider()
    {
        Collider itemCollider = GetComponent<Collider>();
        if (itemCollider == null)
        {
            // Automatically create collider based on sprite size
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                // Create BoxCollider that matches sprite bounds
                BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
                
                // Get sprite bounds and convert to local space
                Bounds spriteBounds = spriteRenderer.bounds;
                Vector3 localSize = transform.InverseTransformVector(spriteBounds.size);
                
                boxCollider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), 0.5f);
                boxCollider.isTrigger = false; // Make it solid for clicking
                
                DebugLog($"{foodType}: Auto-created collider with size {boxCollider.size}");
            }
            else
            {
                // Fallback: create default sized collider
                BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(1f, 1f, 0.5f);
                
                Debug.LogWarning($"{foodType}: No sprite found, created default collider");
            }
        }
        else
        {
            DebugLog($"{foodType}: Using existing collider");
        }
    }

    // Handle mouse clicks on this item
    void OnMouseDown()
    {
        // Check if we're clicking over UI elements
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool popupActive = popupCanvas != null && popupCanvas.activeInHierarchy;
        
        DebugLog($"{foodType} clicked. Over UI: {overUI}, Popup active: {popupActive}");

        // Block clicks if over UI AND popup is active
        if (overUI && popupActive) 
        {
            DebugLog($"{foodType}: Click blocked by popup");
            return;
        }

        // Process the item click
        OnItemClicked();
    }

    // Main logic for serving this item
    void OnItemClicked()
    {
        DebugLog($"Player clicked on {foodType}");

        // Check if OrderSystem is available
        if (orderSystem == null)
        {
            Debug.LogError($"{foodType}: OrderSystem reference is missing!");
            return;
        }

        // Check if there's an active order
        if (!orderSystem.IsOrderActive())
        {
            DebugLog($"{foodType}: No active order to serve");
            return;
        }

        // Try to serve this item to the current order
        bool itemServed = orderSystem.TryServeItem(foodType);
        
        if (itemServed)
        {
            DebugLog($"{foodType}: Successfully served!");
            OnItemServedSuccessfully();
        }
        else
        {
            DebugLog($"{foodType}: Not needed in current order");
            OnItemRejected();
        }
    }

    // Called when item was successfully served
    void OnItemServedSuccessfully()
    {
        // TODO: Add success animation/feedback here later
        // For now, just log success
        DebugLog($"{foodType}: Item served successfully");
    }

    // Called when item was not needed for current order
    void OnItemRejected()
    {
        // TODO: Add rejection animation/feedback here later
        // For now, just log rejection
        DebugLog($"{foodType}: Item rejected - not needed");
        
        // Optional: Add shake effect or other visual feedback
        StartCoroutine(ShakeEffect());
    }

    // Simple shake effect for wrong item clicks
    System.Collections.IEnumerator ShakeEffect()
    {
        Vector3 originalPosition = transform.position;
        float shakeIntensity = 0.1f;
        float shakeDuration = 0.3f;
        
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            
            float x = originalPosition.x + Random.Range(-shakeIntensity, shakeIntensity);
            float y = originalPosition.y + Random.Range(-shakeIntensity, shakeIntensity);
            
            transform.position = new Vector3(x, y, originalPosition.z);
            
            yield return null;
        }
        
        // Return to original position
        transform.position = originalPosition;
    }

    // Debug logging helper
    void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ServeableItem] {message}");
        }
    }

    // Public methods for external access
    public string GetFoodType()
    {
        return foodType;
    }

    public bool CanServe()
    {
        return orderSystem != null && orderSystem.IsOrderActive();
    }

    // Context menu for testing in editor
    [ContextMenu("Test Serve Item")]
    void TestServeItem()
    {
        if (Application.isPlaying)
        {
            OnItemClicked();
        }
        else
        {
            Debug.LogWarning("Can only test serving in Play Mode!");
        }
    }

    // Context menu to recalculate collider
    [ContextMenu("Recalculate Collider")]
    void RecalculateCollider()
    {
        // Remove existing collider
        Collider existingCollider = GetComponent<Collider>();
        if (existingCollider != null)
        {
            DestroyImmediate(existingCollider);
        }
        
        // Setup new collider
        SetupCollider();
        
        Debug.Log($"{foodType}: Collider recalculated!");
    }
}