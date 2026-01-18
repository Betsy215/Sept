using UnityEngine;

// MODIFY: Add IUpgradeable interface to class declaration
public class CoffeeMachine : MonoBehaviour, IDraggable, IUpgradeable
{
    [Header("References")] [SerializeField]
    private BeanContainer beanContainer;

    [SerializeField] private RefillableItem coffeeRefillableItem;
    [SerializeField] private Animator animator;

    [Header("Upgrade Level Sprites")] [SerializeField]
    private Sprite level2Sprite;

    private SpriteRenderer spriteRenderer; // Assign in inspector

    private bool isBrewing = false;

    // ADD: New fields for upgrade system and progressive brewing
    private int currentUpgradeLevel = 1;
    private int cupsBrewedThisSession = 0;

    private void Awake()
    {
        Debug.Log($"🔵 CoffeeMachine Awake() - Time: {Time.frameCount}");
    }

    private void Start()
    {
        Debug.Log($"🟢 CoffeeMachine Start() - currentUpgradeLevel: {currentUpgradeLevel}, Time: {Time.frameCount}");

        if (beanContainer == null)
            beanContainer = GetComponentInChildren<BeanContainer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }


    private void OnMouseUpAsButton()
    {
        if (CanBrew()) StartBrewing();
    }

    private bool CanBrew()
    {
        if (isBrewing) return false;
        if (beanContainer == null || coffeeRefillableItem == null) return false;

        // MODIFY: Check if there's space for at least 1 cup
        return beanContainer.HasEnoughBeans(1) && coffeeRefillableItem.HasSpace(1);
    }

    private void StartBrewing()
    {
        isBrewing = true;
        cupsBrewedThisSession = 0; // Reset counter

        // MODIFY: Consume beans for 1 cup at a time (we'll consume more as we brew)
        beanContainer.ConsumeBeans(1);

        // Trigger animation
        if (animator != null)
            animator.SetTrigger("StartBrewing");

        Debug.Log($"Brewing started... Level {currentUpgradeLevel} machine");
    }

    // REMOVE: Old OnBrewingComplete method completely
    // DELETE: public void OnBrewingComplete() { ... }

    // ADD: New progressive brewing methods
    public void OnFinish1Cup()
    {
        cupsBrewedThisSession++;

        // Add 1 cup to coffee supply
        if (coffeeRefillableItem != null)
            coffeeRefillableItem.IncreaseCount(1);

        Debug.Log($"First cup complete! Cups brewed this session: {cupsBrewedThisSession}");

        // Level 1 machine: Finish brewing after 1 cup
        if (currentUpgradeLevel == 1)
        {
            CompleteBrewing();
        }
        // Level 2+ machine: Continue brewing for second cup
        else
        {
            // Consume beans for second cup
            if (beanContainer != null && beanContainer.HasEnoughBeans(1))
            {
                beanContainer.ConsumeBeans(1);
                Debug.Log("Continuing to brew second cup...");
            }
        }
    }

    public void OnFinish2Cup()
    {
        // Only called for Level 2+ machines (Level 1 stops at OnFinish1Cup)
        if (currentUpgradeLevel >= 2)
        {
            cupsBrewedThisSession++;

            // Add second cup to coffee supply
            if (coffeeRefillableItem != null)
                coffeeRefillableItem.IncreaseCount(1);

            Debug.Log($"Second cup complete! Total cups brewed: {cupsBrewedThisSession}");
        }

        CompleteBrewing();
    }

    // ADD: Centralized brewing completion
    private void CompleteBrewing()
    {
        isBrewing = false;

        Debug.Log($"Brewing complete! Level {currentUpgradeLevel} machine finished {cupsBrewedThisSession} cups.");

        cupsBrewedThisSession = 0; // Reset for next brewing session
    }

    public void SetUpgradeLevel(int level)
    {
        Debug.Log($"🟡 CoffeeMachine SetUpgradeLevel({level}) called - Time: {Time.frameCount}");
        currentUpgradeLevel = level;

        // Initialize components if needed
        if (animator == null)
            animator = GetComponent<Animator>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // Set animator parameter
        if (animator != null)
        {
            animator.SetInteger("BrewingLevel", currentUpgradeLevel);
            Debug.Log($"🎬 CoffeeMachine: Set BrewingLevel parameter to {currentUpgradeLevel}");
        }

        // ✅ ACTUALLY SWITCH THE SPRITE HERE (like Coffee does)
        if (spriteRenderer != null)
            switch (currentUpgradeLevel)
            {
                case 2:
                    if (level2Sprite != null)
                    {
                        spriteRenderer.sprite = level2Sprite;
                        Debug.Log($"🎨 CoffeeMachine: Updated to Level 2 sprite");
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ level2Sprite not assigned!");
                    }

                    break;

                case 1:
                default:
                    // Keep default Level 1 sprite
                    Debug.Log($"🎨 CoffeeMachine: Using Level 1 sprite");
                    break;
            }

        Debug.Log($"⬆️ CoffeeMachine: Upgrade level set to {level}");
    }
}