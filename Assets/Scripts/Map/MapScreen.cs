using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Chapter map (10 October). Chapter 1 is Scorpio: 16 days on the constellation's stars, joined by thin
/// lines, on one screen over the night view of the shop. Played days and today can be tapped: the map dims
/// and the pause board shows "Day X", its stars, Play and Cancel. Home goes to the main menu. The right
/// arrow to chapter 2 is shown locked.
/// Flow: Play/Continue on the main menu, Next on the finish board and the pause menu's home all open this
/// scene. Play here starts the chosen day; after a finished day (CameFromDay) it shows the ad, then the Shop,
/// whose Next starts the chosen day.
/// Built in code on its own canvas; pictures are in Resources/Map (drawn by Store/artkit/map_assets.py).
/// </summary>
public class MapScreen : MonoBehaviour
{
    public const string SceneName = "Map";
    public const int ChapterDays = 16;
    public static bool CameFromDay;
    /// The page shown (0 = chapter 1); chosen from today's chapter when the scene opens, moved by the arrows
    private int page;

    public Sprite truckSprite, boardSprite, starSprite, darkStarSprite, homeSprite;

    // Scorpio in the 1080x1920 design space (origin top left): claws top right, tail curling bottom left
    private static readonly Vector2[] Stars =
    {
        new Vector2(640, 730), new Vector2(720, 620), new Vector2(790, 520), new Vector2(880, 435),
        new Vector2(965, 530), new Vector2(800, 710), new Vector2(700, 830), new Vector2(615, 950),
        new Vector2(545, 1090), new Vector2(505, 1230), new Vector2(470, 1380), new Vector2(380, 1510),
        new Vector2(250, 1530), new Vector2(165, 1430), new Vector2(205, 1315), new Vector2(300, 1275)
    };

    // Chapter 2's constellation (16 October: Cancer, the crab, until the owner picks; Store/chapter2/map/map_ideas.py),
    // already in the 1080x1920 design space, drawn without the chapter 1 enlargement
    private static readonly Vector2[] StarsCh2 =
    {
        new Vector2(300, 1480), new Vector2(390, 1380), new Vector2(470, 1260), new Vector2(520, 1130), new Vector2(560, 1000), new Vector2(600, 880), new Vector2(660, 770), new Vector2(760, 700), new Vector2(860, 650), new Vector2(760, 560), new Vector2(650, 520), new Vector2(560, 450), new Vector2(480, 560), new Vector2(420, 680), new Vector2(360, 800), new Vector2(300, 920)
    };
    private static Vector2[] StarsOf(int page) => page == 0 ? Stars : StarsCh2;

    private RectTransform canvasRect;
    private TMP_FontAsset font;
    private GameObject popup;
    private bool leaving;

    private void Start()
    {
        Time.timeScale = 1f;
        font = Resources.Load<TMP_FontAsset>("Fonts & Materials/beachday SDF");
        var sm0 = SessionManager.Instance;
        var done0 = sm0 != null ? sm0.GetLevelsCompleted() : 0;
        page = Mathf.Clamp(done0 / ChapterDays, 0, Chapter.Count - 1);   // the page with today's day
        Build();
    }

    private void ShowPage(int p)
    {
        if (popup != null || leaving) return;
        page = Mathf.Clamp(p, 0, Chapter.Count - 1);
        if (canvasRect != null) Destroy(canvasRect.gameObject);
        Build();
    }

    private static Sprite Load(string n) => Resources.Load<Sprite>("Map/" + n);
    // The constellation is drawn 15% larger and sparser around its centre (owner, 12 October); the design
    // points above are the original 10 October layout
    private const float MapScale = 1.15f;
    private static readonly Vector2 MapCentre = new Vector2(565f, 982f);
    private Vector2 P(Vector2 design)
    {
        var d = page == 0 ? MapCentre + (design - MapCentre) * MapScale : design;
        return new Vector2(d.x - 540f, 960f - d.y);
    }

