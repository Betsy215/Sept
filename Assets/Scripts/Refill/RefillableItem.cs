using System.Collections;
using UnityEngine;
using TMPro;

public class RefillableItem : MonoBehaviour
{
    [Header("Refill Configuration")] [Tooltip("Enable refill system for this item")]
    public bool enableRefill = true;

    [Tooltip("Maximum count for this item (0 = use system default)")]
    public int customMaxCount = 0;

    [Tooltip("Custom refill time per count (0 = use system default)")]
    public float customRefillTime = 0f;

    [Header("UI References - Assign from child objects")]
    [Tooltip("The Count TextMeshPro component (child of this item)")]
    public TextMeshPro count;

    [Tooltip("The RefillBar GameObject (child of this item)")]
    public GameObject refillBar;

    [Tooltip("The Fill SpriteRenderer inside RefillBar")]
    public SpriteRenderer fill;

    [Tooltip("The Bar SpriteRenderer inside RefillBar")]
    public SpriteRenderer bar;

    [Header("Visual Settings")] [Tooltip("Padding to add above item for count UI (affects bounds checking)")]
    public float topPadding = 0.5f;

    [Header("UI Colors")] public Color inStockTextColor = Color.white;
    public Color outOfStockTextColor = Color.red;

    [Header("References")] [Tooltip("ServeableItem component (auto-found if not assigned)")]
    public ServeableItem serveableItem;

    [Header("Debug")] public bool enableDebugLogs = true;

    // Private variables
    private RefillSystem refillSystem;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Vector3 originalFillScale; // Store the intended fill scale

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

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private void Start()
    {
        // Auto-find child UI components if not assigned
        AutoAssignUIReferences();

        // Register with RefillSystem
        RegisterWithRefillSystem();
    }

    [ContextMenu("Auto-Assign UI References")]
    private void AutoAssignUIReferences()
    {
        if (count == null)
        {
            count = transform.Find("Count")?.GetComponent<TextMeshPro>();
            if (count != null) DebugLog("Auto-assigned Count component");
        }

        if (refillBar == null)
        {
            refillBar = transform.Find("RefillBar")?.gameObject;
            if (refillBar != null) DebugLog("Auto-assigned RefillBar GameObject");
        }

        if (fill == null && refillBar != null)
        {
            fill = refillBar.transform.Find("Fill")?.GetComponent<SpriteRenderer>();
            if (fill != null) DebugLog("Auto-assigned Fill SpriteRenderer");
        }

        if (bar == null && refillBar != null)
        {
            bar = refillBar.transform.Find("Bar")?.GetComponent<SpriteRenderer>();
            if (bar != null) DebugLog("Auto-assigned Bar SpriteRenderer");
        }

        // Store original fill scale for animation
        SetupUIReferences();
    }

    private void SetupUIReferences()
    {
        // Store the original fill scale for animation
        if (fill != null)
        {
            originalFillScale = fill.transform.localScale;
            DebugLog($"Stored original fill scale: {originalFillScale}");
        }
    }

    private void RegisterWithRefillSystem()
    {
        var refillSystemObj = FindObjectOfType<RefillSystem>();
        if (refillSystemObj != null)
        {
            refillSystemObj.RegisterRefillableItem(this);
            DebugLog("Successfully registered with RefillSystem");
        }
        else
        {
            DebugLog("RefillSystem not found, will retry...");
            Invoke("RegisterWithRefillSystem", 0.1f);
        }
    }

    public void Initialize(RefillSystem system)
    {
        refillSystem = system;
        DebugLog($"Initialize called with RefillSystem: {refillSystem != null}");

        // Set up refill parameters
        maxCount = customMaxCount > 0 ? customMaxCount : refillSystem.GetDefaultMaxCount();
        refillTimePerCount = customRefillTime > 0 ? customRefillTime : refillSystem.GetRefillTimePerCount();
        currentCount = maxCount; // Start with full count

        DebugLog(
            $"Parameters set - maxCount: {maxCount}, refillTime: {refillTimePerCount}, currentCount: {currentCount}");

        // Initialize UI
        SetUIVisible(false); // Start hidden
        UpdateCountDisplay();
        UpdateVisualState();

        DebugLog($"Initialized with max count: {maxCount}, refill time: {refillTimePerCount}s");
    }

    #region UI Management

    public void SetGameplayMode(bool gameplayMode)
    {
        isGameplayMode = gameplayMode;

        DebugLog($"SetGameplayMode called: {gameplayMode}");

        // Show/hide count UI based on gameplay mode and refill enabled
        var shouldShowCountUI = gameplayMode && enableRefill;
        SetCountUIVisible(shouldShowCountUI);

        // Status bar stays hidden unless actively refilling
        SetStatusBarVisible(false);

        if (shouldShowCountUI)
            UpdateCountDisplay();

        // Stop any ongoing refill when leaving gameplay mode
        if (!gameplayMode && refillCoroutine != null)
        {
            StopCoroutine(refillCoroutine);
            refillCoroutine = null;
            isRefilling = false;
        }

        DebugLog($"Gameplay mode set to: {gameplayMode}, Count UI visible: {shouldShowCountUI}");
    }

