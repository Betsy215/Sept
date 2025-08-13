using UnityEngine;
using TMPro;
using System.Collections;

public class SimpleScorePopup : MonoBehaviour
{
    [Header("Animation Settings")]
    public float initialAnimationDuration = 3f;
    public float moveUpDistance = 100f;
    public float startScale = 0.8f;
    public float endScale = 1.2f;
    
    [Header("Display Duration")]
    public float stayDuration = 2f; // How long popup stays visible after animation
    public bool autoDisappear = true; // Whether popup should disappear automatically
    public float fadeOutDuration = 0.5f; // How long fade out takes
    
    [Header("Colors")]
    public Color itemPointsColor = Color.green;
    public Color bonusPointsColor = Color.yellow;
    public Color combinedColor = Color.yellow; // Color when showing both base + bonus
    
    [Header("Popup Positioning")]
    public Vector2 defaultSpawnPosition = new Vector2(-280, 650); // Default spawn position
    public float randomRange = 150f; // Random horizontal spread
    
    [Header("Font Settings")]
    public TMP_FontAsset popupFont; // Custom font (leave null for default)
    public float fontSize = 100f;
    public FontStyles fontStyle = FontStyles.Bold;
    
    [Header("Popup Size")]
    public Vector2 popupSize = new Vector2(800, 300); // Size of the popup RectTransform
    
    private TextMeshProUGUI textComponent;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 startPosition;
    private bool isAnimationComplete = false;
    
    // Static reference to track current popup
    private static SimpleScorePopup currentPopup;
    
    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        // Add CanvasGroup if not present
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    /// <summary>
    /// Show popup with combined base and bonus points
    /// </summary>
    /// <param name="basePoints">Base points to display</param>
    /// <param name="bonusPoints">Bonus points to display (0 if no bonus)</param>
    public void ShowCombinedPopup(int basePoints, int bonusPoints = 0)
    {
        // Destroy previous popup if it exists
        if (currentPopup != null && currentPopup != this)
        {
            Destroy(currentPopup.gameObject);
        }
        
        // Set this as the current popup
        currentPopup = this;
        
        // Create combined text
        string popupText = "";
        Color textColor = itemPointsColor;
        
        if (bonusPoints > 0)
        {
            // Show both base and bonus points with line break, entire text in yellow
            popupText = $"+{basePoints}\n+{bonusPoints} BONUS!";
            textColor = Color.yellow; // Entire text is yellow when there's bonus
        }
        else
        {
            // Show only base points
            string prefix = basePoints > 0 ? "+" : "";
            popupText = $"{prefix}{basePoints}";
            textColor = itemPointsColor;
        }
        
        // Set text content and color
        textComponent.text = popupText;
        textComponent.color = textColor;
        
        // Store starting position
        startPosition = rectTransform.anchoredPosition;
        
        // Start initial animation
        StartCoroutine(InitialAnimation());
    }
    
    /// <summary>
    /// Show popup with points (backwards compatibility)
    /// </summary>
    /// <param name="points">Points to display</param>
    /// <param name="isBonus">Is this a bonus popup?</param>
    public void ShowPopup(int points, bool isBonus = false)
    {
        if (isBonus)
        {
            ShowCombinedPopup(0, points);
        }
        else
        {
            ShowCombinedPopup(points, 0);
        }
    }
    
    /// <summary>
    /// Initial popup animation - ends in visible state
    /// </summary>
    IEnumerator InitialAnimation()
    {
        float elapsedTime = 0f;
        Vector3 targetPosition = startPosition + Vector3.up * moveUpDistance;
        
        // Set initial state
        rectTransform.localScale = Vector3.one * startScale;
        canvasGroup.alpha = 0f;
        
        while (elapsedTime < initialAnimationDuration)
        {
            float t = elapsedTime / initialAnimationDuration;
            
            // Move upward with easing
            float easedT = EaseOutCubic(t);
            rectTransform.anchoredPosition = Vector3.Lerp(startPosition, targetPosition, easedT);
            
            // Scale animation (grow to final size)
            float scale = Mathf.Lerp(startScale, endScale, easedT);
            rectTransform.localScale = Vector3.one * scale;
            
            // Fade in
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, easedT);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final state
        rectTransform.anchoredPosition = targetPosition;
        rectTransform.localScale = Vector3.one * endScale;
        canvasGroup.alpha = 1f;
        isAnimationComplete = true;
        
        Debug.Log($"Score popup animation complete. Staying visible until next popup.");
        
        // Start auto-disappear timer if enabled
        if (autoDisappear && stayDuration > 0)
        {
            StartCoroutine(AutoDisappearTimer());
        }
    }
    
