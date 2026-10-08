using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class StarProgressBar : MonoBehaviour
{
    [Header("Star GameObjects")]
    public Transform star1;
    public Transform star2;
    public Transform star3;
    
    [Header("Star UI Images")]
    public Image star1Image;
    public Image star2Image;
    public Image star3Image;
    
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
        // Look for Image components instead of SpriteRenderer
        if (star1 != null && star1Image == null)
            star1Image = star1.GetComponent<Image>();
            
        if (star2 != null && star2Image == null)
            star2Image = star2.GetComponent<Image>();
            
        if (star3 != null && star3Image == null)
            star3Image = star3.GetComponent<Image>();
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
                
                // StartCoroutine throws if the GameObject is inactive in the hierarchy
                if (newStarStates[i] && useAnimations && gameObject.activeInHierarchy)
                {
                    StartCoroutine(AnimateStarEarned(i));
                }
            }
        }
    }
    
    void UpdateStarVisual(int starIndex, bool isEarned)
    {
        // Use Image component instead of SpriteRenderer
        Image starImage = GetStarImage(starIndex);
        if (starImage == null) return;

        starImage.sprite = isEarned ? starFilledSprite : starUnfilledSprite;
    }
    
    // New method to get Image component instead of SpriteRenderer
    Image GetStarImage(int starIndex)
    {
        switch (starIndex)
        {
            case 0: return star1Image;
            case 1: return star2Image;
            case 2: return star3Image;
            default: return null;
        }
    }
    
    void UpdateTexts(float currentScore)
    {
        if (currentScoreText != null)
        {
            currentScoreText.text = $"Score: {currentScore:F0}";
        }
        
        if (nextStarText != null && currentLevelData != null)
        {
            float scoreToNext = currentLevelData.GetScoreToNextStar(currentScore);
            if (scoreToNext > 0)
            {
                nextStarText.text = $"{scoreToNext:F0} to next star";
            }
            else
            {
                nextStarText.text = "Max stars earned!";
            }
        }
        
        if (performanceText != null && currentLevelData != null)
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
    
    IEnumerator AnimateStarEarned(int starIndex)
    {
        Transform starTransform = GetStarTransform(starIndex);
        if (starTransform == null) yield break;
        
        Vector3 originalScale = originalStarScales[starIndex];
        Vector3 targetScale = originalScale * 1.2f;
        
        // Scale up
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            starTransform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Scale back down
        elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            starTransform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        starTransform.localScale = originalScale;
    }
    
    Transform GetStarTransform(int starIndex)
    {
        switch (starIndex)
        {
            case 0: return star1;
            case 1: return star2;
            case 2: return star3;
            default: return null;
        }
    }
}