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

    public Sprite truckSprite, boardSprite, starSprite, darkStarSprite, homeSprite;

    // Scorpio in the 1080x1920 design space (origin top left): claws top right, tail curling bottom left
    private static readonly Vector2[] Stars =
    {
        new Vector2(640, 730), new Vector2(720, 620), new Vector2(790, 520), new Vector2(880, 435),
        new Vector2(965, 530), new Vector2(800, 710), new Vector2(700, 830), new Vector2(615, 950),
        new Vector2(545, 1090), new Vector2(505, 1230), new Vector2(470, 1380), new Vector2(380, 1510),
        new Vector2(250, 1530), new Vector2(165, 1430), new Vector2(205, 1315), new Vector2(300, 1275)
    };

    private RectTransform canvasRect;
    private TMP_FontAsset font;
    private GameObject popup;
    private bool leaving;

    private void Start()
    {
        Time.timeScale = 1f;
        font = Resources.Load<TMP_FontAsset>("Fonts & Materials/beachday SDF");
        Build();
    }

    private static Sprite Load(string n) => Resources.Load<Sprite>("Map/" + n);
    private static Vector2 P(Vector2 design) => new Vector2(design.x - 540f, 960f - design.y);

    private void Build()
    {
        var go = new GameObject("MapCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0f; // keep the width; taller phones get more background top and bottom
        canvasRect = go.GetComponent<RectTransform>();

        // Background: the night shop, covering the screen
        var bg = NewImage("Background", canvasRect, Load("bg_ch1"));
        bg.rectTransform.anchorMin = bg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = bg.sprite != null ? bg.sprite.rect.width / bg.sprite.rect.height : 0.55f;

        var sm = SessionManager.Instance;
        var completed = sm != null ? sm.GetLevelsCompleted() : 0;
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
            var isToday = !allDone && i == today;
            var pos = P(Stars[i]);
            var size = isToday ? 104f : 84f;
            var dot = NewImage("Day" + (i + 1), canvasRect, Load(isToday ? "day_today" : played ? "day_done" : "day_locked"));
            Place(dot.rectTransform, pos, new Vector2(size, size));
            var label = NewText("Number", dot.rectTransform, (i + 1).ToString(), isToday ? 38f : 30f,
                isToday ? new Color(1f, 0.98f, 0.92f) : played ? new Color(0.43f, 0.24f, 0.08f) : new Color(0.47f, 0.43f, 0.39f));
            Stretch(label.rectTransform);
            label.rectTransform.offsetMax = new Vector2(0, 4f);

            if (played)
            {
                var stars = sm != null ? sm.GetLevelStars(i) : 0;
                for (var k = 0; k < 3; k++)
                {
                    var st = NewImage("Star", canvasRect, k < stars ? starSprite : darkStarSprite);
                    Place(st.rectTransform, pos + new Vector2(-32f + k * 32f, -40f + (k == 1 ? 6f : 0f)), new Vector2(30f, 30f));
                    st.raycastTarget = false;
                }
            }
            if (played || isToday)
            {
                var day = i;
                var btn = dot.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => OpenDay(day));
            }
            else dot.raycastTarget = false;
        }

        // The truck beside today's star (beside day 16 once the chapter is done)
        if (truckSprite != null)
        {
            var at = P(Stars[allDone ? ChapterDays - 1 : today]);
            var truck = NewImage("Truck", canvasRect, truckSprite);
            var h = 120f; var w = h * truckSprite.rect.width / truckSprite.rect.height;
            // beside today's star; above day 16 once the chapter is done (to its right it would cover day 11)
            var offset = allDone ? new Vector2(0f, 110f) : new Vector2(at.x < 300f ? 44f + w / 2f : -44f - w / 2f, -80f);
            if (!allDone && at.x >= 300f) offset = new Vector2(44f + w / 2f, -80f);
            Place(truck.rectTransform, at + offset, new Vector2(w, h));
            truck.raycastTarget = false;
        }

        // Title, one line
        var title = NewImage("Title", canvasRect, Load("title_ch1"));
        title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -60f);
        title.rectTransform.sizeDelta = new Vector2(980f, 140f);
        title.raycastTarget = false;

        // Chapter 2: the right arrow, locked
        var arrow = NewImage("NextChapterLocked", canvasRect, Load("arrow_right_locked"));
        arrow.rectTransform.anchorMin = arrow.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        arrow.rectTransform.pivot = new Vector2(1f, 0.5f);
        arrow.rectTransform.anchoredPosition = new Vector2(10f, -260f);
        arrow.SetNativeSize();
        arrow.raycastTarget = false;

        // Coins and home, along the bottom
        var pill = NewImage("Coins", canvasRect, Load("pill"));
        pill.rectTransform.anchorMin = pill.rectTransform.anchorMax = new Vector2(0f, 0f);
        pill.rectTransform.pivot = new Vector2(0f, 0f);
        pill.rectTransform.anchoredPosition = new Vector2(36f, 40f);
        pill.rectTransform.sizeDelta = new Vector2(300f, 100f);
        pill.raycastTarget = false;
        var coins = NewText("Amount", pill.rectTransform, "$ " + (sm != null ? sm.GetTotalScore() : 0f).ToString("N0"), 52f, new Color(0.43f, 0.24f, 0.08f));
        Stretch(coins.rectTransform);

        var home = NewImage("Home", canvasRect, homeSprite);
        home.rectTransform.anchorMin = home.rectTransform.anchorMax = new Vector2(1f, 0f);
        home.rectTransform.pivot = new Vector2(1f, 0f);
        home.rectTransform.anchoredPosition = new Vector2(-36f, 30f);
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
