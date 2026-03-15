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

        if (animator != null)
            animator.enabled = false;
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
        Debug.Log($"☕ Consumed 1 bean. Level {currentUpgradeLevel} machine starting...");

        if (animator != null)
        {
            var brewingLevel = currentUpgradeLevel;
            if (currentUpgradeLevel == 2 && !coffeeRefillableItem.HasSpace(2))
            {
                brewingLevel = 1;
                Debug.Log("☕ Level 2 machine but only 1 space - animating as Level 1");
            }

            animator.enabled = true;
            animator.Rebind(); // force immediate reinitialization
            animator.SetInteger("BrewingLevel", brewingLevel);
            animator.SetTrigger("StartBrewing");
            Debug.Log($"🟢 Animator triggered - BrewingLevel: {brewingLevel}");
        }
    }

    public void OnFinish1Cup()
    {
        cupsBrewedThisSession++;

        if (coffeeRefillableItem != null)
            coffeeRefillableItem.IncreaseCount(1);

        Debug.Log($"☕ Cup 1 done! Cups this session: {cupsBrewedThisSession}");

        if (currentUpgradeLevel == 1)
            CompleteBrewing();
        // Level 2: animation continues automatically to OnFinish2Cup — no extra bean consumed
    }

    public void OnFinish2Cup()
    {
        if (currentUpgradeLevel >= 2)
        {
            cupsBrewedThisSession++;

            if (coffeeRefillableItem != null)
                coffeeRefillableItem.IncreaseCount(1);

            Debug.Log($"☕ Cup 2 done! Total cups brewed: {cupsBrewedThisSession}");
        }

        CompleteBrewing();
    }

    private void CompleteBrewing()
    {
        isBrewing = false;

        if (animator != null)
            animator.enabled = false;

        RestoreUpgradeSprite();

        Debug.Log($"✅ Brewing complete! Level {currentUpgradeLevel} machine finished {cupsBrewedThisSession} cup(s).");
        cupsBrewedThisSession = 0;
    }

    private void RestoreUpgradeSprite()
    {
        if (spriteRenderer == null) return;

        switch (currentUpgradeLevel)
        {
            case 2:
                if (level2Sprite != null)
                    spriteRenderer.sprite = level2Sprite;
                break;
            case 1:
            default:
                break;
        }
    }

    public void SetUpgradeLevel(int level)
    {
        Debug.Log($"🟡 CoffeeMachine SetUpgradeLevel({level}) called - Time: {Time.frameCount}");
        currentUpgradeLevel = level;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null)
            animator = GetComponent<Animator>();

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