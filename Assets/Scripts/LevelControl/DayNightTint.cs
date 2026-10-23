using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Evening and night over the last two customers of a day.
///
/// Stage 0 (day) is untouched. When the second-to-last customer is due (orders completed reaches
/// ordersPerLevel - 2) the background image eases to a dusk blue over a few seconds and a faint moon
/// appears; when the last customer is due it deepens to night with a moon and stars. Only the
/// background image under the main Canvas is tinted, so the table, food and UI stay as they are.
/// While the kitchen is open, the view through its painted window gets the same evening or night
/// (an overlay anchored to the window's share of the kitchen background, so it follows any screen size).
/// Each new day starts in daylight because the scene reloads.
///
/// Everything it draws is built at runtime; no scene wiring. Added by LevelManager.Start.
/// </summary>
public class DayNightTint : MonoBehaviour
{
    private static readonly Color DayColor = Color.white;
    private static readonly Color DuskColor = new Color(0.66f, 0.67f, 0.92f);
    private static readonly Color NightColor = new Color(0.24f, 0.27f, 0.56f);

    private const float FadeSeconds = 3f;      // time to move one stage (day->dusk, dusk->night)
    private const float DuskSkyAlpha = 0.35f;  // how visible the moon/stars are at dusk

    private OrderSystem orderSystem;
    private Image background;
    private CanvasGroup sky;
    private Image[] stars = new Image[0];
    private float[] starPhase = new float[0];

    // Kitchen window overlay; rebuilt whenever the kitchen scene is (re)loaded while it is not daytime
    private Image kitchenWindow;
    private CanvasGroup kitchenSky;
    private static readonly Color WindowDusk = new Color(0.30f, 0.34f, 0.72f, 0.40f);
    private static readonly Color WindowNight = new Color(0.07f, 0.09f, 0.36f, 0.78f);

    private float stage;        // 0 = day, 1 = dusk, 2 = night (fractional while fading)
    /// The running day's stage for other scenes (the chapter 2 kitchen window fades its night picture by it); a
    /// plain static value written every frame, because the tint's object is hidden while the kitchen is open and
    /// a reference to it reads as null then (found 16 October: the window stayed at day)
    public static float CurrentStage { get; private set; }

    private static Sprite starSprite;

    public void Begin(OrderSystem os)
    {
        CurrentStage = 0f;
        orderSystem = os;

        var canvasGo = GameObject.Find("Canvas");
        var bg = canvasGo != null ? canvasGo.transform.Find("bg") : null;
        background = bg != null ? bg.GetComponent<Image>() : null;
        if (background == null)
        {
            Debug.LogWarning("DayNightTint: Canvas/bg image not found, night mode off for this scene");
            enabled = false;
            return;
        }

        // Chapter 2 (16 October): the night is a painted picture of the beach that fades in over the day one;
        // no moon and stars of our own then
        var nightArt = Chapter.Art<Sprite>(SessionManager.CurrentChapter, "bg_night");
        if (nightArt != null) BuildNightPicture(bg, nightArt); else BuildSky(bg);
    }

    private Image nightPicture;

