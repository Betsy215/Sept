using System.Collections;
using UnityEngine;

public class RefillableItem : MonoBehaviour
{
    [Header("Refill Configuration")] [Tooltip("Enable refill system for this item")]
    public bool enableRefill = true; // Changed: Now defaults to true

    [Tooltip("Maximum count for this item (0 = use system default)")]
    public int customMaxCount = 0;

    [Tooltip("Custom refill time per count (0 = use system default)")]
    public float customRefillTime = 0f;

    [Header("Visual Settings")] [Tooltip("Padding to add above item for count UI (affects bounds checking)")]
    public float topPadding = 0.5f;

    [Tooltip("Custom offset for count UI above this item (0 = use system default)")]
    public float customCountUIOffset = 0f;

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
        // Register with RefillSystem (try multiple times if needed)
        RegisterWithRefillSystem();
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
            // Retry after a short delay
            Invoke("RegisterWithRefillSystem", 0.1f);
        }
    }

    public void Initialize(RefillSystem system)
    {
        refillSystem = system;

        DebugLog($"Initialize called with RefillSystem: {refillSystem != null}");

        // Check for EventSystem (required for pointer events)
        var eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        DebugLog($"EventSystem found: {eventSystem != null}");

        // Set up refill parameters
        maxCount = customMaxCount > 0 ? customMaxCount : refillSystem.GetDefaultMaxCount();
        refillTimePerCount = customRefillTime > 0 ? customRefillTime : refillSystem.GetRefillTimePerCount();
        currentCount = maxCount; // Start with full count

        DebugLog(
            $"Parameters set - maxCount: {maxCount}, refillTime: {refillTimePerCount}, currentCount: {currentCount}");

        // Create UI elements
        DebugLog("About to create UI elements...");
        CreateCountUI();
        DebugLog($"After CreateCountUI - countUI is null: {countUI == null}");

        CreateStatusBar();
        DebugLog($"After CreateStatusBar - statusBar is null: {statusBar == null}");

        // Update UI
        UpdateCountDisplay();
        UpdateVisualState();

        DebugLog($"Initialized with max count: {maxCount}, refill time: {refillTimePerCount}s");
    }

    private void CreateCountUI()
    {
        DebugLog($"CreateCountUI - enableRefill: {enableRefill}, refillSystem: {refillSystem != null}");

        if (!enableRefill || refillSystem == null)
        {
            DebugLog(
                "Cannot create count UI: enableRefill=" + enableRefill + ", refillSystem=" + (refillSystem != null));
            return;
        }

        DebugLog("Attempting to create count UI...");
        var countUIObj = refillSystem.CreateCountUI(transform);

        if (countUIObj != null)
        {
            DebugLog("Count UI GameObject created successfully");
            DebugLog($"Created GameObject name: {countUIObj.name}");
            DebugLog($"GameObject has {countUIObj.transform.childCount} children");

            // Debug: List all components on the root GameObject
            DebugLog("=== ROOT GAMEOBJECT COMPONENTS ===");
            var rootComponents = countUIObj.GetComponents<Component>();
            foreach (var comp in rootComponents) DebugLog($"Root component: {comp.GetType().Name}");

            // Debug: List all components on child GameObjects
            for (var i = 0; i < countUIObj.transform.childCount; i++)
            {
                var child = countUIObj.transform.GetChild(i);
                DebugLog($"=== CHILD {i}: {child.name} ===");
                var childComponents = child.GetComponents<Component>();
                foreach (var comp in childComponents) DebugLog($"Child {i} component: {comp.GetType().Name}");
            }

            // Look for RefillCountUI component in the GameObject and its children
            countUI = countUIObj.GetComponent<RefillCountUI>();
            if (countUI == null)
            {
                DebugLog("RefillCountUI not found on root, searching in children...");
                countUI = countUIObj.GetComponentInChildren<RefillCountUI>();

                if (countUI != null)
                    DebugLog($"RefillCountUI found on child: {countUI.gameObject.name}");
                else
                    DebugLog("ERROR: RefillCountUI component not found anywhere in hierarchy!");
            }
            else
            {
                DebugLog("RefillCountUI found on root GameObject");
            }

            if (countUI != null)
            {
                DebugLog("RefillCountUI component found, initializing...");

                // Use custom offset if set, otherwise use system default
                var offsetToUse = customCountUIOffset > 0 ? customCountUIOffset : refillSystem.GetCountUIOffset();
                countUI.Initialize(this, offsetToUse);
                DebugLog($"Count UI initialized with offset: {offsetToUse}");
            }
        }
        else
        {
            DebugLog("ERROR: Failed to create count UI GameObject!");
        }
    }

    private void CreateStatusBar()
    {
        if (!enableRefill || refillSystem == null)
        {
            DebugLog("Cannot create status bar: enableRefill=" + enableRefill + ", refillSystem=" +
                     (refillSystem != null));
            return;
        }

        DebugLog("Attempting to create status bar...");
        var statusBarObj = refillSystem.CreateStatusBar(transform);

        if (statusBarObj != null)
        {
            DebugLog("Status bar GameObject created successfully");

            // Look for RefillStatusBar component in the GameObject and its children
            statusBar = statusBarObj.GetComponent<RefillStatusBar>();
            if (statusBar == null)
            {
                DebugLog("RefillStatusBar not found on root, searching in children...");
                statusBar = statusBarObj.GetComponentInChildren<RefillStatusBar>();
            }

            if (statusBar != null)
            {
                DebugLog("RefillStatusBar component found, initializing...");
                statusBar.Initialize(this, refillSystem.GetStatusBarOffset());
                DebugLog("Status bar initialized successfully");
            }
            else
            {
                DebugLog("ERROR: RefillStatusBar component not found in GameObject or children!");
            }
        }
        else
        {
            DebugLog("ERROR: Failed to create status bar GameObject!");
        }
    }

    public void SetGameplayMode(bool gameplayMode)
    {
        isGameplayMode = gameplayMode;

        DebugLog($"SetGameplayMode called: {gameplayMode}");
        DebugLog($"countUI exists: {countUI != null}, statusBar exists: {statusBar != null}");
        DebugLog($"enableRefill: {enableRefill}");

        // Show/hide count UI based on gameplay mode
        if (countUI != null)
        {
            var shouldShow = gameplayMode && enableRefill;
            DebugLog(
                $"Setting count UI visible: {shouldShow} (gameplayMode:{gameplayMode} && enableRefill:{enableRefill})");
            countUI.SetVisible(shouldShow);

            // Also check the GameObject state after setting visibility
            DebugLog($"After SetVisible - countUI GameObject active: {countUI.gameObject.activeInHierarchy}");
        }
        else
        {
            DebugLog("WARNING: countUI is null, cannot show/hide");
        }

        // Always hide status bar when not in gameplay mode
        if (statusBar != null)
        {
            statusBar.SetVisible(false);
            DebugLog("Status bar hidden");
        }
        else
        {
            DebugLog("WARNING: statusBar is null");
        }

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
        else
        {
            DebugLog("Was not holding");
        }
    }

    private IEnumerator HoldDetectionCoroutine()
    {
        DebugLog("Hold detection coroutine started, waiting 0.3 seconds...");

        // Wait for hold threshold (e.g., 0.3 seconds)
        yield return new WaitForSeconds(0.3f);

        DebugLog("Hold threshold reached, checking if still valid...");

        // If we reach here, it's a hold, not a click
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
        Bounds bounds;

        // Try BoxCollider (3D) first since that's what we're using
        var boxCollider3D = GetComponent<BoxCollider>();
        if (boxCollider3D != null)
        {
            bounds = boxCollider3D.bounds;
            DebugLog("Using BoxCollider (3D) bounds");
        }
        else
        {
            // Fallback to any Collider (3D)
            var collider3D = GetComponent<Collider>();
            if (collider3D != null)
            {
                bounds = collider3D.bounds;
                DebugLog("Using Collider (3D) bounds");
            }
            else
            {
                // Fallback to Renderer bounds
                var renderer = GetComponent<Renderer>();
                if (renderer != null)
                {
                    bounds = renderer.bounds;
                    DebugLog("Using Renderer bounds");
                }
                else
                {
                    // Final fallback: create bounds based on transform position
                    bounds = new Bounds(transform.position, Vector3.one);
                    DebugLog("No colliders or Renderer found, using default bounds");
                }
            }
        }

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

    [ContextMenu("Test Count UI Position")]
    public void TestCountUIPosition()
    {
        if (countUI != null)
        {
            countUI.SetVisible(true);
            countUI.UpdateCount(currentCount, maxCount);
            if (customCountUIOffset > 0)
                DebugLog($"Count UI shown at current offset: {customCountUIOffset}");

            else
                DebugLog($"Count UI shown at current offset: {refillSystem.GetCountUIOffset()}");
        }
        else
        {
            DebugLog("Count UI not available for testing");
        }
    }

    [ContextMenu("Move Count UI Higher")]
    public void MoveCountUIHigher()
    {
        if (customCountUIOffset <= 0)
            customCountUIOffset = refillSystem != null ? refillSystem.GetCountUIOffset() : 0.8f;

        customCountUIOffset += 0.2f;

        if (countUI != null)
        {
            countUI.Initialize(this, customCountUIOffset);
            DebugLog($"Count UI offset increased to: {customCountUIOffset}");
        }
    }

    [ContextMenu("Move Count UI Lower")]
    public void MoveCountUILower()
    {
        if (customCountUIOffset <= 0)
            customCountUIOffset = refillSystem != null ? refillSystem.GetCountUIOffset() : 0.8f;

        customCountUIOffset = Mathf.Max(0.1f, customCountUIOffset - 0.2f);

        if (countUI != null)
        {
            countUI.Initialize(this, customCountUIOffset);
            DebugLog($"Count UI offset decreased to: {customCountUIOffset}");
        }
    }

    [ContextMenu("Force Show Count UI For Testing")]
    public void ForceShowCountUIForTesting()
    {
        DebugLog("=== FORCE SHOWING COUNT UI FOR TESTING ===");

        if (countUI != null)
        {
            DebugLog("CountUI exists, forcing visibility...");

            // Force the GameObject active
            countUI.gameObject.SetActive(true);

            // Force SetVisible
            countUI.SetVisible(true);

            // Update count display
            countUI.UpdateCount(currentCount, maxCount);

            // Run test visibility method
            countUI.TestUIVisibility();

            DebugLog("Force show completed");
        }
        else
        {
            DebugLog("ERROR: countUI is null!");
        }

        DebugLog("=== FORCE SHOW TEST COMPLETE ===");
    }

    [ContextMenu("Force Show Count UI")]
    public void ForceShowCountUI()
    {
        DebugLog("Forcing count UI to show for testing...");

        if (countUI != null)
        {
            countUI.SetVisible(true);
            countUI.UpdateCount(currentCount, maxCount);
            DebugLog("Count UI forced visible");
        }
        else
        {
            DebugLog("ERROR: countUI is null, cannot force show");
        }
    }

    [ContextMenu("Enable Refill and Recreate UI")]
    public void EnableRefillAndRecreateUI()
    {
        DebugLog("Enabling refill and recreating UI...");
        enableRefill = true;

        if (refillSystem != null)
        {
            // Destroy existing UI
            if (countUI != null)
            {
                DestroyImmediate(countUI.gameObject);
                countUI = null;
            }

            if (statusBar != null)
            {
                DestroyImmediate(statusBar.gameObject);
                statusBar = null;
            }

            // Recreate UI
            CreateCountUI();
            CreateStatusBar();
            UpdateCountDisplay();

            DebugLog($"Refill enabled and UI recreated - countUI: {countUI != null}");
        }
        else
        {
            DebugLog("ERROR: RefillSystem not found");
        }
    }

    [ContextMenu("Test Hold Detection")]
    public void TestHoldDetection()
    {
        DebugLog("Testing hold detection manually...");
        OnPointerDown();

        // Simulate hold for 1 second
        StartCoroutine(TestHoldCoroutine());
    }

    private IEnumerator TestHoldCoroutine()
    {
        yield return new WaitForSeconds(1f);
        OnPointerUp();
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"RefillableItem ({GetFoodType()}): {message}");
    }
}