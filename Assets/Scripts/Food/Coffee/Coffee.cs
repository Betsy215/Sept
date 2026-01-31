using UnityEngine;

public class Coffee : MonoBehaviour, IUpgradeable
{
    [Header("Coffee Sprites by Level")] [SerializeField]
    private Sprite[] level1CoffeeSprites = new Sprite[3]; // l1cup0, l1cup1, l1cup2

    [SerializeField] private Sprite[] level2CoffeeSprites = new Sprite[4]; // l2cup0, l2cup1, l2cup2, l2cup3  

    private int currentUpgradeLevel = 1;
    private RefillableItem refillableItem;
    private SpriteRenderer spriteRenderer;
    private Sprite[] currentSpriteArray; // Active sprite set based on upgrade level

    private void Start()
    {
        // Get components
        refillableItem = GetComponent<RefillableItem>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (refillableItem != null)
            // Subscribe to count change events
            refillableItem.OnCountChanged += HandleCountChanged;
        else
            Debug.LogError("Coffee: RefillableItem component not found!");

        Debug.Log($"Coffee: Start() - Level: {currentUpgradeLevel}");
    }

    public void SetUpgradeLevel(int level)
    {
        currentUpgradeLevel = level;
        Debug.Log($"Coffee: Upgrade level set to {level}");
        ApplyUpgradeLevel();
    }

    private void ApplyUpgradeLevel()
    {
        if (refillableItem == null) return;

        var maxCount = currentUpgradeLevel == 1 ? 2 : 3;
        refillableItem.OverrideMaxCount(maxCount);

        SetupSpriteArrayForUpgradeLevel();

        // Update visual to current count
        HandleCountChanged(refillableItem.GetCurrentCount(), refillableItem.GetMaxCount());

        Debug.Log($"Coffee: Applied level {currentUpgradeLevel} configuration (maxCount: {maxCount})");
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
        // Validate sprites
        for (var i = 0; i <= maxCount; i++)
            if (i >= currentSpriteArray.Length || currentSpriteArray[i] == null)
                Debug.LogError($"Coffee: Missing sprite at index {i}");
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (refillableItem != null) refillableItem.OnCountChanged -= HandleCountChanged;
    }

    // Event handler - called whenever RefillableItem count changes
    private void HandleCountChanged(int currentCount, int maxCount)
    {
        Debug.Log($"Coffee: Count changed to {currentCount}/{maxCount}");
        UpdateCoffeeSprite(currentCount, maxCount);
    }

    private void UpdateCoffeeSprite(int currentCount, int maxCount)
    {
        if (spriteRenderer == null || currentSpriteArray == null) return;

        // Get the sprite index (0, 1, 2 for level 1; 0, 1, 2, 3 for level 2)
        var spriteIndex = Mathf.Clamp(currentCount, 0, currentSpriteArray.Length - 1);
        spriteIndex = Mathf.Clamp(spriteIndex, 0, maxCount);

        // Set the sprite
        if (spriteIndex < currentSpriteArray.Length && currentSpriteArray[spriteIndex] != null)
        {
            spriteRenderer.sprite = currentSpriteArray[spriteIndex];
            Debug.Log($"Coffee: Set sprite for {currentCount} cups (index {spriteIndex})");
        }
        else
        {
            Debug.LogError($"Coffee: No sprite found for {currentCount} cups");
        }
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
            Debug.Log("Coffee: Testing Level 1 setup");
        }
    }

    [ContextMenu("Test Level 2 Setup")]
    private void TestLevel2Setup()
    {
        if (refillableItem != null)
        {
            currentSpriteArray = level2CoffeeSprites;
            HandleCountChanged(refillableItem.GetCurrentCount(), 3);
            Debug.Log("Coffee: Testing Level 2 setup");
        }
    }
}