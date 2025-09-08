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
    
    [Header("Audio Feedback")]
    [Tooltip("Play rejection sound through AudioManager (recommended)")]
    public bool useAudioManager = true;
    
    [Header("Animation Settings")]
    [Tooltip("How intense the shake effect should be")]
    public float shakeIntensity = 0.15f;
    [Tooltip("How long the shake effect lasts")]
    public float shakeDuration = 0.4f;
    [Tooltip("How many times the item shakes")]
    public int shakeCount = 3;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        // Auto-find OrderSystem if not assigned
        if (orderSystem == null)
        {
            orderSystem = FindObjectOfType<OrderSystem>();
            if (orderSystem == null)
            {
                Debug.LogError($"ServeableItem {foodType}: OrderSystem not found in scene!");
                return;
            }
        }

        // Audio setup is handled by AudioManager - no individual AudioSource needed
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
        // ✅ SUCCESS FEEDBACK: Order items already pop and disappear via ServedItemVisual
        // The visual feedback happens on the order display items, not on the serveable item
        // This is correct behavior - player sees the order item disappear with pop effect
        
        DebugLog($"{foodType}: Item served successfully - order item will pop and disappear");
        
        // Optional: Could add subtle success effect on the serveable item here
        // For now, keeping it clean since the main feedback is on the order
    }

    // Called when item was not needed for current order
    void OnItemRejected()
    {
        // ❌ REJECTION FEEDBACK: Shake the item and play rejection sound
        DebugLog($"{foodType}: Item rejected - playing shake and sound feedback");
        
        // Start shake animation
        StartCoroutine(ShakeWithSound());
    }

    // Enhanced shake effect with sound for wrong item clicks
    System.Collections.IEnumerator ShakeWithSound()
    {
        // Play rejection sound immediately
        PlayRejectionSound();
        
        Vector3 originalPosition = transform.position;
        float shakeDelay = shakeDuration / (shakeCount * 2); // Time for each shake direction
        
        for (int i = 0; i < shakeCount; i++)
        {
            // Shake right
            transform.position = originalPosition + new Vector3(shakeIntensity, 0, 0);
            yield return new WaitForSeconds(shakeDelay);
            
            // Shake left
            transform.position = originalPosition + new Vector3(-shakeIntensity, 0, 0);
            yield return new WaitForSeconds(shakeDelay);
        }
        
        // Return to original position
        transform.position = originalPosition;
        
        DebugLog($"{foodType}: Shake effect completed");
    }

    // Play rejection sound effect through AudioManager
    void PlayRejectionSound()
    {
        if (useAudioManager && AudioManager.Instance != null)
        {
            // Use the centralized AudioManager for consistent audio control
            AudioManager.Instance.PlayWrongItemSFX();
            DebugLog($"{foodType}: Played rejection sound via AudioManager");
        }
        else
        {
            DebugLog($"{foodType}: AudioManager not available for rejection sound");
        }
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

    // Context menu to test rejection feedback
    [ContextMenu("Test Rejection Feedback")]
    void TestRejectionFeedback()
    {
        if (Application.isPlaying)
        {
            OnItemRejected();
        }
        else
        {
            Debug.LogWarning("Can only test rejection in Play Mode!");
        }
    }
    
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