    private void Build()
    {
        var go = new GameObject("MapCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand; // never smaller than 1080 x 1920 in either axis: on an iPad the title and the coins pill stayed clear of the days (review, 12 October)
        canvasRect = go.GetComponent<RectTransform>();

        // Background: the night shop (chapter 1) or the night beach (chapter 2), covering the screen
        var bg = NewImage("Background", canvasRect, Load("bg_ch" + (page + 1)) ?? Load("bg_ch1"));
        bg.rectTransform.anchorMin = bg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = bg.sprite != null ? bg.sprite.rect.width / bg.sprite.rect.height : 0.55f;

        var sm = SessionManager.Instance;
        var Stars = StarsOf(page);
        var first = page * ChapterDays;                           // this page's first day index
        var completed = Mathf.Clamp((sm != null ? sm.GetLevelsCompleted() : 0) - first, 0, ChapterDays);   // days done on this page
        var todayHere = sm == null || (sm.GetLevelsCompleted() / ChapterDays) == page;   // today's day is on this page
        var today = Mathf.Min(completed, ChapterDays - 1);      // the next day to play
        var allDone = completed >= ChapterDays;

        // Lines between the stars: bright up to today, faint after
        for (var i = 0; i < Stars.Length - 1; i++)
        {
            var a = P(Stars[i]); var b = P(Stars[i + 1]);
            var line = NewImage("Line" + (i + 1), canvasRect, null);
            line.color = i < today || allDone ? new Color(1f, 0.965f, 0.86f, 1f) : new Color(0.9f, 0.88f, 0.82f, 0.55f);
            var rt = line.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(Vector2.Distance(a, b), i < today || allDone ? 6f : 4f);
            rt.anchoredPosition = (a + b) / 2f;
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
            line.raycastTarget = false;
        }

        // Days
        for (var i = 0; i < Stars.Length; i++)
        {
            var played = i < completed;
            var isToday = todayHere && !allDone && i == today;   // a page ahead of today has no today (21 October)
            var pos = P(Stars[i]);
            var size = isToday ? 104f : 84f;
            var dayStars = played && sm != null ? sm.GetLevelStars(first + i) : 0;
            if (played && dayStars >= 3)
            {
                // Three stars: the day lights up like a star, a warm glow pulsing behind the circle (no yellow circle)
                var glow = NewImage("Glow", canvasRect, GlowSprite());
                Place(glow.rectTransform, pos, new Vector2(size * 2.6f, size * 2.6f));
                glow.color = new Color(1f, 0.9f, 0.55f, 0.9f);
                glow.raycastTarget = false;
                glow.gameObject.AddComponent<MapGlowPulse>();
            }
            var dot = NewImage("Day" + (i + 1), canvasRect, Load(isToday ? "day_today" : played ? "day_done" : "day_locked"));
            if (played && dayStars >= 3) dot.color = new Color(1f, 0.98f, 0.9f);
            Place(dot.rectTransform, pos, new Vector2(size, size));
            if (isToday) dot.gameObject.AddComponent<MapTodayBounce>();   // today's circle bounces (owner, 13 October)
            var label = NewText("Number", dot.rectTransform, (first + i + 1).ToString(), isToday ? 38f : 30f,
                isToday ? new Color(1f, 0.98f, 0.92f) : played ? new Color(0.43f, 0.24f, 0.08f) : new Color(0.47f, 0.43f, 0.39f));
            Stretch(label.rectTransform);
            label.rectTransform.offsetMax = new Vector2(0, 4f);

            if (played)
            {
                var stars = dayStars;
                for (var k = 0; k < 3; k++)
                {
                    var st = NewImage("Star", canvasRect, k < stars ? starSprite : darkStarSprite);
                    // under the circle, clear of its edge (circle radius 42, star 30)
                    Place(st.rectTransform, pos + new Vector2(-32f + k * 32f, -68f + (k == 1 ? 6f : 0f)), new Vector2(30f, 30f));
                    st.raycastTarget = false;
                }
            }
            if (played || isToday)
            {
                var day = first + i;
                var btn = dot.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => OpenDay(day));
            }
            else dot.raycastTarget = false;
        }

        // The truck beside today's star (beside day 16 once the chapter is done); on a page ahead of today there is no truck
        if (truckSprite != null && (todayHere || allDone))
        {
            var at = P(Stars[allDone ? ChapterDays - 1 : today]);
            var truck = NewImage("Truck", canvasRect, truckSprite);
            var h = 120f; var w = h * truckSprite.rect.width / truckSprite.rect.height;
            // beside today's star; above day 16 once the chapter is done (to its right it would cover day 11)
            var offset = allDone ? new Vector2(0f, 110f)
                : at.x < 370f ? new Vector2(44f + w / 2f, -80f)   // 370: with the 1.15 map, day 4 at x 387 clipped the truck on the right
                : new Vector2(540f - 10f - w / 2f - at.x, -140f); // day 5 sits near the right edge: tuck the truck below it
            Place(truck.rectTransform, at + offset, new Vector2(w, h));
            truck.raycastTarget = false;
        }

        // Title, one line
        var title = NewImage("Title", canvasRect, Load("title_ch" + (page + 1)) ?? Load("title_ch1"));
        title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -170f);   // under the Dynamic Island (owner, 13 October: the banner was covered)
        title.rectTransform.sizeDelta = new Vector2(980f, 140f);
        title.raycastTarget = false;

