using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The menu book (owner, 14 October 2026): during the arrangement phase the player chooses which bought foods are in
/// play. The menu is a paper with stickers on it; a food that is on is a sticker stuck on the paper (a little tilted,
/// a little off its spot, like a real sticker), a food that is off waits in a carousel at the top of the screen.
/// Tap a sticker on the paper to peel it off, tap one in the carousel to stick it on. The paper opens by itself two
/// seconds into the arrangement and can be dragged anywhere. When closed it stays on screen as a small version of
/// itself, centred under the top buttons; a tap grows it back (owner, 16 October). Rules (station caps, at least two on) live in
/// SessionManager; a refused sticker shakes. No words on the paper but MENU and DONE (owner, 14 October). Never on the Day 0 tutorial. Sticker art: Resources/UI/stickers/food.
/// </summary>
public class MenuBook : MonoBehaviour
{
    private const int SortOrder = 40;                  // under the tutorial canvas (50), over the arrangement UI (0)
    private const float AutoOpenDelay = 2f;            // seconds after the arrangement starts (owner)
    private const float StickerSize = 225f;            // on the paper
    private const float CarouselSize = 140f;
    private const float FlySeconds = 0.38f;
    private const float CarouselTop = 170f;            // under the settings and pause buttons
    private static readonly Vector2 MiniSpot = new Vector2(0f, -328f);   // the small menu's centre: centred, under the top buttons
    private const float MiniScale = 0.18f;             // the small menu is the paper itself, scaled (162 x 203)
    private const float ZoomSeconds = 0.34f;
    private static readonly Color Brown = new Color(0.8f, 0.47f, 0.16f);   // button letters, orange like READY (owner, 16 October)
    private static readonly Color Ink = new Color(0.09f, 0.06f, 0.04f);     // titles: black with the cream rim
    private static readonly Color Cream = new Color(1f, 0.98f, 0.93f);

    private LevelManager levelManager;
    private GamePhaseManager phaseManager;
    private TMP_FontAsset font;
    private Canvas canvas;
    private RectTransform canvasRect;
    private GameObject button;                          // a clear tap catcher over the small menu
    private CanvasGroup group;                          // the paper's taps and drags, off while it is small
    private GameObject doneButton;                      // hidden while the paper is small
    private Vector2 openPos = new Vector2(0f, -170f);   // where the paper sits when open (the player may drag it)
    private bool open;
    private Coroutine motion;
    private GameObject panel;                           // the paper (draggable)
    private RectTransform board;                        // where the stickers sit on the paper
    private GameObject carousel;
    private RectTransform carouselContent;
    private readonly Dictionary<string, RectTransform> placed = new Dictionary<string, RectTransform>();
    private bool busy;                                  // a sticker is flying
    private bool autoOpened;

    public bool IsOpen => open;

    /// Real seconds per frame, or the recorder's fixed step while a clip is being captured (Time.captureFramerate).
    private static float Dt => Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;
    private bool animating;

    public void Begin(LevelManager lm, GamePhaseManager gpm)
    {
        levelManager = lm;
        phaseManager = gpm;
        font = Resources.Load<TMP_FontAsset>("Fonts & Materials/beachday SDF");
        BuildCanvas();
        BuildButton();
        StartCoroutine(AutoOpen());
    }

    private bool Arranging => phaseManager != null && phaseManager.GetCurrentPhase() == GamePhase.ARRANGEMENT
                              && phaseManager.arrangmentUI != null && phaseManager.arrangmentUI.activeInHierarchy;

    private IEnumerator AutoOpen()
    {
        var t = 0f;
        while (t < AutoOpenDelay)
        {
            if (Arranging) t += Dt; else t = 0f;
            yield return null;
        }
        if (!autoOpened && Arranging) Open();
    }

