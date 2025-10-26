using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [Header("UI Elements")] public TextMeshProUGUI inGameScoreText; // For in-game score display
    public TextMeshProUGUI finalScoreText; // For level complete panel
    public TextMeshProUGUI totalScoreText; // Session total score
    public TextMeshProUGUI feedbackText; // For showing "+10 points!" etc.

    [Header("UI Overlay Elements")] public TextMeshProUGUI overlayScoreText; // Your new overlay score text

    [Header("Game References")] public OrderSystem orderSystem; // Reference to order system
    public LevelManager levelManager; // Reference to level manager
    public StarProgressBar starProgressBar;

    [Header("Per-Item Scoring Settings")] public int pointsPerItem = 10; // Points for each correct item served
    public int timeBonusMultiplier = 5; // Points per second remaining when order completed

    [Header("Item-Specific Points")] public ItemPointValues[] itemPoints; // Specific points for different items

    [Header("Score Popup Settings")]
    public bool enableScorePopups = true; // Set to false temporarily if animation still delayed

    public Canvas gameCanvas; // Assign your main game Canvas
    public TMP_FontAsset popupFont; // FIXED: Assign Beachday SDF font here
    public Sprite popupBackgroundSprite;

    [Header("Popup Management")] public bool clearPopupOnLevelEnd = true; // Clear popup when level ends
    public bool clearPopupOnLevelStart = true; // Clear popup when level starts

    [Header("Audio")] public AudioSource audioSource;
    public AudioClip pointsSound;
    public AudioClip bonusSound;
    public AudioClip penaltySound;

    // Score tracking
    private float currentScore = 0; // Level score (for star progress)
    public float currentOrderItemPoints = 0; // Track points for current order items
    private float totalTipsEarned = 0f;

    [Serializable]
    public class ItemPointValues
    {
        public string itemType;
        public int points;
    }

    private void Start()
    {
        // Try to auto-find the in-game score text if not assigned
        if (inGameScoreText == null)
        {
            // Look for common names for in-game score display
            GameObject[] possibleObjects =
            {
                GameObject.Find("scoreText"),
                GameObject.Find("ScoreText"),
                GameObject.Find("InGameScore"),
                GameObject.Find("CurrentScore"),
                GameObject.Find("LevelScore")
            };

            foreach (var obj in possibleObjects)
                if (obj != null)
                {
                    var textComponent = obj.GetComponent<TextMeshProUGUI>();
                    if (textComponent != null && obj != finalScoreText?.gameObject)
                    {
                        inGameScoreText = textComponent;
                        Debug.Log($"ScoreManager: Auto-found in-game score text: {obj.name}");
                        break;
                    }
                }
        }

        // Auto-find game canvas if not assigned
        if (gameCanvas == null)
        {
            gameCanvas = FindObjectOfType<Canvas>();
            if (gameCanvas != null)
            {
                Debug.Log("ScoreManager: Auto-found Canvas for popups");
            }
            else
            {
                Debug.LogWarning("ScoreManager: No Canvas found! Score popups will be disabled.");
                enableScorePopups = false;
            }
        }

        UpdateScoreUI();

        // Subscribe to session events
        SetupSessionEvents();

        // Update total score display
        UpdateTotalScoreUI();
    }

    // UPDATED: Enhanced session event setup
    private void SetupSessionEvents()
    {
        if (SessionManager.Instance != null)
        {
            // Remove old subscription to avoid duplicates
            SessionManager.Instance.OnTotalScoreChanged -= UpdateTotalScoreDisplay;
            // Subscribe to session total score changes
            SessionManager.Instance.OnTotalScoreChanged += UpdateTotalScoreDisplay;

            Debug.Log("ScoreManager: Subscribed to session total score changes");
        }
    }

    // UPDATED: Reset score method with popup clearing
    public void ResetScore()
    {
        currentScore = 0;
        currentOrderItemPoints = 0; // Reset current order tracking

        // Clear popup when starting new level
        if (clearPopupOnLevelStart) ClearScorePopup();

        UpdateScoreUI();
        starProgressBar.UpdateDisplay(0);
        Debug.Log("Score reset for new level");
    }


    public void AwardItemPoints(string itemType)
    {
        var points = GetPointsForItem(itemType);

        currentScore += points;
        currentOrderItemPoints += points; // Accumulate but don't add to session yet

        starProgressBar.UpdateDisplay(currentScore);
        PlayPointsSound();
    }

    public void AwardOrderCompletionBonus(float remainingTime, float basepoints)
    {
        var timeBonusPoints = remainingTime * timeBonusMultiplier;
        var tipRaw = basepoints * timeBonusPoints / 100f;
        var tip = (float)Math.Round(tipRaw, 2, MidpointRounding.AwayFromZero);
        totalTipsEarned += tip;

        currentScore += tip;

        // ADD ALL POINTS TO SESSION NOW
        var totalPoints = currentOrderItemPoints + tip;
        if (SessionManager.Instance != null && SessionManager.Instance.HasActiveSession())
            SessionManager.Instance.AddScoreImmediately(totalPoints);

        UpdateScoreUI(); // Score display updates here
        starProgressBar.UpdateDisplay(currentScore);

        StartCoroutine(DelayedCombinedPopup(currentOrderItemPoints, tip));
        PlayBonusSound();

        currentOrderItemPoints = 0;
    }

    public float GetTotalTipsEarned()
    {
        return totalTipsEarned;
    }

    // NEW: Show one combined popup after order completion
    private IEnumerator DelayedCombinedPopup(float itemPoints, float timeBonus)
    {
        // Wait for served item animation to finish
        yield return new WaitForSeconds(0.2f);

        // Show combined popup
        ShowScorePopup(itemPoints, timeBonus);
    }

    public void ApplyOrderExpiredPenalty()
    {
        // FIX: Still award points earned before expiration
        if (currentOrderItemPoints > 0)
        {
            // Add accumulated points to session
            if (SessionManager.Instance != null && SessionManager.Instance.HasActiveSession())
                SessionManager.Instance.AddScoreImmediately(currentOrderItemPoints);

            UpdateScoreUI(); // Update score display

            // Show popup with earned points (no bonus)
            StartCoroutine(DelayedCombinedPopup(currentOrderItemPoints, 0f));
        }

        ShowFeedback("Order Expired!", Color.red);
        PlayPenaltySound();

        currentOrderItemPoints = 0;
        Debug.Log("Order expired - awarded points for items served");
    }

    // UPDATED: Show level score instead of session total score
    private void UpdateScoreUI()
    {
        var displayScore = SessionManager.Instance.GetTotalScore();

        var scoreDisplayText = "$ " + displayScore.ToString("F2");

        if (inGameScoreText != null)
            inGameScoreText.text = scoreDisplayText;
        else
            Debug.LogWarning("ScoreManager: inGameScoreText is null!");

        if (finalScoreText != null)
            finalScoreText.text = "Level Score: " + currentScore.ToString("F2");

        if (overlayScoreText != null)
            overlayScoreText.text = scoreDisplayText;
    }

    // Update total score UI
    private void UpdateTotalScoreUI()
    {
        if (totalScoreText != null && SessionManager.Instance != null)
        {
            var totalScore = SessionManager.Instance.GetTotalScore();
            totalScoreText.text = "Total Score: " + totalScore.ToString("F2");
        }
    }

    // UPDATED: Callback for session total score changes
    private void UpdateTotalScoreDisplay(float f)
    {
        // Update dedicated total score text if you have one
        if (totalScoreText != null) totalScoreText.text = "Total Score: " + f.ToString("F2");

        Debug.Log($"ScoreManager: Total score updated to {f}");
    }

    // UPDATED: Now shows combined popup only
    private void ShowScorePopup(float basePoints, float bonusPoints = 0)
    {
        if (!enableScorePopups)
        {
            Debug.Log("Score popups are disabled in settings");
            return;
        }

        if (gameCanvas == null)
        {
            Debug.LogError("gameCanvas is null! Cannot create popup.");
            return;
        }

        Debug.Log($"Creating COMBINED popup: itemPoints={basePoints}, timeBonus={bonusPoints}");

        // Create one popup with combined points
        var popup = SimpleScorePopup.CreateCombinedPopupWithFont(
            gameCanvas.transform,
            basePoints,
            bonusPoints,
            popupFont,
            popupBackgroundSprite
        );
    }

    // NEW: Method to manually clear current popup
    public void ClearScorePopup()
    {
        SimpleScorePopup.ClearCurrentPopup();
    }

    // NEW: Method called when level ends (call this from LevelManager)
    public void OnLevelEnd()
    {
        // Clear popup when level ends if enabled
        if (clearPopupOnLevelEnd) ClearScorePopup();

        Debug.Log("ScoreManager: Level ended");
    }


    // Helper method to get points for specific item types
    private int GetPointsForItem(string itemType)
    {
        // Check if there are specific points for this item type
        foreach (var itemPoint in itemPoints)
            if (itemPoint.itemType == itemType)
                return itemPoint.points;

        // Return default points if no specific value found
        return pointsPerItem;
    }

    private void AddScore(float points)
    {
        currentScore += points;

        // UNCOMMENT THIS LINE:
        if (SessionManager.Instance != null && SessionManager.Instance.HasActiveSession())
            SessionManager.Instance.AddScoreImmediately(points);

        UpdateScoreUI();
        starProgressBar.UpdateDisplay(currentScore);
    }

    // Helper method to show feedback text
    private void ShowFeedback(string message, Color? color = null)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = color ?? Color.green;

            // Auto-hide feedback after a delay
            CancelInvoke("HideFeedback");
            Invoke("HideFeedback", 2f);
        }
        else
        {
            // If no feedback text component, just log it
            Debug.Log($"Score Feedback: {message}");
        }
    }

    private void HideFeedback()
    {
        if (feedbackText != null)
            feedbackText.text = "";
    }

    // Audio methods
    private void PlayPointsSound()
    {
        if (audioSource != null && pointsSound != null)
            audioSource.PlayOneShot(pointsSound);
        else if (AudioManager.Instance != null) AudioManager.Instance.PlayItemPickup();
    }

    private void PlayBonusSound()
    {
        if (audioSource != null && bonusSound != null)
            audioSource.PlayOneShot(bonusSound);
        else if (AudioManager.Instance != null) AudioManager.Instance.PlayOrderComplete();
    }

    private void PlayPenaltySound()
    {
        if (audioSource != null && penaltySound != null) audioSource.PlayOneShot(penaltySound);
    }

    // Clean up events when destroyed
    private void OnDestroy()
    {
        if (SessionManager.Instance != null) SessionManager.Instance.OnTotalScoreChanged -= UpdateTotalScoreDisplay;
    }

    // Public getters
    public float GetCurrentScore()
    {
        return currentScore;
    }
}