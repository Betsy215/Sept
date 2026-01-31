using UnityEngine;

public class CoffeeMachine : MonoBehaviour, IDraggable, IUpgradeable
{
    [Header("References")] [SerializeField]
    private BeanContainer beanContainer;

    [SerializeField] private RefillableItem coffeeRefillableItem;
    [SerializeField] private Animator animator;

    [Header("Upgrade Level Sprites")] [SerializeField]
    private Sprite level2Sprite;

    private SpriteRenderer spriteRenderer;
    private bool isBrewing = false;
    private int currentUpgradeLevel = 1;
    private int cupsBrewedThisSession = 0;

    private void Start()
    {
        Debug.Log($"🟢 CoffeeMachine Start() - currentUpgradeLevel: {currentUpgradeLevel}, Time: {Time.frameCount}");

        if (beanContainer == null)
            beanContainer = GetComponentInChildren<BeanContainer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // ✅ DISABLE animator at start - only enable during brewing
        if (animator != null)
        {
            animator.enabled = true;
            Debug.Log("🔴 Animator disabled at Start - will enable only during brewing");
        }
    }

    private void OnMouseUpAsButton()
    {
        if (CanBrew())
            StartBrewing();
    }

    private bool CanBrew()
    {
        if (isBrewing) return false;
        if (beanContainer == null || coffeeRefillableItem == null) return false;

        return beanContainer.HasEnoughBeans(1) && coffeeRefillableItem.HasSpace(1);
    }

    private void StartBrewing()
    {
        isBrewing = true;
        cupsBrewedThisSession = 0;

        beanContainer.ConsumeBeans(1);

        // ✅ Enable animator ONLY when brewing starts
        if (animator != null)
        {
            // animator.enabled = true;

            // Set BrewingLevel based on available space
            var brewingLevel = currentUpgradeLevel;

            // Level 2 machine with only 1 space left → brew as Level 1
            if (currentUpgradeLevel == 2 && coffeeRefillableItem.HasSpace(1) && !coffeeRefillableItem.HasSpace(2))
            {
                brewingLevel = 1;
                Debug.Log("☕ Level 2 machine but only 1 space - brewing 1 cup");
            }

            animator.SetInteger("BrewingLevel", brewingLevel);
            animator.SetTrigger("StartBrewing");
            Debug.Log($"🟢 Animator enabled for brewing - BrewingLevel set to {brewingLevel}");
        }

        Debug.Log($"☕ Brewing started... Level {currentUpgradeLevel} machine");
    }

    public void OnFinish1Cup()
    {
        cupsBrewedThisSession++;

        if (coffeeRefillableItem != null)
            coffeeRefillableItem.IncreaseCount(1);

        Debug.Log($"☕ First cup complete! Cups brewed this session: {cupsBrewedThisSession}");

        // Level 1 machine: Finish brewing after 1 cup
        if (currentUpgradeLevel == 1)
        {
            CompleteBrewing();
        }
        // Level 2+ machine: Continue brewing for second cup
        else
        {
            if (beanContainer != null && beanContainer.HasEnoughBeans(1))
            {
                beanContainer.ConsumeBeans(1);
                Debug.Log("☕ Continuing to brew second cup...");
            }
        }
    }

    public void OnFinish2Cup()
    {
        // Only called for Level 2+ machines
        if (currentUpgradeLevel >= 2)
        {
            cupsBrewedThisSession++;

            if (coffeeRefillableItem != null)
                coffeeRefillableItem.IncreaseCount(1);

            Debug.Log($"☕ Second cup complete! Total cups brewed: {cupsBrewedThisSession}");
        }

        CompleteBrewing();
    }

    private void CompleteBrewing()
    {
        isBrewing = false;

        // ✅ Disable animator after brewing completes
        if (animator != null)
            // animator.enabled = false;
            Debug.Log("🔴 Animator disabled after brewing");

        Debug.Log($"✅ Brewing complete! Level {currentUpgradeLevel} machine finished {cupsBrewedThisSession} cups.");
        cupsBrewedThisSession = 0;
    }

    public void SetUpgradeLevel(int level)
    {
        Debug.Log($"🟡 CoffeeMachine SetUpgradeLevel({level}) called - Time: {Time.frameCount}");
        currentUpgradeLevel = level;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null)
            animator = GetComponent<Animator>();

        // Change sprite (animator is already disabled from Start)
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
                        Debug.LogWarning("⚠️ level2Sprite not assigned in Inspector!");
                    }

                    break;

                case 1:
                default:
                    Debug.Log($"🎨 CoffeeMachine: Using Level 1 sprite (default)");
                    break;
            }
        else
            Debug.LogError("❌ CoffeeMachine: SpriteRenderer not found!");

        Debug.Log($"⬆️ CoffeeMachine: Upgrade level set to {level}");
    }
}