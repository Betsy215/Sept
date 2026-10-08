using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The coffee machine on the table. A tap (through the EventSystem, like ServeableItem) consumes
/// one bean and plays the brew animation; animation events add the cups to the Coffee item.
/// </summary>
public class CoffeeMachine : MonoBehaviour, IDraggable, IUpgradeable, IPointerClickHandler
{
    [Header("References")] [SerializeField]
    private BeanContainer beanContainer;

    [SerializeField] private RefillableItem coffeeRefillableItem;
    [SerializeField] private Animator animator;

    [Tooltip("Canvas that holds the pause / level-complete popups. Taps are ignored while it is active. Auto-found from a ServeableItem if not assigned.")]
    [SerializeField] private GameObject popupCanvas;

    [Header("Upgrade Level Sprites")] [SerializeField]
    private Sprite level2Sprite;

    private SpriteRenderer spriteRenderer;
    private bool isBrewing = false;
    private int currentUpgradeLevel = 1;
    private int cupsBrewedThisSession = 0;

    // Level the running brew was started with (a level-2 machine with one free slot brews as level 1).
    private int activeBrewingLevel = 1;

    // False during the arrangement phase, where a tap on the machine is the start of a drag.
    private bool brewingEnabled = true;

    private void Start()
    {
        Debug.Log($"🟢 CoffeeMachine Start() - currentUpgradeLevel: {currentUpgradeLevel}, Time: {Time.frameCount}");

        if (beanContainer == null)
            beanContainer = GetComponentInChildren<BeanContainer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (popupCanvas == null)
        {
            var anyServeable = FindObjectOfType<ServeableItem>();
            if (anyServeable != null) popupCanvas = anyServeable.popupCanvas;
        }

        if (animator != null && !isBrewing)
            animator.enabled = false;
    }

    /// <summary>
    /// IDraggable: GamePhaseManager turns dragging on for the arrangement phase and off for play.
    /// Brewing is only allowed while dragging is off, so a tap during arrangement never consumes a bean.
    /// </summary>
    public void SetDraggingEnabled(bool enabled)
    {
        brewingEnabled = !enabled;

        var draggable = GetComponent<DraggableFood>();
        if (draggable == null && enabled)
            draggable = gameObject.AddComponent<DraggableFood>();

        if (draggable != null)
            draggable.SetDraggingEnabled(enabled);
    }

    private bool CanAcceptInput()
    {
        if (!brewingEnabled) return false;
        if (Time.timeScale == 0f) return false; // paused
        if (popupCanvas != null && popupCanvas.activeInHierarchy) return false; // pause / level-complete popup is up
        return true;
    }

    // Fires only when the finger went down and came back up on this machine (or its bean container).
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!CanAcceptInput()) return;

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

        activeBrewingLevel = currentUpgradeLevel >= 2 ? 2 : 1;
        if (activeBrewingLevel == 2 && !coffeeRefillableItem.HasSpace(2))
        {
            activeBrewingLevel = 1;
            Debug.Log("☕ Level 2 machine but only 1 space - animating as Level 1");
        }

        if (animator != null)
        {
            animator.enabled = true;
            animator.Rebind(); // force immediate reinitialization
            animator.SetInteger("BrewingLevel", activeBrewingLevel);
            animator.SetTrigger("StartBrewing");
            Debug.Log($"🟢 Animator triggered - BrewingLevel: {activeBrewingLevel}");
        }
        else
        {
            // No animator to raise the finish events: deliver the cups right away instead of
            // leaving the machine stuck in isBrewing forever with a bean gone.
            Debug.LogWarning("CoffeeMachine: no Animator - brewing instantly");
            OnFinish1Cup();
            if (isBrewing) OnFinish2Cup();
        }
    }

    // Animation event
    public void OnFinish1Cup()
    {
        if (!isBrewing) return; // stray event from a looping clip after completion

        cupsBrewedThisSession++;

        if (coffeeRefillableItem != null)
            coffeeRefillableItem.IncreaseCount(1);

        Debug.Log($"☕ Cup 1 done! Cups this session: {cupsBrewedThisSession}");

        // Decide on the level this brew was started with, not the upgrade level: a level-2
        // machine that had room for only one cup plays the 1-cup clip, which never reaches
        // OnFinish2Cup, and the clip loops - every loop would add a free cup.
        if (activeBrewingLevel == 1)
            CompleteBrewing();
        // Level 2: animation continues automatically to OnFinish2Cup — no extra bean consumed
    }

    // Animation event
    public void OnFinish2Cup()
    {
        if (!isBrewing) return;

        if (activeBrewingLevel >= 2)
        {
            cupsBrewedThisSession++;

            if (coffeeRefillableItem != null)
                coffeeRefillableItem.IncreaseCount(1);

            Debug.Log($"☕ Cup 2 done! Total cups brewed: {cupsBrewedThisSession}");
        }

        CompleteBrewing();
    }

    // Also raised as an animation event at the end of both brew clips.
    private void CompleteBrewing()
    {
        if (!isBrewing) return;
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