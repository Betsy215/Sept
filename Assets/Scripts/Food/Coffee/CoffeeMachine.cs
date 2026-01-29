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
        Debug.Log($"coffeetest: CoffeeMachine.Start() - currentUpgradeLevel: {currentUpgradeLevel}");

        if (beanContainer == null)
            beanContainer = GetComponentInChildren<BeanContainer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // ✅ DISABLE animator at start - only enable during brewing
        if (animator != null)
        {
            animator.enabled = false;
            Debug.Log("🔴 Animator disabled at Start - will enable only during brewing");
        }

        if (coffeeRefillableItem != null)
            Debug.Log(
                $"coffeetest: CoffeeMachine.Start() - coffeeRefillableItem: {coffeeRefillableItem.gameObject.name}, current count: {coffeeRefillableItem.GetCurrentCount()}/{coffeeRefillableItem.GetMaxCount()}");
        else
            Debug.LogError($"coffeetest: CoffeeMachine.Start() - coffeeRefillableItem is NULL!");
    }

    private void OnMouseUpAsButton()
    {
        Debug.Log($"coffeetest: CoffeeMachine.OnMouseUpAsButton() - CanBrew: {CanBrew()}");
        if (CanBrew())
            StartBrewing();
    }

    private bool CanBrew()
    {
        if (isBrewing)
        {
            Debug.Log($"coffeetest: CoffeeMachine.CanBrew() - FALSE: Already brewing");
            return false;
        }

        if (beanContainer == null || coffeeRefillableItem == null)
        {
            Debug.Log(
                $"coffeetest: CoffeeMachine.CanBrew() - FALSE: beanContainer={beanContainer != null}, coffeeRefillableItem={coffeeRefillableItem != null}");
            return false;
        }

        var hasEnoughBeans = beanContainer.HasEnoughBeans(1);
        var hasSpace = coffeeRefillableItem.HasSpace(1);

        Debug.Log(
            $"coffeetest: CoffeeMachine.CanBrew() - hasEnoughBeans: {hasEnoughBeans}, hasSpace: {hasSpace}, coffee: {coffeeRefillableItem.GetCurrentCount()}/{coffeeRefillableItem.GetMaxCount()}");

        return hasEnoughBeans && hasSpace;
    }

    private void StartBrewing()
    {
        isBrewing = true;
        cupsBrewedThisSession = 0;

        Debug.Log(
            $"coffeetest: CoffeeMachine.StartBrewing() - currentUpgradeLevel: {currentUpgradeLevel}, coffee before: {coffeeRefillableItem.GetCurrentCount()}/{coffeeRefillableItem.GetMaxCount()}");

        beanContainer.ConsumeBeans(1);

        // ✅ Enable animator ONLY when brewing starts
        if (animator != null)
        {
            animator.enabled = true;
            animator.SetInteger("BrewingLevel", currentUpgradeLevel);
            animator.SetTrigger("StartBrewing");
            Debug.Log("🟢 Animator enabled for brewing");
            Debug.Log($"coffeetest: CoffeeMachine.StartBrewing() - Animator enabled, trigger set");
        }

        Debug.Log($"☕ Brewing started... Level {currentUpgradeLevel} machine");
    }

    public void OnFinish1Cup()
    {
        Debug.Log(
            $"coffeetest: CoffeeMachine.OnFinish1Cup() - ENTRY: cupsBrewedThisSession={cupsBrewedThisSession}, currentUpgradeLevel={currentUpgradeLevel}");
        Debug.Log(
            $"coffeetest: CoffeeMachine.OnFinish1Cup() - Coffee BEFORE increase: {coffeeRefillableItem.GetCurrentCount()}/{coffeeRefillableItem.GetMaxCount()}");

        cupsBrewedThisSession++;

        if (coffeeRefillableItem != null)
            coffeeRefillableItem.IncreaseCount(1);

        Debug.Log(
            $"coffeetest: CoffeeMachine.OnFinish1Cup() - Coffee AFTER increase: {coffeeRefillableItem.GetCurrentCount()}/{coffeeRefillableItem.GetMaxCount()}");
        Debug.Log($"☕ First cup complete! Cups brewed this session: {cupsBrewedThisSession}");

        // Level 1 machine: Finish brewing after 1 cup
        if (currentUpgradeLevel == 1)
        {
            Debug.Log($"coffeetest: CoffeeMachine.OnFinish1Cup() - Level 1 machine: Calling CompleteBrewing()");
            CompleteBrewing();
        }
        // Level 2+ machine: Continue brewing for second cup
        else
        {
            Debug.Log(
                $"coffeetest: CoffeeMachine.OnFinish1Cup() - Level 2+ machine: Checking if should continue brewing");

            if (beanContainer != null && beanContainer.HasEnoughBeans(1))
            {
                beanContainer.ConsumeBeans(1);
                Debug.Log("☕ Continuing to brew second cup...");
                Debug.Log($"coffeetest: CoffeeMachine.OnFinish1Cup() - Continuing to brew second cup, beans consumed");
            }
            else
            {
                Debug.Log(
                    $"coffeetest: CoffeeMachine.OnFinish1Cup() - NOT continuing: beanContainer={beanContainer != null}, hasEnoughBeans={beanContainer?.HasEnoughBeans(1)}");
            }
        }
    }

    public void OnFinish2Cup()
    {
        Debug.Log($"coffeetest: CoffeeMachine.OnFinish2Cup() - ENTRY: currentUpgradeLevel={currentUpgradeLevel}");
        Debug.Log(
            $"coffeetest: CoffeeMachine.OnFinish2Cup() - Coffee BEFORE increase: {coffeeRefillableItem.GetCurrentCount()}/{coffeeRefillableItem.GetMaxCount()}");

        // Only called for Level 2+ machines
        if (currentUpgradeLevel >= 2)
        {
            cupsBrewedThisSession++;

            if (coffeeRefillableItem != null)
                coffeeRefillableItem.IncreaseCount(1);

            Debug.Log(
                $"coffeetest: CoffeeMachine.OnFinish2Cup() - Coffee AFTER increase: {coffeeRefillableItem.GetCurrentCount()}/{coffeeRefillableItem.GetMaxCount()}");
            Debug.Log($"☕ Second cup complete! Total cups brewed: {cupsBrewedThisSession}");
        }
        else
        {
            Debug.Log($"coffeetest: CoffeeMachine.OnFinish2Cup() - Skipping (currentUpgradeLevel < 2)");
        }

        Debug.Log($"coffeetest: CoffeeMachine.OnFinish2Cup() - Calling CompleteBrewing()");
        CompleteBrewing();
    }

    private void CompleteBrewing()
    {
        Debug.Log(
            $"coffeetest: CoffeeMachine.CompleteBrewing() - ENTRY: cupsBrewedThisSession={cupsBrewedThisSession}, final coffee: {coffeeRefillableItem.GetCurrentCount()}/{coffeeRefillableItem.GetMaxCount()}");

        isBrewing = false;

        // ✅ Disable animator after brewing completes
        if (animator != null)
        {
            animator.enabled = false;
            Debug.Log("🔴 Animator disabled after brewing");
        }

        Debug.Log($"✅ Brewing complete! Level {currentUpgradeLevel} machine finished {cupsBrewedThisSession} cups.");
        Debug.Log($"coffeetest: CoffeeMachine.CompleteBrewing() - EXIT: Brewing complete");

        cupsBrewedThisSession = 0;
    }

    public void SetUpgradeLevel(int level)
    {
        Debug.Log($"🟡 CoffeeMachine SetUpgradeLevel({level}) called - Time: {Time.frameCount}");
        Debug.Log($"coffeetest: CoffeeMachine.SetUpgradeLevel({level}) - ENTRY");

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

        // Set parameter for when animator gets enabled during brewing
        if (animator != null)
        {
            animator.SetInteger("BrewingLevel", currentUpgradeLevel);
            Debug.Log($"🎬 CoffeeMachine: Set BrewingLevel parameter to {currentUpgradeLevel}");
            Debug.Log(
                $"coffeetest: CoffeeMachine.SetUpgradeLevel() - Set BrewingLevel parameter to {currentUpgradeLevel}");
        }

        Debug.Log($"⬆️ CoffeeMachine: Upgrade level set to {level}");
    }
}