    private void SetUIVisible(bool visible)
    {
        SetCountUIVisible(visible && enableRefill);
        SetStatusBarVisible(false); // Status bar only shows during refill
    }

    private void SetCountUIVisible(bool visible)
    {
        if (count != null)
        {
            count.gameObject.SetActive(visible);
            DebugLog($"Count UI visibility set to: {visible}");
        }
    }

    private void SetStatusBarVisible(bool visible)
    {
        if (refillBar != null)
        {
            refillBar.SetActive(visible);
            DebugLog($"Status bar visibility set to: {visible}");
        }
    }

    public void UpdateCountDisplay()
    {
        if (count != null)
        {
            count.text = currentCount.ToString();

            // Update color based on stock status
            var isOutOfStock = currentCount <= 0;
            count.color = isOutOfStock ? outOfStockTextColor : inStockTextColor;

            DebugLog($"Count updated: {currentCount}/{maxCount}, outOfStock: {isOutOfStock}");
        }
    }

    private void SetStatusBarFill(float amount)
    {
        if (fill != null)
        {
            // Scale the fill sprite
            var scale = originalFillScale;
            scale.x = originalFillScale.x * Mathf.Clamp01(amount);
            fill.transform.localScale = scale;

            // Move the sprite left as it scales to create left-to-right fill effect
            var pos = fill.transform.localPosition;
            var missingWidth = originalFillScale.x * (1f - amount);
            pos.x = -missingWidth * 0.5f; // Move left by half the missing width
            fill.transform.localPosition = pos;
        }
    }

    #endregion

    #region Refill Logic

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

    // Called by ServeableItem for hold detection
    public void OnPointerDown()
    {
        DebugLog(
            $"OnPointerDown called - enableRefill:{enableRefill}, isGameplayMode:{isGameplayMode}, currentCount:{currentCount}, maxCount:{maxCount}");

        if (!enableRefill || !isGameplayMode || currentCount >= maxCount)
        {
            DebugLog(
                $"Hold detection blocked - enableRefill:{enableRefill}, isGameplayMode:{isGameplayMode}, currentCount:{currentCount}/{maxCount}");
            return;
        }

        DebugLog("Starting hold detection...");
        holdStartTime = Time.time;
        holdDetectionCoroutine = StartCoroutine(HoldDetectionCoroutine());
    }

    public void OnPointerUp()
    {
        DebugLog("OnPointerUp called");

        if (holdDetectionCoroutine != null)
        {
            DebugLog("Stopping hold detection coroutine");
            StopCoroutine(holdDetectionCoroutine);
            holdDetectionCoroutine = null;
        }

        if (isHolding)
        {
            DebugLog("Was holding, stopping refill");
            StopRefilling();
        }
    }

    private IEnumerator HoldDetectionCoroutine()
    {
        DebugLog("Hold detection coroutine started, waiting 0.3 seconds...");
        yield return new WaitForSeconds(0.3f);

        DebugLog("Hold threshold reached, checking if still valid...");

        if (!isHolding && currentCount < maxCount)
        {
            DebugLog("Valid hold detected, starting refill!");
            StartRefilling();
        }
        else
        {
            DebugLog($"Hold invalid - isHolding:{isHolding}, currentCount:{currentCount}, maxCount:{maxCount}");
        }
    }

    private void StartRefilling()
    {
        if (isRefilling || currentCount >= maxCount) return;

        isHolding = true;
        isRefilling = true;

        // Show status bar with animation
        SetStatusBarVisible(true);
        StartCoroutine(AnimateStatusBar(refillTimePerCount));

        refillCoroutine = StartCoroutine(RefillCoroutine());
        DebugLog("Started refilling");
    }

