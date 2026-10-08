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
    private Coroutine transitionCoroutine;

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
            
            // Start with transparent (scene visible) and NOT blocking raycasts
            if (fadeImage != null)
            {
                SetFadeAlpha(0f);
                fadeImage.raycastTarget = false; // IMPORTANT: Don't block raycasts when invisible
            }
            
            // Ensure canvas is disabled initially
            if (transitionCanvas != null)
            {
                transitionCanvas.gameObject.SetActive(false);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        // Never leave a stale static pointing at a destroyed manager (callers null-check Instance)
        if (Instance == this) Instance = null;
    }

    void CreateTransitionUI()
    {
        Debug.Log("SceneTransitionManager: Creating transition UI");
        
        // Create Canvas
        GameObject canvasGO = new GameObject("TransitionCanvas");
        canvasGO.transform.SetParent(transform);
        
        transitionCanvas = canvasGO.AddComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 9999; // Ensure it's on top
        
        // Add Canvas Scaler for responsive UI
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        
        // Add Graphic Raycaster (needed for UI)
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // Create Fade Image
        GameObject imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(canvasGO.transform, false);
        
        fadeImage = imageGO.AddComponent<Image>();
        fadeImage.color = fadeColor;
        fadeImage.raycastTarget = false; // IMPORTANT: Start with raycasts disabled
        
        // Make it fill the entire screen
        RectTransform rectTransform = imageGO.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        Debug.Log("SceneTransitionManager: Transition UI created successfully");
    }
    
    public void TransitionToScene(string sceneName)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("SceneTransitionManager: Already transitioning, ignoring request");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneTransitionManager: TransitionToScene called with an empty scene name");
            return;
        }

        if (!isActiveAndEnabled)
        {
            // StartCoroutine would throw and the scene change would be lost; load directly instead
            Debug.LogWarning("SceneTransitionManager: Inactive, loading scene without a fade");
            SceneManager.LoadScene(sceneName);
            return;
        }

        transitionCoroutine = StartCoroutine(TransitionCoroutine(sceneName));
    }

    IEnumerator TransitionCoroutine(string sceneName)
    {
        isTransitioning = true;

        Debug.Log($"SceneTransitionManager: Starting transition to {sceneName}");

        try
        {
            // Ensure transition UI is active and can block raycasts during transition
            if (transitionCanvas != null)
            {
                transitionCanvas.gameObject.SetActive(true);
            }

            if (fadeImage != null)
            {
                fadeImage.raycastTarget = true; // Enable raycast blocking during transition
            }

            // Phase 1: Fade OUT (scene becomes invisible)
            yield return StartCoroutine(FadeOut());

            // Phase 2: Load new scene
            Debug.Log($"SceneTransitionManager: Loading scene {sceneName}");
            SceneManager.LoadScene(sceneName);

            // Phase 3: Fade IN (new scene becomes visible)
            yield return StartCoroutine(FadeIn());

            Debug.Log($"SceneTransitionManager: Transition to {sceneName} complete");
        }
        finally
        {
            // Runs on completion and when the coroutine is stopped or the iterator disposed, so a
            // transition can never leave the opaque, raycast-blocking canvas up with
            // isTransitioning stuck true (which would also reject every later transition).
            SetFadeAlpha(0f);

            if (fadeImage != null)
            {
                fadeImage.raycastTarget = false; // Disable raycast blocking
            }

            if (transitionCanvas != null)
            {
                transitionCanvas.gameObject.SetActive(false); // Hide the canvas completely
            }

            transitionCoroutine = null;
            isTransitioning = false;
        }
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
    
    // Public method to check if currently transitioning
    public bool IsTransitioning()
    {
        return isTransitioning;
    }
    
    // Method to manually hide transition (useful for debugging)
    public void HideTransition()
    {
        // Resetting the flag while the coroutine is still running would let a second transition
        // start on top of it; stop the running one first.
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        if (fadeImage != null)
        {
            SetFadeAlpha(0f);
            fadeImage.raycastTarget = false;
        }
        
        if (transitionCanvas != null)
        {
            transitionCanvas.gameObject.SetActive(false);
        }
        
        isTransitioning = false;
    }
}