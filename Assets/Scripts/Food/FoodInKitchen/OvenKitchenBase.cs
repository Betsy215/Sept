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
/// To add a new oven, subclass this and implement OnRefill().
/// Set bakeTime in the Inspector (default 10s).
/// Override GetCooldownKey() if needed (defaults to class name).
/// </summary>
public abstract class OvenKitchenBase : MonoBehaviour, IPointerClickHandler
{
    [Header("Sprites")] [Tooltip("The oven's idle sprite (assigned automatically from Image on Awake)")]
    public Sprite defaultSprite;

    [Tooltip("The oven's baking sprite")] public Sprite bakingSprite;

    [Header("Audio")] public AudioClip interactSound;
    public AudioClip bakeCompleteSound;

    [Header("Bake Time")] [Tooltip("How long the oven bakes before refilling the food item")]
    public float bakeTime = 10f;

    [Header("Debug")] public bool enableDebugLogs = true;

    private Image image;
    private KitchenTimerHelper timerHelper;

    protected virtual void Awake()
    {
        image = GetComponent<Image>();

        // Capture the default sprite from the Image if not manually assigned
        if (defaultSprite == null && image != null)
            defaultSprite = image.sprite;

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

        var remaining = bakeTime - (Time.realtimeSinceStartup - pickTime);

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
        if (eventData.pointerCurrentRaycast.gameObject != gameObject) return;
        if (CooldownRegistry.IsOnCooldown(GetCooldownKey(), bakeTime)) return;

        DebugLog($"{gameObject.name} clicked — starting bake!");

        if (interactSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(interactSound);

        SetSprite(bakingSprite);
        CooldownRegistry.RecordPickTime(GetCooldownKey());
        timerHelper.StartTimer(GetCooldownKey(), bakeTime, OnBakeComplete);
    }

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
    /// Unique key for the cooldown registry. Defaults to class name.
    /// </summary>
    protected virtual string GetCooldownKey()
    {
        return GetType().Name;
    }

    /// <summary>
    /// Override to call the correct RefillToFull() for this oven's food item.
    /// </summary>
    protected abstract void OnRefill();

    protected void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"[{GetType().Name}] {message}");
    }
}