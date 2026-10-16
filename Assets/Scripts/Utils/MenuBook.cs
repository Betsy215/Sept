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
/// seconds into the arrangement and can be dragged anywhere. Rules (station caps, at least two on) live in
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
    private static readonly Vector2 BookSpot = new Vector2(80f, -385f);   // the small book, under the sticker line
    private static readonly Vector2 BookSize = new Vector2(116f, 146f);   // the cover's shape (900 x 1130), small
    private const float ZoomSeconds = 0.32f;
    private const float FlipSeconds = 0.3f;
    private static readonly Color Brown = new Color(0.36f, 0.22f, 0.09f);
    private static readonly Color Cream = new Color(1f, 0.98f, 0.93f);

    private LevelManager levelManager;
    private GamePhaseManager phaseManager;
    private TMP_FontAsset font;
    private Canvas canvas;
    private RectTransform canvasRect;
    private GameObject button;                          // the small closed book, there during the arrangement
    private RectTransform hinge;                        // the cover that zooms in and swings open (spine on the left)
    private Image coverImage;
    private Coroutine bookMotion;
    private GameObject panel;                           // the paper (draggable)
    private RectTransform board;                        // where the stickers sit on the paper
    private GameObject carousel;
    private RectTransform carouselContent;
    private readonly Dictionary<string, RectTransform> placed = new Dictionary<string, RectTransform>();
    private bool busy;                                  // a sticker is flying
    private bool autoOpened;

    public bool IsOpen => panel != null && panel.activeSelf;

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
        var arranging = Arranging;
        // The small book is only there during the arrangement, never during the day (owner, 16 October)
        if (button != null)
        {
            var show = arranging && !IsOpen && !animating;
            if (button.activeSelf != show) { button.SetActive(show); if (show && !justClosed) StartCoroutine(Pop(button.transform, 0.6f)); }
            justClosed = false;
        }
        if (!arranging && (IsOpen || animating)) Close();
    }
    private bool justClosed;

    #region Open, close, switch

    public void Open()
    {
        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession()) return;
        if (IsOpen || animating) return;
        if (panel == null) BuildPanel();
        autoOpened = true;
        Refresh(null);
        ShowArrangeHint(false);
        bookMotion = StartCoroutine(OpenBook());
    }

    public void Close()
    {
        if (!Arranging)
        {
            // The day started (or the scene is leaving): no motion, just put everything away
            if (bookMotion != null) StopCoroutine(bookMotion);
            bookMotion = null; animating = false;
            if (hinge != null) hinge.gameObject.SetActive(false);
            if (panel != null) panel.SetActive(false);
            if (carousel != null) carousel.SetActive(false);
            busy = false;
            return;
        }
        if (!IsOpen || animating) return;
        busy = false;
        bookMotion = StartCoroutine(CloseBook());
    }

    /// The small book zooms up to the paper's place, then its cover swings open on the spine (owner, 16 October).
    private IEnumerator OpenBook()
    {
        animating = true; busy = true;
        button.SetActive(false);
        var paper = (RectTransform)panel.transform;
        SetCover(SmallBookCenter(), BookSize, 0f);
        var target = paper.anchoredPosition;
        var size = paper.rect.size;
        for (var t = 0f; t < ZoomSeconds; t += Dt)
        {
            var k = t / ZoomSeconds; var e = 1f - (1f - k) * (1f - k) * (1f - k);
            SetCover(Vector2.LerpUnclamped(SmallBookCenter(), target, e), Vector2.LerpUnclamped(BookSize, size, e), 0f);
            hinge.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * Mathf.PI) * 4f);
            yield return null;
        }
        SetCover(target, size, 0f);
        panel.SetActive(true);
        carousel.SetActive(true);
        hinge.SetAsLastSibling();
        for (var t = 0f; t < FlipSeconds; t += Dt)
        {
            var k = t / FlipSeconds;
            SetCover(target, size, 90f * k * k);                             // swings faster as it falls open
            yield return null;
        }
        hinge.gameObject.SetActive(false);
        StartCoroutine(Pop(carousel.transform, 0.85f));
        animating = false; busy = false; bookMotion = null;
    }

    /// Done: the cover swings shut over the paper, then the book shrinks back to its spot under the sticker line.
    private IEnumerator CloseBook()
    {
        animating = true; busy = true;
        var paper = (RectTransform)panel.transform;
        var from = paper.anchoredPosition;
        var size = paper.rect.size;
        carousel.SetActive(false);
        SetCover(from, size, 90f);
        hinge.SetAsLastSibling();
        for (var t = 0f; t < FlipSeconds; t += Dt)
        {
            var k = 1f - t / FlipSeconds;
            SetCover(from, size, 90f * (1f - (1f - k) * (1f - k)));
            yield return null;
        }
        SetCover(from, size, 0f);
        panel.SetActive(false);
        for (var t = 0f; t < ZoomSeconds; t += Dt)
        {
            var k = t / ZoomSeconds; var e = k * k * (3f - 2f * k);
            SetCover(Vector2.LerpUnclamped(from, SmallBookCenter(), e), Vector2.LerpUnclamped(size, BookSize, e), 0f);
            hinge.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Sin(k * Mathf.PI) * 4f);
            yield return null;
        }
        hinge.gameObject.SetActive(false);
        justClosed = true;
        animating = false; busy = false; bookMotion = null;
        ShowArrangeHint(true);
        button.SetActive(true);
        StartCoroutine(Pop(button.transform, 1.15f));                       // lands with a little bounce
    }

    /// The small book's centre in the canvas's centre-anchored units.
    private Vector2 SmallBookCenter()
    {
        var size = canvasRect.rect.size;
        return new Vector2(-size.x / 2f + BookSpot.x, size.y / 2f + BookSpot.y);
    }

    /// Places the cover by its centre and size, opened by angle degrees round the spine (0 = shut, 90 = edge on).
    private void SetCover(Vector2 center, Vector2 size, float angle)
    {
        if (hinge == null) BuildCover();
        hinge.gameObject.SetActive(true);
        hinge.anchoredPosition = center - new Vector2(size.x / 2f, 0f);
        coverImage.rectTransform.sizeDelta = size;
        coverImage.rectTransform.anchoredPosition = new Vector2(size.x / 2f, 0f);
        hinge.localRotation = Quaternion.identity;
        hinge.localScale = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), 1f, 1f);
        var shade = Mathf.Lerp(1f, 0.72f, angle / 90f);                  // the cover darkens as it turns away
        coverImage.color = new Color(shade, shade, shade, 1f);
    }

    private void BuildCover()
    {
        var h = new GameObject("BookCover", typeof(RectTransform)); h.transform.SetParent(canvasRect, false);
        hinge = (RectTransform)h.transform; hinge.anchorMin = hinge.anchorMax = new Vector2(0.5f, 0.5f); hinge.pivot = new Vector2(0f, 0.5f);
        hinge.sizeDelta = Vector2.zero;
        coverImage = NewImage("Cover", hinge, Color.white);
        var s = Resources.Load<Sprite>("UI/menu_cover"); if (s != null) coverImage.sprite = s; else coverImage.color = new Color(0.36f, 0.57f, 0.83f);
        coverImage.raycastTarget = true;                                      // swallows taps while it moves
        var crt = coverImage.rectTransform; crt.anchorMin = crt.anchorMax = new Vector2(0f, 0.5f); crt.pivot = new Vector2(0.5f, 0.5f);
        h.SetActive(false);
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
            if (!wasOn) StartCoroutine(Pop(to, 1.12f));                     // pressed onto the paper
        }
        busy = false;
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
        // The small closed menu book (owner, 16 October): under the sticker line at the top left, only during the
        // arrangement; a tap zooms it in and opens it
        var img = NewImage("MenuBookButton", canvasRect, Color.white);
        var sprite = Resources.Load<Sprite>("UI/menu_cover");
        if (sprite != null) img.sprite = sprite; else img.color = new Color(0.36f, 0.57f, 0.83f);
        img.preserveAspect = true;
        var rt = img.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = BookSpot; rt.sizeDelta = BookSize;
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
        Place(crt, 0f, -170f, 900f, 1130f);   // hugs the 3 x 3 board of nine stickers
        panel.AddComponent<DragMove>().Init(crt, canvasRect);

        var title = NewText("Title", crt, font, 72f, Brown); title.text = "MENU"; title.alignment = TextAlignmentOptions.Center;
        Place(title.rectTransform, 0f, 425f, 800f, 90f);

        var b = new GameObject("Board", typeof(RectTransform)); b.transform.SetParent(crt, false);
        board = (RectTransform)b.transform; Place(board, 0f, 5f, 760f, 720f);


        var done = NewImage("DoneButton", crt, new Color(0.98f, 0.78f, 0.36f));
        Place(done.rectTransform, 0f, -415f, 280f, 100f);
        var dt = NewText("Label", done.rectTransform, font, 50f, Brown); dt.text = "DONE"; dt.alignment = TextAlignmentOptions.Center; Stretch(dt.rectTransform);
        done.gameObject.AddComponent<Button>().onClick.AddListener(Close);

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

    private RectTransform MakeSticker(string food, RectTransform parent, float size)
    {
        var img = NewImage("Sticker_" + food, parent, Color.white);
        var s = Resources.Load<Sprite>("UI/stickers/" + food);
        if (s != null) img.sprite = s; else img.color = Cream;
        img.preserveAspect = true;
        var rt = img.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(size, size);
        if (s == null)
        {
            var t = NewText("Name", rt, font, 36f, Brown); t.text = food; t.alignment = TextAlignmentOptions.Center; Stretch(t.rectTransform);
        }
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