    private void BuildNightPicture(Transform bg, Sprite nightArt)
    {
        var src = (RectTransform)bg;
        var go = new GameObject("NightPicture", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(bg.parent, false);
        go.transform.SetSiblingIndex(bg.GetSiblingIndex() + 1);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = src.anchorMin; rt.anchorMax = src.anchorMax; rt.pivot = src.pivot;
        rt.anchoredPosition = src.anchoredPosition; rt.sizeDelta = src.sizeDelta; rt.localScale = src.localScale;
        nightPicture = go.GetComponent<Image>();
        nightPicture.sprite = nightArt; nightPicture.preserveAspect = background.preserveAspect;
        nightPicture.color = new Color(1f, 1f, 1f, 0f);
        nightPicture.raycastTarget = false;
    }

    private void Update()
    {
        if (orderSystem == null || background == null) return;

        var wanted = WantedStage();
        stage = Mathf.MoveTowards(stage, wanted, Time.deltaTime / FadeSeconds);
        CurrentStage = stage;

        if (nightPicture != null)
        {
            // the painted night fades in: half way at dusk, fully at night; the day picture under it is only lightly tinted
            nightPicture.color = new Color(1f, 1f, 1f, stage <= 1f ? 0.5f * stage : 0.5f + 0.5f * (stage - 1f));
            background.color = Color.Lerp(DayColor, new Color(0.8f, 0.8f, 0.9f), Mathf.Clamp01(stage));
        }
        else
        background.color = stage <= 1f
            ? Color.Lerp(DayColor, DuskColor, stage)
            : Color.Lerp(DuskColor, NightColor, stage - 1f);

        if (sky != null)
        {
            sky.alpha = stage <= 1f ? DuskSkyAlpha * stage : DuskSkyAlpha + (1f - DuskSkyAlpha) * (stage - 1f);
            for (var i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;
                var c = stars[i].color;
                c.a = 0.55f + 0.45f * Mathf.Sin(Time.time * 2.2f + starPhase[i]);
                stars[i].color = c;
            }
        }

        UpdateKitchenWindow();
    }

    private void UpdateKitchenWindow()
    {
        if (stage <= 0.001f) return;
        if (kitchenWindow == null && !BuildKitchenWindow()) return; // kitchen not open

        kitchenWindow.color = stage <= 1f
            ? new Color(WindowDusk.r, WindowDusk.g, WindowDusk.b, WindowDusk.a * stage)
            : Color.Lerp(WindowDusk, WindowNight, stage - 1f);
        if (kitchenSky != null)
            kitchenSky.alpha = stage <= 1f ? DuskSkyAlpha * stage : DuskSkyAlpha + (1f - DuskSkyAlpha) * (stage - 1f);
    }

    private bool BuildKitchenWindow()
    {
        var kitchen = SceneManager.GetSceneByName("KitchenScene");
        if (!kitchen.isLoaded) return false;

        Transform bg = null;
        foreach (var root in kitchen.GetRootGameObjects())
        {
            var canvas = root.GetComponentInChildren<Canvas>(true);
            if (canvas != null) bg = canvas.transform.Find("bg");
            if (bg != null) break;
        }
        if (bg == null) return false;

        // The window pane's share of the kitchen background image (measured on the painting)
        var go = new GameObject("NightWindow", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(bg, false);
        // Over the apple tree (bg/tree, chapter 1) so the tree darkens with the sky and the strip of daytime sky cut
        // into its sprite is tinted too (owner, 21 October: the tree's crop and the night overlay looked bad with
        // the tree left bright on top); still under the shelf, ovens and basket that sit in front of the window.
        // Chapter 2 hides the tree and draws its own window view, so the overlay stays first there.
        var tree = bg.Find("tree");
        go.transform.SetSiblingIndex(tree != null && tree.gameObject.activeSelf ? tree.GetSiblingIndex() + 1 : 0);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.502f, 0.556f);
        rt.anchorMax = new Vector2(0.922f, 0.803f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        kitchenWindow = go.GetComponent<Image>();
        kitchenWindow.color = new Color(0, 0, 0, 0);
        kitchenWindow.raycastTarget = false;

        var skyGo = new GameObject("Sky", typeof(RectTransform), typeof(CanvasGroup));
        skyGo.transform.SetParent(rt, false);
        var skyRt = (RectTransform)skyGo.transform;
        skyRt.anchorMin = Vector2.zero;
        skyRt.anchorMax = Vector2.one;
        skyRt.offsetMin = Vector2.zero;
        skyRt.offsetMax = Vector2.zero;
        kitchenSky = skyGo.GetComponent<CanvasGroup>();
        kitchenSky.alpha = 0f;
        kitchenSky.interactable = false;
        kitchenSky.blocksRaycasts = false;

        var moon = Resources.Load<Sprite>("Tutorial/moon");
        if (moon != null) MakeAnchoredImage(skyRt, "Moon", moon, new Vector2(0.70f, 0.66f), new Vector2(0.92f, 0.90f));
        // in the open sky to the right of the tree (the tree fills the left 60 % of the glass), clear of the moon
        MakeAnchoredImage(skyRt, "Star", StarSprite(), new Vector2(0.65f, 0.86f), new Vector2(0.70f, 0.92f));
        MakeAnchoredImage(skyRt, "Star", StarSprite(), new Vector2(0.93f, 0.57f), new Vector2(0.97f, 0.61f));
        MakeAnchoredImage(skyRt, "Star", StarSprite(), new Vector2(0.66f, 0.60f), new Vector2(0.69f, 0.64f));
        return true;
    }

    private static Image MakeAnchoredImage(RectTransform parent, string name, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    private LevelManager levelManager;

    private int WantedStage()
    {
        // Timed days (13 October): dusk at 65% of the day, night at 85%
        if (levelManager == null) levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null && levelManager.DayStarted && levelManager.DayDuration > 0f)
        {
            var elapsed = 1f - levelManager.DayTimeLeft / levelManager.DayDuration;
            return elapsed >= 0.85f ? 2 : elapsed >= 0.65f ? 1 : 0;
        }
        // Short custom days (under 4 orders) never get night, so the tint cannot start at the first customer.
        if (orderSystem.ordersPerLevel < 4) return 0;
        var remaining = orderSystem.ordersPerLevel - orderSystem.OrdersCompleted;
        if (remaining <= 1) return 2;
        if (remaining == 2) return 1;
        return 0;
    }

    // ---- runtime-built moon and stars, drawn just above the background image ----

    private void BuildSky(Transform bg)
    {
        var go = new GameObject("NightSky", typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(bg.parent, false);
        go.transform.SetSiblingIndex(bg.GetSiblingIndex() + 1);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        sky = go.GetComponent<CanvasGroup>();
        sky.alpha = 0f;
        sky.interactable = false;
        sky.blocksRaycasts = false;

        var moon = Resources.Load<Sprite>("Tutorial/moon");
        if (moon != null) MakeImage(rt, "Moon", moon, new Vector2(40f, 540f), new Vector2(150f, 150f)); // open sky between the bubble and the customer, under the day board

        // A handful of stars in the sky gap of the background; positions are in the 1080x1920 canvas space
        Vector3[] spots =
        {
            new Vector3(-330f, 520f, 1.0f), new Vector3(-140f, 420f, 0.7f), new Vector3(40f, 560f, 0.8f),
            new Vector3(130f, 330f, 0.6f), new Vector3(-250f, 250f, 0.8f), new Vector3(420f, 460f, 0.7f),
            new Vector3(-60f, 240f, 0.5f), new Vector3(230f, 160f, 0.6f)
        };
        stars = new Image[spots.Length];
        starPhase = new float[spots.Length];
        for (var i = 0; i < spots.Length; i++)
        {
            var size = 22f * spots[i].z;
            stars[i] = MakeImage(rt, "Star", StarSprite(), new Vector2(spots[i].x, spots[i].y), new Vector2(size, size));
            starPhase[i] = i * 1.7f;
        }
    }

    private static Image MakeImage(RectTransform parent, string name, Sprite sprite, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    private static Sprite StarSprite()
    {
        if (starSprite != null) return starSprite;
        const int n = 32;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color[n * n];
        for (var y = 0; y < n; y++)
        for (var x = 0; x < n; x++)
        {
            var dx = (x + 0.5f - n / 2f) / (n / 2f);
            var dy = (y + 0.5f - n / 2f) / (n / 2f);
            var d = Mathf.Sqrt(dx * dx + dy * dy);
            var a = Mathf.Clamp01(1f - d);
            a = a * a * a; // soft glow
            px[y * n + x] = new Color(1f, 0.98f, 0.85f, a);
        }
        tex.SetPixels(px);
        tex.Apply();
        starSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        return starSprite;
    }
}
