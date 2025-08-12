using UnityEngine;
using TMPro;
using System.Collections;

public class SimpleScorePopup : MonoBehaviour
{
    [Header("Animation Settings")]
    public float animationDuration = 3f;
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
        startPosition = isBonus? new Vector2(-460,100):rectTransform.anchoredPosition;
        
        
        // Start animation
        StartCoroutine(AnimatePopup());
    }
    
    /// <summary>
    /// Main popup animation
    /// </summary>
    IEnumerator AnimatePopup()
    {
        float elapsedTime = 0f;
        Vector3 targetPosition = startPosition + Vector3.up * moveUpDistance;
        
        // Set initial state
        rectTransform.localScale = Vector3.one * startScale;
        canvasGroup.alpha = 1f;
        
        while (elapsedTime < animationDuration)
        {
            float t = elapsedTime / animationDuration;
            
            // Move upward with easing
            float easedT = EaseOutCubic(t);
            rectTransform.anchoredPosition = Vector3.Lerp(startPosition, targetPosition, easedT);
            
            // Scale animation (grow then shrink slightly)
            float scale = Mathf.Lerp(startScale, endScale, Mathf.Sin(t * Mathf.PI));
            rectTransform.localScale = Vector3.one * scale;
            
            // Fade out near the end
            float alpha = t < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);
            canvasGroup.alpha = alpha;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Animation complete - destroy popup
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
        
        // Add TextMeshPro component
        TextMeshProUGUI textComp = popupObj.AddComponent<TextMeshProUGUI>();
        textComp.text = $"+{points}";
        textComp.fontSize = fontSize;
        textComp.fontStyle = fontStyle;
        textComp.alignment = TextAlignmentOptions.Center;
        
        // Set custom font if provided
        if (font != null)
        {
            textComp.font = font;
        }
        
        // Setup RectTransform
        RectTransform rectTrans = popupObj.GetComponent<RectTransform>();
        rectTrans.sizeDelta = new Vector2(800, 800);
        
        // Set position
        if (position.HasValue)
        {
            rectTrans.anchoredPosition = position.Value;
        }
        else
        {
            rectTrans.anchoredPosition = Vector2.zero; // Center of screen
        }
        
        // Add popup script
        SimpleScorePopup popup = popupObj.AddComponent<SimpleScorePopup>();
        
        // Start the popup
        popup.ShowPopup(points, isBonus);
        
        return popup;
    }
}