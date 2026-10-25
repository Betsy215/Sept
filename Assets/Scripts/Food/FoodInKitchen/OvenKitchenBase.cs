using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// A kitchen oven. Any oven bakes any recipe (owner, 25 October 2026: "every oven to be available for any recipe"):
///   1. A tap on an idle oven opens the recipe picker (OvenPicker): the notes of the oven foods on today's menu.
///      With a single recipe on the menu the bake starts at once.
///   2. The pick starts the bake: the oven swaps to its lit sprite and shows that recipe's note for as long as it bakes.
///      An idle oven shows no note.
///   3. When the bake ends the recipe's table item is refilled, the note goes, the oven goes dark again.
///   4. The bake survives the kitchen closing (the timer runs on a persistent helper, the state in CooldownRegistry
///      and Recipes, both keyed by the oven's spot).
/// Ovens are bought (owner, 25 October): the player starts with one (the top one), the shop's "OvenUpgrade" card adds
/// the second and the third (SessionManager.OvensOwned). An oven not yet owned stands dark with a lock and ignores taps
/// (OvenAssigner sets that up at kitchen load). The three subclasses are kept only because the scene points at them.
/// </summary>
public abstract class OvenKitchenBase : MonoBehaviour, IPointerClickHandler
{
    [Header("Sprites")] [Tooltip("The oven's idle sprite (assigned automatically from Image on Awake)")]
    public Sprite defaultSprite;

    [Tooltip("The oven's baking sprite")] public Sprite bakingSprite;

    [Header("Audio")] public AudioClip interactSound;
    public AudioClip bakeCompleteSound;

    [Header("Bake Time")] [Tooltip("Fallback bake time; the recipe's own time in BakeTimes wins")]
    public float bakeTime = 10f;

    [Header("Recipe")]
    [Tooltip("The recipe this oven is baking now (set by the picker, cleared when the bake ends); empty = idle. Read-only in the Inspector.")]
    public string food = "";

    /// Bake time per recipe, in seconds (owner: the ovens are all the same, each recipe has its own time).
    public static readonly Dictionary<string, float> BakeTimes =
        new Dictionary<string, float> { { "Bread", 8f }, { "Choux", 10f }, { "Cake", 15f }, { "Cookies", 6f } };

    /// The recipe each oven (by cooldown key) is baking, kept across kitchen loads with the pick times.
    public static readonly Dictionary<string, string> Recipes = new Dictionary<string, string>();

    /// This oven's bake time now: the recipe's, else the Inspector value.
    public float BakeTime => !string.IsNullOrEmpty(food) && BakeTimes.TryGetValue(food, out var t) ? t : bakeTime;

    /// False for an oven the player has not bought yet (OvenAssigner decides at kitchen load).
    [System.NonSerialized] public bool owned = true;

    [Header("Debug")] public bool enableDebugLogs = true;

    private Image image;
    private KitchenTimerHelper timerHelper;
    private string cooldownKey;   // cached in Awake: the timer's callback can run on a destroyed instance (the kitchen closed mid-bake),
                                  // where transform and gameObject throw MissingReferenceException (the 25 October scan)

    protected virtual void Awake()
    {
        image = GetComponent<Image>();

        // Capture the default sprite from the Image if not manually assigned
        if (defaultSprite == null && image != null)
            defaultSprite = image.sprite;

        OvenAssigner.EnsureAssigned();   // owned and locked ovens, the note pictures (once per kitchen load)
        KitchenWindow.Apply();           // chapter 2: the beach through the window (16 October)
        KitchenBlender.Apply();          // the blender as its own object, every chapter (17 October)
        KitchenWaffleMachine.Apply();    // chapter 2: the waffle machine that refills the cone holder (22 October)

        cooldownKey = "Oven:" + (transform.parent != null ? transform.parent.name + "/" : "") + gameObject.name;
        // The bake timer must outlive the kitchen scene, so it always runs on the persistent helper.
        timerHelper = KitchenTimerHelper.GetOrCreate($"[OvenHelper] {GetCooldownKey()}");
        CheckBakingStateOnWake();
    }