        // The next chapter's arrow: locked while there is no next chapter to show, a button once there is
        // (21 October) and only once every day of this page is done: before that a new player could open chapter 2 and play day 17 first
        var nextOpen = page + 1 < Chapter.Count && completed >= ChapterDays;
        var arrow = NewImage(nextOpen ? "NextChapter" : "NextChapterLocked", canvasRect, Load(nextOpen ? "arrow_right" : "arrow_right_locked"));
        arrow.rectTransform.anchorMin = arrow.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        arrow.rectTransform.pivot = new Vector2(1f, 0.5f);
        arrow.rectTransform.anchoredPosition = new Vector2(10f, -260f);
        arrow.SetNativeSize();
        arrow.raycastTarget = nextOpen;
        if (nextOpen) { var ab = arrow.gameObject.AddComponent<Button>(); ab.transition = Selectable.Transition.None; ab.onClick.AddListener(() => ShowPage(page + 1)); }
        if (page > 0)
        {
            var back = NewImage("PreviousChapter", canvasRect, Load("arrow_left"));
            back.rectTransform.anchorMin = back.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            back.rectTransform.pivot = new Vector2(0f, 0.5f);
            back.rectTransform.anchoredPosition = new Vector2(-10f, -260f);
            back.SetNativeSize();
            var bb = back.gameObject.AddComponent<Button>(); bb.transition = Selectable.Transition.None; bb.onClick.AddListener(() => ShowPage(page - 1));
        }

        // Coins and home, along the bottom
        var pill = NewImage("Coins", canvasRect, Load("pill"));
        pill.rectTransform.anchorMin = pill.rectTransform.anchorMax = new Vector2(0f, 0f);
        pill.rectTransform.pivot = new Vector2(0f, 0f);
        pill.rectTransform.anchoredPosition = new Vector2(36f, 130f);    // above the home indicator (owner, 13 October: too close to the bottom edge)
        pill.rectTransform.sizeDelta = new Vector2(300f, 100f);
        pill.raycastTarget = false;
        var coins = NewText("Amount", pill.rectTransform, "$ " + Mathf.FloorToInt(sm != null ? sm.GetTotalScore() : 0f).ToString("N0"), 52f, new Color(0.43f, 0.24f, 0.08f));
        Stretch(coins.rectTransform);

