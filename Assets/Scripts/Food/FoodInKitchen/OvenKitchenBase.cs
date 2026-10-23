using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Base class for kitchen oven items that:
///   1. Swap to a "baking" sprite when clicked
///   2. Refill the target food item after a bake time
///   3. Swap back to the default sprite when done
///   4. Persist baking state across kitchen scene unload/reload
///
/// Shared ovens (owner, 16 October 2026): the ovens are all the same. When the kitchen loads, OvenAssigner hands
/// each oven one of the oven foods that are on the menu, in the order they are on; the note on the oven shows that
/// food, the bake time is the recipe's (BakeTimes), and the bake refills that food. An oven with nothing to bake
/// shows no note and ignores taps. The three subclasses are kept only because the scene points at them.
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

    [Header("Shared ovens (owner, 16 October 2026)")]
    [Tooltip("The food this oven bakes today. OvenAssigner sets it from the menu when the kitchen loads; empty = an idle oven")]
    public string food = "";

    /// Bake time per recipe, in seconds (owner: the ovens are all the same, each recipe has its own time).
    public static readonly System.Collections.Generic.Dictionary<string, float> BakeTimes =
        new System.Collections.Generic.Dictionary<string, float> { { "Bread", 8f }, { "Choux", 10f }, { "Cake", 15f }, { "Cookies", 6f } };

    /// This oven's bake time today: the recipe's, else the Inspector value.
    public float BakeTime => !string.IsNullOrEmpty(food) && BakeTimes.TryGetValue(food, out var t) ? t : bakeTime;

    [Header("Debug")] public bool enableDebugLogs = true;

    private Image image;
    private KitchenTimerHelper timerHelper;

    protected virtual void Awake()
    {
        image = GetComponent<Image>();

        // Capture the default sprite from the Image if not manually assigned
        if (defaultSprite == null && image != null)
            defaultSprite = image.sprite;

        OvenAssigner.EnsureAssigned();
        KitchenWindow.Apply();   // chapter 2: the beach through the window (16 October)
        KitchenBlender.Apply();  // the blender as its own object, every chapter (17 October)   // which food this oven bakes today, from the menu (once per kitchen load)
        KitchenWaffleMachine.Apply();   // chapter 2: the waffle machine that refills the cone holder (22 October)

        // The bake timer must outlive the kitchen scene, so it always runs on the persistent helper.
        timerHelper = KitchenTimerHelper.GetOrCreate($"[OvenHelper] {GetCooldownKey()}");
        CheckBakingStateOnWake();
    }

    /// <summary>
    /// On scene reload: if still baking, restore the baking sprite and re-attach to the running
    /// timer (or resume the remaining time if no timer is running).
    /// </summary>
    private void CheckBakingStateOnWake()
    {
        var pickTime = CooldownRegistry.GetPickTime(GetCooldownKey());
        if (pickTime < 0f) return; // Not baking — show idle

        var remaining = BakeTime - (Time.realtimeSinceStartup - pickTime);

        if (remaining > 0f)
        {
            DebugLog($"Still baking ({remaining:F1}s remaining) — restoring baking sprite");
            SetSprite(bakingSprite);
            timerHelper.StartTimer(GetCooldownKey(), remaining, OnBakeComplete);
        }
        else
        {
            // Finished baking while we were away — refill immediately
            DebugLog("Bake finished while scene was unloaded — refilling now");
            CooldownRegistry.ClearPickTime(GetCooldownKey());
            OnRefill();
            SetSprite(defaultSprite);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.timeScale == 0f) return; // paused: UI taps still arrive, gameplay must not
        // Accept taps on the oven or any of its decorations (the bread-note sticker covers the
        // middle of each oven window; rejecting it made centre taps do nothing).
        var hit = eventData.pointerCurrentRaycast.gameObject;
        if (hit == null || !hit.transform.IsChildOf(transform)) return;
        if (string.IsNullOrEmpty(food)) return;                                   // an idle oven: nothing on the menu for it
        if (CooldownRegistry.IsOnCooldown(GetCooldownKey(), BakeTime)) return;

        DebugLog($"{gameObject.name} clicked — starting bake!");

        if (interactSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(interactSound);

        SetSprite(bakingSprite);
        CooldownRegistry.RecordPickTime(GetCooldownKey());
        timerHelper.StartTimer(GetCooldownKey(), BakeTime, OnBakeComplete);
    }

    /// True while this oven's bake is running.
    public bool IsBaking => CooldownRegistry.IsOnCooldown(GetCooldownKey(), BakeTime);

    private void OnBakeComplete()
    {
        DebugLog("Bake complete!");
        CooldownRegistry.ClearPickTime(GetCooldownKey());
        OnRefill();
        SetSprite(defaultSprite);
        if (bakeCompleteSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(bakeCompleteSound);
    }

    private void SetSprite(Sprite sprite)
    {
        if (image != null && sprite != null)
            image.sprite = sprite;
    }

    /// <summary>
    /// Unique key for the cooldown registry: the food being baked (a running bake belongs to its recipe, whichever
    /// oven holds it today). An oven with no food falls back to its class name.
    /// </summary>
    protected virtual string GetCooldownKey()
    {
        return string.IsNullOrEmpty(food) ? GetType().Name : "Oven:" + food;
    }

    /// <summary>
    /// Refills the table item of the food this oven bakes today.
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
        if (enableDebugLogs) Debug.Log($"[{GetType().Name}] {message}");
    }
}