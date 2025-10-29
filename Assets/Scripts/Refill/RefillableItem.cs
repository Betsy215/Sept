using System.Collections;
using UnityEngine;

public class RefillableItem : MonoBehaviour
{
    [Header("Refill Configuration")] [Tooltip("Enable refill system for this item")]
    public bool enableRefill = true;

    [Tooltip("Maximum count for this item (0 = use system default)")]
    public int customMaxCount = 0;

    [Tooltip("Custom refill time per count (0 = use system default)")]
    public float customRefillTime = 0f;

    [Header("Visual Settings")] [Tooltip("Padding to add above item for count UI (affects bounds checking)")]
    public float topPadding = 0.5f;

    [Header("References")] [Tooltip("ServeableItem component (auto-found if not assigned)")]
    public ServeableItem serveableItem;

    [Header("Debug")] public bool enableDebugLogs = true;

    // Private variables
    private RefillSystem refillSystem;
    private RefillCountUI countUI;
    private RefillStatusBar statusBar;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    // Refill state
    private int currentCount;
    private int maxCount;
    private float refillTimePerCount;
    private bool isOutOfStock = false;
    private bool isGameplayMode = false;
    private bool isRefilling = false;

    // Hold detection
    private bool isHolding = false;
    private float holdStartTime;
    private Coroutine refillCoroutine;
    private Coroutine holdDetectionCoroutine;

