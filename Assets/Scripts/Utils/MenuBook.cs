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
    private const float AutoOpenDelay = 0.6f;          // seconds after the arrangement starts (was 2: owner, 14 November 2026, "open faster with less delay")
    private const float StickerSize = 225f;            // on the paper
    private const float CarouselSizeSmall = 280f;      // doubled (owner, 16 October: the icons were too small)
    /// Larger on phones (owner, 24 November 2026: "the selection of available stickers on top is a little small, I forgot
    /// it sometimes, make it larger"); an iPad keeps 280, its open paper reaches up to the strip.
    private static bool BigCarousel => Screen.width > 0 && Screen.height / (float)Screen.width >= 1.7f;
    /// Bigger again (owner, 24 November 2026: "make the menu selection on top even bigger"): as big as the room between
    /// the top buttons and the open paper allows on this screen, at most 440, never under 280. Worked out from the
    /// screen as the canvas scaler sees it (1080 x 1920, half width half height).
    private static float CarouselSize
    {
        get
        {
            if (!BigCarousel || Screen.width <= 0) return CarouselSizeSmall;
            var scale = Mathf.Sqrt(Screen.width / 1080f * (Screen.height / 1920f)); var canvasH = Screen.height / Mathf.Max(0.01f, scale);
            var room = canvasH / 2f - CarouselTop - (-170f + 565f) - 30f;   // from under the top buttons down to the open paper's top
            return Mathf.Clamp(room - 20f, CarouselSizeSmall, 440f);
        }
    }
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
        if (lessonBanner != null && !lessonRunning && (Input.GetMouseButtonDown(0) || Input.touchCount > 0)) EndLesson();   // the player's own finger: the banner goes, the touch still counts
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
        // the small menu wiggles now and then so it reads as tappable (owner, 25 October 2026)
        if (!wiggling && Time.unscaledTime >= nextWiggle) { nextWiggle = Time.unscaledTime + 3f; StartCoroutine(RockMini()); }
    }

    private bool wiggling; private float nextWiggle = 1.5f;

    /// A short rock about the small menu's centre: six degrees each way, dying out over half a second
    private IEnumerator RockMini()
    {
        wiggling = true; var t = 0f; var rt = panel != null ? (RectTransform)panel.transform : null;
        while (t < 0.5f && rt != null && !open && !animating)
        {
            t += Dt; rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 6f) * 6f * (1f - t / 0.5f));
            yield return null;
        }
        if (rt != null) rt.localRotation = Quaternion.identity;
        wiggling = false;
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
            EndLesson();
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
        if (SessionManager.Instance != null && SessionManager.Instance.MenuTutorialDue) lesson = StartCoroutine(Lesson());
    }

    /// Done: the paper shrinks back to its spot under the top buttons and stays there, small, until tapped.
    private IEnumerator Shrink()
    {
        EndLesson();
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
        crt.anchoredPosition = MiniCenter(); crt.localScale = Vector3.one * MiniScale; if (!wiggling) crt.localRotation = Quaternion.identity;
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
        if (phaseManager != null) phaseManager.ShowHint(show);   // the hint itself is the tutorial's only (18 November 2026)
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


    #region First-time lesson

    /// The menu's lesson (owner, 26 November 2026: "The first time we show the menu for player to choose recipes, add a
    /// tutorial step, Banner - 'You can remove or add items to today's menu', Then show an animation of removing a sticker
    /// and adding it back"). On the first open of the first menu a player meets (SessionManager.MenuTutorialDue, day 2):
    /// the tutorial's banner, then a hand taps a sticker's cross, the sticker peels off and flies to the strip of stickers
    /// to add, the hand taps it there and it flies back to its spot. Only pictures move: the save and the table are never
    /// touched, and the paper is laid out again from the save at the end. Touches wait during the demo (about four
    /// seconds); after it, the player's first touch takes the banner away (and still counts), as does Done.
    public const string LessonText = "You can remove or add items to today's menu";
    private Coroutine lesson;
    private bool lessonRunning;
    private GameObject lessonBanner, lessonBlocker, lessonFlier, lessonSlot;
    private RectTransform lessonHand;

    private bool LessonGone => !open || panel == null || !panel.activeSelf;

    private IEnumerator Lesson()
    {
        SessionManager.Instance.MarkMenuTutorialSeen();
        lessonRunning = true; busy = true;
        // touches wait while the pictures move (the Done button and the stickers lie under this)
        var block = NewImage("LessonBlocker", canvasRect, new Color(0f, 0f, 0f, 0f)); Stretch(block.rectTransform); block.raycastTarget = true;
        lessonBlocker = block.gameObject;
        ShowLessonBanner();
        for (var t = 0f; t < 1.0f; t += Dt) { if (LessonGone) { EndLesson(); yield break; } yield return null; }

        // a sticker of today's menu that may come off (not the chapter's fixed food)
        RectTransform sticker = null; string food = null; var sm = SessionManager.Instance;
        foreach (var pass in new[] { true, false })
        {
            if (levelManager != null && levelManager.serveableItems != null)
                foreach (var item in levelManager.serveableItems)
                {
                    if (item == null) continue; var f = item.GetFoodType();
                    if (!placed.TryGetValue(f, out var rt) || rt == null || SessionManager.IsFixedFood(f)) continue;
                    if (pass && !sm.CanDisableFood(f, out _)) continue;
                    sticker = rt; food = f; break;
                }
            if (sticker != null) break;
        }
        if (sticker == null) { StopLessonDemo(); yield break; }   // nothing to take off: the banner alone
        var cross = sticker.Find("Remove") as RectTransform;
        var wig = sticker.GetComponent<Wiggle>(); if (wig != null) wig.enabled = false;

        // the hand (the tutorial's pointing cookie hand, fingertip at its bottom middle) comes to the cross
        var handSprite = Resources.Load<Sprite>("Tutorial/pointer_hand"); if (handSprite == null) handSprite = Resources.Load<Sprite>("Tutorial/arrow");
        var hi = NewImage("LessonHand", canvasRect, Color.white); hi.sprite = handSprite; hi.preserveAspect = true; hi.raycastTarget = false;
        lessonHand = hi.rectTransform; lessonHand.anchorMin = lessonHand.anchorMax = new Vector2(0.5f, 0.5f); lessonHand.pivot = new Vector2(0.5f, 0f);
        lessonHand.sizeDelta = new Vector2(170f, 170f); lessonHand.SetAsLastSibling();
        GlowFx.Shine(hi);
        var crossAt = Local(cross != null ? cross : sticker);
        yield return MoveHand(crossAt + new Vector2(140f, -260f), crossAt, 0.5f, true);
        if (LessonGone) { EndLesson(); yield break; }
        yield return PressHand(crossAt);
        if (LessonGone) { EndLesson(); yield break; }

        // peeling off: the corner lifts (as a real tap does), then the sticker flies to the strip at the top
        var startRot = sticker.eulerAngles.z;
        for (var t = 0f; t < 0.12f; t += Dt)
        {
            var k = Mathf.Clamp01(t / 0.12f);
            sticker.localScale = Vector3.one * (1f + 0.12f * k); sticker.localRotation = Quaternion.Euler(0, 0, startRot + 8f * k);
            yield return null;
        }
        if (LessonGone) { EndLesson(); yield break; }
        var sprite = sticker.GetComponent<Image>().sprite;
        var paperRot = sticker.eulerAngles.z; var paperSize = sticker.rect.size * (sticker.lossyScale.x / canvasRect.lossyScale.x);
        var paperPos = sticker.position;
        sticker.gameObject.SetActive(false);
        // its place in the strip, as Refresh would lay it out (in the foods' order), empty until the sticker lands
        var slot = NewImage("LessonSlot_" + food, carouselContent, new Color(1f, 1f, 1f, 0f)); slot.sprite = sprite; slot.preserveAspect = true; slot.raycastTarget = false;
        var srt = slot.rectTransform; srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f); srt.sizeDelta = new Vector2(CarouselSize, CarouselSize);
        srt.localRotation = Quaternion.Euler(0, 0, (StableHash(food) % 9 - 4) * 1.5f);
        srt.SetSiblingIndex(StripIndex(food));
        lessonSlot = slot.gameObject;
        LayoutRebuilder.ForceRebuildLayoutImmediate(carouselContent);
        yield return null;
        var stripRot = srt.eulerAngles.z; var stripSize = srt.rect.size * (srt.lossyScale.x / canvasRect.lossyScale.x);
        var handFrom = lessonHand.anchoredPosition;
        yield return Fly(sprite, paperPos, paperSize, paperRot, srt, stripSize, stripRot, handFrom);
        if (LessonGone) { EndLesson(); yield break; }
        slot.color = Color.white;
        yield return Pop(srt, 1.08f);
        for (var t = 0f; t < 0.25f; t += Dt) { if (LessonGone) { EndLesson(); yield break; } yield return null; }

        // the hand taps it in the strip and it goes back onto the paper, pressed on
        var stripAt = Local(srt) + new Vector2(0f, -CarouselSize * 0.15f);
        yield return MoveHand(lessonHand.anchoredPosition, stripAt, 0.35f, false);
        if (LessonGone) { EndLesson(); yield break; }
        yield return PressHand(stripAt);
        if (LessonGone) { EndLesson(); yield break; }
        slot.color = new Color(1f, 1f, 1f, 0f);
        var target = NewImage("LessonTarget", board, new Color(1f, 1f, 1f, 0f)).rectTransform;   // the paper spot, as a world point
        target.anchorMin = target.anchorMax = new Vector2(0.5f, 0.5f); target.position = paperPos; target.sizeDelta = Vector2.one;
        yield return Fly(sprite, srt.position, stripSize, stripRot, target, paperSize / 1.12f, startRot, lessonHand.anchoredPosition);
        Destroy(target.gameObject);
        if (LessonGone) { EndLesson(); yield break; }
        Destroy(lessonSlot); lessonSlot = null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(carouselContent);
        sticker.localScale = Vector3.one; sticker.localRotation = Quaternion.Euler(0, 0, startRot);
        sticker.gameObject.SetActive(true);
        StartCoroutine(Pop(sticker, 1.12f)); PlayPlacedSound(food);
        // the hand leaves
        var img = lessonHand.GetComponent<Image>();
        for (var t = 0f; t < 0.35f; t += Dt)
        {
            if (lessonHand == null) break;
            var k = t / 0.35f; img.color = new Color(1f, 1f, 1f, 1f - k);
            lessonHand.anchoredPosition += new Vector2(0f, -220f * Dt);
            yield return null;
        }
        StopLessonDemo();
    }

    /// The demo is over: the paper again exactly as the save says, touches back; the banner stays until a touch.
    private void StopLessonDemo()
    {
        if (lessonFlier != null) Destroy(lessonFlier); if (lessonSlot != null) Destroy(lessonSlot); if (lessonBlocker != null) Destroy(lessonBlocker);
        if (lessonHand != null) Destroy(lessonHand.gameObject);
        lessonFlier = lessonSlot = lessonBlocker = null; lessonHand = null;
        if (lessonRunning) { lessonRunning = false; busy = false; if (board != null && open) Refresh(null); }
        lesson = null;
    }

    /// Everything of the lesson away (a touch after the demo, Done, the day starting).
    private void EndLesson()
    {
        if (lesson != null) StopCoroutine(lesson);
        lesson = null;
        StopLessonDemo();
        if (lessonBanner != null) Destroy(lessonBanner);
        lessonBanner = null;
    }

    /// The tutorial's banner as the tutorials draw it (TutorialDirector.Say: the gingham card, brown hand letters,
    /// a second line makes it taller), low on the screen under the paper, where nothing of the menu is; on a screen
    /// with no room there (an iPad) it lies over the paper's foot.
    private void ShowLessonBanner()
    {
        // the taped note every tutorial banner is (TapedNote, owner 27 November 2026)
        var box = NewImage("LessonBanner", canvasRect, Color.white);
        box.raycastTarget = false;
        var brt = box.rectTransform; brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f); brt.pivot = new Vector2(0.5f, 0f);
        brt.sizeDelta = new Vector2(940f, 210f);
        var caption = NewText("Caption", brt, font, 60f, TapedNote.Ink);
        var note = TapedNote.Dress(box, caption, 60f); TapedNote.FitWords(caption.rectTransform);
        caption.text = LessonText;
        caption.ForceMeshUpdate();
        var lines = Mathf.Max(1, caption.textInfo.lineCount);
        brt.sizeDelta = new Vector2(940f, 210f + (lines - 1) * 70f);
        // under the paper, above the phone's home bar; where there is no room for it there (an iPad), on the paper's
        // empty lower half just above Done: it used to be squeezed under the paper and lay over Done
        var half = canvasRect.rect.height / 2f;
        var safeBottom = Screen.height > 0 ? Screen.safeArea.yMin / Screen.height * canvasRect.rect.height : 0f;
        var floor = -half + safeBottom + 18f;
        var paperY = ((RectTransform)panel.transform).anchoredPosition.y; var paperFoot = paperY - 1130f / 2f;
        var room = paperFoot - 10f - floor; var scale = 1f; var y = floor;
        if (room >= brt.sizeDelta.y * 0.8f) scale = Mathf.Min(1f, room / brt.sizeDelta.y);
        else y = paperY - 300f;                                                          // Done's top is 315 under the paper's middle
        brt.localScale = Vector3.one * scale;
        brt.anchoredPosition = new Vector2(0f, y);
        lessonBanner = box.gameObject;
        if (note != null) note.Appear();
    }

    /// The sticker's place in the strip when it is off: after the stickers of foods that come before it.
    private int StripIndex(string food)
    {
        var order = new List<string>();
        if (levelManager != null && levelManager.serveableItems != null) foreach (var item in levelManager.serveableItems) if (item != null && !order.Contains(item.GetFoodType())) order.Add(item.GetFoodType());
        var mine = order.IndexOf(food); var index = 0;
        for (var i = 0; i < carouselContent.childCount; i++)
        {
            var n = carouselContent.GetChild(i).name; if (!n.StartsWith("Sticker_")) continue;
            if (order.IndexOf(n.Substring(8)) < mine) index = i + 1;
        }
        return index;
    }

    /// A point of the canvas (centre-anchored units) for a rect anywhere on this canvas.
    private Vector2 Local(RectTransform rt)
    {
        var sp = RectTransformUtility.WorldToScreenPoint(null, rt.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out var local);
        return local;
    }

    private IEnumerator MoveHand(Vector2 from, Vector2 to, float seconds, bool fadeIn)
    {
        var img = lessonHand.GetComponent<Image>();
        for (var t = 0f; t < seconds; t += Dt)
        {
            if (lessonHand == null || LessonGone) yield break;
            var k = Mathf.SmoothStep(0f, 1f, t / seconds);
            lessonHand.anchoredPosition = Vector2.Lerp(from, to, k);
            if (fadeIn) img.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t / (seconds * 0.6f)));
            yield return null;
        }
        if (lessonHand != null) { lessonHand.anchoredPosition = to; img.color = Color.white; }
    }

    /// A tap: the hand dips onto the point, presses, and lifts.
    private IEnumerator PressHand(Vector2 at)
    {
        for (var t = 0f; t < 0.32f; t += Dt)
        {
            if (lessonHand == null || LessonGone) yield break;
            var k = t / 0.32f; var dip = Mathf.Sin(k * Mathf.PI);
            lessonHand.anchoredPosition = at + new Vector2(0f, -22f * dip);
            lessonHand.localScale = Vector3.one * (1f - 0.1f * dip);
            yield return null;
        }
        if (lessonHand != null) { lessonHand.anchoredPosition = at; lessonHand.localScale = Vector3.one; }
    }

    /// One flight as Move draws it (the hop, the grow in the middle, the turn), a little slower for the eye; the hand rides along.
    private IEnumerator Fly(Sprite sprite, Vector3 fromPos, Vector2 fromSize, float fromRot, RectTransform to, Vector2 toSize, float toRot, Vector2 handFrom)
    {
        var flier = NewImage("LessonFlier", canvasRect, Color.white); flier.sprite = sprite; flier.preserveAspect = true; flier.raycastTarget = false;
        lessonFlier = flier.gameObject;
        var frt = flier.rectTransform; frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.5f);
        frt.SetSiblingIndex(lessonHand != null ? lessonHand.GetSiblingIndex() : canvasRect.childCount - 1);   // under the hand
        const float seconds = 0.55f;
        for (var time = 0f; time < seconds; time += Dt)
        {
            if (LessonGone || to == null) yield break;
            var k = Mathf.Clamp01(time / seconds); var e = 1f - (1f - k) * (1f - k);
            frt.position = Vector3.Lerp(fromPos, to.position, e) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 60f * canvasRect.lossyScale.y);
            frt.sizeDelta = Vector2.Lerp(fromSize, toSize, e) * (1f + 0.15f * Mathf.Sin(k * Mathf.PI));
            frt.rotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(fromRot, toRot, e));
            if (lessonHand != null && k > 0.25f)   // the hand lets go first, then drifts after the sticker
            {
                var hk = Mathf.SmoothStep(0f, 1f, (k - 0.25f) / 0.75f);
                lessonHand.anchoredPosition = Vector2.Lerp(handFrom, Local(frt) + new Vector2(0f, -toSize.y * 0.15f), hk * 0.5f);
            }
            yield return null;
        }
        Destroy(lessonFlier); lessonFlier = null;
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


        // the cookie slab of the arrangement's READY button with DONE on it (owner, 25 October 2026); the plain yellow
        // rectangle with a text label stays as the fallback when the picture is missing
        var doneSprite = Resources.Load<Sprite>("UI/btn_done");
        var done = NewImage("DoneButton", crt, doneSprite != null ? Color.white : new Color(0.98f, 0.78f, 0.36f));
        if (doneSprite != null) { done.sprite = doneSprite; done.preserveAspect = true; Place(done.rectTransform, 0f, -415f, 300f, 200f); }
        else
        {
            Place(done.rectTransform, 0f, -415f, 280f, 100f);
            var dt = NewText("Label", done.rectTransform, font, 50f, Brown); dt.text = "DONE"; dt.alignment = TextAlignmentOptions.Center; Stretch(dt.rectTransform);
        }
        done.gameObject.AddComponent<Button>().onClick.AddListener(Close);
        doneButton = done.gameObject;

        // The carousel: under the settings and pause buttons, the stickers that are off, scrolled sideways
        var strip = NewImage("StickerCarousel", canvasRect, new Color(1f, 1f, 1f, 0f));   // no background (owner)
        carousel = strip.gameObject;
        var srt = strip.rectTransform; srt.anchorMin = new Vector2(0f, 1f); srt.anchorMax = new Vector2(1f, 1f); srt.pivot = new Vector2(0.5f, 1f);
        srt.anchoredPosition = new Vector2(0f, -CarouselTop); srt.sizeDelta = new Vector2(-40f, CarouselSize + 20f);
        strip.gameObject.AddComponent<RectMask2D>();
        var scroll = strip.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = true; scroll.vertical = false; scroll.movementType = ScrollRect.MovementType.Elastic;
        var content = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(srt, false);
        carouselContent = (RectTransform)content.transform;
        carouselContent.anchorMin = new Vector2(0f, 0f); carouselContent.anchorMax = new Vector2(0f, 1f); carouselContent.pivot = new Vector2(0f, 0.5f);
        carouselContent.offsetMin = Vector2.zero; carouselContent.offsetMax = Vector2.zero;
        var hl = content.GetComponent<HorizontalLayoutGroup>(); hl.spacing = -90f * CarouselSize / CarouselSizeSmall;   // a little overlap, like a pile of stickers hl.padding = new RectOffset(24, 24, 20, 20); hl.childAlignment = TextAnchor.MiddleLeft;
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
                if (item != null && OwnsFood(sm, item.GetFoodType()) && !foods.Contains(item.GetFoodType()))
                    foods.Add(item.GetFoodType());

        // Paper slots (three across): a food the player has placed keeps its spot; a food without one (just bought) takes the
        // first slot no placed sticker sits on, so it never lands on top of another (owner, 25 October 2026: new stickers
        // appeared in the first spot over what was there)
        var taken = new HashSet<int>();
        foreach (var f in foods)
            if (sm.IsFoodItemEnabled(f) && SessionManager.IsFoodInSeason(f) && sm.TryGetMenuStickerSpot(f, out var sp))
                taken.Add(Mathf.Clamp(Mathf.RoundToInt(sp.x / 255f) + 1, 0, 2) + 3 * Mathf.Clamp(Mathf.RoundToInt((245f - sp.y) / 250f), 0, 9));
        var nextFree = 0;
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
                var hasSpot = sm.TryGetMenuStickerSpot(food, out var spot);
                var slot = 0;
                if (!hasSpot) { while (taken.Contains(nextFree)) nextFree++; slot = nextFree; taken.Add(slot); }
                var col = slot % 3; var row = slot / 3;
                var x = (col - 1) * 255f + (float)(rng.NextDouble() * 40 - 20);
                var y = 245f - row * 250f + (float)(rng.NextDouble() * 30 - 15);
                var rot = (float)(rng.NextDouble() * 8 - 4);   // a small tilt only; the sprites are drawn straight (owner, 16 October)
                var st = MakeSticker(food, board, StickerSize);
                if (hasSpot) { x = spot.x; y = spot.y; }   // where the player left it
                st.anchoredPosition = new Vector2(x, y); st.localRotation = Quaternion.Euler(0, 0, rot);
                st.gameObject.AddComponent<StickerDrag>().Init(this, food, board);
                if (!SessionManager.IsFixedFood(food)) AddCross(st, food);   // the chapter's fixed food has no cross: it stays (owner, 16 October)
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

    /// Bought (or the chapter's fixed food), in season or not: IsFoodItemPurchased says no to an out-of-season food, which
    /// hid it from the carousel instead of greying it (21 October).
    private static bool OwnsFood(SessionManager sm, string food) =>
        SessionManager.IsFixedFood(food) || (sm.GetCurrentSession()?.purchasedFoodItems?.Contains(food) ?? false) || (SessionManager.IsFoodInSeason(food) && sm.IsFoodItemPurchased(food));

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
        var wig = img.gameObject.AddComponent<Wiggle>(); wig.phase = StableHash(food) % 628 / 100f;
        if (parent == carouselContent) { wig.amp = 5f; wig.pulse = 0.05f; }   // the stickers to add wiggle clearly (owner, 24 November 2026)
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
        public float phase, amp = 2.2f, pulse;   // degrees; pulse: a soft grow-and-shrink share (0 = none)
        private float baseRot; private bool started; private Vector3 baseScale;
        private void OnEnable() { started = false; }
        private void LateUpdate()
        {
            if (!started) { baseRot = transform.localEulerAngles.z; if (baseRot > 180f) baseRot -= 360f; baseScale = transform.localScale; started = true; }
            var t = Time.unscaledTime * 2.2f + phase;
            transform.localRotation = Quaternion.Euler(0, 0, baseRot + Mathf.Sin(t) * amp + Mathf.Sin(t * 2.7f) * amp * 0.27f);
            if (pulse > 0f) transform.localScale = baseScale * (1f + pulse * Mathf.Sin(t * 1.6f));
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