    // Add this to RefillableItem.cs
    private void OnDrawGizmos()
    {
        if (!enableDebugLogs) return; // Only show when debug is enabled

        // Get the bounds with padding
        var bounds = GetBoundsWithPadding();

        // Set gizmo color based on state
        if (Application.isPlaying)
        {
            if (isGameplayMode)
                Gizmos.color = enableRefill ? Color.green : Color.gray;
            else
                Gizmos.color = Color.yellow; // Arrangement mode
        }
        else
        {
            Gizmos.color = Color.cyan; // Editor mode
        }

        // Draw the bounds as a wireframe cube
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        // Draw the original collider bounds in a different color for comparison
        var baseBounds = GetBaseBounds();
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(baseBounds.center, baseBounds.size);
    }

// Optional: More detailed debug method
    [ContextMenu("Show Bounds Info")]
    public void ShowBoundsInfo()
    {
        var bounds = GetBoundsWithPadding();
        var baseBounds = GetBaseBounds();

        DebugLog("=== BOUNDS DEBUG INFO ===");
        DebugLog($"Base Bounds - Center: {baseBounds.center}, Size: {baseBounds.size}");
        DebugLog($"Padded Bounds - Center: {bounds.center}, Size: {bounds.size}");
        DebugLog($"Top Padding: {topPadding}");
        DebugLog($"Gameplay Mode: {isGameplayMode}");
        DebugLog($"Enable Refill: {enableRefill}");

        if (count != null && count.gameObject.activeInHierarchy)
        {
            var countBounds = count.bounds;
            DebugLog($"Count UI Bounds: {countBounds}");
        }
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
        SetStatusBarVisible(false);

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
                if (currentCount < maxCount)
                    StartCoroutine(AnimateStatusBar(refillTimePerCount));
            }
        }

        // Stop refilling when full
        if (currentCount >= maxCount)
            StopRefilling();
    }

    private IEnumerator AnimateStatusBar(float duration)
    {
        var elapsed = 0f;

        while (elapsed < duration && isRefilling)
        {
            var progress = elapsed / duration;
            SetStatusBarFill(progress);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetStatusBarFill(1f);
        yield return new WaitForSeconds(0.1f);

        if (!isRefilling)
            SetStatusBarVisible(false);
    }

    #endregion

    #region Visual State

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

            // REPLACE with this:
            refillSystem.OnItemBecameEmpty();
        }
        else if (!isOutOfStock && wasOutOfStock)
        {
            // Just restocked
            spriteRenderer.color = originalColor;
            DebugLog("Item is back in stock");
        }
    }

    #endregion

    #region Bounds Calculation

    public Bounds GetBoundsWithPadding()
    {
        // Get base item bounds
        var bounds = GetBaseBounds();

        // Calculate UI bounds when visible
        var totalTopPadding = topPadding;
        // Remove totalBottomPadding since status bar is centered

        if (enableRefill && isGameplayMode)
            // Include Count UI bounds (above food item)
            if (count != null && count.gameObject.activeInHierarchy)
            {
                var countBounds = count.bounds;
                var countUITop = countBounds.max.y - transform.position.y;
                totalTopPadding = Mathf.Max(totalTopPadding, countUITop + 0.1f);
            }

        // Remove RefillBar bounds calculation since it's centered, not below
        // Expand bounds to include UI space (only top padding needed)
        bounds.size = new Vector3(bounds.size.x, bounds.size.y + totalTopPadding, bounds.size.z);
        bounds.center = new Vector3(bounds.center.x, bounds.center.y + totalTopPadding / 2f, bounds.center.z);

        return bounds;
    }

    private Bounds GetBaseBounds()
    {
        // Try BoxCollider (3D) first
        var boxCollider3D = GetComponent<BoxCollider>();
        if (boxCollider3D != null)
            return boxCollider3D.bounds;

        // Try any Collider (3D)
        var collider3D = GetComponent<Collider>();
        if (collider3D != null)
            return collider3D.bounds;

        // Try Renderer
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
            return renderer.bounds;

        // Fallback
        return new Bounds(transform.position, Vector3.one);
    }

    private Bounds GetChildRendererBounds(GameObject parent)
    {
        var renderers = parent.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(parent.transform.position, Vector3.zero);

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    #endregion

    #region Public Getters

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

    #endregion

    #region Debug Methods

    [ContextMenu("Test Count UI")]
    public void TestCountUI()
    {
        DebugLog("Testing Count UI...");
        if (count != null)
        {
            SetCountUIVisible(true);
            UpdateCountDisplay();
            DebugLog("Count UI should now be visible");
        }
        else
        {
            DebugLog("ERROR: Count component not assigned!");
        }
    }

    [ContextMenu("Test Status Bar")]
    public void TestStatusBar()
    {
        DebugLog("Testing Status Bar...");
        if (refillBar != null)
        {
            SetStatusBarVisible(true);
            SetStatusBarFill(0.5f); // 50% filled
            DebugLog("Status bar should now be visible at 50%");
        }
        else
        {
            DebugLog("ERROR: RefillBar not assigned!");
        }
    }

    [ContextMenu("Test Hold Detection")]
    public void TestHoldDetection()
    {
        DebugLog("Testing hold detection manually...");
        OnPointerDown();
        StartCoroutine(TestHoldCoroutine());
    }

    private IEnumerator TestHoldCoroutine()
    {
        yield return new WaitForSeconds(1f);
        OnPointerUp();
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"RefillableItem ({GetFoodType()}): {message}");
    }

    #endregion
}