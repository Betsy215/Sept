using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class SimpleScorePopup : MonoBehaviour
{
    [Header("Animation Settings")] public float initialAnimationDuration = 1f;
    public float moveUpDistance = 100f;
    public float startScale = 0.8f;
    public float endScale = 1.2f;

    [Header("Display Duration")] public float stayDuration = 4f; // How long popup stays visible after animation
    public bool autoDisappear = true; // Whether popup should disappear automatically
    public float fadeOutDuration = 0.5f; // How long fade out takes

    [Header("Colors")] public Color itemPointsColor = Color.green;
    public Color bonusPointsColor = Color.yellow;
    public Color combinedColor = Color.yellow; // Color when showing both base + bonus

    [Header("Popup Positioning")] public Vector2 defaultSpawnPosition = new(0, 520); // centre, between the two order bubbles (two customers, 13 October); was (-100, 350), on the left customer
    public float randomRange = 150f; // Random horizontal spread

    [Header("Font Settings")] public TMP_FontAsset popupFont; // Custom font (leave null for default)
    public float fontSize = 50f;
    public FontStyles fontStyle = FontStyles.Normal;


    private TextMeshProUGUI textComponent;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 startPosition;
    private bool isAnimationComplete = false;
    private bool isDestroying = false;

    // Static reference to track current popup
    private static SimpleScorePopup currentPopup;

    /// Where the next receipt goes, on screen: the served customer's bubble (set by OrderSystem.CompleteOrder just before
    /// the points are awarded; used once). Unset: the old place in the middle.
    public static Vector2? NextAtScreen;
    // Over the served customer it rises further and leaves sooner, so it is gone before the next customer orders there
    private const float RiseOverCustomer = 140f, StayOverCustomer = 1.1f;

    public static Vector2 ScreenPointOf(Transform t)
    {
        var canvas = t.GetComponentInParent<Canvas>();
        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) cam = canvas.rootCanvas.worldCamera;
        else if (canvas == null) cam = Camera.main;
        return RectTransformUtility.WorldToScreenPoint(cam, t.position);
    }

    private void Awake()
    {
        // Don't try to find text component here - it will be assigned manually
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    // ---- tip levels (owner, 23 November 2026: "for tips, i want different colors to mean different things so we have
    // three levels - perfect, fast, just ok"; then "use nice instead of fast", green for Perfect and yellow for Nice,
    // each with a filled stamp). The level is the customer's patience when the order is finished, as the flowers show
    // it: every served item fills the meter up to the next whole flower, so an order always ends on whole flowers.
    // Chapter 1 (three flowers): all three = Perfect, two = Nice, one = Just OK. Chapter 2 (four): three or four =
    // Perfect, two = Nice, one = Just OK. As shares of the full time: Perfect from 0.7, Nice from 0.4 (the lines lie
    // between the flower steps, so they hold for three and for four flowers). The money is not changed; the receipt names it:
    // the tips line takes the level's colour and a small tag sits on the paper's corner (none for Just OK).
    // Proposal and colours: Store/ui/tip_levels_2026-11-23.
    public enum TipLevel { None = -1, Ok = 0, Nice = 1, Perfect = 2 }
    public const float PerfectShare = 0.7f, NiceShare = 0.4f;
    /// Set by the order that was just finished (OrderSystem); ScoreManager hands it to the receipt it opens.
    public static TipLevel PendingTip = TipLevel.None, NextTip = TipLevel.None;
    public static TipLevel LevelFor(float timeLeft, float fullTime)
    {
        if (fullTime <= 0f) return TipLevel.Ok;
        var share = timeLeft / fullTime;
        return share >= PerfectShare ? TipLevel.Perfect : share >= NiceShare ? TipLevel.Nice : TipLevel.Ok;
    }
    private static readonly Color TipGreen = new Color32(38, 150, 84, 255), TipGreenDark = new Color32(20, 104, 56, 255),
        TipYellow = new Color32(236, 172, 18, 255), TipYellowDark = new Color32(150, 96, 8, 255), TipGrey = new Color32(120, 100, 88, 255);

    /// The tips line in its level's colour, as a second text laid exactly over the first (same words, the other line
    /// see-through in each), so the yellow can carry its own brown rim: plain yellow does not read on the white paper.
    private void ColourTips(float basePoints, float bonusPoints, TipLevel level)
    {
        var total = " " + Money.Format(basePoints); var tips = " Tips: " + Money.Format(bonusPoints);
        textComponent.text = total + "\n<alpha=#00>" + tips;
        var go = new GameObject("TipsLine", typeof(RectTransform)); go.transform.SetParent(textComponent.transform.parent, false);
        var rt = (RectTransform)go.transform; var from = textComponent.rectTransform;
        rt.anchorMin = from.anchorMin; rt.anchorMax = from.anchorMax; rt.offsetMin = from.offsetMin; rt.offsetMax = from.offsetMax; rt.anchoredPosition = from.anchoredPosition;
        var line = go.AddComponent<TextMeshProUGUI>();
        line.font = textComponent.font; line.fontSize = textComponent.fontSize; line.fontStyle = textComponent.fontStyle; line.alignment = textComponent.alignment; line.raycastTarget = false;
        line.text = "<alpha=#00>" + total + "\n<alpha=#FF>" + tips;
        line.color = level == TipLevel.Perfect ? TipGreen : level == TipLevel.Nice ? TipYellow : TipGrey;
        if (level == TipLevel.Nice) line.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, TipYellowDark);   // its own copy of the material
    }

    /// The level's tag on the paper's top right corner: a filled rounded label, a little turned.
    private void AddTipTag(TipLevel level)
    {
        if (level != TipLevel.Perfect && level != TipLevel.Nice) return;
        var perfect = level == TipLevel.Perfect;
        var go = new GameObject("TipTag", typeof(RectTransform), typeof(Image)); go.transform.SetParent(transform, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(perfect ? 122f : 88f, 46f); rt.anchoredPosition = new Vector2(12f, 4f); rt.localRotation = Quaternion.Euler(0f, 0f, -8f);
        var img = go.GetComponent<Image>(); img.sprite = TagSprite(); img.type = Image.Type.Sliced; img.color = perfect ? TipGreen : TipYellow; img.raycastTarget = false;
        var edge = go.AddComponent<Outline>(); edge.effectColor = perfect ? TipGreenDark : TipYellowDark; edge.effectDistance = new Vector2(2f, -2f);
        var word = new GameObject("Word", typeof(RectTransform)); word.transform.SetParent(rt, false);
        var wrt = (RectTransform)word.transform; wrt.anchorMin = Vector2.zero; wrt.anchorMax = Vector2.one; wrt.offsetMin = wrt.offsetMax = Vector2.zero;
        var t = word.AddComponent<TextMeshProUGUI>(); t.font = textComponent != null ? textComponent.font : null;
        var plain = Resources.Load<Material>("Fonts & Materials/beachday SDF - Plain"); if (plain != null && t.font != null && t.font.name.StartsWith("beachday")) t.fontSharedMaterial = plain;
        t.text = perfect ? "PERFECT!" : "NICE!"; t.fontSize = 25f; t.alignment = TextAlignmentOptions.Center; t.enableWordWrapping = false; t.raycastTarget = false;
        t.color = perfect ? new Color32(255, 250, 235, 255) : new Color32(110, 66, 6, 255);
    }

    private static Sprite tagSprite;
    private static Sprite TagSprite()
    {
        if (tagSprite != null) return tagSprite;
        const int S = 48, R = 14;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false); var px = new Color[S * S];
        for (var y = 0; y < S; y++)
            for (var x = 0; x < S; x++)
            {
                var cx = Mathf.Clamp(x, R, S - 1 - R); var cy = Mathf.Clamp(y, R, S - 1 - R);
                px[y * S + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(R - Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) + 0.5f));
            }
        tex.SetPixels(px); tex.Apply();
        tagSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
        return tagSprite;
    }

    public void ShowCombinedPopup(float basePoints, float bonusPoints = 0)
    {
        // Destroy previous popup if it exists
        if (currentPopup != null && currentPopup != this) Destroy(currentPopup.gameObject);

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
        var popupText = "";
        var textColor = itemPointsColor;

        if (bonusPoints > 0)
        {
            // Whole dollars in the one money look (26 November 2026); the amounts come rounded from ScoreManager
            popupText = " " + Money.Format(basePoints) + "\n Tips: " + Money.Format(bonusPoints);
            textColor = new Color(0.09f, 0.06f, 0.04f);   // money: black letters, cream rim from the font material (owner, 16 October)
        }
        else
        {
            popupText = Money.Format(basePoints);
            textColor = new Color(0.09f, 0.06f, 0.04f);   // money: black letters, cream rim from the font material (owner, 16 October)
        }

        // Set text content and color
        textComponent.text = popupText;
        textComponent.color = textColor;
        var level = NextTip; NextTip = TipLevel.None;
        if (bonusPoints > 0 && level != TipLevel.None) { ColourTips(basePoints, bonusPoints, level); AddTipTag(level); }

        // Store starting position
        startPosition = rectTransform.anchoredPosition;

        // Start initial animation
        StartCoroutine(InitialAnimation());
    }


    /// <summary>
    /// Initial popup animation - ends in visible state
    /// </summary>
    private IEnumerator InitialAnimation()
    {
        var elapsedTime = 0f;
        var targetPosition = startPosition + Vector3.up * moveUpDistance;

        // Set initial state
        rectTransform.localScale = Vector3.one * startScale;
        canvasGroup.alpha = 0f;

        while (elapsedTime < initialAnimationDuration)
        {
            var t = elapsedTime / initialAnimationDuration;

            // Move upward with easing
            var easedT = EaseOutCubic(t);
            rectTransform.anchoredPosition = Vector3.Lerp(startPosition, targetPosition, easedT);

            // Scale animation (grow to final size)
            var scale = Mathf.Lerp(startScale, endScale, easedT);
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
        if (autoDisappear && stayDuration > 0) StartCoroutine(AutoDisappearTimer());
    }

    /// <summary>
    /// Timer to automatically destroy popup after stay duration
    /// </summary>
    private IEnumerator AutoDisappearTimer()
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
        // ClearCurrentPopup() and the auto-disappear timer can both land on the same popup;
        // only one fade-out should run.
        if (isDestroying) return;
        isDestroying = true;

        if (!gameObject.activeInHierarchy)
        {
            // Can't run a coroutine on an inactive object (e.g. canvas hidden at level end)
            if (currentPopup == this) currentPopup = null;
            Destroy(gameObject);
            return;
        }

        // Quick fade out before destroying
        StartCoroutine(FadeOutAndDestroy());
    }

    /// <summary>
    /// Quick fade out animation before destruction
    /// </summary>
    private IEnumerator FadeOutAndDestroy()
    {
        var startAlpha = canvasGroup.alpha;
        var elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            var t = elapsedTime / fadeOutDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Clear reference if this was the current popup
        if (currentPopup == this) currentPopup = null;

        Destroy(gameObject);
    }

    /// <summary>
    /// Smooth easing function
    /// </summary>
    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    public static SimpleScorePopup CreateCombinedPopupWithFont(Transform parent, float basePoints,
        float bonusPoints = 0, TMP_FontAsset customFont = null, Sprite backgroundSprite = null)
    {
        // Create PARENT container (no graphic components)
        var popupObj = new GameObject("ScorePopup");
        popupObj.transform.SetParent(parent, false);

        var rectTransform = popupObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(300, 300);

        // Add popup script to parent
        // Add popup script to parent
        var popup = popupObj.AddComponent<SimpleScorePopup>();

        // Set position
        rectTransform.anchoredPosition = popup.defaultSpawnPosition + new Vector2(
            Random.Range(-popup.randomRange / 2f, popup.randomRange / 2f),
            Random.Range(-50f, 50f)
        );
        if (NextAtScreen.HasValue && parent is RectTransform parentRect)
        {
            var canvas = parentRect.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, NextAtScreen.Value, cam, out var local))
            {
                // the parent's pivot to its centre, where a new RectTransform's anchors are
                var r = parentRect.rect; local -= new Vector2(r.center.x, r.center.y);
                // the two places mirror each other about the screen's middle (owner, 18 November 2026: "position them
                // symmetrically on the left and right. don't go too much towards the edge"): the served side picks the
                // place; the receipt (300 wide at its full 1.2 size) stays 60 inside the edge
                var half = r.width / 2f; var receiptHalf = 150f * popup.endScale;
                var side = Mathf.Min(half * 0.36f, half - receiptHalf - 60f);
                local.x = Mathf.Sign(local.x == 0f ? 1f : local.x) * Mathf.Max(0f, side);
                // the height it always had (owner, 18 November 2026, after a test run: "the receipt is too low, use the
                // previous height"): it ends where the old receipt ended (620), starting a little under it so the rise shows.
                // At the bubble's own height it began 150 to 300 lower than that.
                local.y = popup.defaultSpawnPosition.y - 40f;
                rectTransform.anchoredPosition = local;
                popup.moveUpDistance = RiseOverCustomer; popup.stayDuration = StayOverCustomer;
            }
        }
        NextAtScreen = null;

        // The receipt restyled as a till receipt (owner, 19 November 2026: "restyle the receipt card too ... improve our
        // receipt using that style"; "don't put title text on receipt"): zigzag ends, dashed lines, no title (Resources/UI/receipt_paper, the same
        // shape and paper place as the white receipt it replaces; Store/ui/receipt_2026-11-19/build.py)
        var restyled = backgroundSprite != null ? Resources.Load<Sprite>("UI/receipt_paper") : null;
        if (restyled != null) backgroundSprite = restyled;

        // Create BACKGROUND child (separate GameObject)
        if (backgroundSprite != null)
        {
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(popupObj.transform, false);

            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgObj.AddComponent<Image>();
            bgImage.sprite = backgroundSprite;

            // ADD SHADOW COMPONENT
            var shadow = bgObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f); // Semi-transparent black
            shadow.effectDistance = new Vector2(15f, -15f); // Offset: right 5, down 5
            shadow.useGraphicAlpha = true; // Respect the image's alpha

            Debug.Log($"Added background image: {backgroundSprite.name}");
        }

        // Create TEXT child (separate GameObject)
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(popupObj.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Above the white receipt's barcode (paper 4.png; the owner went back to the white receipt on 16 October
        // after the gingham note of 14 October, which had the text at (0, -24))
        textRect.anchoredPosition = new Vector2(-5f, 20f);

        var textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.fontSize = popup.fontSize;
        textComponent.fontStyle = popup.fontStyle;
        textComponent.alignment = TextAlignmentOptions.Center;

        if (customFont != null)
            textComponent.font = customFont;
        else if (popup.popupFont != null) textComponent.font = popup.popupFont;

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
        if (currentPopup != null) currentPopup.DestroyPopup();
    }

    /// <summary>
    /// Check if there's currently a popup visible
    /// </summary>
    public static bool HasActivePopup()
    {
        return currentPopup != null;
    }

    private void OnDestroy()
    {
        // Clear reference if this was the current popup
        if (currentPopup == this) currentPopup = null;
    }
}