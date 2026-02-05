using UnityEngine;
using UnityEngine.EventSystems;

public class ServeableItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDraggable
{
    [Header("Item Settings")] [Tooltip("Type of food this item represents (must match OrderSystem food types)")]
    public string foodType = ""; // e.g., "Bread", "Apple", "Juice", etc.

    [Header("References")] [Tooltip("Reference to OrderSystem - will auto-find if not assigned")]
    public OrderSystem orderSystem;

    [Header("Visual Feedback")] [Tooltip("Canvas used for UI popup detection")]
    public GameObject popupCanvas;

    [Header("Audio Feedback")] [Tooltip("Play rejection sound through AudioManager (recommended)")]
    public bool useAudioManager = true;

    [Header("Animation Settings")] [Tooltip("How intense the shake effect should be")]
    public float shakeIntensity = 0.15f;

    [Tooltip("How long the shake effect lasts")]
    public float shakeDuration = 0.4f;

    [Tooltip("How many times the item shakes")]
    public int shakeCount = 3;

    [Header("Debug")] public bool enableDebugLogs = true;

    [Header("Phase Management")] private bool servingEnabled = true;

    // NEW: Refill system integration
    private RefillableItem refillableItem;
    private RefillSystem refillSystem;

    public void SetDraggingEnabled(bool enabled)
    {
        var draggable = GetComponent<DraggableFood>();
        if (draggable == null && enabled)
            draggable = gameObject.AddComponent<DraggableFood>();

        if (draggable != null)
            draggable.SetDraggingEnabled(enabled);

        // Disable serving during arrangement, enable during play
        SetServingEnabled(!enabled);
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
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

        // NEW: Get refill components
        refillableItem = GetComponent<RefillableItem>();
        refillSystem = FindObjectOfType<RefillSystem>();

        // Audio setup is handled by AudioManager - no individual AudioSource needed
        DebugLog($"ServeableItem {foodType} initialized successfully");
    }

    public void SetServingEnabled(bool enabled)
    {
        servingEnabled = enabled;

        if (enableDebugLogs) Debug.Log($"{foodType}: Serving {(enabled ? "enabled" : "disabled")}");
    }

    public string GetFoodType()
    {
        return foodType;
    }

    // NEW: IPointerDownHandler implementation for hold detection
    // NOTE: This works with both 2D and 3D colliders
    public void OnPointerDown(PointerEventData eventData)
    {
        DebugLog($"ServeableItem OnPointerDown - servingEnabled:{servingEnabled}");

        if (!servingEnabled) return;

        // Check if UI popup is blocking
        if (IsUIBlocking())
        {
            DebugLog("UI blocking, ignoring pointer down");
            return;
        }

        // Forward to refillable item for hold detection
        if (refillableItem != null)
        {
            DebugLog("Forwarding OnPointerDown to RefillableItem");
            refillableItem.OnPointerDown();
        }
        else
        {
            DebugLog("No RefillableItem found to forward to");
        }

        DebugLog($"{foodType}: Pointer down detected");
    }

    // NEW: IPointerUpHandler implementation for hold detection
    public void OnPointerUp(PointerEventData eventData)
    {
        DebugLog($"ServeableItem OnPointerUp - servingEnabled:{servingEnabled}");

        if (!servingEnabled) return;

        // Forward to refillable item
        if (refillableItem != null)
        {
            DebugLog("Forwarding OnPointerUp to RefillableItem");
            refillableItem.OnPointerUp();
        }
        else
        {
            DebugLog("No RefillableItem found to forward to");
        }

        // Check if it was a click (not a hold)
        // This will be handled by the OnMouseUpAsButton method
        DebugLog($"{foodType}: Pointer up detected");
    }

    // Keep existing OnMouseUpAsButton for click detection
    private void OnMouseUpAsButton()
    {
        if (!servingEnabled) return;

        // Check if UI popup is blocking
        if (IsUIBlocking()) return;

        // Check if item is out of stock
        if (refillableItem != null && refillableItem.IsOutOfStock())
        {
            DebugLog($"{foodType}: Cannot serve - out of stock!");
            OnItemRejected(); // Play rejection feedback
            return;
        }

        // Process the item click
        OnItemClicked();
    }

    private bool IsUIBlocking()
    {
        // Check if clicking over UI elements
        var overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        var popupActive = popupCanvas != null && popupCanvas.activeInHierarchy;

        DebugLog($"{foodType}: UI Check - Over UI: {overUI}, Popup active: {popupActive}");

        // Block clicks if over UI AND popup is active
        if (overUI && popupActive)
        {
            DebugLog($"{foodType}: Click blocked by popup");
            return true;
        }

        return false;
    }

    // Main logic for serving this item
    private void OnItemClicked()
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
        var itemServed = orderSystem.TryServeItem(foodType);

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
    private void OnItemServedSuccessfully()
    {
        // ✅ SUCCESS FEEDBACK: Order items already pop and disappear via ServedItemVisual
        // The visual feedback happens on the order display items, not on the serveable item
        // This is correct behavior - player sees the order item disappear with pop effect

        DebugLog($"{foodType}: Item served successfully - order item will pop and disappear");

        // NEW: Notify refill system that item was served correctly
        if (refillSystem != null) refillSystem.OnItemServed(foodType, true);

        SendMessage("OnServedSuccessfully", SendMessageOptions.DontRequireReceiver);
    }

    // Called when item was not needed for current order
    private void OnItemRejected()
    {
        // ❌ REJECTION FEEDBACK: Shake the item and play rejection sound
        DebugLog($"{foodType}: Item rejected - playing shake and sound feedback");

        // NEW: Notify refill system that item was clicked but not served
        if (refillSystem != null) refillSystem.OnItemServed(foodType, false);

        // Start shake animation
        StartCoroutine(ShakeAnimation());

        // Play rejection sound
        PlayRejectionSound();
    }

    private System.Collections.IEnumerator ShakeAnimation()
    {
        var originalPosition = transform.position;

        for (var i = 0; i < shakeCount; i++)
        {
            // Shake left
            transform.position = originalPosition + Vector3.right * shakeIntensity;
            yield return new WaitForSeconds(shakeDuration / (shakeCount * 2f));

            // Shake right  
            transform.position = originalPosition + Vector3.left * shakeIntensity;
            yield return new WaitForSeconds(shakeDuration / (shakeCount * 2f));
        }

        // Return to original position
        transform.position = originalPosition;
    }

    private void PlayRejectionSound()
    {
        if (useAudioManager && AudioManager.Instance != null)
        {
            // AudioManager should have a rejection sound clip
            AudioManager.Instance.PlaySFX(AudioManager.Instance.GetComponent<AudioSource>()?.clip);
            DebugLog($"{foodType}: Playing rejection sound via AudioManager");
        }
        else
        {
            DebugLog($"{foodType}: AudioManager not available for rejection sound");
        }
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"ServeableItem ({foodType}): {message}");
    }
}