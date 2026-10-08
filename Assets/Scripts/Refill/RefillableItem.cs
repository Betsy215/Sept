using System.Collections;
using UnityEngine;

public class RefillableItem : MonoBehaviour
{
    [Header("Refill Configuration")] [Tooltip("Enable refill system for count tracking")]
    public bool enableRefill = true;

    [Tooltip("Enable hold-to-refill gesture (requires enableRefill = true)")]
    public bool enableHoldToRefill = true;

    [Tooltip("Maximum count for this item (0 = use system default)")]
    public int customMaxCount = 0;

    [Tooltip("Starting count for this item (-1 = start full, 0+ = specific count)")]
    public int customStartingCount = -1;

    [Tooltip("Custom refill time per count (0 = use system default)")]
    public float customRefillTime = 0f;

    [Header("UI References - Assign from child objects")] [Tooltip("The RefillBar GameObject (child of this item)")]
    public GameObject refillBar;

    [Tooltip("The Fill SpriteRenderer inside RefillBar")]
    public SpriteRenderer fill;

    [Tooltip("The Bar SpriteRenderer inside RefillBar")]
    public SpriteRenderer bar;

    [Header("References")] [Tooltip("ServeableItem component (auto-found if not assigned)")]
    public ServeableItem serveableItem;

    [Header("Debug")] public bool enableDebugLogs = true;

    [Header("Events")] public System.Action<int, int> OnCountChanged;

    // Private variables
    private RefillSystem refillSystem;
    private SpriteRenderer spriteRenderer;
    private Vector3 originalFillScale; // Store the intended fill scale

    // Refill state
    private int currentCount;
    private int maxCount;
    private float refillTimePerCount;
    private bool isOutOfStock = false;
    private bool isGameplayMode = false;
    private bool isRefilling = false;

    // Set by OverrideMaxCount (upgrades). LevelManager may apply upgrades before RefillSystem
    // calls Initialize, so Initialize must not clobber an override that already happened.
    private bool hasMaxCountOverride = false;

    // Hold detection
    private bool isHolding = false;

    /// True while a hold-to-refill is in progress for the current press.
    public bool IsRefilling => isRefilling || isHolding;
    private float holdStartTime;
    private Coroutine refillCoroutine;
    private Coroutine holdDetectionCoroutine;
    private Coroutine statusBarCoroutine;

    private void Awake()
    {
        // Get components
        serveableItem = GetComponent<ServeableItem>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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

        // Set up refill parameters (keep an upgrade override that was applied before we were initialized)
        if (!hasMaxCountOverride)
            maxCount = customMaxCount > 0 ? customMaxCount : refillSystem.GetDefaultMaxCount();
        refillTimePerCount = customRefillTime > 0 ? customRefillTime : refillSystem.GetRefillTimePerCount();
        if (customStartingCount == -1)
            currentCount = maxCount; // Default: start full
        else
            currentCount = Mathf.Clamp(customStartingCount, 0, maxCount); // Custom starting count
        isOutOfStock = currentCount <= 0;

        DebugLog(
            $"Parameters set - maxCount: {maxCount}, refillTime: {refillTimePerCount}, currentCount: {currentCount}");

        // Initialize UI
        SetUIVisible(false); // Start hidden

        DebugLog($"Initialized with max count: {maxCount}, refill time: {refillTimePerCount}s");
    }

    #region UI Management

    public void SetGameplayMode(bool gameplayMode)
    {
        isGameplayMode = gameplayMode;

        DebugLog($"SetGameplayMode called: {gameplayMode}");

        // Status bar stays hidden unless actively refilling
        SetStatusBarVisible(false);

        // Stop any ongoing hold or refill when leaving gameplay mode
        if (!gameplayMode)
            ResetHoldState();

        DebugLog($"Gameplay mode set to: {gameplayMode}");
    }

    private void OnDisable()
    {
        // Coroutines die with the object; make sure the flags and the bar do not outlive them,
        // otherwise the next press on this item is treated as a hold that never ends.
        ResetHoldState();
    }

    /// Cancel hold detection, refilling and the bar animation, and clear every hold flag.
    private void ResetHoldState()
    {
        if (holdDetectionCoroutine != null)
        {
            StopCoroutine(holdDetectionCoroutine);
            holdDetectionCoroutine = null;
        }

        if (refillCoroutine != null)
        {
            StopCoroutine(refillCoroutine);
            refillCoroutine = null;
        }

        if (statusBarCoroutine != null)
        {
            StopCoroutine(statusBarCoroutine);
            statusBarCoroutine = null;
        }

        isHolding = false;
        isRefilling = false;
        SetStatusBarVisible(false);
    }

    private void SetUIVisible(bool visible)
    {
        SetStatusBarVisible(false); // Status bar only shows during refill
    }

    private void SetStatusBarVisible(bool visible)
    {
        if (refillBar != null)
        {
            refillBar.SetActive(visible);
            DebugLog($"Status bar visibility set to: {visible}");
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
            OnCountChanged?.Invoke(currentCount, maxCount);
            if (currentCount <= 0)
                isOutOfStock = true;
            DebugLog($"Item served correctly. Count: {currentCount}/{maxCount}");
        }
        else
        {
            DebugLog("Item clicked but not needed - count unchanged");
        }
    }

    public void IncreaseCount(int amount)
    {
        if (amount <= 0) return;

        var oldCount = currentCount;
        currentCount = Mathf.Min(currentCount + amount, maxCount);

        if (currentCount != oldCount)
        {
            isOutOfStock = false;
            OnCountChanged?.Invoke(currentCount, maxCount);
            DebugLog($"Count increased by {amount}. New count: {currentCount}/{maxCount}");
        }
    }

