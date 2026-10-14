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
/// SessionManager; a refused sticker shakes. Never on the Day 0 tutorial. Sticker art: Resources/UI/stickers/food.
/// </summary>
public class MenuBook : MonoBehaviour
{
    private const int SortOrder = 40;                  // under the tutorial canvas (50), over the arrangement UI (0)
    private const float AutoOpenDelay = 2f;            // seconds after the arrangement starts (owner)
    private const float StickerSize = 240f;            // on the paper
    private const float CarouselSize = 190f;
    private const float FlySeconds = 0.38f;
    private static readonly Color Brown = new Color(0.36f, 0.22f, 0.09f);
    private static readonly Color Cream = new Color(1f, 0.98f, 0.93f);

    private LevelManager levelManager;
    private GamePhaseManager phaseManager;
    private TMP_FontAsset font;
    private Canvas canvas;
    private RectTransform canvasRect;
    private GameObject button;
    private GameObject panel;                           // the paper (draggable)
    private RectTransform board;                        // where the stickers sit on the paper
    private GameObject carousel;
    private RectTransform carouselContent;
    private TextMeshProUGUI counters, note;
    private readonly Dictionary<string, RectTransform> placed = new Dictionary<string, RectTransform>();
    private bool busy;                                  // a sticker is flying
    private bool autoOpened;

