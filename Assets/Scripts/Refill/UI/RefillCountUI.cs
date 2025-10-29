using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RefillCountUI : MonoBehaviour
{
    [Header("UI Components")] [Tooltip("Text component to display the count")]
    public TextMeshProUGUI countText;

    [Tooltip("Background image (optional)")]
    public Image backgroundImage;

    [Header("Visual Settings")] [Tooltip("Color when item is in stock")]
    public Color inStockColor = Color.white;

    [Tooltip("Color when item is out of stock")]
    public Color outOfStockColor = Color.red;

    [Tooltip("Background color when in stock")]
    public Color backgroundInStockColor = new(0f, 0f, 0f, 0.7f);

    [Tooltip("Background color when out of stock")]
    public Color backgroundOutOfStockColor = new(0.8f, 0f, 0f, 0.7f);

    [Header("Animation")] [Tooltip("Scale animation when count changes")]
    public float scaleAnimationDuration = 0.2f;

    [Tooltip("Scale multiplier for animation")]
    public float scaleMultiplier = 1.3f;

    // Private variables
    private RefillableItem parentItem;
    private Camera mainCamera;
    private RectTransform rectTransform;
    private float verticalOffset;
    private Vector3 originalScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = transform.localScale;
        mainCamera = Camera.main;

        // Auto-find components if not assigned
        if (countText == null) countText = GetComponentInChildren<TextMeshProUGUI>();

        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
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

    public void UpdateCount(int currentCount, int maxCount)
    {
        if (countText != null)
        {
            countText.text = currentCount.ToString();

            // Update colors based on stock status
            var isOutOfStock = currentCount <= 0;
            countText.color = isOutOfStock ? outOfStockColor : inStockColor;

            if (backgroundImage != null)
                backgroundImage.color = isOutOfStock ? backgroundOutOfStockColor : backgroundInStockColor;

            // Play scale animation
            PlayScaleAnimation();
        }
    }

    private void PlayScaleAnimation()
    {
        // Simple scale animation using LeanTween or basic coroutine
        StopAllCoroutines();
        StartCoroutine(ScaleAnimationCoroutine());
    }

    private System.Collections.IEnumerator ScaleAnimationCoroutine()
    {
        // Scale up
        var elapsed = 0f;
        var startScale = originalScale;
        var targetScale = originalScale * scaleMultiplier;

        while (elapsed < scaleAnimationDuration / 2f)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / (scaleAnimationDuration / 2f);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        // Scale down
        elapsed = 0f;
        startScale = targetScale;
        targetScale = originalScale;

        while (elapsed < scaleAnimationDuration / 2f)
        {
            elapsed += Time.deltaTime;
            var t = elapsed / (scaleAnimationDuration / 2f);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}