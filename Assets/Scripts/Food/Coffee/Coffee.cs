using UnityEngine;
using System.Collections;

public class Coffee : MonoBehaviour, IUpgradeable
{
    [Header("Coffee Sprites by Level")] [SerializeField]
    private Sprite[] level1CoffeeSprites = new Sprite[3]; // l1cup0, l1cup1, l1cup2

    [SerializeField] private Sprite[] level2CoffeeSprites = new Sprite[4]; // l2cup0, l2cup1, l2cup2, l2cup3  

    [SerializeField] private Sprite level1cup1_serving; // Only serving sprite needed for level 1
    [SerializeField] private Sprite level2cup1_serving; // l2cup1_serving
    [SerializeField] private Sprite level2cup2_serving;
    [SerializeField] private Sprite level2cup0_serving;
    [SerializeField] private Sprite level1cup0_serving; // l2cup2_serving

    [Header("Serving Animation")] [SerializeField]
    private float plateRestoreDelay = 1f; // Delay before showing empty plate after serving

    private int currentUpgradeLevel = 1;
    private RefillableItem refillableItem;
    private SpriteRenderer spriteRenderer;
    private int previousCount;
    private Coroutine plateRestoreCoroutine;
    private Sprite[] currentSpriteArray; // Active normal sprite set based on upgrade level

    private void Start()
    {
        // Get components
        refillableItem = GetComponent<RefillableItem>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (refillableItem != null)
            // Subscribe to count change events
            refillableItem.OnCountChanged += HandleCountChanged;
        else
            Debug.LogError("Coffees: RefillableItem component not found!");

        Debug.Log($"coffeetest: Coffee.Start() - Level: {currentUpgradeLevel}");
    }

    public void SetUpgradeLevel(int level)
    {
        currentUpgradeLevel = level;
        Debug.Log($"Coffee: Upgrade level set to {level}");
        Debug.Log($"coffeetest: Coffee.SetUpgradeLevel({level}) called");
        ApplyUpgradeLevel();
    }

    private void ApplyUpgradeLevel()
    {
        if (refillableItem == null) return;

        var maxCount = currentUpgradeLevel == 1 ? 2 : 3;
        refillableItem.OverrideMaxCount(maxCount);

        SetupSpriteArrayForUpgradeLevel();

        previousCount = refillableItem.GetCurrentCount();
        HandleCountChanged(refillableItem.GetCurrentCount(), refillableItem.GetMaxCount());

        Debug.Log($"Coffee: Applied level {currentUpgradeLevel} configuration (maxCount: {maxCount})");
        Debug.Log($"coffeetest: Coffee.ApplyUpgradeLevel() - maxCount: {maxCount}, currentCount: {previousCount}");
    }

    private void SetupSpriteArrayForUpgradeLevel()
    {
        switch (currentUpgradeLevel)
        {
            case 1:
                currentSpriteArray = level1CoffeeSprites;
                Debug.Log("Coffee: Using Level 1 sprites (2-cup capacity)");
                break;

            case 2:
            default:
                currentSpriteArray = level2CoffeeSprites;
                Debug.Log("Coffee: Using Level 2+ sprites (3-cup capacity)");
                break;
        }

        ValidateSpriteArrays(currentUpgradeLevel == 1 ? 2 : 3);
    }

