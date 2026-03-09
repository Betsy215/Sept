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

    private MonoBehaviour coroutineRunner;

    protected virtual void Awake()
    {
        SetupCoroutineRunner();
        CheckCooldownOnWake();
    }

    private void SetupCoroutineRunner()
    {
        // Coroutines stop when their GameObject is disabled via SetActive(false).
        // Delegate the respawn timer to a parent or persistent helper that stays alive.
        if (transform.parent != null && transform.parent.gameObject.activeInHierarchy)
            coroutineRunner = transform.parent.GetComponent<MonoBehaviour>();

        if (coroutineRunner == null || coroutineRunner == this)
        {
            // Reuse existing helper for this key if one exists
            var helperName = $"[TimerHelper] {GetCooldownKey()}";
            var existing = GameObject.Find(helperName);
            if (existing != null)
            {
                coroutineRunner = existing.GetComponent<KitchenTimerHelper>();
            }
            else
            {
                var helper = new GameObject(helperName);
                DontDestroyOnLoad(helper);
                coroutineRunner = helper.AddComponent<KitchenTimerHelper>();
                DebugLog($"Created KitchenTimerHelper '{helperName}'");
            }
        }
    }

    /// <summary>
    /// On scene reload, check if the cooldown is still active and hide immediately
    /// if so, or start the remaining timer.
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
            coroutineRunner.StartCoroutine(RespawnAfter(remaining));
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

        coroutineRunner.StartCoroutine(RespawnAfter(respawnDelay));
        gameObject.SetActive(false);
        DebugLog($"Hidden. Reappearing in {respawnDelay}s...");
    }

    private IEnumerator RespawnAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(delay); // Realtime so it works across scene loads

        if (this != null && gameObject != null) // Guard: object might be destroyed
        {
            gameObject.SetActive(true);
            CooldownRegistry.ClearPickTime(GetCooldownKey());
            DebugLog("Reappeared — ready again!");
        }
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
/// Minimal persistent MonoBehaviour that runs the respawn coroutine
/// while the kitchen item is hidden (SetActive false).
/// </summary>
public class KitchenTimerHelper : MonoBehaviour
{
}