    private void Awake()
    {
        // Get components
        serveableItem = GetComponent<ServeableItem>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    private void Start()
    {
        // Register with RefillSystem
        var refillSystemObj = FindObjectOfType<RefillSystem>();
        if (refillSystemObj != null) refillSystemObj.RegisterRefillableItem(this);
    }

    public void Initialize(RefillSystem system)
    {
        refillSystem = system;

        // Set up refill parameters
        maxCount = customMaxCount > 0 ? customMaxCount : refillSystem.GetDefaultMaxCount();
        refillTimePerCount = customRefillTime > 0 ? customRefillTime : refillSystem.GetRefillTimePerCount();
        currentCount = maxCount; // Start with full count

        // Create UI elements
        CreateCountUI();
        CreateStatusBar();

        // Update UI
        UpdateCountDisplay();
        UpdateVisualState();

        DebugLog($"Initialized with max count: {maxCount}, refill time: {refillTimePerCount}s");
    }

    private void CreateCountUI()
    {
        if (!enableRefill || refillSystem == null) return;

        var countUIObj = refillSystem.CreateCountUI(transform);
        if (countUIObj != null)
        {
            countUI = countUIObj.GetComponent<RefillCountUI>();
            if (countUI != null) countUI.Initialize(this, refillSystem.GetCountUIOffset());
        }
    }

    private void CreateStatusBar()
    {
        if (!enableRefill || refillSystem == null) return;

        var statusBarObj = refillSystem.CreateStatusBar(transform);
        if (statusBarObj != null)
        {
            statusBar = statusBarObj.GetComponent<RefillStatusBar>();
            if (statusBar != null) statusBar.Initialize(this, refillSystem.GetStatusBarOffset());
        }
    }

    public void SetGameplayMode(bool gameplayMode)
    {
        isGameplayMode = gameplayMode;

        // Show/hide count UI based on gameplay mode
        if (countUI != null) countUI.SetVisible(gameplayMode && enableRefill);

        // Always hide status bar when not in gameplay mode
        if (statusBar != null) statusBar.SetVisible(false);

        // Stop any ongoing refill when leaving gameplay mode
        if (!gameplayMode && refillCoroutine != null)
        {
            StopCoroutine(refillCoroutine);
            refillCoroutine = null;
            isRefilling = false;
        }

        DebugLog($"Gameplay mode set to: {gameplayMode}");
    }

    public void OnItemServed(bool wasCorrect)
    {
        if (!enableRefill || !isGameplayMode || isOutOfStock) return;

        if (wasCorrect)
        {
            currentCount = Mathf.Max(0, currentCount - 1);
            UpdateCountDisplay();
            UpdateVisualState();

            DebugLog($"Item served correctly. Count: {currentCount}/{maxCount}");
        }
        else
        {
            DebugLog("Item clicked but not needed - count unchanged");
        }
    }

    private void UpdateCountDisplay()
    {
        if (countUI != null) countUI.UpdateCount(currentCount, maxCount);
    }

    private void UpdateVisualState()
    {
        if (spriteRenderer == null) return;

        var wasOutOfStock = isOutOfStock;
        isOutOfStock = currentCount <= 0;

        if (isOutOfStock && !wasOutOfStock)
        {
            // Just went out of stock
            spriteRenderer.color = refillSystem.GetOutOfStockColor();
            DebugLog("Item is now out of stock");
        }
        else if (!isOutOfStock && wasOutOfStock)
        {
            // Just restocked
            spriteRenderer.color = originalColor;
            DebugLog("Item is back in stock");
        }
    }

    // Called by ServeableItem or input system for hold detection
    public void OnPointerDown()
    {
        if (!enableRefill || !isGameplayMode || currentCount >= maxCount) return;

        holdStartTime = Time.time;
        holdDetectionCoroutine = StartCoroutine(HoldDetectionCoroutine());
    }

    public void OnPointerUp()
    {
        if (holdDetectionCoroutine != null)
        {
            StopCoroutine(holdDetectionCoroutine);
            holdDetectionCoroutine = null;
        }

        if (isHolding) StopRefilling();
    }

    private IEnumerator HoldDetectionCoroutine()
    {
        // Wait for hold threshold (e.g., 0.3 seconds)
        yield return new WaitForSeconds(0.3f);

        // If we reach here, it's a hold, not a click
        if (!isHolding && currentCount < maxCount) StartRefilling();
    }

    private void StartRefilling()
    {
        if (isRefilling || currentCount >= maxCount) return;

        isHolding = true;
        isRefilling = true;

        // Show status bar
        if (statusBar != null)
        {
            statusBar.SetVisible(true);
            statusBar.StartRefillAnimation(refillTimePerCount);
        }

        refillCoroutine = StartCoroutine(RefillCoroutine());
        DebugLog("Started refilling");
    }

    private void StopRefilling()
    {
        isHolding = false;
        isRefilling = false;

        if (refillCoroutine != null)
        {
            StopCoroutine(refillCoroutine);
            refillCoroutine = null;
        }

        // Hide status bar
        if (statusBar != null) statusBar.SetVisible(false);

        DebugLog("Stopped refilling");
    }

    private IEnumerator RefillCoroutine()
    {
        while (isRefilling && currentCount < maxCount)
        {
            yield return new WaitForSeconds(refillTimePerCount);

            if (isRefilling && currentCount < maxCount)
            {
                currentCount++;
                UpdateCountDisplay();
                UpdateVisualState();

                DebugLog($"Refilled! Count: {currentCount}/{maxCount}");

                // Restart status bar animation for next count
                if (statusBar != null && currentCount < maxCount) statusBar.StartRefillAnimation(refillTimePerCount);
            }
        }

        // Stop refilling when full
        if (currentCount >= maxCount) StopRefilling();
    }

    // Public getters
    public string GetFoodType()
    {
        return serveableItem != null ? serveableItem.foodType : "Unknown";
    }

    public bool IsOutOfStock()
    {
        return isOutOfStock;
    }

    public int GetCurrentCount()
    {
        return currentCount;
    }

    public int GetMaxCount()
    {
        return maxCount;
    }

    public float GetTopPadding()
    {
        return topPadding;
    }

    // Method to get bounds including padding (for DraggableFood collision detection)
    public Bounds GetBoundsWithPadding()
    {
        var bounds = GetComponent<Collider2D>()?.bounds ??
                     GetComponent<Renderer>()?.bounds ?? new Bounds(transform.position, Vector3.one);

        // Add top padding
        bounds.size = new Vector3(bounds.size.x, bounds.size.y + topPadding, bounds.size.z);
        bounds.center = new Vector3(bounds.center.x, bounds.center.y + topPadding / 2f, bounds.center.z);

        return bounds;
    }

    private void OnDestroy()
    {
        // Clean up UI elements
        if (countUI != null) Destroy(countUI.gameObject);
        if (statusBar != null) Destroy(statusBar.gameObject);

        // Unregister from system
        if (refillSystem != null) refillSystem.UnregisterRefillableItem(this);
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"RefillableItem ({GetFoodType()}): {message}");
    }
}