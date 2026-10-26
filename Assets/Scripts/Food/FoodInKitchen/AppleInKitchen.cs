using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The apples on the kitchen tree (owner, 25 October 2026: "apples like coconuts, several on the tree, one per tap").
/// Three apples; every tap takes one and puts a share of the basket's stock on the table (a third of the basket per
/// apple, so three taps fill it); when the last one is taken the tree is bare and grows all three back after
/// respawnDelay seconds (the timer and the pick time live on the persistent helpers, so leaving the kitchen does not
/// reset them). Pictures Resources/Kitchen/apples_3 .. apples_1 (Store/artkit/apples_states.py); when they are missing the
/// scene's own picture stays and a tap still takes one apple.
/// Until 25 October one tap refilled the whole basket and hid the apples (KitchenItemWithTimer); respawnDelay,
/// interactSound and the pop fields keep their names so the scene's values still apply.
/// </summary>
public class AppleInKitchen : MonoBehaviour, IPointerClickHandler
{
    public const int Full = 3;
    public static int Count = Full;                      // apples left on the tree, kept while the game runs
    [Header("Audio")] public AudioClip interactSound;
    [Header("Respawn")] public float respawnDelay = 8f;
    [Header("Pop Effect")] public float popScale = 1.3f;
    public float popDuration = 0.12f;
    public float returnDuration = 0.08f;
    [Header("Debug")] public bool enableDebugLogs = true;

    private Image image; private Vector3 originalScale; private KitchenTimerHelper timer; private Sprite[] states;
    private const string Key = "AppleInKitchen";

    private void Awake()
    {
        image = GetComponent<Image>(); originalScale = transform.localScale;
        states = new Sprite[Full];
        for (var i = 1; i <= Full; i++) states[i - 1] = Resources.Load<Sprite>("Kitchen/apples_" + i);
        timer = KitchenTimerHelper.GetOrCreate("[TimerHelper] " + Key);
        var pick = CooldownRegistry.GetPickTime(Key);
        if (pick < 0f) { if (Count <= 0) Count = Full; }                               // no regrow pending (a new game cleared it): full
        else
        {
            var remaining = respawnDelay - (Time.realtimeSinceStartup - pick);
            if (remaining > 0f) { Count = 0; timer.StartTimer(Key, remaining, Regrow); }
            else { CooldownRegistry.ClearPickTime(Key); Count = Full; }
        }
        Show();
    }

    public void Show()
    {
        if (image == null) return;
        image.enabled = Count > 0;
        if (Count > 0 && states[Mathf.Clamp(Count, 1, Full) - 1] != null) image.sprite = states[Count - 1];
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.timeScale == 0f) return;
        if (eventData.pointerCurrentRaycast.gameObject != gameObject) return;
        Take();
    }

    /// One apple off the tree into the basket; false when there is nothing to take or the basket is full
    public bool Take()
    {
        if (Count <= 0) return false;
        var basket = FindObjectOfType<Apple>(); var refill = basket != null ? basket.GetComponent<RefillableItem>() : null;
        if (refill != null && !refill.HasSpace(1)) { StartCoroutine(Pop(0.5f)); return false; }     // the basket is full: the apple stays
        Count--;
        if (refill != null)
        {
            var share = Mathf.Max(1, Mathf.CeilToInt(refill.GetMaxCount() / (float)Full));
            var room = refill.GetMaxCount() - refill.GetCurrentCount();
            refill.IncreaseCount(Mathf.Min(share, room));
        }
        if (interactSound != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(interactSound);
        StartCoroutine(Pop(1f));
        if (Count <= 0)
        {
            CooldownRegistry.RecordPickTime(Key);
            timer.StartTimer(Key, respawnDelay, Regrow);
            StartCoroutine(HideAfter(popDuration + returnDuration));
        }
        else Show();
        if (enableDebugLogs) Debug.Log($"[AppleInKitchen] apple taken, {Count} left on the tree");
        return true;
    }

    private IEnumerator HideAfter(float t) { yield return new WaitForSecondsRealtime(t); if (Count <= 0) Show(); }

    private void Regrow()
    {
        CooldownRegistry.ClearPickTime(Key); Count = Full;
        if (this != null && gameObject != null) Show();
    }

    private IEnumerator Pop(float amount)
    {
        var target = originalScale * (1f + (popScale - 1f) * amount); var e = 0f;
        while (e < popDuration) { e += Time.unscaledDeltaTime; transform.localScale = Vector3.LerpUnclamped(originalScale, target, Mathf.SmoothStep(0f, 1f, e / popDuration)); yield return null; }
        e = 0f;
        while (e < returnDuration) { e += Time.unscaledDeltaTime; transform.localScale = Vector3.LerpUnclamped(target, originalScale, Mathf.SmoothStep(0f, 1f, e / returnDuration)); yield return null; }
        transform.localScale = originalScale;
    }

    private void OnDisable() { transform.localScale = originalScale; }
}