        var home = NewImage("Home", canvasRect, homeSprite);
        home.rectTransform.anchorMin = home.rectTransform.anchorMax = new Vector2(1f, 0f);
        home.rectTransform.pivot = new Vector2(1f, 0f);
        home.rectTransform.anchoredPosition = new Vector2(-36f, 120f);
        home.rectTransform.sizeDelta = new Vector2(130f, 130f);
        var hb = home.gameObject.AddComponent<Button>();
        hb.onClick.AddListener(GoHome);
    }

    // ---- day popup on the pause board ----

    private void OpenDay(int day)
    {
        if (popup != null || leaving) return;
        popup = new GameObject("DayPopup", typeof(RectTransform));
        var root = popup.GetComponent<RectTransform>();
        root.SetParent(canvasRect, false);
        Stretch(root);

        var dim = NewImage("Dim", root, null);
        dim.color = new Color(0.08f, 0.06f, 0.12f, 0.6f);
        Stretch(dim.rectTransform); // swallows taps on the map

        var board = NewImage("Board", root, boardSprite);
        Place(board.rectTransform, new Vector2(0f, 40f), new Vector2(660f, 878f)); // board.png's own shape (433 x 576)
        board.raycastTarget = false;

        var title = NewText("DayTitle", board.rectTransform, "Day " + (day + 1), 110f, new Color(1f, 0.965f, 0.886f));
        title.outlineWidth = 0.3f; title.outlineColor = new Color32(110, 60, 20, 255);
        title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -250f);
        title.rectTransform.sizeDelta = new Vector2(560f, 140f);

        var stars = SessionManager.Instance != null ? SessionManager.Instance.GetLevelStars(day) : 0;
        for (var k = 0; k < 3; k++)
        {
            var st = NewImage("Star", board.rectTransform, k < stars ? starSprite : darkStarSprite);
            st.rectTransform.anchorMin = st.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            st.rectTransform.anchoredPosition = new Vector2(-130f + k * 130f, -410f + (k == 1 ? 18f : 0f));
            st.rectTransform.sizeDelta = new Vector2(110f, 110f);
            st.raycastTarget = false;
        }

        MakeButton(board.rectTransform, Load("btn_play"), new Vector2(-140f, -640f), () => PlayDay(day));
        MakeButton(board.rectTransform, Load("btn_cancel"), new Vector2(140f, -640f), CloseDay);
    }

    private static Sprite glowSprite;
    /// A soft round glow, made once at runtime (like DayNightTint's stars)
    private static Sprite GlowSprite()
    {
        if (glowSprite != null) return glowSprite;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color[n * n];
        for (var y = 0; y < n; y++)
        for (var x = 0; x < n; x++)
        {
            var dx = (x + 0.5f - n / 2f) / (n / 2f); var dy = (y + 0.5f - n / 2f) / (n / 2f);
            var a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
            px[y * n + x] = new Color(1f, 1f, 1f, a * a * a);
        }
        tex.SetPixels(px); tex.Apply();
        glowSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        return glowSprite;
    }

    private void MakeButton(RectTransform parent, Sprite sprite, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var img = NewImage(sprite != null ? sprite.name : "Button", parent, sprite);
        img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        img.rectTransform.anchoredPosition = pos;
        img.rectTransform.sizeDelta = sprite != null ? new Vector2(sprite.rect.width, sprite.rect.height) * 0.9f : new Vector2(250f, 110f);
        var b = img.gameObject.AddComponent<Button>();
        b.onClick.AddListener(onClick);
    }

    private void CloseDay()
    {
        if (popup != null) Destroy(popup);
        popup = null;
    }

    private void PlayDay(int day)
    {
        if (leaving) return;
        leaving = true;
        var sm = SessionManager.Instance;
        if (sm != null) sm.SetCurrentLevel(day);
        if (AudioManager.Instance != null) AudioManager.Instance.StopMusic();

        if (CameFromDay)
        {
            // After a finished day: the ad, then the Shop; the Shop's Next starts the chosen day
            CameFromDay = false;
            InterstitialAdService.Instance.ShowAfterLevelThen(() => SceneTransitionManager.Instance.TransitionToScene("Shop"));
        }
        else SceneTransitionManager.Instance.TransitionToScene("GameSceneOne");
    }

    private void GoHome()
    {
        if (leaving) return;
        leaving = true;
        CameFromDay = false;
        SceneTransitionManager.Instance.TransitionToScene("MainMenu");
    }

    // ---- helpers ----

    private static Image NewImage(string name, RectTransform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = sprite != null;
        return img;
    }

    private TextMeshProUGUI NewText(string name, RectTransform parent, string text, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Capline;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        return t;
    }

    private static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}

/// Today's circle on the map bounces: a little hop every 1.4 s with a squash on landing (owner, 13 October).
/// The number inside is a child, so it bounces along.
public class MapTodayBounce : MonoBehaviour
{
    private RectTransform rt; private Vector2 rest; private float t;
    private void Awake() { rt = GetComponent<RectTransform>(); rest = rt.anchoredPosition; }
    private void Update()
    {
        if (rt == null) return;
        t += Time.unscaledDeltaTime;
        var phase = Mathf.Repeat(t, 1.4f);
        var hop = phase < 0.5f ? Mathf.Sin(phase / 0.5f * Mathf.PI) : 0f;         // up and down in half a second, then rest
        rt.anchoredPosition = rest + new Vector2(0f, 26f * hop);
        var squash = phase >= 0.5f && phase < 0.62f ? 1f - 0.12f * Mathf.Sin((phase - 0.5f) / 0.12f * Mathf.PI) : 1f;
        rt.localScale = new Vector3(2f - squash, squash, 1f);                      // wider as it lands, back to round
    }
}

/// Slow pulse of a three-star day's glow on the map (alpha 0.55 to 0.95, about once a second)
public class MapGlowPulse : MonoBehaviour
{
    private Image img; private float t;
    private void Awake() { img = GetComponent<Image>(); t = Random.value * 6f; }
    private void Update()
    {
        if (img == null) return;
        t += Time.unscaledDeltaTime;
        var c = img.color; c.a = 0.75f + 0.2f * Mathf.Sin(t * 5f); img.color = c;
        transform.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(t * 5f));
    }
}