    private void Update()
    {
        if (!Arranging)
        {
            if (open || animating || (panel != null && panel.activeSelf)) Close();
            return;
        }
        // During the arrangement the menu is always on screen: full size, or small under the top buttons when
        // closed (owner, 16 October: the small menu is just a small version of the menu)
        if (open || animating) return;
        if (panel == null) { BuildPanel(); Refresh(null); }
        if (!panel.activeSelf)
        {
            Refresh(null);
            panel.SetActive(true);
            ShowMini(true);
            PlaceMini();
        }
        PlaceMini();                                                         // follows the screen size (iPad)
        if (!button.activeSelf) button.SetActive(true);
    }

    #region Open, close, switch

    public void Open()
    {
        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession()) return;
        if (open || animating) return;
        if (panel == null) BuildPanel();
        autoOpened = true;
        Refresh(null);
        ShowArrangeHint(false);
        motion = StartCoroutine(Grow());
    }

    public void Close()
    {
        if (!Arranging)
        {
            // The day started (or the scene is leaving): no motion, just put everything away
            if (motion != null) StopCoroutine(motion);
            motion = null; animating = false; open = false;
            if (panel != null) { panel.SetActive(false); var crt = (RectTransform)panel.transform; crt.localScale = Vector3.one; crt.localRotation = Quaternion.identity; }
            if (carousel != null) carousel.SetActive(false);
            if (button != null) button.SetActive(false);
            busy = false;
            return;
        }
        if (!open || animating) return;
        busy = false;
        motion = StartCoroutine(Shrink());
    }

    /// The small menu grows up to its place and comes alive.
    private IEnumerator Grow()
    {
        animating = true; busy = true;
        button.SetActive(false);
        var crt = (RectTransform)panel.transform;
        panel.SetActive(true);
        ShowMini(true);
        var from = MiniCenter(); var to = openPos;
        for (var t = 0f; t < ZoomSeconds; t += Dt)
        {
            var k = t / ZoomSeconds; var e = 1f - (1f - k) * (1f - k) * (1f - k);
            crt.anchoredPosition = Vector2.LerpUnclamped(from, to, e);
            crt.localScale = Vector3.one * Mathf.LerpUnclamped(MiniScale, 1f, e);
            crt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI) * 3f);
            yield return null;
        }
        crt.anchoredPosition = to; crt.localScale = Vector3.one; crt.localRotation = Quaternion.identity;
        open = true;
        ShowMini(false);
        carousel.SetActive(true);
        StartCoroutine(Pop(carousel.transform, 0.85f));
        animating = false; busy = false; motion = null;
    }

    /// Done: the paper shrinks back to its spot under the top buttons and stays there, small, until tapped.
    private IEnumerator Shrink()
    {
        animating = true; busy = true;
        var crt = (RectTransform)panel.transform;
        carousel.SetActive(false);
        openPos = crt.anchoredPosition;                                       // where the player left it
        open = false;
        ShowMini(true);
        var from = openPos; var to = MiniCenter();
        for (var t = 0f; t < ZoomSeconds; t += Dt)
        {
            var k = t / ZoomSeconds; var e = k * k * (3f - 2f * k);
            crt.anchoredPosition = Vector2.LerpUnclamped(from, to, e);
            crt.localScale = Vector3.one * Mathf.LerpUnclamped(1f, MiniScale, e);
            crt.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Sin(k * Mathf.PI) * 3f);
            yield return null;
        }
        PlaceMini();
        animating = false; busy = false; motion = null;
        ShowArrangeHint(true);
        button.SetActive(true);
    }

    /// The small menu's centre in the canvas's centre-anchored units: under the top buttons, and just above the
    /// "Drag to arrange" card wherever that sits (it is higher on an iPad).
    private Vector2 MiniCenter()
    {
        var size = canvasRect.rect.size;
        var y = size.y / 2f + MiniSpot.y;
        var half = 1130f * MiniScale / 2f;
        var cardTop = float.NegativeInfinity;
        if (phaseManager != null)
        {
            var cards = new[] { phaseManager.tutorialPanel != null ? phaseManager.tutorialPanel.transform as RectTransform : null,
                                phaseManager.arrangmentUI != null ? phaseManager.arrangmentUI.transform.Find("HintCard") as RectTransform : null };
            foreach (var card in cards)
            {
                if (card == null) continue;
                var corners = new Vector3[4]; card.GetWorldCorners(corners);
                var c = card.GetComponentInParent<Canvas>(); var ucam = c != null && c.renderMode != RenderMode.ScreenSpaceOverlay ? c.worldCamera : null;
                var sp = RectTransformUtility.WorldToScreenPoint(ucam, corners[1]);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out var local)) cardTop = Mathf.Max(cardTop, local.y);
            }
        }
        if (!float.IsInfinity(cardTop)) y = Mathf.Max(y, cardTop + half + 14f);   // the card is higher on an iPad
        y = Mathf.Min(y, size.y / 2f - half - 20f);                                   // never off the top
        return new Vector2(MiniSpot.x, y);
    }

    private void PlaceMini()
    {
        var crt = (RectTransform)panel.transform;
        crt.anchoredPosition = MiniCenter(); crt.localScale = Vector3.one * MiniScale; crt.localRotation = Quaternion.identity;
    }

    /// Small: nothing on the paper answers taps or drags, Done is hidden, and the clear tap catcher sits on top.
    private void ShowMini(bool mini)
    {
        if (group != null) { group.interactable = !mini; group.blocksRaycasts = !mini; }
        if (doneButton != null) doneButton.SetActive(!mini);
        if (mini && button != null) button.transform.SetAsLastSibling();
    }

    /// The arrangement's "Drag to arrange" card would sit over the paper's top: hidden while the menu is open.
    private void ShowArrangeHint(bool show)
    {
        if (phaseManager == null) return;
        if (phaseManager.tutorialPanel != null) phaseManager.tutorialPanel.SetActive(show);
        if (phaseManager.tutorialText != null) phaseManager.tutorialText.gameObject.SetActive(show);
        var card = phaseManager.arrangmentUI != null ? phaseManager.arrangmentUI.transform.Find("HintCard") : null;
        if (card != null) card.gameObject.SetActive(show);
    }

    /// Switch a food without the motion (tests). False when the rules refused.
    public bool Toggle(string foodType)
    {
        var ok = Apply(foodType);
        Refresh(null);
        return ok;
    }

    /// Switch a food with the motion, as a tap would (tests).
    public bool TapSticker(string foodType)
    {
        if (!IsOpen) return false;
        var home = FindHome(foodType, SessionManager.Instance.IsFoodItemEnabled(foodType));
        if (home == null) return false;
        OnStickerTapped(foodType, home);
        return true;
    }

    private bool Apply(string foodType)
    {
        var sm = SessionManager.Instance; if (sm == null) return false;
        var ok = sm.SetFoodEnabled(foodType, !sm.IsFoodItemEnabled(foodType));
        if (ok && levelManager != null) levelManager.ReapplyServeableItems();
        return ok;
    }

    /// A tap on a sticker: peel it off the paper or stick it on, with the flight in between.
    private void OnStickerTapped(string food, RectTransform from)
    {
        if (busy) return;
        var sm = SessionManager.Instance; if (sm == null) return;
        var on = sm.IsFoodItemEnabled(food);
        string why;
        if (!(on ? sm.CanDisableFood(food, out why) : sm.CanEnableFood(food, out why)))
        {
            StartCoroutine(Shake(from));
            return;
        }
        StartCoroutine(Move(food, from, on));
    }

    private IEnumerator Move(string food, RectTransform from, bool wasOn)
    {
        busy = true;
        var wig = from.GetComponent<Wiggle>(); if (wig != null) wig.enabled = false;
        var sprite = from.GetComponent<Image>().sprite;
        var startRot = from.eulerAngles.z;
        var startSize = from.rect.size * (from.lossyScale.x / canvasRect.lossyScale.x);

        // Peeling off: the corner lifts first (a little bigger, a little turned)
        if (wasOn)
        {
            var t = 0f;
            while (t < 0.12f)
            {
                t += Dt; var k = Mathf.Clamp01(t / 0.12f);
                from.localScale = Vector3.one * (1f + 0.12f * k); from.localRotation = Quaternion.Euler(0, 0, startRot + 8f * k);
                yield return null;
            }
            startRot = from.eulerAngles.z; startSize *= 1.12f;
        }
        var startPos = from.position;

        Apply(food);
        Refresh(food);                                                     // the new home exists but is hidden
        yield return null;                                                 // let the carousel lay itself out
        var to = FindHome(food, !wasOn);
        if (to == null) { Refresh(null); busy = false; yield break; }

        var flier = NewImage("Flying_" + food, canvasRect, Color.white); flier.sprite = sprite; flier.preserveAspect = true; flier.raycastTarget = false;
        var frt = flier.rectTransform; frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f); frt.SetAsLastSibling();
        var endSize = to.rect.size * (to.lossyScale.x / canvasRect.lossyScale.x); var endRot = to.eulerAngles.z;
        var time = 0f;
        while (time < FlySeconds)
        {
            time += Dt;
            var k = Mathf.Clamp01(time / FlySeconds); var e = 1f - (1f - k) * (1f - k);   // ease out
            frt.position = Vector3.Lerp(startPos, to.position, e) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 60f * canvasRect.lossyScale.y);
            frt.sizeDelta = Vector2.Lerp(startSize, endSize, e) * (1f + 0.15f * Mathf.Sin(k * Mathf.PI));
            frt.rotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(startRot, endRot, e));
            yield return null;
        }
        Destroy(flier.gameObject);
        if (to != null)
        {
            var img = to.GetComponent<Image>(); if (img != null) img.color = Color.white;
            if (!wasOn) { StartCoroutine(Pop(to, 1.12f)); PlayPlacedSound(food); }   // pressed onto the paper
        }
        busy = false;
    }

    /// The item's own serve sound when its sticker lands on the paper (owner, 16 October: a "placed" sound; foods
    /// without a serve sound stay silent for now). The table itself changed the moment the sticker was tapped.
    private void PlayPlacedSound(string food)
    {
        if (levelManager == null || levelManager.serveableItems == null || AudioManager.Instance == null) return;
        foreach (var item in levelManager.serveableItems)
            if (item != null && item.GetFoodType() == food && item.serveSound != null) { AudioManager.Instance.PlaySFX(item.serveSound); return; }
    }

    #endregion

    #region Building

    private void BuildCanvas()
    {
        var go = new GameObject("MenuBookCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortOrder;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect = go.GetComponent<RectTransform>();
    }

    private void BuildButton()
    {
        // A clear tap catcher the size of the small menu (owner, 16 October): a tap on the small menu opens it
        var img = NewImage("MenuTapCatcher", canvasRect, new Color(0f, 0f, 0f, 0f));
        var rt = img.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = MiniSpot; rt.sizeDelta = new Vector2(900f * MiniScale, 1130f * MiniScale);
        img.gameObject.AddComponent<Button>().onClick.AddListener(Open);
        button = img.gameObject;
        button.SetActive(false);
    }

    private void BuildPanel()
    {
        // The paper: the gingham recipe card at the size the owner liked, below the carousel
        var card = NewImage("MenuPaper", canvasRect, Color.white);
        // Cream paper with a hand-drawn blue band on all four edges, a line then a dotted line (owner, 14 October)
        var cardSprite = Resources.Load<Sprite>("UI/menu_paper");
        if (cardSprite != null) card.sprite = cardSprite; else card.color = Cream;
        panel = card.gameObject;
        var crt = card.rectTransform;
        Place(crt, openPos.x, openPos.y, 900f, 1130f);   // hugs the 3 x 3 board of nine stickers
        panel.AddComponent<DragMove>().Init(crt, canvasRect);
        group = panel.AddComponent<CanvasGroup>();

        var title = NewText("Title", crt, font, 72f, Ink); title.text = "MENU"; title.alignment = TextAlignmentOptions.Center;
        Place(title.rectTransform, 0f, 425f, 800f, 90f);

        var b = new GameObject("Board", typeof(RectTransform)); b.transform.SetParent(crt, false);
        board = (RectTransform)b.transform; Place(board, 0f, 5f, 760f, 720f);


        var done = NewImage("DoneButton", crt, new Color(0.98f, 0.78f, 0.36f));
        Place(done.rectTransform, 0f, -415f, 280f, 100f);
        var dt = NewText("Label", done.rectTransform, font, 50f, Brown); dt.text = "DONE"; dt.alignment = TextAlignmentOptions.Center; Stretch(dt.rectTransform);
        done.gameObject.AddComponent<Button>().onClick.AddListener(Close);
        doneButton = done.gameObject;

        // The carousel: under the settings and pause buttons, the stickers that are off, scrolled sideways
        var strip = NewImage("StickerCarousel", canvasRect, new Color(1f, 1f, 1f, 0f));   // no background (owner)
        carousel = strip.gameObject;
        var srt = strip.rectTransform; srt.anchorMin = new Vector2(0f, 1f); srt.anchorMax = new Vector2(1f, 1f); srt.pivot = new Vector2(0.5f, 1f);
        srt.anchoredPosition = new Vector2(0f, -CarouselTop); srt.sizeDelta = new Vector2(-40f, 160f);
        strip.gameObject.AddComponent<RectMask2D>();
        var scroll = strip.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = true; scroll.vertical = false; scroll.movementType = ScrollRect.MovementType.Elastic;
        var content = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(srt, false);
        carouselContent = (RectTransform)content.transform;
        carouselContent.anchorMin = new Vector2(0f, 0f); carouselContent.anchorMax = new Vector2(0f, 1f); carouselContent.pivot = new Vector2(0f, 0.5f);
        carouselContent.offsetMin = Vector2.zero; carouselContent.offsetMax = Vector2.zero;
        var hl = content.GetComponent<HorizontalLayoutGroup>(); hl.spacing = -45f;   // a little overlap, like a pile of stickers hl.padding = new RectOffset(24, 24, 20, 20); hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = false; hl.childControlHeight = false; hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = carouselContent; scroll.viewport = srt;

        panel.SetActive(false);
        carousel.SetActive(false);
    }

    /// Lays out the paper and the carousel from the save. hidden = a food whose new home stays invisible until its
    /// flying sticker lands there.
    private void Refresh(string hidden)
    {
        if (board == null) return;
        var sm = SessionManager.Instance; if (sm == null) return;
        for (var i = board.childCount - 1; i >= 0; i--) DestroyImmediate(board.GetChild(i).gameObject);
        for (var i = carouselContent.childCount - 1; i >= 0; i--) DestroyImmediate(carouselContent.GetChild(i).gameObject);
        placed.Clear();

        var foods = new List<string>();
        if (levelManager != null && levelManager.serveableItems != null)
            foreach (var item in levelManager.serveableItems)
                if (item != null && sm.IsFoodItemPurchased(item.GetFoodType()) && !foods.Contains(item.GetFoodType()))
                    foods.Add(item.GetFoodType());

        var offCount = 0;
        for (var i = 0; i < foods.Count; i++)
        {
            var food = foods[i];
            if (!SessionManager.IsFoodInSeason(food))
            {
                // Another chapter's food (the apple on a beach day): greyed in the carousel, never on the paper, no tap.
                // The save keeps it as it was, so it comes back by itself on its own chapter's days (owner, 16 October).
                offCount++;
                var st = MakeSticker(food, carouselContent, CarouselSize, true);
                st.localRotation = Quaternion.Euler(0, 0, (StableHash(food) % 9 - 4) * 1.5f);
                continue;
            }
            if (sm.IsFoodItemEnabled(food))
            {
                // Its own spot on the paper (three across), nudged and tilted the same way every time
                var rng = new System.Random(StableHash(food));
                var col = i % 3; var row = i / 3;
                var x = (col - 1) * 255f + (float)(rng.NextDouble() * 40 - 20);
                var y = 245f - row * 250f + (float)(rng.NextDouble() * 30 - 15);
                var rot = (float)(rng.NextDouble() * 18 - 9);
                var st = MakeSticker(food, board, StickerSize);
                if (sm.TryGetMenuStickerSpot(food, out var spot)) { x = spot.x; y = spot.y; }   // where the player left it
                st.anchoredPosition = new Vector2(x, y); st.localRotation = Quaternion.Euler(0, 0, rot);
                st.gameObject.AddComponent<StickerDrag>().Init(this, food, board);
                AddCross(st, food);
                placed[food] = st;
                if (food == hidden) st.GetComponent<Image>().color = new Color(1, 1, 1, 0);
            }
            else
            {
                offCount++;
                var st = MakeSticker(food, carouselContent, CarouselSize);
                st.localRotation = Quaternion.Euler(0, 0, (StableHash(food) % 9 - 4) * 1.5f);
                if (food == hidden) st.GetComponent<Image>().color = new Color(1, 1, 1, 0);
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(carouselContent);
    }

    private static int StableHash(string s)
    {
        unchecked { var h = 17; foreach (var c in s) h = h * 31 + c; return h & 0x7fffffff; }
    }

    /// The sticker paper's colour says the food's chapter (owner, 16 October): yellow for a food of every chapter,
    /// mint for chapter 1's own (the apple, the coffee), blue for chapter 2's (coconut, ice cream). No words about it in the game.
    /// Used for a food without a painted sticker; the painted stickers carry their paper colour themselves.
    public static Color PaperColour(string food)
    {
        switch (SessionManager.ChapterOfFood(food))
        {
            case 1: return new Color(0.78f, 0.93f, 0.85f);
            case 2: return new Color(0.77f, 0.86f, 0.96f);
            default: return new Color(0.99f, 0.91f, 0.67f);
        }
    }

    private RectTransform MakeSticker(string food, RectTransform parent, float size, bool greyed = false)
    {
        var img = NewImage("Sticker_" + food, parent, Color.white);
        var s = Resources.Load<Sprite>("UI/stickers/" + food);
        if (s != null) img.sprite = s; else img.color = PaperColour(food);
        if (greyed) img.color = new Color(0.55f, 0.55f, 0.55f, 0.7f);
        img.preserveAspect = true;
        var rt = img.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(size, size);
        if (s == null)
        {
            var t = NewText("Name", rt, font, 36f, Brown); t.text = food; t.alignment = TextAlignmentOptions.Center; Stretch(t.rectTransform);
        }
        if (greyed) return rt;
        if (parent != board) img.gameObject.AddComponent<Button>().onClick.AddListener(() => OnStickerTapped(food, rt));
        img.gameObject.AddComponent<Wiggle>().phase = StableHash(food) % 628 / 100f;
        return rt;
    }

    /// A red cross on the sticker's corner (owner, 14 October: a quick way to take items off when the table is too
    /// full). Tapping it peels the sticker off into the carousel.
    private void AddCross(RectTransform sticker, string food)
    {
        var cross = NewImage("Remove", sticker, new Color(0.86f, 0.24f, 0.2f));
        var spr = Resources.Load<Sprite>("UI/cross"); if (spr != null) { cross.sprite = spr; cross.color = Color.white; }
        var crt = cross.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(1f, 1f); crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = new Vector2(-34f, -34f); crt.sizeDelta = new Vector2(60f, 60f);
        if (spr == null)
        {
            var t = NewText("X", crt, font, 46f, Color.white); t.text = "x"; t.alignment = TextAlignmentOptions.Center; Stretch(t.rectTransform);
        }
        cross.gameObject.AddComponent<Button>().onClick.AddListener(() => OnStickerTapped(food, sticker));
    }

    private RectTransform FindHome(string food, bool onPaper)
    {
        if (onPaper) return placed.TryGetValue(food, out var rt) ? rt : null;
        return carouselContent != null ? carouselContent.Find("Sticker_" + food) as RectTransform : null;
    }

    #endregion

    #region Little motions

    private IEnumerator Pop(Transform t, float from)
    {
        var time = 0f;
        while (time < 0.18f && t != null)
        {
            time += Dt; var k = Mathf.Clamp01(time / 0.18f);
            t.localScale = Vector3.one * Mathf.LerpUnclamped(from, 1f, 1f - (1f - k) * (1f - k));
            yield return null;
        }
        if (t != null) t.localScale = Vector3.one;
    }

    private IEnumerator Shake(RectTransform t)
    {
        var start = t.anchoredPosition; var time = 0f;
        while (time < 0.3f && t != null) { time += Dt; t.anchoredPosition = start + new Vector2(Mathf.Sin(time * 60f) * 12f * (1f - time / 0.3f), 0f); yield return null; }
        if (t != null) t.anchoredPosition = start;
    }

    #endregion

    /// Drags a sticker around the paper (owner, 14 October: anywhere, overlapping is fine). It comes to the front, lifts
    /// a little while held, and its spot is saved. A drag never counts as a tap, so it does not peel the sticker off.
    public class StickerDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private MenuBook book; private string food; private RectTransform rt, area;
        public void Init(MenuBook b, string f, RectTransform board) { book = b; food = f; area = board; rt = (RectTransform)transform; }
        public void OnBeginDrag(PointerEventData e) { if (book.busy) return; rt.SetAsLastSibling(); rt.localScale = Vector3.one * 1.08f; }
        public void OnDrag(PointerEventData e)
        {
            if (book.busy) return;
            rt.position += (Vector3)e.delta;
            var half = area.rect.size / 2f;
            var p = rt.anchoredPosition; rt.anchoredPosition = new Vector2(Mathf.Clamp(p.x, -half.x, half.x), Mathf.Clamp(p.y, -half.y, half.y));
        }
        public void OnEndDrag(PointerEventData e)
        {
            rt.localScale = Vector3.one;
            if (SessionManager.Instance != null) SessionManager.Instance.SetMenuStickerSpot(food, rt.anchoredPosition);
        }

        /// Tests and recordings: a smooth drag by delta (paper units) over some frames.
        public IEnumerator DragBy(Vector2 delta, int frames)
        {
            var e = new PointerEventData(EventSystem.current);
            OnBeginDrag(e);
            for (var i = 0; i < frames; i++) { e.delta = delta * (area.lossyScale.x / frames); OnDrag(e); yield return null; }
            OnEndDrag(e);
        }
    }

    /// A sticker's gentle idle wiggle (owner, 14 October): a small rock round its own tilt, each on its own beat.
    public class Wiggle : MonoBehaviour
    {
        public float phase;
        private float baseRot; private bool started;
        private void OnEnable() { started = false; }
        private void LateUpdate()
        {
            if (!started) { baseRot = transform.localEulerAngles.z; if (baseRot > 180f) baseRot -= 360f; started = true; }
            var t = Time.unscaledTime * 2.2f + phase;
            transform.localRotation = Quaternion.Euler(0, 0, baseRot + Mathf.Sin(t) * 2.2f + Mathf.Sin(t * 2.7f) * 0.6f);
        }
    }

    /// Drags the paper with a finger anywhere on it (taps on stickers still go to the stickers).
    public class DragMove : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private RectTransform rt, area;
        public void Init(RectTransform target, RectTransform canvasArea) { rt = target; area = canvasArea; }
        public void OnBeginDrag(PointerEventData e) { }
        public void OnDrag(PointerEventData e)
        {
            rt.position += (Vector3)e.delta;
            // keep at least a third of the paper on screen
            var half = area.rect.size / 2f; var p = rt.anchoredPosition; var s = rt.rect.size / 3f;
            rt.anchoredPosition = new Vector2(Mathf.Clamp(p.x, -half.x - s.x, half.x + s.x), Mathf.Clamp(p.y, -half.y - s.y, half.y + s.y));
        }
    }

    #region UI helpers

    private static Image NewImage(string name, RectTransform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    private static TextMeshProUGUI NewText(string name, RectTransform parent, TMP_FontAsset font, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    #endregion
}
