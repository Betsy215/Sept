using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to the apple Image in KitchenScene.
/// On click: pops, plays sound, hides itself, refills Apple in game scene,
/// then reappears after respawnDelay seconds — even if you leave and return to the kitchen.
/// </summary>
public class AppleInKitchen : KitchenItemWithTimer
{
    [Header("Pop Effect")] public float popScale = 1.3f;
    public float popDuration = 0.12f;
    public float returnDuration = 0.08f;

    private Vector3 originalScale;

    protected override void Awake()
    {
        base.Awake();
        originalScale = transform.localScale;
    }

    protected override void OnClick()
    {
        StartCoroutine(PopEffect());
    }

    protected override void OnRefill()
    {
        var apple = FindObjectOfType<Apple>();
        if (apple != null)
        {
            apple.RefillToFull();
            DebugLog("Apple refilled to full!");
        }
        else
        {
            DebugLog("ERROR: Apple not found in game scene!");
        }
    }

    private IEnumerator PopEffect()
    {
        var targetScale = originalScale * popScale;

        var elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, elapsed / popDuration);
            transform.localScale = Vector3.LerpUnclamped(originalScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;

        elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, elapsed / returnDuration);
            transform.localScale = Vector3.LerpUnclamped(targetScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
    }
}