    /// <summary>
    /// On scene reload: if still baking, restore the lit sprite and the note and re-attach to the running
    /// timer (or resume the remaining time if no timer is running).
    /// </summary>
    private void CheckBakingStateOnWake()
    {
        var key = GetCooldownKey();
        var pickTime = CooldownRegistry.GetPickTime(key);
        if (pickTime < 0f || !Recipes.TryGetValue(key, out var recipe) || string.IsNullOrEmpty(recipe))
        {
            if (pickTime >= 0f) CooldownRegistry.ClearPickTime(key);   // a pick time without a recipe (a reload mid-bake): idle
            food = ""; ShowNote(false); return;
        }
        food = recipe;
        var remaining = BakeTime - (Time.realtimeSinceStartup - pickTime);

        if (remaining > 0f)
        {
            DebugLog($"Still baking {food} ({remaining:F1}s remaining) — restoring baking sprite");
            SetSprite(bakingSprite); ShowNote(true);
            timerHelper.StartTimer(key, remaining, OnBakeComplete);
        }
        else
        {
            // Finished baking while we were away — refill immediately
            DebugLog("Bake finished while scene was unloaded — refilling now");
            CooldownRegistry.ClearPickTime(key);
            OnRefill();
            Recipes.Remove(key); food = "";
            SetSprite(defaultSprite); ShowNote(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.timeScale == 0f) return; // paused: UI taps still arrive, gameplay must not
        // Accept taps on the oven or any of its decorations (the note covers the middle of each oven window;
        // rejecting it made centre taps do nothing).
        var hit = eventData.pointerCurrentRaycast.gameObject;
        if (hit == null || !hit.transform.IsChildOf(transform)) return;
        if (!owned) return;                                                       // locked: bought in the shop
        if (IsBaking) return;

        var recipes = OvenAssigner.RecipesOnTheMenu();
        if (recipes.Count == 0) { DebugLog("nothing to bake on today's menu"); return; }
        if (recipes.Count == 1) { StartBake(recipes[0]); return; }                 // one recipe: no picker to pick from
        OvenPicker.Open(this, recipes);
    }

    /// Start baking a recipe now (the picker's choice, or the only one on the menu).
    public void StartBake(string recipe)
    {
        if (!owned || IsBaking || string.IsNullOrEmpty(recipe)) return;
        food = recipe;
        var key = GetCooldownKey();
        Recipes[key] = recipe;
        DebugLog($"{gameObject.name} bakes {recipe} for {BakeTime:F0} s");

        if (interactSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(interactSound);

        SetSprite(bakingSprite); ShowNote(true);
        CooldownRegistry.RecordPickTime(key);
        timerHelper.StartTimer(key, BakeTime, OnBakeComplete);
    }

    /// True while this oven's bake is running.
    public bool IsBaking => CooldownRegistry.GetPickTime(GetCooldownKey()) >= 0f && CooldownRegistry.IsOnCooldown(GetCooldownKey(), BakeTime);

    /// True when some oven is baking this recipe right now (the picker greys it out).
    public static bool IsRecipeBaking(string recipe)
    {
        foreach (var kv in Recipes)
            if (kv.Value == recipe && BakeTimes.TryGetValue(recipe, out var t) && CooldownRegistry.IsOnCooldown(kv.Key, t)) return true;
        return false;
    }

    private void OnBakeComplete()
    {
        var key = GetCooldownKey();
        DebugLog("Bake complete: " + key);
        CooldownRegistry.ClearPickTime(key);
        if (string.IsNullOrEmpty(food) && Recipes.TryGetValue(key, out var r)) food = r;   // a destroyed instance keeps its field; a fresh one reads the registry
        OnRefill();
        Recipes.Remove(key); food = "";
        SetSprite(defaultSprite); ShowNote(false);
        if (bakeCompleteSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(bakeCompleteSound);
    }

    private void SetSprite(Sprite sprite)
    {
        if (this == null || image == null || sprite == null) return;   // a destroyed instance's callback
        image.sprite = sprite;
    }

    /// The note on the oven: the recipe's picture while it bakes, nothing otherwise (owner, 25 October).
    private void ShowNote(bool on)
    {
        if (this == null) return;   // the callback of a destroyed instance
        foreach (Transform child in transform)
        {
            if (!child.name.StartsWith("sticker") || child.GetComponent<KitchenFoodGate>() != null) continue;
            var img = child.GetComponent<Image>();
            if (on && img != null) { var s = OvenAssigner.NoteFor(food); if (s != null) { img.sprite = s; img.preserveAspect = true; } }
            child.gameObject.SetActive(on && img != null && img.sprite != null);
            break;   // the first note only
        }
    }

    /// <summary>
    /// Unique key for the cooldown registry: the oven's spot (a bake belongs to the oven it runs in, whatever the recipe).
    /// </summary>
    protected virtual string GetCooldownKey()
    {
        return cooldownKey ?? ("Oven:" + (transform.parent != null ? transform.parent.name + "/" : "") + gameObject.name);
    }

    /// <summary>
    /// Refills the table item of the recipe this oven baked.
    /// </summary>
    protected virtual void OnRefill()
    {
        switch (food)
        {
            case "Bread": { var f = FindObjectOfType<Bread>(); if (f != null) f.RefillToFull(); break; }
            case "Choux": { var f = FindObjectOfType<Choux>(); if (f != null) f.RefillToFull(); break; }
            case "Cake": { var f = FindObjectOfType<Cake>(); if (f != null) f.RefillToFull(); break; }
            case "Cookies": { var f = FindObjectOfType<Cookies>(); if (f != null) f.RefillToFull(); break; }
            default: DebugLog("no food to refill"); return;
        }
        DebugLog(food + " refilled to full!");
    }

    protected void DebugLog(string message)
    {
        if (this != null && enableDebugLogs) Debug.Log($"[{GetType().Name}] {message}");
    }
}
