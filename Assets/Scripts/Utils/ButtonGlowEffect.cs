using System.Collections;
using UnityEngine;

public class ButtonGlowEffect : MonoBehaviour
{
    [Header("References")] public RectTransform shineImage;

    [Header("Sweep Settings")] public float sweepDuration = 0.6f;
    public float sweepInterval = 2.5f;
    public float startX = -300f;
    public float endX = 300f;

    private Coroutine sweepLoopCoroutine;

    private void Start()
    {
        if (shineImage == null)
        {
            Debug.LogWarning("[ButtonGlowEffect] shineImage not assigned!");
            return;
        }

        SetShineX(startX);
        shineImage.gameObject.SetActive(false); // hidden by default
    }

    public void EnableGlow(int sweepCount = 0) // 0 = infinite
    {
        if (shineImage == null) return;
        if (sweepLoopCoroutine != null) return;

        shineImage.gameObject.SetActive(true);
        sweepLoopCoroutine = StartCoroutine(SweepLoop(sweepCount));
    }

    public void DisableGlow()
    {
        if (sweepLoopCoroutine != null)
        {
            StopCoroutine(sweepLoopCoroutine);
            sweepLoopCoroutine = null;
        }

        if (shineImage != null)
        {
            SetShineX(startX);
            shineImage.gameObject.SetActive(false);
        }
    }

    private IEnumerator SweepLoop(int count)
    {
        var completed = 0;
        while (count == 0 || completed < count)
        {
            yield return new WaitForSeconds(sweepInterval);
            yield return StartCoroutine(Sweep());
            completed++;
        }

        DisableGlow();
    }

    private IEnumerator Sweep()
    {
        var elapsed = 0f;

        while (elapsed < sweepDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, elapsed / sweepDuration);
            SetShineX(Mathf.Lerp(startX, endX, t));
            yield return null;
        }

        SetShineX(startX);
    }

    private void SetShineX(float x)
    {
        var pos = shineImage.anchoredPosition;
        pos.x = x;
        shineImage.anchoredPosition = pos;
    }
}