    /// <summary>
    /// Timer to automatically destroy popup after stay duration
    /// </summary>
    IEnumerator AutoDisappearTimer()
    {
        yield return new WaitForSeconds(stayDuration);
        
        // Only disappear if we're still the current popup
        if (currentPopup == this)
        {
            Debug.Log($"Popup auto-disappearing after {stayDuration} seconds");
            DestroyPopup();
        }
    }
    
    /// <summary>
    /// Manually destroy this popup (called when a new one is created)
    /// </summary>
    public void DestroyPopup()
    {
        // Quick fade out before destroying
        StartCoroutine(FadeOutAndDestroy());
    }
    
    /// <summary>
    /// Quick fade out animation before destruction
    /// </summary>
    IEnumerator FadeOutAndDestroy()
    {
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeOutDuration)
        {
            float t = elapsedTime / fadeOutDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Clear reference if this was the current popup
        if (currentPopup == this)
        {
            currentPopup = null;
        }
        
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Smooth easing function
    /// </summary>
    float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
    
    /// <summary>
    /// Static method to create combined popup using internal configuration
    /// </summary>
    /// <param name="parent">Canvas to spawn popup on</param>
    /// <param name="basePoints">Base points to show</param>
    /// <param name="bonusPoints">Bonus points to show (0 if none)</param>
    /// <returns>Created popup</returns>
    public static SimpleScorePopup CreateCombinedPopup(Transform parent, int basePoints, int bonusPoints = 0)
    {
        // Create popup GameObject
        GameObject popupObj = new GameObject("ScorePopup");
        popupObj.transform.SetParent(parent, false);
        
        // Add RectTransform first
        RectTransform rectTransform = popupObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(800, 300); // Default size
        
        // Calculate position with randomness using default settings
        Vector2 defaultPos = new Vector2(0, 100); // Default spawn position
        float randomRangeValue = 150f; // Default random range
        
        Vector2 randomOffset = new Vector2(
            Random.Range(-randomRangeValue / 2f, randomRangeValue / 2f),
            Random.Range(-50f, 50f)
        );
        rectTransform.anchoredPosition = defaultPos + randomOffset;
        
        // Add TextMeshPro component with default settings
        TextMeshProUGUI textComponent = popupObj.AddComponent<TextMeshProUGUI>();
        textComponent.fontSize = 36f; // Default font size
        textComponent.fontStyle = FontStyles.Bold; // Default font style
        textComponent.alignment = TextAlignmentOptions.Center;
        
        // Add popup script and configure
        SimpleScorePopup popup = popupObj.AddComponent<SimpleScorePopup>();
        
        // Now override with the component's actual settings if different
        rectTransform.sizeDelta = popup.popupSize;
        rectTransform.anchoredPosition = popup.defaultSpawnPosition + new Vector2(
            Random.Range(-popup.randomRange / 2f, popup.randomRange / 2f),
            Random.Range(-50f, 50f)
        );
        textComponent.fontSize = popup.fontSize;
        textComponent.fontStyle = popup.fontStyle;
        
        // Set custom font if specified
        if (popup.popupFont != null)
        {
            textComponent.font = popup.popupFont;
        }
        
        // Show the popup
        popup.ShowCombinedPopup(basePoints, bonusPoints);
        
        Debug.Log($"Popup created at position: {rectTransform.anchoredPosition}");
        
        return popup;
    }
    
    /// <summary>
    /// Static method to create popup quickly (backward compatibility) - uses internal config
    /// </summary>
    /// <param name="parent">Canvas to spawn popup on</param>
    /// <param name="points">Points to show</param>
    /// <param name="isBonus">Is bonus popup?</param>
    /// <param name="position">Override position (optional)</param>
    /// <returns>Created popup</returns>
    public static SimpleScorePopup CreatePopup(Transform parent, int points, bool isBonus = false, Vector2? position = null)
    {
        // Create using the new combined method
        SimpleScorePopup popup;
        if (isBonus)
        {
            popup = CreateCombinedPopup(parent, 0, points);
        }
        else
        {
            popup = CreateCombinedPopup(parent, points, 0);
        }
        
        // Override position if specified
        if (position.HasValue)
        {
            popup.rectTransform.anchoredPosition = position.Value;
        }
        
        return popup;
    }
    
    /// <summary>
    /// Static method to create combined popup with specified font
    /// </summary>
    /// <param name="parent">Canvas to spawn popup on</param>
    /// <param name="basePoints">Base points to show</param>
    /// <param name="bonusPoints">Bonus points to show (0 if none)</param>
    /// <param name="customFont">Font to use (null for default)</param>
    /// <returns>Created popup</returns>
    public static SimpleScorePopup CreateCombinedPopupWithFont(Transform parent, int basePoints, int bonusPoints = 0, TMP_FontAsset customFont = null)
    {
        // Create popup GameObject
        GameObject popupObj = new GameObject("ScorePopup");
        popupObj.transform.SetParent(parent, false);
        
        // Add RectTransform first
        RectTransform rectTransform = popupObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(800, 300); // Default size
        
        // Calculate position with randomness using default settings
        Vector2 defaultPos = new Vector2(0, 100); // Default spawn position
        float randomRangeValue = 150f; // Default random range
        
        Vector2 randomOffset = new Vector2(
            Random.Range(-randomRangeValue / 2f, randomRangeValue / 2f),
            Random.Range(-50f, 50f)
        );
        rectTransform.anchoredPosition = defaultPos + randomOffset;
        
        // Add TextMeshPro component with default settings
        TextMeshProUGUI textComponent = popupObj.AddComponent<TextMeshProUGUI>();
        textComponent.fontSize = 36f; // Default font size
        textComponent.fontStyle = FontStyles.Bold; // Default font style
        textComponent.alignment = TextAlignmentOptions.Center;
        
        // Add popup script and configure
        SimpleScorePopup popup = popupObj.AddComponent<SimpleScorePopup>();
        
        // Now override with the component's actual settings if different
        rectTransform.sizeDelta = popup.popupSize;
        rectTransform.anchoredPosition = popup.defaultSpawnPosition + new Vector2(
            Random.Range(-popup.randomRange / 2f, popup.randomRange / 2f),
            Random.Range(-50f, 50f)
        );
        textComponent.fontSize = popup.fontSize;
        textComponent.fontStyle = popup.fontStyle;
        
        // Set font priority: customFont > popup.popupFont > default
        if (customFont != null)
        {
            textComponent.font = customFont;
            Debug.Log($"Using custom font: {customFont.name}");
        }
        else if (popup.popupFont != null)
        {
            textComponent.font = popup.popupFont;
            Debug.Log($"Using popup component font: {popup.popupFont.name}");
        }
        else
        {
            Debug.Log("Using default font");
        }
        
        Debug.Log($"Popup timing: Animation={popup.initialAnimationDuration}s, Stay={popup.stayDuration}s, AutoDisappear={popup.autoDisappear}");
        
        // Show the popup
        popup.ShowCombinedPopup(basePoints, bonusPoints);
        
        Debug.Log($"Popup created at position: {rectTransform.anchoredPosition}");
        
        return popup;
    }
    
    /// <summary>
    /// LEGACY: Static method to create combined popup with custom font (for backwards compatibility)
    /// </summary>
    [System.Obsolete("Use CreateCombinedPopupWithFont(parent, basePoints, bonusPoints, font) instead")]
    public static SimpleScorePopup CreateCombinedPopupWithFont(Transform parent, int basePoints, int bonusPoints, Vector2? position, TMP_FontAsset font, float fontSize, FontStyles fontStyle)
    {
        // Use the new method and ignore extra parameters
        SimpleScorePopup popup = CreateCombinedPopupWithFont(parent, basePoints, bonusPoints, font);
        
        // Override position if specified
        if (position.HasValue)
        {
            popup.rectTransform.anchoredPosition = position.Value;
        }
        
        return popup;
    }
    
    /// <summary>
    /// LEGACY: Static method to create popup with custom font (for backwards compatibility)
    /// </summary>
    [System.Obsolete("Use CreatePopup() instead - configuration is now handled internally")]
    public static SimpleScorePopup CreatePopupWithFont(Transform parent, int points, bool isBonus = false, Vector2? position = null, TMP_FontAsset font = null, float fontSize = 36f, FontStyles fontStyle = FontStyles.Bold)
    {
        return CreatePopup(parent, points, isBonus, position);
    }
    
    /// <summary>
    /// Static method to manually clear current popup
    /// </summary>
    public static void ClearCurrentPopup()
    {
        if (currentPopup != null)
        {
            currentPopup.DestroyPopup();
        }
    }
    
    /// <summary>
    /// Check if there's currently a popup visible
    /// </summary>
    public static bool HasActivePopup()
    {
        return currentPopup != null;
    }
    
    void OnDestroy()
    {
        // Clear reference if this was the current popup
        if (currentPopup == this)
        {
            currentPopup = null;
        }
    }
}