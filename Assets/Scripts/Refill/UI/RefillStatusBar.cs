using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RefillStatusBar : MonoBehaviour
{
    [Header("UI Components")] [Tooltip("Fill image that shows progress")]
    public Image fillImage;

    [Tooltip("Background image")] public Image backgroundImage;

    [Header("Visual Settings")] [Tooltip("Color of the fill bar")]
    public Color fillColor = Color.green;

    [Tooltip("Background color")] public Color backgroundColor = new(0f, 0f, 0f, 0.5f);

    [Tooltip("Width of the status bar")] public float barWidth = 1f;

    [Tooltip("Height of the status bar")] public float barHeight = 0.1f;

    [Header("Animation")] [Tooltip("Smooth fill animation")]
    public bool smoothFill = true;

    // Private variables
    private RefillableItem parentItem;
    private Camera mainCamera;
    private RectTransform rectTransform;
    private float verticalOffset;
    private Coroutine fillCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;

        // Auto-find components if not assigned
        if (fillImage == null)
        {
            var fillTransform = transform.Find("Fill");
            if (fillTransform != null) fillImage = fillTransform.GetComponent<Image>();
        }

        if (backgroundImage == null) backgroundImage = GetComponent<Image>();

        // Set up initial appearance
        SetupVisuals();
    }

    private void SetupVisuals()
    {
        // Set colors
        if (fillImage != null)
        {
            fillImage.color = fillColor;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.type = Image.Type.Filled;
            fillImage.fillAmount = 0f;
        }

        if (backgroundImage != null) backgroundImage.color = backgroundColor;

        // Set size
        rectTransform.sizeDelta = new Vector2(barWidth * 100f, barHeight * 100f); // Convert to pixels
    }

    public void Initialize(RefillableItem item, float offset)
    {
        parentItem = item;
        verticalOffset = offset;

        // Initially hidden
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (parentItem != null && gameObject.activeInHierarchy) UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (parentItem == null || mainCamera == null) return;

        // Get world position of parent item
        var worldPos = parentItem.transform.position;
        worldPos.y += verticalOffset;

        // Convert to screen space
        var screenPos = mainCamera.WorldToScreenPoint(worldPos);

        // Set UI position
        rectTransform.position = screenPos;
    }

    public void StartRefillAnimation(float duration)
    {
        if (fillCoroutine != null) StopCoroutine(fillCoroutine);

        fillCoroutine = StartCoroutine(FillAnimationCoroutine(duration));
    }

    private IEnumerator FillAnimationCoroutine(float duration)
    {
        if (fillImage == null) yield break;

        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var progress = elapsed / duration;

            if (smoothFill)
                fillImage.fillAmount = Mathf.Lerp(0f, 1f, progress);
            else
                fillImage.fillAmount = progress;

            yield return null;
        }

        // Ensure it reaches 1.0
        fillImage.fillAmount = 1f;

        // Brief pause at full, then reset for next cycle
        yield return new WaitForSeconds(0.1f);
        fillImage.fillAmount = 0f;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);

        if (!visible)
        {
            // Stop any ongoing animation
            if (fillCoroutine != null)
            {
                StopCoroutine(fillCoroutine);
                fillCoroutine = null;
            }

            // Reset fill
            if (fillImage != null) fillImage.fillAmount = 0f;
        }
    }

    public void SetFillAmount(float amount)
    {
        if (fillImage != null) fillImage.fillAmount = Mathf.Clamp01(amount);
    }
}