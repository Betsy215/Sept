using UnityEngine;
using System.Collections;

/// <summary>
/// Handles cake/log cake visual display based on count.
/// Shows 0-6 cake pieces visually through sprite switching.
/// Always 6 pieces max (no upgrade levels).
/// No animations - just clean sprite changes.
/// 
/// SETUP REQUIREMENTS:
/// - RefillableItem.enableRefill = TRUE (enable count tracking)
/// - RefillableItem.enableHoldToRefill = FALSE (disable hold gesture)
/// - RefillableItem.customMaxCount = 6
/// - RefillableItem.customStartingCount = -1 (start full)
/// - Remove Count and RefillBar UI children
/// 
/// COMPATIBILITY:
/// - Works with ServeableItem (foodType = "Cake" or "Log Cake")
/// - Works with RefillableItem for count management
/// - Works with OrderSystem for orders
/// - Works with SessionManager for saving
/// - Does NOT implement IUpgradeable (no upgrades)
/// </summary>
public class Cake : MonoBehaviour
{
    [Header("Cake Sprites")] [Tooltip("Sprite array: Index 0 = empty plate, 1-6 = cake pieces")] [SerializeField]
    private Sprite[] cakeSprites = new Sprite[7]; // 0-6 cake pieces

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
            Debug.LogError("Cake: RefillableItem component not found!");
            return;
        }

        if (serveableItem == null)
            Debug.LogWarning("Cake: ServeableItem component not found!");

        // Validate sprites
        ValidateSpriteArray();

        // Wait for RefillableItem to be initialized
        StartCoroutine(InitializeAfterRefillableItem());
    }

    /// <summary>
    /// Wait for RefillableItem to be initialized by RefillSystem before setting up Cake
    /// </summary>
    private IEnumerator InitializeAfterRefillableItem()
    {
        // Wait one frame for RefillableItem.Start() and RefillSystem initialization
        yield return null;

        // Subscribe to count change events
        refillableItem.OnCountChanged += HandleCountChanged;

        // Initialize with current count (now properly initialized)
        previousCount = refillableItem.GetCurrentCount();
        UpdateCakeSprite(previousCount);

        DebugLog($"Cake initialized with count: {previousCount}/{refillableItem.GetMaxCount()}");

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

        // Check max count is 6
        if (refillableItem.GetMaxCount() != 6)
            Debug.LogWarning(
                $"Cake: maxCount is {refillableItem.GetMaxCount()}, expected 6. Set customMaxCount to 6 in RefillableItem.");

        // Check enableRefill is TRUE (required for count tracking)
        if (!refillableItem.enableRefill)
            Debug.LogError(
                "Cake: enableRefill MUST be TRUE for count tracking to work! Check RefillableItem component.");

        // Check enableHoldToRefill is FALSE (cake doesn't use hold gesture)
        if (refillableItem.enableHoldToRefill)
            Debug.LogWarning(
                "Cake: enableHoldToRefill should be FALSE (cake uses different refill mechanism). Check RefillableItem component.");

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
        UpdateCakeSprite(currentCount);
    }

    /// <summary>
    /// Updates the sprite based on current count
    /// </summary>
    private void UpdateCakeSprite(int currentCount)
    {
        if (spriteRenderer == null) return;

        // Clamp to valid range
        currentCount = Mathf.Clamp(currentCount, 0, 6);

        // Set sprite
        if (currentCount < cakeSprites.Length && cakeSprites[currentCount] != null)
        {
            spriteRenderer.sprite = cakeSprites[currentCount];
            DebugLog($"Sprite updated: cake_{currentCount}");
        }
        else
        {
            Debug.LogError($"Cake: Missing sprite at index {currentCount}!");
        }
    }

    /// <summary>
    /// Validates all sprites are assigned
    /// </summary>
    private void ValidateSpriteArray()
    {
        for (var i = 0; i <= 6; i++)
            if (i >= cakeSprites.Length || cakeSprites[i] == null)
                Debug.LogError($"Cake: Missing sprite at index {i}! Assign cake_{i} in inspector.");
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[Cake] {message}");
    }

    #region Editor Testing

#if UNITY_EDITOR
    [ContextMenu("Test: Show 0 Pieces (Empty)")]
    private void Test0Pieces()
    {
        UpdateCakeSprite(0);
    }

    [ContextMenu("Test: Show 1 Piece")]
    private void Test1Piece()
    {
        UpdateCakeSprite(1);
    }

    [ContextMenu("Test: Show 2 Pieces")]
    private void Test2Pieces()
    {
        UpdateCakeSprite(2);
    }

    [ContextMenu("Test: Show 3 Pieces")]
    private void Test3Pieces()
    {
        UpdateCakeSprite(3);
    }

    [ContextMenu("Test: Show 4 Pieces")]
    private void Test4Pieces()
    {
        UpdateCakeSprite(4);
    }

    [ContextMenu("Test: Show 5 Pieces")]
    private void Test5Pieces()
    {
        UpdateCakeSprite(5);
    }

    [ContextMenu("Test: Show 6 Pieces (Full)")]
    private void Test6Pieces()
    {
        UpdateCakeSprite(6);
    }
#endif

    #endregion
}