    public bool HasSpace(int amount)
    {
        return currentCount + amount <= maxCount;
    }

    // Called by ServeableItem for hold detection
    public void OnPointerDown()
    {
        DebugLog(
            $"OnPointerDown called - enableRefill:{enableRefill}, enableHoldToRefill:{enableHoldToRefill}, isGameplayMode:{isGameplayMode}, currentCount:{currentCount}, maxCount:{maxCount}");

        // Check if hold-to-refill is enabled
        if (!enableRefill || !enableHoldToRefill || !isGameplayMode || currentCount >= maxCount)
        {
            DebugLog(
                $"Hold detection blocked - enableRefill:{enableRefill}, enableHoldToRefill:{enableHoldToRefill}, isGameplayMode:{isGameplayMode}, currentCount:{currentCount}/{maxCount}");
            return;
        }

        // A second finger on the same item must not spawn a second detection coroutine; the
        // first one would lose its handle and start a refill that no pointer-up can stop.
        if (holdDetectionCoroutine != null || isHolding)
        {
            DebugLog("Hold already in progress - ignoring extra pointer");
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
        holdDetectionCoroutine = null;

        if (!isHolding && isGameplayMode && currentCount < maxCount)
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
        RestartStatusBarAnimation();

        refillCoroutine = StartCoroutine(RefillCoroutine());
        DebugLog("Started refilling");
    }

    private void RestartStatusBarAnimation()
    {
        if (statusBarCoroutine != null)
            StopCoroutine(statusBarCoroutine);
        statusBarCoroutine = StartCoroutine(AnimateStatusBar(refillTimePerCount));
    }

    private void StopRefilling()
    {
        // Called from OnPointerUp: the press is over, so the hold is over too.
        isHolding = false;
        FinishRefilling();
        DebugLog("Stopped refilling");
    }

    /// Stop the refill itself but keep isHolding, so the press that filled the item is still
    /// reported as consumed by the refill and the release does not serve a cup.
    private void FinishRefilling()
    {
        isRefilling = false;

        if (refillCoroutine != null)
        {
            StopCoroutine(refillCoroutine);
            refillCoroutine = null;
        }

        // Hide status bar
        SetStatusBarVisible(false);
    }

    private IEnumerator RefillCoroutine()
    {
        while (isRefilling && currentCount < maxCount)
        {
            yield return new WaitForSeconds(refillTimePerCount);

            if (isRefilling && currentCount < maxCount)
            {
                currentCount++;
                OnCountChanged?.Invoke(currentCount, maxCount);

                DebugLog($"Refilled! Count: {currentCount}/{maxCount}");

                // Restart status bar animation for next count
                if (currentCount < maxCount)
                    RestartStatusBarAnimation();
            }
        }

        // Full: stop the refill but keep the hold flag until the finger lifts (see FinishRefilling)
        refillCoroutine = null;
        FinishRefilling();
        DebugLog("Refill complete - item is full");
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

        statusBarCoroutine = null;
        if (!isRefilling)
            SetStatusBarVisible(false);
    }

    #endregion

    #region Visual State

    #endregion

    #region Bounds Calculation

    public Bounds GetBoundsWithPadding()
    {
        // Get base item bounds
        var bounds = GetBaseBounds();

        // Expand bounds to include UI space (only top padding needed)
        bounds.size = new Vector3(bounds.size.x, bounds.size.y, bounds.size.z);
        bounds.center = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z);

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

    #endregion

    #region Debug Methods

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

    public void RefillToFull()
    {
        if (currentCount == maxCount) return;
        currentCount = maxCount;
        isOutOfStock = false;
        Debug.Log(
            $"[RefillableItem] Firing OnCountChanged. Listeners: {OnCountChanged?.GetInvocationList().Length ?? 0}");
        OnCountChanged?.Invoke(currentCount, maxCount);
    }

    public void OverrideMaxCount(int newMaxCount)
    {
        if (newMaxCount <= 0) return;

        maxCount = newMaxCount;
        hasMaxCountOverride = true;

        // ✅ If item was set to start full (customStartingCount = -1), update currentCount to new max
        if (customStartingCount == -1)
        {
            currentCount = maxCount;
            OnCountChanged?.Invoke(currentCount, maxCount);
            DebugLog($"Max count overridden to {maxCount}, current count updated to full");
        }
        else
        {
            // Otherwise clamp current count to new max (in case new max is lower)
            currentCount = Mathf.Clamp(currentCount, 0, maxCount);
            isOutOfStock = currentCount <= 0;
            OnCountChanged?.Invoke(currentCount, maxCount);
            DebugLog($"Max count overridden to {maxCount}, current count clamped to {currentCount}");
        }
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

    // Gizmos for debugging bounds
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

    [ContextMenu("Show Bounds Info")]
    public void ShowBoundsInfo()
    {
        var bounds = GetBoundsWithPadding();
        var baseBounds = GetBaseBounds();

        DebugLog("=== BOUNDS DEBUG INFO ===");
        DebugLog($"Base Bounds - Center: {baseBounds.center}, Size: {baseBounds.size}");
        DebugLog($"Padded Bounds - Center: {bounds.center}, Size: {bounds.size}");
        DebugLog($"Gameplay Mode: {isGameplayMode}");
        DebugLog($"Enable Refill: {enableRefill}");
        DebugLog($"Enable Hold To Refill: {enableHoldToRefill}");
    }

    #endregion
}