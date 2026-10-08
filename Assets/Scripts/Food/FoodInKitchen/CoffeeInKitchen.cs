using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class CoffeeInKitchen : MonoBehaviour, IPointerClickHandler
{
    [Header("Audio")] public AudioClip refillSound;

    [Header("Pop Effect")] public float popScale = 1.3f;
    public float popDuration = 0.15f;
    public float returnDuration = 0.1f;

    [Header("Debug")] public bool enableDebugLogs = true;

    private Vector3 originalScale;
    private Coroutine popCoroutine;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.timeScale == 0f) return; // paused: UI taps still arrive, gameplay must not

        DebugLog("Coffee clicked - refilling beans...");

        if (refillSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(refillSound);
            DebugLog("Playing refill sound");
        }

        var beanContainer = FindObjectOfType<BeanContainer>();
        if (beanContainer != null)
            beanContainer.RefillToFull();
        else
            DebugLog("ERROR: BeanContainer not found!");

        if (popCoroutine != null)
            StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(PopEffect());
    }

    private IEnumerator PopEffect()
    {
        var targetScale = originalScale * popScale;

        var elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.SmoothStep(0f, 1f, elapsed / popDuration);
            transform.localScale = Vector3.LerpUnclamped(originalScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;

        elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.SmoothStep(0f, 1f, elapsed / returnDuration);
            transform.localScale = Vector3.LerpUnclamped(targetScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"[CoffeeInKitchen] {message}");
    }
}