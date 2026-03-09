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

        ValidateSpriteArray();
        StartCoroutine(InitializeAfterRefillableItem());
    }

    private IEnumerator InitializeAfterRefillableItem()
    {
        yield return null;

        refillableItem.OnCountChanged += HandleCountChanged;
        previousCount = refillableItem.GetCurrentCount();
        UpdateCakeSprite(previousCount);

        DebugLog($"Cake initialized with count: {previousCount}/{refillableItem.GetMaxCount()}");

        VerifySettings();
    }

    private void OnDestroy()
    {
        if (refillableItem != null)
            refillableItem.OnCountChanged -= HandleCountChanged;
    }

    /// <summary>
    /// Called by CakeOvenInKitchen to refill cake to full.
    /// </summary>
    public void RefillToFull()
    {
        refillableItem?.RefillToFull();
    }

    private void VerifySettings()
    {
        if (refillableItem == null) return;

        if (refillableItem.GetMaxCount() != 6)
            Debug.LogWarning(
                $"Cake: maxCount is {refillableItem.GetMaxCount()}, expected 6. Set customMaxCount to 6 in RefillableItem.");

        if (!refillableItem.enableRefill)
            Debug.LogError(
                "Cake: enableRefill MUST be TRUE for count tracking to work! Check RefillableItem component.");

        if (refillableItem.enableHoldToRefill)
            Debug.LogWarning(
                "Cake: enableHoldToRefill should be FALSE. Check RefillableItem component.");

        DebugLog("Settings verified");
    }

    private void HandleCountChanged(int currentCount, int maxCount)
    {
        var wasServed = currentCount < previousCount;
        var wasRefilled = currentCount > previousCount;
        previousCount = currentCount;

        DebugLog($"Count changed: {currentCount}/{maxCount} ({(wasServed ? "served" : wasRefilled ? "refilled" : "changed")})");

        UpdateCakeSprite(currentCount);
    }

    private void UpdateCakeSprite(int currentCount)
    {
        if (spriteRenderer == null) return;

        currentCount = Mathf.Clamp(currentCount, 0, 6);

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
    private void Test0Pieces() { UpdateCakeSprite(0); }

    [ContextMenu("Test: Show 1 Piece")]
    private void Test1Piece() { UpdateCakeSprite(1); }

    [ContextMenu("Test: Show 2 Pieces")]
    private void Test2Pieces() { UpdateCakeSprite(2); }

    [ContextMenu("Test: Show 3 Pieces")]
    private void Test3Pieces() { UpdateCakeSprite(3); }

    [ContextMenu("Test: Show 4 Pieces")]
    private void Test4Pieces() { UpdateCakeSprite(4); }

    [ContextMenu("Test: Show 5 Pieces")]
    private void Test5Pieces() { UpdateCakeSprite(5); }

    [ContextMenu("Test: Show 6 Pieces (Full)")]
    private void Test6Pieces() { UpdateCakeSprite(6); }
#endif
    #endregion
}