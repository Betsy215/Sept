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

    [Header("Debug")] [Tooltip("Enable debug logging for positioning")]
    public bool enableDebugLogs = true; // Enable by default for troubleshooting

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

        DebugLog($"Awake: rectTransform={rectTransform != null}, mainCamera={mainCamera != null}");

        // Check Canvas setup
        var parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            DebugLog($"Canvas found: {parentCanvas.name}, renderMode: {parentCanvas.renderMode}");

            if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                DebugLog($"Canvas worldCamera: {parentCanvas.worldCamera != null}");
                if (parentCanvas.worldCamera != null)
                    DebugLog($"Canvas camera name: {parentCanvas.worldCamera.name}");
                else
                    DebugLog("WARNING: Canvas is set to Camera mode but no camera assigned!");
            }

            var scaler = parentCanvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                DebugLog($"Canvas scaler mode: {scaler.uiScaleMode}");
                DebugLog($"Reference resolution: {scaler.referenceResolution}");
            }
        }
        else
        {
            DebugLog("ERROR: No Canvas found in parent hierarchy!");
        }

        // Auto-find components if not assigned
        if (countText == null)
        {
            countText = GetComponentInChildren<TextMeshProUGUI>();
            DebugLog($"CountText auto-found: {countText != null}");
            if (countText != null)
                DebugLog(
                    $"CountText GameObject: {countText.gameObject.name}, active: {countText.gameObject.activeInHierarchy}");
        }

        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
            DebugLog($"BackgroundImage auto-found: {backgroundImage != null}");
            if (backgroundImage != null)
                DebugLog(
                    $"BackgroundImage GameObject: {backgroundImage.gameObject.name}, active: {backgroundImage.gameObject.activeInHierarchy}");
        }

        // Check initial RectTransform settings
        DebugLog($"Initial RectTransform position: {rectTransform.position}");
        DebugLog($"Initial RectTransform localPosition: {rectTransform.localPosition}");
        DebugLog($"Initial RectTransform size: {rectTransform.rect.size}");
    }

    public void Initialize(RefillableItem item, float offset)
    {
        parentItem = item;
        verticalOffset = offset;

        DebugLog($"Initialize: parentItem={parentItem?.name}, offset={offset}");
        DebugLog($"Camera found: {mainCamera != null}");
        DebugLog($"RectTransform: {rectTransform != null}");
        DebugLog($"CountText: {countText != null}");
        DebugLog($"BackgroundImage: {backgroundImage != null}");

        // Check GameObject hierarchy
        DebugLog($"GameObject active: {gameObject.activeInHierarchy}");
        DebugLog($"GameObject name: {gameObject.name}");
        DebugLog($"Children count: {transform.childCount}");

        // Check child objects
        for (var i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            DebugLog($"Child {i}: {child.name}, active: {child.gameObject.activeInHierarchy}");
        }

        // Initially hidden
        SetVisible(false);

        // Force an immediate position update for testing
        if (parentItem != null) UpdatePosition();
    }

    private void LateUpdate()
    {
        if (parentItem != null && gameObject.activeInHierarchy) UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (parentItem == null || mainCamera == null)
        {
            DebugLog("UpdatePosition failed: parentItem or camera missing");
            return;
        }

        // Check RectTransform size first
        if (rectTransform.rect.size.magnitude < 0.1f)
        {
            DebugLog($"WARNING: RectTransform size is too small: {rectTransform.rect.size}");
            // Force a minimum size
            rectTransform.sizeDelta = new Vector2(40, 40);
            DebugLog($"Set minimum size to: {rectTransform.sizeDelta}");
        }

        // Get world position of parent item
        var worldPos = parentItem.transform.position;
        worldPos.y += verticalOffset;

        // Convert to screen space
        var screenPos = mainCamera.WorldToScreenPoint(worldPos);

        // Check if position is valid
        if (screenPos.z < 0)
        {
            DebugLog("Object is behind camera, hiding UI");
            SetVisible(false);
            return;
        }

        // Log all positioning details
        DebugLog($"POSITIONING: Food item: {parentItem.name}");
        DebugLog($"  World pos: {parentItem.transform.position}");
        DebugLog($"  World pos + offset: {worldPos}");
        DebugLog($"  Screen pos: {screenPos}");
        DebugLog($"  RectTransform size: {rectTransform.rect.size}");

        // For different Canvas render modes, we might need different positioning
        var parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            DebugLog($"  Canvas render mode: {parentCanvas.renderMode}");

            // Use direct screen position for Overlay mode (most reliable)
            rectTransform.position = screenPos;
            DebugLog($"  Set direct screen position: {screenPos}");

            // Log final position
            DebugLog($"  Final RectTransform position: {rectTransform.position}");
            DebugLog($"  Final RectTransform localPosition: {rectTransform.localPosition}");
            DebugLog($"  Final RectTransform size: {rectTransform.rect.size}");
        }
        else
        {
            // Fallback
            rectTransform.position = screenPos;
            DebugLog($"  Fallback position: {screenPos}");
        }
    }

    // Test method to force UI to specific position
    [ContextMenu("Test UI Visibility")]
    public void TestUIVisibility()
    {
        DebugLog("=== TESTING UI VISIBILITY ===");

        // Force UI to center of screen for testing
        rectTransform.position = new Vector3(Screen.width / 2, Screen.height / 2, 0);

        // Make sure it's active and visible
        gameObject.SetActive(true);
        SetVisible(true);

        // Set a test count
        if (countText != null)
        {
            countText.text = "TEST";
            countText.color = Color.red; // Make it very visible
            DebugLog("Set test text to 'TEST' in red");
        }

        // Make background visible
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.yellow; // Make it very visible
            DebugLog("Set background to yellow");
        }

        DebugLog($"Test position set to screen center: {rectTransform.position}");
        DebugLog($"GameObject active: {gameObject.activeInHierarchy}");
        DebugLog("=== TEST COMPLETE ===");
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"RefillCountUI: {message}");
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
        if (gameObject != null)
        {
            gameObject.SetActive(visible);
            DebugLog($"SetVisible({visible}) - GameObject active: {gameObject.activeInHierarchy}");
        }
        else
        {
            DebugLog("ERROR: Cannot set visibility - gameObject is null!");
        }
    }
}