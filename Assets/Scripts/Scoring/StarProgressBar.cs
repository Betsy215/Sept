using UnityEngine;
using TMPro;
using System.Collections;

public class StarProgressBar : MonoBehaviour
{
    [Header("Star GameObjects")]
    public Transform star1;
    public Transform star2;
    public Transform star3;
    
    [Header("Star Sprite Renderers")]
    public SpriteRenderer star1Renderer;
    public SpriteRenderer star2Renderer;
    public SpriteRenderer star3Renderer;
    
    [Header("Star Sprites")]
    public Sprite starFilledSprite;
    public Sprite starUnfilledSprite;
    
    [Header("Optional Text Displays")]
    public TextMeshProUGUI currentScoreText;
    public TextMeshProUGUI nextStarText;
    public TextMeshProUGUI performanceText;
    
    [Header("Animation Settings")]
    public bool useAnimations = true;
    
    [Header("Level Data")]
    public LevelData currentLevelData;
    
    private float lastDisplayedScore = -1;
    private bool[] starStates = new bool[3];
    private Vector3[] originalStarScales = new Vector3[3];
    
    void Start()
    {
        StoreOriginalStarScales();
        AutoFindComponents();
        InitializeStars();
    }
    
    void AutoFindComponents()
    {
        if (star1 != null && star1Renderer == null)
            star1Renderer = star1.GetComponent<SpriteRenderer>();
            
        if (star2 != null && star2Renderer == null)
            star2Renderer = star2.GetComponent<SpriteRenderer>();
            
        if (star3 != null && star3Renderer == null)
            star3Renderer = star3.GetComponent<SpriteRenderer>();
    }
    
    public void Initialize(LevelData levelData)
    {
        currentLevelData = levelData;
        InitializeStars();
        UpdateDisplay(0);
    }
    
    void InitializeStars()
    {
        SetAllStarsUnfilled();
    }
    
    void StoreOriginalStarScales()
    {
        originalStarScales[0] = star1 != null ? star1.localScale : Vector3.one;
        originalStarScales[1] = star2 != null ? star2.localScale : Vector3.one;
        originalStarScales[2] = star3 != null ? star3.localScale : Vector3.one;
    }
    
    public void UpdateDisplay(float currentScore)
    {
        if (currentLevelData == null) return;
        if (lastDisplayedScore == currentScore) return;
        
        lastDisplayedScore = currentScore;
        
        UpdateStars(currentScore);
        UpdateTexts(currentScore);
    }
    
    void UpdateStars(float currentScore)
    {
        if (currentLevelData == null) return;
        
        bool[] newStarStates = {
            currentLevelData.HasEarnedStar(currentScore, 1),
            currentLevelData.HasEarnedStar(currentScore, 2),
            currentLevelData.HasEarnedStar(currentScore, 3)
        };
        
        for (int i = 0; i < 3; i++)
        {
            if (newStarStates[i] != starStates[i])
            {
                starStates[i] = newStarStates[i];
                UpdateStarVisual(i, newStarStates[i]);
                
                if (newStarStates[i] && useAnimations)
                {
                    StartCoroutine(AnimateStarEarned(i));
                }
            }
        }
    }
    
    void UpdateStarVisual(int starIndex, bool isEarned)
    {
        SpriteRenderer starRenderer = GetStarRenderer(starIndex);
        if (starRenderer == null) return;
        
        if (starFilledSprite != null && starUnfilledSprite != null)
        {
            starRenderer.sprite = isEarned ? starFilledSprite : starUnfilledSprite;
        }
    }
    
    IEnumerator AnimateStarEarned(int starIndex)
    {
        Transform starTransform = GetStarTransform(starIndex);
        if (starTransform == null) yield break;
        
        Vector3 originalScale = originalStarScales[starIndex];
        Vector3 targetScale = originalScale * 1.3f;
        
        float duration = 0.4f;
        float elapsed = 0f;
        
        starTransform.localScale = originalScale * 0.7f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            if (progress < 0.6f)
            {
                float scaleProgress = progress / 0.6f;
                starTransform.localScale = Vector3.Lerp(originalScale * 0.7f, targetScale, scaleProgress);
            }
            else
            {
                float scaleProgress = (progress - 0.6f) / 0.4f;
                starTransform.localScale = Vector3.Lerp(targetScale, originalScale, scaleProgress);
            }
            
            yield return null;
        }
        
        starTransform.localScale = originalScale;
    }
    
    Transform GetStarTransform(int index)
    {
        switch (index)
        {
            case 0: return star1;
            case 1: return star2;
            case 2: return star3;
            default: return null;
        }
    }
    
    SpriteRenderer GetStarRenderer(int index)
    {
        switch (index)
        {
            case 0: return star1Renderer;
            case 1: return star2Renderer;
            case 2: return star3Renderer;
            default: return null;
        }
    }
    
    void UpdateTexts(float currentScore)
    {
        if (currentLevelData == null) return;
        
        if (currentScoreText != null)
        {
            currentScoreText.text = $"Score: {currentScore}";
        }
        
        if (nextStarText != null)
        {
            float scoreToNext = currentLevelData.GetScoreToNextStar(currentScore);
            if (scoreToNext > 0)
            {
                nextStarText.text = $"{scoreToNext} to next star";
            }
            else
            {
                nextStarText.text = "Max stars earned!";
            }
        }
        
        if (performanceText != null)
        {
            performanceText.text = currentLevelData.GetPerformanceDescription(currentScore);
        }
    }
    
    void SetAllStarsUnfilled()
    {
        for (int i = 0; i < 3; i++)
        {
            starStates[i] = false;
            UpdateStarVisual(i, false);
        }
    }
    
    public void SetStarSprites(Sprite filled, Sprite unfilled)
    {
        starFilledSprite = filled;
        starUnfilledSprite = unfilled;
        
        for (int i = 0; i < 3; i++)
        {
            UpdateStarVisual(i, starStates[i]);
        }
    }
    
    public int GetEarnedStarsCount()
    {
        int count = 0;
        for (int i = 0; i < 3; i++)
        {
            if (starStates[i]) count++;
        }
        return count;
    }
}