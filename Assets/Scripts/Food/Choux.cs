using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

/// <summary>
/// Handles bread board visual display based on count.
/// Shows 0-4 bread pieces visually through sprite switching.
/// Always 4 breads max (no upgrade levels).
/// No animations - just clean sprite changes.
/// 
/// SETUP REQUIREMENTS:
/// - RefillableItem.enableRefill = TRUE (enable count tracking)
/// - RefillableItem.enableHoldToRefill = FALSE (disable hold gesture)
/// - RefillableItem.customMaxCount = 4
/// - RefillableItem.customStartingCount = -1 (start full)
/// - Remove Count and RefillBar UI children
/// 
/// COMPATIBILITY:
/// - Works with ServeableItem (foodType = "Bread")
/// - Works with RefillableItem for count management
/// - Works with OrderSystem for orders
/// - Works with SessionManager for saving
/// - Does NOT implement IUpgradeable (no upgrades)
/// </summary>
public class Choux : MonoBehaviour
{
    [FormerlySerializedAs("breadSprites")]
    [Header("Bread Sprites")]
    [Tooltip("Sprite array: Index 0 = empty board, 1-4 = bread pieces")]
    [SerializeField]
    private Sprite[] chouxSprites = new Sprite[5]; // 0-4 breads

    [Header("Debug")] public bool enableDebugLogs = true;

    // References
    private RefillableItem refillableItem;
    private SpriteRenderer spriteRenderer;
    private ServeableItem serveableItem;
    private int previousCount;

    private void Start()
    {
        // Get components
        refillableItem = GetComponent<RefillableItem>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        serveableItem = GetComponent<ServeableItem>();

        if (refillableItem == null)
        {
            Debug.LogError("Bread: RefillableItem component not found!");
            return;
        }

        if (serveableItem == null)
            Debug.LogWarning("Bread: ServeableItem component not found!");

        // Validate sprites
        ValidateSpriteArray();

        // FIXED: Wait for RefillableItem to be initialized
        StartCoroutine(InitializeAfterRefillableItem());
    }

    /// <summary>
    /// Wait for RefillableItem to be initialized by RefillSystem before setting up Bread
    /// </summary>
    private IEnumerator InitializeAfterRefillableItem()
    {
        // Wait one frame for RefillableItem.Start() and RefillSystem initialization
        yield return null;

        // Subscribe to count change events
        refillableItem.OnCountChanged += HandleCountChanged;

        // Initialize with current count (now properly initialized)
        previousCount = refillableItem.GetCurrentCount();
        UpdateBreadSprite(previousCount);

        DebugLog($"Bread initialized with count: {previousCount}/{refillableItem.GetMaxCount()}");

        // Verify settings
        VerifySettings();
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (refillableItem != null)
            refillableItem.OnCountChanged -= HandleCountChanged;
    }

    /// <summary>
    /// Verifies RefillableItem settings are correct
    /// </summary>
    private void VerifySettings()
    {
        if (refillableItem == null) return;

        // Check max count is 4
        if (refillableItem.GetMaxCount() != 4)
            Debug.LogWarning(
                $"Bread: maxCount is {refillableItem.GetMaxCount()}, expected 4. Set customMaxCount to 4 in RefillableItem.");

        // Check enableRefill is TRUE (required for count tracking)
        if (!refillableItem.enableRefill)
            Debug.LogError(
                "Bread: enableRefill MUST be TRUE for count tracking to work! Check RefillableItem component.");

        // Check enableHoldToRefill is FALSE (bread doesn't use hold gesture)
        // NOTE: This field only exists if you've updated RefillableItem.cs
        if (refillableItem.enableHoldToRefill)
            Debug.LogWarning(
                "Bread: enableHoldToRefill should be FALSE (bread uses different refill mechanism). Check RefillableItem component.");

        DebugLog("Settings verified");
    }

    /// <summary>
    /// Event handler - called when RefillableItem count changes
    /// </summary>
    private void HandleCountChanged(int currentCount, int maxCount)
    {
        var wasServed = currentCount < previousCount;
        var wasRefilled = currentCount > previousCount;
        previousCount = currentCount;

        DebugLog(
            $"Count changed: {currentCount}/{maxCount} ({(wasServed ? "served" : wasRefilled ? "refilled" : "changed")})");

        // Update sprite
        UpdateBreadSprite(currentCount);
    }

    /// <summary>
    /// Updates the sprite based on current count
    /// </summary>
    private void UpdateBreadSprite(int currentCount)
    {
        if (spriteRenderer == null) return;

        // Clamp to valid range
        currentCount = Mathf.Clamp(currentCount, 0, 4);

        // Set sprite
        if (currentCount < chouxSprites.Length && chouxSprites[currentCount] != null)
        {
            spriteRenderer.sprite = chouxSprites[currentCount];
            DebugLog($"Sprite updated: bread_{currentCount}");
        }
        else
        {
            Debug.LogError($"Bread: Missing sprite at index {currentCount}!");
        }
    }

    /// <summary>
    /// Validates all sprites are assigned
    /// </summary>
    private void ValidateSpriteArray()
    {
        for (var i = 0; i <= 4; i++)
            if (i >= chouxSprites.Length || chouxSprites[i] == null)
                Debug.LogError($"Bread: Missing sprite at index {i}! Assign bread_{i} in inspector.");
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[Bread] {message}");
    }

    #region Editor Testing

#if UNITY_EDITOR
    [ContextMenu("Test: Show 0 Breads (Empty)")]
    private void Test0Breads()
    {
        UpdateBreadSprite(0);
    }

    [ContextMenu("Test: Show 1 Bread")]
    private void Test1Bread()
    {
        UpdateBreadSprite(1);
    }

    [ContextMenu("Test: Show 2 Breads")]
    private void Test2Breads()
    {
        UpdateBreadSprite(2);
    }

    [ContextMenu("Test: Show 3 Breads")]
    private void Test3Breads()
    {
        UpdateBreadSprite(3);
    }

    [ContextMenu("Test: Show 4 Breads (Full)")]
    private void Test4Breads()
    {
        UpdateBreadSprite(4);
    }
#endif

    #endregion
}