    private void ValidateSpriteArrays(int maxCount)
    {
        // Validate normal sprites
        for (var i = 0; i <= maxCount; i++)
            if (i >= currentSpriteArray.Length || currentSpriteArray[i] == null)
                Debug.LogError($"Coffees: Missing normal sprite at index {i}");

        // Validate serving sprites based on level
        if (maxCount == 2)
        {
            if (level1cup1_serving == null) Debug.LogError("Coffees: Missing level1cup1_serving sprite");
        }
        else if (maxCount == 3)
        {
            if (level2cup1_serving == null) Debug.LogError("Coffees: Missing level2cup1_serving sprite");
            if (level2cup2_serving == null) Debug.LogError("Coffees: Missing level2cup2_serving sprite");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (refillableItem != null) refillableItem.OnCountChanged -= HandleCountChanged;

        // Stop any running coroutine
        if (plateRestoreCoroutine != null) StopCoroutine(plateRestoreCoroutine);
    }

    // Event handler - called whenever RefillableItem count changes
    private void HandleCountChanged(int currentCount, int maxCount)
    {
        var wasServed = currentCount < previousCount;

        Debug.Log(
            $"coffeetest: Coffee.HandleCountChanged - ENTRY: currentCount={currentCount}, maxCount={maxCount}, previousCount={previousCount}, wasServed={wasServed}");

        previousCount = currentCount;

        Debug.Log($"Coffees: Count changed to {currentCount}/{maxCount} ({(wasServed ? "served" : "brewed")})");

        if (wasServed)
        {
            Debug.Log($"coffeetest: Coffee.HandleCountChanged - Serving path: showPlatesAfterDelay=true");
            // Cup was served - show immediate change then restore plates after delay
            UpdateCoffeeVisuals(currentCount, maxCount, true);
        }
        else
        {
            Debug.Log($"coffeetest: Coffee.HandleCountChanged - Brewing path: showPlatesAfterDelay=false");
            // Coffee was brewed - show immediate change
            UpdateCoffeeVisuals(currentCount, maxCount, false);
        }
    }

    private void UpdateCoffeeVisuals(int currentCount, int maxCount, bool showPlatesAfterDelay)
    {
        Debug.Log(
            $"coffeetest: Coffee.UpdateCoffeeVisuals - currentCount={currentCount}, maxCount={maxCount}, showPlatesAfterDelay={showPlatesAfterDelay}");

        if (showPlatesAfterDelay)
        {
            // Stop any existing coroutine
            if (plateRestoreCoroutine != null)
            {
                Debug.Log($"coffeetest: Coffee.UpdateCoffeeVisuals - Stopping existing coroutine");
                StopCoroutine(plateRestoreCoroutine);
            }

            // Show empty space immediately, then restore plates after delay
            SetCoffeeSprite(currentCount, maxCount, true);
            plateRestoreCoroutine = StartCoroutine(RestorePlatesAfterDelay(currentCount, maxCount));

            Debug.Log("Coffees: Starting serving animation");
        }
        else
        {
            // Brewing - show plates immediately
            Debug.Log($"coffeetest: Coffee.UpdateCoffeeVisuals - Calling SetCoffeeSprite (no delay)");
            SetCoffeeSprite(currentCount, maxCount, false);
        }
    }

    private IEnumerator RestorePlatesAfterDelay(int currentCount, int maxCount)
    {
        Debug.Log($"coffeetest: Coffee.RestorePlatesAfterDelay - Starting coroutine, will wait {plateRestoreDelay}s");
        yield return new WaitForSeconds(plateRestoreDelay);

        Debug.Log($"coffeetest: Coffee.RestorePlatesAfterDelay - Delay finished, restoring plates");
        // Restore plates (normal display)
        SetCoffeeSprite(currentCount, maxCount, false);
        plateRestoreCoroutine = null;

        Debug.Log("Coffees: Plates restored");
    }

    private void SetCoffeeSprite(int currentCount, int maxCount, bool showEmptySpaces)
    {
        Debug.Log(
            $"coffeetest: Coffee.SetCoffeeSprite - ENTRY: currentCount={currentCount}, maxCount={maxCount}, showEmptySpaces={showEmptySpaces}");

        if (spriteRenderer == null || currentSpriteArray == null) return;

        Sprite targetSprite = null;
        var spriteType = "normal";

        if (showEmptySpaces && currentCount > 0)
        {
            // Try to get serving sprite for current count
            targetSprite = GetServingSprite(currentCount, maxCount);
            if (targetSprite != null) spriteType = "serving";
        }

        // If no serving sprite available or not showing empty spaces, use normal sprite
        if (targetSprite == null)
        {
            var spriteIndex = Mathf.Clamp(currentCount, 0, currentSpriteArray.Length - 1);
            spriteIndex = Mathf.Clamp(spriteIndex, 0, maxCount);

            if (spriteIndex < currentSpriteArray.Length && currentSpriteArray[spriteIndex] != null)
                targetSprite = currentSpriteArray[spriteIndex];

            Debug.Log($"coffeetest: Coffee.SetCoffeeSprite - Using normal sprite at index {spriteIndex}");
        }

        // Set the sprite
        if (targetSprite != null)
        {
            spriteRenderer.sprite = targetSprite;
            Debug.Log($"Coffees: Set {spriteType} sprite for {currentCount} cups");
            Debug.Log($"coffeetest: Coffee.SetCoffeeSprite - SUCCESS: Set {spriteType} sprite for {currentCount} cups");
        }
        else
        {
            Debug.LogError($"Coffees: No sprite found for {currentCount} cups");
            Debug.LogError($"coffeetest: Coffee.SetCoffeeSprite - ERROR: No sprite found for {currentCount} cups");
        }
    }

    private Sprite GetServingSprite(int currentCount, int maxCount)
    {
        if (maxCount == 2)
        {
            // Level 1: Serving sprites for 2-cup capacity
            if (currentCount == 1 && level1cup1_serving != null)
                return level1cup1_serving;
            if (currentCount == 0 && level1cup0_serving != null) // ✅ ADD
                return level1cup0_serving;
        }
        else if (maxCount == 3)
        {
            // Level 2: Serving sprites for 3-cup capacity
            if (currentCount == 2 && level2cup2_serving != null)
                return level2cup2_serving;
            if (currentCount == 1 && level2cup1_serving != null)
                return level2cup1_serving;
            if (currentCount == 0 && level2cup0_serving != null) // ✅ ADD
                return level2cup0_serving;
        }

        return null;
    }

    // Public method to refresh sprite setup (useful if maxCount changes after upgrades)
    public void RefreshSpriteSetup()
    {
        SetupSpriteArrayForUpgradeLevel();

        if (refillableItem != null) HandleCountChanged(refillableItem.GetCurrentCount(), refillableItem.GetMaxCount());
    }

    // Debug methods
    [ContextMenu("Test Level 1 Setup")]
    private void TestLevel1Setup()
    {
        if (refillableItem != null)
        {
            currentSpriteArray = level1CoffeeSprites;
            HandleCountChanged(refillableItem.GetCurrentCount(), 2);
            Debug.Log("Coffees: Testing Level 1 setup");
        }
    }

    [ContextMenu("Test Level 2 Setup")]
    private void TestLevel2Setup()
    {
        if (refillableItem != null)
        {
            currentSpriteArray = level2CoffeeSprites;
            HandleCountChanged(refillableItem.GetCurrentCount(), 3);
            Debug.Log("Coffees: Testing Level 2 setup");
        }
    }

    [ContextMenu("Test Serving Animation")]
    private void TestServingAnimation()
    {
        if (refillableItem != null)
        {
            var currentCount = refillableItem.GetCurrentCount();
            var maxCount = refillableItem.GetMaxCount();

            // Show serving sprite immediately
            SetCoffeeSprite(currentCount, maxCount, true);

            // Schedule normal sprite after delay
            StartCoroutine(TestServingCoroutine(currentCount, maxCount));

            Debug.Log("Coffees: Testing serving animation");
        }
    }

    private IEnumerator TestServingCoroutine(int currentCount, int maxCount)
    {
        yield return new WaitForSeconds(plateRestoreDelay);
        SetCoffeeSprite(currentCount, maxCount, false);
        Debug.Log("Coffees: Test serving animation complete");
    }
}