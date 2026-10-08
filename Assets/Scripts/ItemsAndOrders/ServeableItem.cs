using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// A food item on the table that can be tapped to serve the current order, or held to refill.
/// All input goes through the EventSystem (Physics2DRaycaster on the camera), so it behaves
/// the same for mouse and touch and is ignored while the game is paused.
/// </summary>
public class ServeableItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IDraggable
{
    [Header("Item Settings")] [Tooltip("Type of food this item represents (must match OrderSystem food types)")]
    public string foodType = ""; // e.g., "Bread", "Apple", "Juice", etc.

    [Header("References")] [Tooltip("Reference to OrderSystem - will auto-find if not assigned")]
    public OrderSystem orderSystem;

    [Header("Visual Feedback")] [Tooltip("Canvas that holds the pause / level-complete popups. Taps are ignored while it is active.")]
    public GameObject popupCanvas;

    [Header("Audio Feedback")] [Tooltip("Play rejection sound through AudioManager (recommended)")]
    public bool useAudioManager = true;

    [Header("Animation Settings")] [Tooltip("How intense the shake effect should be")]
    public float shakeIntensity = 0.15f;

    [Tooltip("How long the shake effect lasts")]
    public float shakeDuration = 0.4f;

    [Tooltip("How many times the item shakes")]
    public int shakeCount = 3;

    public AudioClip serveSound;

    [Header("Debug")] public bool enableDebugLogs = true;

    private bool servingEnabled = true;

    private RefillableItem refillableItem;
    private RefillSystem refillSystem;

    // True when the current press turned into a hold-to-refill, so releasing must not serve.
    private bool pressConsumedByRefill;

    // Pointer (finger) that owns the current press. Other fingers are ignored until it lifts,
    // so a second tap cannot cancel a hold-to-refill in progress.
    private const int NoPointer = int.MinValue;
    private int activePointerId = NoPointer;
    private int lastReleasedPointerId = NoPointer;

    private Coroutine shakeRoutine;
    private Vector3 restPosition;

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
        if (orderSystem == null)
        {
            orderSystem = FindObjectOfType<OrderSystem>();
            if (orderSystem == null)
            {
                Debug.LogError($"ServeableItem {foodType}: OrderSystem not found in scene!");
                return;
            }
        }

        refillableItem = GetComponent<RefillableItem>();
        refillSystem = FindObjectOfType<RefillSystem>();

        // The scene leaves this unassigned on every item; LevelManager knows the popup canvas.
        if (popupCanvas == null)
        {
            var levelManager = FindObjectOfType<LevelManager>();
            if (levelManager != null) popupCanvas = levelManager.popupCanvas;
        }

        DebugLog($"ServeableItem {foodType} initialized successfully");
    }

    private void OnDisable()
    {
        activePointerId = NoPointer;
        lastReleasedPointerId = NoPointer;
        pressConsumedByRefill = false;

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
            transform.position = restPosition;
        }
    }

    public void SetServingEnabled(bool enabled)
    {
        servingEnabled = enabled;
        DebugLog($"Serving {(enabled ? "enabled" : "disabled")}");
    }

    public string GetFoodType()
    {
        return foodType;
    }

    #region Input

    private bool CanAcceptInput()
    {
        if (!servingEnabled) return false;
        if (Time.timeScale == 0f) return false; // paused
        if (popupCanvas != null && popupCanvas.activeInHierarchy) return false; // pause / level-complete popup is up
        return true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointerId != NoPointer) return; // another finger already owns this item
        activePointerId = eventData.pointerId;
        pressConsumedByRefill = false;

        if (!CanAcceptInput()) return;

        // Start hold-to-refill detection
        if (refillableItem != null) refillableItem.OnPointerDown();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;
        lastReleasedPointerId = activePointerId;
        activePointerId = NoPointer;

        if (refillableItem == null) return;

        // Remember whether this press became a refill before the refill logic clears its state.
        pressConsumedByRefill = refillableItem.IsRefilling;
        refillableItem.OnPointerUp();
    }

    // Fires only when the finger went down and came back up on this same item.
    public void OnPointerClick(PointerEventData eventData)
    {
        // Only the finger that owned the press may serve. OnPointerUp has already cleared the owner,
        // so compare against the id it recorded on the way down.
        if (eventData.pointerId != lastReleasedPointerId) return;

        if (pressConsumedByRefill)
        {
            pressConsumedByRefill = false;
            return;
        }

        if (!CanAcceptInput()) return;

        if (refillableItem != null && refillableItem.IsOutOfStock())
        {
            DebugLog("Cannot serve - out of stock!");
            OnItemRejected();
            return;
        }

        OnItemClicked();
    }

    #endregion

    private void OnItemClicked()
    {
        if (orderSystem == null)
        {
            Debug.LogError($"{foodType}: OrderSystem reference is missing!");
            return;
        }

        if (!orderSystem.IsOrderActive())
        {
            DebugLog("No active order to serve");
            return;
        }

        if (orderSystem.TryServeItem(foodType))
        {
            DebugLog("Successfully served!");
            OnItemServedSuccessfully();
        }
        else
        {
            DebugLog("Not needed in current order");
            OnItemRejected();
        }
    }

    private void OnItemServedSuccessfully()
    {
        if (serveSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(serveSound);

        if (refillSystem != null) refillSystem.OnItemServed(foodType, true);

        SendMessage("OnServedSuccessfully", SendMessageOptions.DontRequireReceiver);
    }

    private void OnItemRejected()
    {
        DebugLog("Item rejected - playing shake and sound feedback");

        if (refillSystem != null) refillSystem.OnItemServed(foodType, false);

        // Restart the shake from the resting position so rapid taps cannot drift the item.
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            transform.position = restPosition;
        }

        shakeRoutine = StartCoroutine(ShakeAnimation());
        PlayRejectionSound();
    }

    private IEnumerator ShakeAnimation()
    {
        restPosition = transform.position;
        var step = shakeDuration / (shakeCount * 2f);

        for (var i = 0; i < shakeCount; i++)
        {
            transform.position = restPosition + Vector3.right * shakeIntensity;
            yield return new WaitForSeconds(step);

            transform.position = restPosition + Vector3.left * shakeIntensity;
            yield return new WaitForSeconds(step);
        }

        transform.position = restPosition;
        shakeRoutine = null;
    }

    private void PlayRejectionSound()
    {
        if (useAudioManager && AudioManager.Instance != null)
            AudioManager.Instance.PlayWrongItemSFX();
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"ServeableItem ({foodType}): {message}");
    }
}
