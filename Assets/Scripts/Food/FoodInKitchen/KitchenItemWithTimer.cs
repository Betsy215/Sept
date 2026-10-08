using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Base class for kitchen UI items that:
///   1. Play a sound when clicked
///   2. Hide themselves (item is "used")
///   3. Trigger a refill in the game scene
///   4. Reappear after a configurable delay
///   5. Correctly persist their cooldown across kitchen scene unload/reload
///
/// To add a new kitchen item, subclass this and implement OnRefill().
/// Override GetCooldownKey() if multiple instances of the same subclass exist.
/// For visual effects override OnClick().
/// </summary>
public abstract class KitchenItemWithTimer : MonoBehaviour, IPointerClickHandler
{
    [Header("Audio")] public AudioClip interactSound;

    [Header("Respawn")] [Tooltip("Seconds before this kitchen item reappears after being used")]
    public float respawnDelay = 5f;

    [Header("Debug")] public bool enableDebugLogs = true;

    private KitchenTimerHelper timerHelper;

    protected virtual void Awake()
    {
        // Coroutines stop when their GameObject is disabled via SetActive(false) or the kitchen
        // scene is unloaded, so the respawn timer lives on a persistent helper instead.
        timerHelper = KitchenTimerHelper.GetOrCreate($"[TimerHelper] {GetCooldownKey()}");
        CheckCooldownOnWake();
    }

    /// <summary>
    /// On scene reload, check if the cooldown is still active and hide immediately
    /// if so, re-attaching to the timer that is already running (or starting the remainder).
    /// </summary>
    private void CheckCooldownOnWake()
    {
        var pickTime = CooldownRegistry.GetPickTime(GetCooldownKey());
        if (pickTime < 0f) return; // Never picked — show normally

        var elapsed = Time.realtimeSinceStartup - pickTime;
        var remaining = respawnDelay - elapsed;

        if (remaining > 0f)
        {
            // Still on cooldown — hide and wait for the remaining time
            DebugLog($"Cooldown still active ({remaining:F1}s remaining) — hiding on wake");
            gameObject.SetActive(false);
            timerHelper.StartTimer(GetCooldownKey(), remaining, OnRespawn);
        }
        else
        {
            // Cooldown already expired while we were away — clear and show
            DebugLog("Cooldown expired while scene was unloaded — showing immediately");
            CooldownRegistry.ClearPickTime(GetCooldownKey());
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.pointerCurrentRaycast.gameObject != gameObject) return;
        if (CooldownRegistry.IsOnCooldown(GetCooldownKey(), respawnDelay)) return;

        DebugLog($"{gameObject.name} clicked!");

        if (interactSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(interactSound);

        OnClick();
        OnRefill();

        // Record the pick time BEFORE hiding
        CooldownRegistry.RecordPickTime(GetCooldownKey());

        timerHelper.StartTimer(GetCooldownKey(), respawnDelay, OnRespawn);

        // Let any click effect (e.g. a pop) finish before deactivating, otherwise
        // SetActive(false) kills the effect coroutine on the same frame it starts.
        var hideDelay = GetHideDelay();
        if (hideDelay > 0f)
            StartCoroutine(HideAfter(hideDelay));
        else
            Hide();
    }

    private IEnumerator HideAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        // Only hide if the respawn has not already fired in the meantime.
        if (CooldownRegistry.IsOnCooldown(GetCooldownKey(), respawnDelay))
            Hide();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
        DebugLog($"Hidden. Reappearing in {respawnDelay}s...");
    }

    private void OnRespawn()
    {
        CooldownRegistry.ClearPickTime(GetCooldownKey());

        if (this != null && gameObject != null) // Guard: kitchen may have been unloaded
        {
            gameObject.SetActive(true);
            DebugLog("Reappeared — ready again!");
        }
    }

    /// <summary>
    /// Seconds to keep the item visible after a click (for click effects). Default: hide immediately.
    /// </summary>
    protected virtual float GetHideDelay()
    {
        return 0f;
    }

    /// <summary>
    /// Unique key used to persist cooldown state across scene reloads.
    /// Defaults to the class name. Override if you have multiple instances.
    /// </summary>
    protected virtual string GetCooldownKey()
    {
        return GetType().Name;
    }

    /// <summary>
    /// Called on click before hiding. Override to add visual effects.
    /// </summary>
    protected virtual void OnClick()
    {
    }

    /// <summary>
    /// Override to call the correct RefillToFull() for this item.
    /// </summary>
    protected abstract void OnRefill();

    protected void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"[{GetType().Name}] {message}");
    }
}

/// <summary>
/// Static registry that tracks pick times across scene reloads.
/// Uses Time.realtimeSinceStartup so it's immune to scene lifecycle.
/// </summary>
public static class CooldownRegistry
{
    private static readonly System.Collections.Generic.Dictionary<string, float> pickTimes = new();

    public static void RecordPickTime(string key)
    {
        pickTimes[key] = Time.realtimeSinceStartup;
    }

    public static float GetPickTime(string key)
    {
        return pickTimes.TryGetValue(key, out var t) ? t : -1f;
    }

    public static bool IsOnCooldown(string key, float delay)
    {
        if (!pickTimes.TryGetValue(key, out var t)) return false;
        return Time.realtimeSinceStartup - t < delay;
    }

    public static void ClearPickTime(string key)
    {
        pickTimes.Remove(key);
    }
}

/// <summary>
/// Persistent (DontDestroyOnLoad) MonoBehaviour that runs one realtime timer per cooldown key
/// while the kitchen item is hidden or the kitchen scene is unloaded.
/// A timer that is already running is never started twice; a new kitchen instance simply
/// re-attaches its completion callback to the running timer.
/// </summary>
public class KitchenTimerHelper : MonoBehaviour
{
    private readonly System.Collections.Generic.Dictionary<string, Coroutine> running = new();
    private readonly System.Collections.Generic.Dictionary<string, System.Action> callbacks = new();

    /// <summary>
    /// Find the persistent helper GameObject with this name, or create it.
    /// </summary>
    public static KitchenTimerHelper GetOrCreate(string helperName)
    {
        var existing = GameObject.Find(helperName);
        if (existing != null)
        {
            var helper = existing.GetComponent<KitchenTimerHelper>();
            if (helper != null) return helper;
        }

        var go = new GameObject(helperName);
        DontDestroyOnLoad(go);
        return go.AddComponent<KitchenTimerHelper>();
    }

    public bool IsRunning(string key)
    {
        return running.ContainsKey(key);
    }

    /// <summary>
    /// Start a realtime timer for <paramref name="key"/> unless one is already running.
    /// The completion callback is (re)attached either way, so a reloaded kitchen instance
    /// receives the completion of a timer started by a previous instance.
    /// </summary>
    public void StartTimer(string key, float delay, System.Action onComplete)
    {
        callbacks[key] = onComplete;

        if (running.ContainsKey(key)) return;

        running[key] = StartCoroutine(Run(key, delay));
    }

    private IEnumerator Run(string key, float delay)
    {
        yield return new WaitForSecondsRealtime(delay); // Realtime so it works across scene loads

        running.Remove(key);

        if (callbacks.TryGetValue(key, out var onComplete))
        {
            callbacks.Remove(key);
            onComplete?.Invoke();
        }
    }
}