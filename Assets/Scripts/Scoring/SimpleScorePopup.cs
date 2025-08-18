using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class SimpleScorePopup : MonoBehaviour
{
    [Header("Animation Settings")]
    public float initialAnimationDuration = 1f;
    public float moveUpDistance = 100f;
    public float startScale = 0.8f;
    public float endScale = 1.2f;
    
    [Header("Display Duration")]
    public float stayDuration = 4f; // How long popup stays visible after animation
    public bool autoDisappear = true; // Whether popup should disappear automatically
    public float fadeOutDuration = 0.5f; // How long fade out takes
    
    [Header("Colors")]
    public Color itemPointsColor = Color.green;
    public Color bonusPointsColor = Color.yellow;
    public Color combinedColor = Color.yellow; // Color when showing both base + bonus
    
    [Header("Popup Positioning")]
    public Vector2 defaultSpawnPosition = new Vector2(-280, 200); // Default spawn position
    public float randomRange = 150f; // Random horizontal spread
    
    [Header("Font Settings")]
    public TMP_FontAsset popupFont; // Custom font (leave null for default)
    public float fontSize = 70f;
    public FontStyles fontStyle = FontStyles.Normal;
    
   
    
    private TextMeshProUGUI textComponent;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 startPosition;
    private bool isAnimationComplete = false;
    
    // Static reference to track current popup
    private static SimpleScorePopup currentPopup;
    
    void Awake()
    {
        // Don't try to find text component here - it will be assigned manually
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    public void ShowCombinedPopup(float basePoints, float bonusPoints = 0)
    {
        // Destroy previous popup if it exists
        if (currentPopup != null && currentPopup != this)
        {
            Destroy(currentPopup.gameObject);
        }
    
        currentPopup = this;
    
        // FIND TEXT COMPONENT IF NULL
        if (textComponent == null)
        {
            textComponent = GetComponent<TextMeshProUGUI>();
            Debug.Log($"Found text component: {textComponent != null}");
        }
    
        // Check if text component exists
        if (textComponent == null)
        {
            Debug.LogError("TextMeshProUGUI component not found!");
            return;
        }
    
        // Create combined text
        string popupText = "";
        Color textColor = itemPointsColor;
    
        if (bonusPoints > 0)
        {
            popupText = $"Sale:\n${basePoints}\n Tips:\n${bonusPoints}!";
            textColor = Color.black;
        }
        else
        {
            popupText = $"${basePoints}";
            textColor = Color.black;
        }
    
        // Set text content and color
        textComponent.text = popupText;
        textComponent.color = textColor;
    
        Debug.Log($"Set text: '{popupText}' with color: {textColor}");
    
        // Store starting position
        startPosition = rectTransform.anchoredPosition;
    
        // Start initial animation
        StartCoroutine(InitialAnimation());
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
    
   public static SimpleScorePopup CreateCombinedPopupWithFont(Transform parent, float basePoints, float bonusPoints = 0, TMP_FontAsset customFont = null, Sprite backgroundSprite = null)
{
    // Create PARENT container (no graphic components)
    GameObject popupObj = new GameObject("ScorePopup");
    popupObj.transform.SetParent(parent, false);
    
    RectTransform rectTransform = popupObj.AddComponent<RectTransform>();
    rectTransform.sizeDelta = new Vector2(400, 500);
    
    // Add popup script to parent
    SimpleScorePopup popup = popupObj.AddComponent<SimpleScorePopup>();
    
    // Set position
    rectTransform.anchoredPosition = popup.defaultSpawnPosition + new Vector2(
        Random.Range(-popup.randomRange / 2f, popup.randomRange / 2f),
        Random.Range(-50f, 50f)
    );
    
    // Create BACKGROUND child (separate GameObject)
    if (backgroundSprite != null)
    {
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(popupObj.transform, false);
        
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.sprite = backgroundSprite;
        
        // ADD SHADOW COMPONENT
        Shadow shadow = bgObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.5f); // Semi-transparent black
        shadow.effectDistance = new Vector2(15f, -15f); // Offset: right 5, down 5
        shadow.useGraphicAlpha = true; // Respect the image's alpha
        
        Debug.Log($"Added background image: {backgroundSprite.name}");
    }
    
    // Create TEXT child (separate GameObject)
    GameObject textObj = new GameObject("Text");
    textObj.transform.SetParent(popupObj.transform, false);
    
    RectTransform textRect = textObj.AddComponent<RectTransform>();
    textRect.anchorMin = Vector2.zero;
    textRect.anchorMax = Vector2.one;
    textRect.offsetMin = Vector2.zero;
    textRect.offsetMax = Vector2.zero;
    
    textRect.anchoredPosition = new Vector2(-5f, 20f); 
    
    TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
    textComponent.fontSize = popup.fontSize;
    textComponent.fontStyle = popup.fontStyle;
    textComponent.alignment = TextAlignmentOptions.Center;
    
    if (customFont != null)
    {
        textComponent.font = customFont;
    }
    else if (popup.popupFont != null)
    {
        textComponent.font = popup.popupFont;
    }
    
    // MANUALLY ASSIGN the text component to the popup script
    popup.textComponent = textComponent;
    Debug.Log($"Manually assigned text component: {textComponent != null}");
    
    // Show the popup
    popup.ShowCombinedPopup(basePoints, bonusPoints);
    
    return popup;
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