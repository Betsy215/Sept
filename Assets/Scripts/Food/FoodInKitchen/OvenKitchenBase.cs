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

    [Header("Bake Time")] [Tooltip("How long the oven bakes before refilling the food item")]
    public float bakeTime = 10f;

    [Header("Debug")] public bool enableDebugLogs = true;

    private Image image;
    private MonoBehaviour coroutineRunner;

    protected virtual void Awake()
    {
        image = GetComponent<Image>();

        // Capture the default sprite from the Image if not manually assigned
        if (defaultSprite == null && image != null)
            defaultSprite = image.sprite;

        SetupCoroutineRunner();
        CheckBakingStateOnWake();
    }

    private void SetupCoroutineRunner()
    {
        if (transform.parent != null && transform.parent.gameObject.activeInHierarchy)
            coroutineRunner = transform.parent.GetComponent<MonoBehaviour>();

        if (coroutineRunner == null || coroutineRunner == this)
        {
            var helperName = $"[OvenHelper] {GetCooldownKey()}";
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
                DebugLog($"Created OvenHelper '{helperName}'");
            }
        }
    }

    /// <summary>
    /// On scene reload: if still baking, restore baking sprite and resume remaining timer.
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
            coroutineRunner.StartCoroutine(BakeCoroutine(remaining));
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
        if (eventData.pointerCurrentRaycast.gameObject != gameObject) return;
        if (CooldownRegistry.IsOnCooldown(GetCooldownKey(), bakeTime)) return;

        DebugLog($"{gameObject.name} clicked — starting bake!");

        if (interactSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(interactSound);

        SetSprite(bakingSprite);
        CooldownRegistry.RecordPickTime(GetCooldownKey());
        coroutineRunner.StartCoroutine(BakeCoroutine(bakeTime));
    }

    private IEnumerator BakeCoroutine(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);

        DebugLog("Bake complete!");
        OnRefill();
        SetSprite(defaultSprite);
        CooldownRegistry.ClearPickTime(GetCooldownKey());
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