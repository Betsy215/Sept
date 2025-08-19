using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }
    
    [Header("Transition Settings")]
    [Tooltip("Duration of fade out + fade in (total transition time)")]
    public float transitionDuration = 1f;
    
    [Tooltip("Color to fade to/from (usually black)")]
    public Color fadeColor = Color.black;
    
    [Header("Transition Canvas")]
    [Tooltip("Canvas that should be on top of everything")]
    public Canvas transitionCanvas;
    
    [Tooltip("Image component for the fade effect")]
    public Image fadeImage;
    
    [Header("Auto Setup")]
    [Tooltip("Create transition UI automatically if not assigned")]
    public bool autoCreateTransitionUI = true;
    
    private bool isTransitioning = false;
    
    void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Auto-create transition UI if needed
            if (autoCreateTransitionUI && (transitionCanvas == null || fadeImage == null))
            {
                CreateTransitionUI();
            }
            
            // Start with transparent (scene visible)
            if (fadeImage != null)
            {
                SetFadeAlpha(0f);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void CreateTransitionUI()
    {
        Debug.Log("SceneTransitionManager: Auto-creating transition UI");
        
        // Create canvas
        GameObject canvasGO = new GameObject("SceneTransitionCanvas");
        canvasGO.transform.SetParent(transform);
        
        transitionCanvas = canvasGO.AddComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 9999; // On top of everything
        
        // Add GraphicRaycaster and CanvasScaler
        canvasGO.AddComponent<GraphicRaycaster>();
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Create fade image
        GameObject imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(canvasGO.transform, false);
        
        fadeImage = imageGO.AddComponent<Image>();
        fadeImage.color = fadeColor;
        
        // Make it cover the entire screen
        RectTransform rectTransform = fadeImage.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        Debug.Log("SceneTransitionManager: Transition UI created successfully");
    }
    
    // Main transition method
    public void TransitionToScene(string sceneName)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("SceneTransitionManager: Already transitioning, ignoring request");
            return;
        }
        
        StartCoroutine(TransitionCoroutine(sceneName));
    }
    
    IEnumerator TransitionCoroutine(string sceneName)
    {
        isTransitioning = true;
        
        Debug.Log($"SceneTransitionManager: Starting transition to {sceneName}");
        
        // Ensure transition UI is active
        if (transitionCanvas != null)
            transitionCanvas.gameObject.SetActive(true);
        
        // Phase 1: Fade OUT (scene becomes invisible)
        yield return StartCoroutine(FadeOut());
        
        // Phase 2: Load new scene
        Debug.Log($"SceneTransitionManager: Loading scene {sceneName}");
        SceneManager.LoadScene(sceneName);
        
        // Phase 3: Fade IN (new scene becomes visible)
        yield return StartCoroutine(FadeIn());
        
        isTransitioning = false;
        
        Debug.Log($"SceneTransitionManager: Transition to {sceneName} complete");
    }
    
    IEnumerator FadeOut()
    {
        Debug.Log("SceneTransitionManager: Fading out...");
        
        float halfDuration = transitionDuration / 2f;
        float elapsed = 0f;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time in case game is paused
            float alpha = Mathf.Lerp(0f, 1f, elapsed / halfDuration);
            SetFadeAlpha(alpha);
            yield return null;
        }
        
        SetFadeAlpha(1f); // Ensure fully opaque
    }
    
    IEnumerator FadeIn()
    {
        Debug.Log("SceneTransitionManager: Fading in...");
        
        float halfDuration = transitionDuration / 2f;
        float elapsed = 0f;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / halfDuration);
            SetFadeAlpha(alpha);
            yield return null;
        }
        
        SetFadeAlpha(0f); // Ensure fully transparent
    }
    
    void SetFadeAlpha(float alpha)
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }
    }
    
    // Public methods for external use
    public bool IsTransitioning()
    {
        return isTransitioning;
    }
    
    public void SetTransitionDuration(float duration)
    {
        transitionDuration = duration;
    }
    
    public void SetFadeColor(Color color)
    {
        fadeColor = color;
        if (fadeImage != null)
        {
            Color currentColor = fadeImage.color;
            fadeColor.a = currentColor.a; // Keep current alpha
            fadeImage.color = fadeColor;
        }
    }
    
    // Instant fade methods for special cases
    public void FadeToBlackInstant()
    {
        SetFadeAlpha(1f);
    }
    
    public void FadeToTransparentInstant()
    {
        SetFadeAlpha(0f);
    }
}