    public bool IsOpen => panel != null && panel.activeSelf;

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
            if (Arranging) t += Time.unscaledDeltaTime; else t = 0f;
            yield return null;
        }
        if (!autoOpened && Arranging) Open();
    }

    private void Update()
    {
        var arranging = Arranging;
        if (button != null && button.activeSelf != arranging) button.SetActive(arranging);
        if (!arranging && IsOpen) Close();
    }

    #region Open, close, switch

    public void Open()
    {
        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession()) return;
        if (panel == null) BuildPanel();
        autoOpened = true;
        Refresh(null);
        panel.SetActive(true);
        carousel.SetActive(true);
        ShowArrangeHint(false);
        StartCoroutine(Pop(panel.transform, 0.92f));
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        if (carousel != null) carousel.SetActive(false);
        if (Arranging) ShowArrangeHint(true);
        busy = false;
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
            ShowNote(on ? "Keep at least " + SessionManager.MinEnabledFoods + " on the menu" : SessionManager.StationOf(food) + " is full: take one off first");
            StartCoroutine(Shake(from));
            return;
        }
        StartCoroutine(Move(food, from, on));
    }

    private IEnumerator Move(string food, RectTransform from, bool wasOn)
    {
        busy = true;
        var sprite = from.GetComponent<Image>().sprite;
        var startRot = from.eulerAngles.z;
        var startSize = from.rect.size * (from.lossyScale.x / canvasRect.lossyScale.x);

        // Peeling off: the corner lifts first (a little bigger, a little turned)
        if (wasOn)
        {
            var t = 0f;
            while (t < 0.12f)
            {
                t += Time.unscaledDeltaTime; var k = Mathf.Clamp01(t / 0.12f);
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
            time += Time.unscaledDeltaTime;
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
        var img = NewImage("MenuBookButton", canvasRect, Color.white);
        var sprite = Resources.Load<Sprite>("UI/menu_book");
        if (sprite != null) img.sprite = sprite; else img.color = Cream;
        img.preserveAspect = true;
        Place(img.rectTransform, -250f, 0f, 150f, 150f);
        img.gameObject.AddComponent<Button>().onClick.AddListener(() => { if (IsOpen) Close(); else Open(); });
        button = img.gameObject;
        button.SetActive(false);
    }

    private void BuildPanel()
    {
        // The paper: the gingham recipe card at the size the owner liked, below the carousel
        var card = NewImage("MenuPaper", canvasRect, Color.white);
        var cardSprite = Resources.Load<Sprite>("Tutorial/card");
        if (cardSprite != null) { card.sprite = cardSprite; card.type = Image.Type.Sliced; } else card.color = Cream;
        panel = card.gameObject;
        var crt = card.rectTransform;
        Place(crt, 0f, -190f, 940f, 1320f);
        panel.AddComponent<DragMove>().Init(crt, canvasRect);

        var title = NewText("Title", crt, font, 72f, Brown); title.text = "MENU"; title.alignment = TextAlignmentOptions.Center;
        Place(title.rectTransform, 0f, 570f, 800f, 90f);
        var hint = NewText("Hint", crt, font, 34f, new Color(0.5f, 0.38f, 0.26f)); hint.text = "Tap a sticker to take it off";
        hint.alignment = TextAlignmentOptions.Center; Place(hint.rectTransform, 0f, 505f, 800f, 50f);

        var b = new GameObject("Board", typeof(RectTransform)); b.transform.SetParent(crt, false);
        board = (RectTransform)b.transform; Place(board, 0f, 30f, 860f, 880f);

        note = NewText("Note", crt, font, 34f, new Color(0.75f, 0.25f, 0.2f)); note.alignment = TextAlignmentOptions.Center;
        Place(note.rectTransform, 0f, -455f, 860f, 50f);
        counters = NewText("Counters", crt, font, 38f, Brown); counters.alignment = TextAlignmentOptions.Center;
        Place(counters.rectTransform, 0f, -505f, 860f, 50f);

        var done = NewImage("DoneButton", crt, new Color(0.98f, 0.78f, 0.36f));
        Place(done.rectTransform, 0f, -585f, 280f, 100f);
        var dt = NewText("Label", done.rectTransform, font, 50f, Brown); dt.text = "DONE"; dt.alignment = TextAlignmentOptions.Center; Stretch(dt.rectTransform);
        done.gameObject.AddComponent<Button>().onClick.AddListener(Close);

        // The carousel: under the settings and pause buttons, the stickers that are off, scrolled sideways
        var strip = NewImage("StickerCarousel", canvasRect, new Color(1f, 0.97f, 0.9f, 0.85f));
        carousel = strip.gameObject;
        var srt = strip.rectTransform; srt.anchorMin = new Vector2(0f, 1f); srt.anchorMax = new Vector2(1f, 1f); srt.pivot = new Vector2(0.5f, 1f);
        srt.anchoredPosition = new Vector2(0f, -215f); srt.sizeDelta = new Vector2(-40f, 230f);
        strip.gameObject.AddComponent<RectMask2D>();
        var scroll = strip.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = true; scroll.vertical = false; scroll.movementType = ScrollRect.MovementType.Elastic;
        var content = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(srt, false);
        carouselContent = (RectTransform)content.transform;
        carouselContent.anchorMin = new Vector2(0f, 0f); carouselContent.anchorMax = new Vector2(0f, 1f); carouselContent.pivot = new Vector2(0f, 0.5f);
        carouselContent.offsetMin = Vector2.zero; carouselContent.offsetMax = Vector2.zero;
        var hl = content.GetComponent<HorizontalLayoutGroup>(); hl.spacing = 24f; hl.padding = new RectOffset(24, 24, 20, 20); hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = false; hl.childControlHeight = false; hl.childForceExpandWidth = false; hl.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = carouselContent; scroll.viewport = srt;
        var empty = NewText("Empty", srt, font, 34f, new Color(0.5f, 0.38f, 0.26f)); empty.text = "Every sticker is on the menu"; empty.alignment = TextAlignmentOptions.Center; Stretch(empty.rectTransform);

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
                var x = (col - 1) * 285f + (float)(rng.NextDouble() * 50 - 25);
                var y = 300f - row * 290f + (float)(rng.NextDouble() * 40 - 20);
                var rot = (float)(rng.NextDouble() * 18 - 9);
                var st = MakeSticker(food, board, StickerSize);
                st.anchoredPosition = new Vector2(x, y); st.localRotation = Quaternion.Euler(0, 0, rot);
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
        var empty = carousel.transform.Find("Empty"); if (empty != null) empty.gameObject.SetActive(offCount == 0);
        LayoutRebuilder.ForceRebuildLayoutImmediate(carouselContent);

        counters.text = $"Oven {sm.EnabledCountAtStation("Oven")}/{SessionManager.CapOf("Oven")}    " +
                        $"Machine {sm.EnabledCountAtStation("Machine")}/{SessionManager.CapOf("Machine")}    " +
                        $"Counter {sm.EnabledCountAtStation("Counter")}/{SessionManager.CapOf("Counter")}";
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
        img.gameObject.AddComponent<Button>().onClick.AddListener(() => OnStickerTapped(food, rt));
        return rt;
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
            time += Time.unscaledDeltaTime; var k = Mathf.Clamp01(time / 0.18f);
            t.localScale = Vector3.one * Mathf.LerpUnclamped(from, 1f, 1f - (1f - k) * (1f - k));
            yield return null;
        }
        if (t != null) t.localScale = Vector3.one;
    }

    private IEnumerator Shake(RectTransform t)
    {
        var start = t.anchoredPosition; var time = 0f;
        while (time < 0.3f && t != null) { time += Time.unscaledDeltaTime; t.anchoredPosition = start + new Vector2(Mathf.Sin(time * 60f) * 12f * (1f - time / 0.3f), 0f); yield return null; }
        if (t != null) t.anchoredPosition = start;
    }

    private Coroutine noteClear;

    private void ShowNote(string text)
    {
        if (note == null) return;
        if (noteClear != null) StopCoroutine(noteClear);
        note.text = text; noteClear = StartCoroutine(ClearNote());
    }

    private IEnumerator ClearNote() { yield return new WaitForSecondsRealtime(2.5f); if (note != null) note.text = ""; }

    #endregion

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
