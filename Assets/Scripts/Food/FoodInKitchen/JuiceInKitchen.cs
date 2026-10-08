using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class JuiceInKitchen : MonoBehaviour, IPointerClickHandler
{
    [Header("Audio")] public AudioClip refillSound;

    [Header("Pop Effect")] public float popScale = 1.3f; // How big it pops (1.3 = 30% bigger)
    public float popDuration = 0.15f; // Time to reach peak scale
    public float returnDuration = 0.1f; // Time to return to normal

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

        DebugLog("Juice clicked - refilling juice...");

        // Play sound
        if (refillSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(refillSound);
            DebugLog("Playing refill sound");
        }

        // Find and refill Juice in game scene
        var juice = FindObjectOfType<Juice>();
        if (juice != null)
        {
            juice.RefillToFull();
            DebugLog("Juice refilled to full!");
        }
        else
        {
            DebugLog("ERROR: Juice not found!");
        }

        // Trigger pop effect
        if (popCoroutine != null)
            StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(PopEffect());
    }

    private IEnumerator PopEffect()
    {
        var targetScale = originalScale * popScale;

        // Scale up to pop size
        var elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.SmoothStep(0f, 1f, elapsed / popDuration);
            transform.localScale = Vector3.LerpUnclamped(originalScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;

        // Scale back down to original
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
        if (enableDebugLogs) Debug.Log($"[JuiceInKitchen] {message}");
    }
}