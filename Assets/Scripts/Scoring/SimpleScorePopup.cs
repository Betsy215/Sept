using UnityEngine;
using TMPro;
using System.Collections;

public class SimpleScorePopup : MonoBehaviour
{
    [Header("Animation Settings")]
    public float initialAnimationDuration = 1f; // Initial pop-in animation
    public float moveUpDistance = 100f;
    public float startScale = 0.8f;
    public float endScale = 1.2f;
    
    [Header("Colors")]
    public Color itemPointsColor = Color.green;
    public Color bonusPointsColor = Color.yellow;
    
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
    /// Show popup with points
    /// </summary>
    /// <param name="points">Points to display</param>
    /// <param name="isBonus">Is this a bonus popup?</param>
    public void ShowPopup(int points, bool isBonus = false)
    {
        // Destroy previous popup if it exists
        if (currentPopup != null && currentPopup != this)
        {
            Destroy(currentPopup.gameObject);
        }
        
        // Set this as the current popup
        currentPopup = this;
        
        // Set text content
        string prefix = points > 0 ? "+" : "";
        textComponent.text = $"{prefix}{points}";
        
        // Set color based on type
        textComponent.color = isBonus ? bonusPointsColor : itemPointsColor;
        
        // If it's a bonus, add extra text
        if (isBonus && points > 0)
        {
            textComponent.text += " BONUS!";
        }
        
        // Store starting position
        startPosition = isBonus ? new Vector2(-100, 100) : rectTransform.anchoredPosition;
        
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
        
        // Now stay visible until next popup or manual destruction
        Debug.Log($"Score popup animation complete. Staying visible until next popup.");
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
        float fadeTime = 0.2f;
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeTime)
        {
            float t = elapsedTime / fadeTime;
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
    /// Static method to create popup quickly (backward compatibility)
    /// </summary>
    /// <param name="parent">Canvas to spawn popup on</param>
    /// <param name="points">Points to show</param>
    /// <param name="isBonus">Is bonus popup?</param>
    /// <param name="position">Screen position</param>
    /// <returns>Created popup</returns>
    public static SimpleScorePopup CreatePopup(Transform parent, int points, bool isBonus = false, Vector2? position = null)
    {
        return CreatePopupWithFont(parent, points, isBonus, position, null, 36f, FontStyles.Bold);
    }
    
    /// <summary>
    /// Static method to create popup with custom font
    /// </summary>
    /// <param name="parent">Canvas to spawn popup on</param>
    /// <param name="points">Points to show</param>
    /// <param name="isBonus">Is bonus popup?</param>
    /// <param name="position">Screen position</param>
    /// <param name="font">Custom font asset</param>
    /// <param name="fontSize">Font size</param>
    /// <param name="fontStyle">Font style</param>
    /// <returns>Created popup</returns>
    public static SimpleScorePopup CreatePopupWithFont(Transform parent, int points, bool isBonus = false, Vector2? position = null, TMP_FontAsset font = null, float fontSize = 36f, FontStyles fontStyle = FontStyles.Bold)
    {
        // Create popup GameObject
        GameObject popupObj = new GameObject("ScorePopup");
        popupObj.transform.SetParent(parent, false);
        
        // Add RectTransform
        RectTransform rectTransform = popupObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 60);
        
        // Set position
        if (position.HasValue)
        {
            rectTransform.anchoredPosition = position.Value;
        }
        else
        {
            rectTransform.anchoredPosition = Vector2.zero;
        }
        
        // Add TextMeshPro component
        TextMeshProUGUI textComponent = popupObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = points.ToString();
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = isBonus ? Color.yellow : Color.green;
        
        // Set custom font if provided
        if (font != null)
        {
            textComponent.font = font;
        }
        
        // Add popup script and show
        SimpleScorePopup popup = popupObj.AddComponent<SimpleScorePopup>();
        popup.ShowPopup(points, isBonus);
        
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