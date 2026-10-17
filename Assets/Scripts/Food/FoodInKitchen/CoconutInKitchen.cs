using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// The coconut cluster on the kitchen palm (owner, 17 October 2026): four coconuts; every tap takes one and puts it on
/// the table's coconut tray (when the tray has room; otherwise the tap is ignored and the coconut stays); when the
/// last one is taken the cluster is gone and grows back with all four after respawnDelay seconds, like the apples
/// (the timer and the pick time live on the persistent helpers, so leaving the kitchen does not reset them). Lives on
/// the window's Coconuts image (KitchenWindow builds it in chapter 2 when Coconut is on the menu). Pictures:
/// Resources/Chapter2/coconuts_4 .. coconuts_1 (Store/chapter2/palm/four/coconuts_four.py).
public class CoconutInKitchen : MonoBehaviour, IPointerClickHandler
{
    public const int Full = 4;
    public static int Count = Full;                      // coconuts left on the palm, kept while the game runs
    public Sprite[] states = new Sprite[Full];           // [count - 1]
    public float respawnDelay = 8f;
    public AudioClip pickSound;
    [Header("Pop")] public float popScale = 1.25f, popDuration = 0.12f, returnDuration = 0.08f;

    private Image image; private Vector3 originalScale; private KitchenTimerHelper timer;
    private const string Key = "CoconutInKitchen";

    private void Awake()
    {
        image = GetComponent<Image>(); originalScale = transform.localScale;
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

    private void Show()
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

    /// One coconut off the palm onto the tray; false when there is nothing to take or no room on the tray
    public bool Take()
    {
        if (Count <= 0) return false;
        var tray = FindObjectOfType<Coconut>(); var refill = tray != null ? tray.GetComponent<RefillableItem>() : null;
        if (refill != null && !refill.HasSpace(1)) { StartCoroutine(Pop(0.5f)); return false; }      // the tray is full: the coconut stays
        Count--;
        if (refill != null) refill.IncreaseCount(1);
        if (pickSound != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(pickSound);
        StartCoroutine(Pop(1f));
        if (Count <= 0)
        {
            CooldownRegistry.RecordPickTime(Key);
            timer.StartTimer(Key, respawnDelay, Regrow);
            StartCoroutine(HideAfter(popDuration + returnDuration));
        }
        else Show();
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
