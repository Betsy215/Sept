using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// The update gate (owner, 23 November 2026: "we need to do a force update for any existing download"): once per
/// launch the game asks Apple's public lookup which version is on the App Store. When the store's version is newer
/// than this copy, a card covers the screen with one button, UPDATE, which opens the game's App Store page. There is
/// no way round the card: the player has to update.
///
/// What it cannot do: copies from before this file (2026.11.20 and older) hold no check, so nothing can make them
/// update; they move on by the phone's own automatic updates. From this release on, every later release is enforced.
///
/// It never blocks by mistake:
///  - no answer, no network, a strange answer: no card (the game is playable offline);
///  - the store's version must be at least <see cref="HoursOnStore"/> old: a fresh release takes hours to reach every
///    country's store, and a player sent there too early would find only "Open";
///  - only on the phone (the editor shows it by <see cref="ShowForTest"/> only), and never over a running day: the
///    card waits for the start menu, the map or the shop.
/// The request carries the app's number only; nothing about the player.
public static class UpdateGate
{
    public const string AppId = "6755611355";
    public const float HoursOnStore = 36f;
    private const string LookupUrl = "https://itunes.apple.com/lookup?id=" + AppId;
    private const string StoreUrl = "itms-apps://apps.apple.com/app/id" + AppId;
    private static bool asked, shown;

    [System.Serializable] private class Lookup { public int resultCount; public Result[] results; }
    [System.Serializable] private class Result { public string version; public string currentVersionReleaseDate; public string minimumOsVersion; }

    private class Runner : MonoBehaviour { }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (asked || Application.platform != RuntimePlatform.IPhonePlayer) return;
        asked = true;
        var go = new GameObject("[UpdateGate]"); Object.DontDestroyOnLoad(go);
        go.AddComponent<Runner>().StartCoroutine(Ask(go));
    }

    private static IEnumerator Ask(GameObject host)
    {
        using (var req = UnityWebRequest.Get(LookupUrl))
        {
            req.timeout = 8;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) { Object.Destroy(host); yield break; }
            if (!NeedsUpdate(req.downloadHandler.text, Application.version, System.DateTime.UtcNow, OsVersion())) { Object.Destroy(host); yield break; }
        }
        // never over a running day: wait for a calm screen
        while (!Calm(SceneManager.GetActiveScene().name)) yield return null;
        Show();
    }

    private static bool Calm(string scene) => scene == "MainMenu" || scene == "Map" || scene == "Shop";

    /// The phone's iOS version ("18.7.8"); empty when unknown
    private static string OsVersion()
    {
#if UNITY_IOS
        try { return UnityEngine.iOS.Device.systemVersion ?? ""; } catch (System.Exception) { return ""; }
#else
        return "";
#endif
    }

    /// True when the lookup's answer names a version newer than `mine` that has been on the store long enough, and that
    /// this phone can install: a store version needing a newer iOS than `os` would leave the player before a card with no
    /// way past it, so then no card (a known `os` only).
    public static bool NeedsUpdate(string json, string mine, System.DateTime nowUtc, string os = "")
    {
        try
        {
            var look = JsonUtility.FromJson<Lookup>(json);
            if (look == null || look.results == null || look.results.Length == 0 || string.IsNullOrEmpty(look.results[0].version)) return false;
            if (Compare(look.results[0].version, mine) <= 0) return false;
            if (!string.IsNullOrEmpty(os) && !string.IsNullOrEmpty(look.results[0].minimumOsVersion) && Compare(look.results[0].minimumOsVersion, os) > 0) return false;
            if (!System.DateTime.TryParse(look.results[0].currentVersionReleaseDate, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var released)) return false;
            return (nowUtc - released).TotalHours >= HoursOnStore;
        }
        catch (System.Exception) { return false; }
    }

    /// Versions as numbers, part by part ("2026.11.23" against "2026.11.20"; "26.11.22" is older than both).
    /// Positive when a is newer. A part that is not a number counts as 0.
    public static int Compare(string a, string b)
    {
        var pa = (a ?? "").Split('.'); var pb = (b ?? "").Split('.');
        for (var i = 0; i < Mathf.Max(pa.Length, pb.Length); i++)
        {
            long x = 0, y = 0;
            if (i < pa.Length) long.TryParse(pa[i].Trim(), out x);
            if (i < pb.Length) long.TryParse(pb[i].Trim(), out y);
            if (x != y) return x > y ? 1 : -1;
        }
        return 0;
    }

    /// The card, for a look in the editor (a recipe calls it).
    public static void ShowForTest() { shown = false; Show(); }

    private static void Show()
    {
        if (shown) return;
        shown = true;
        var go = new GameObject("UpdateGateCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Object.DontDestroyOnLoad(go);
        var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30000;   // over everything
        var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var root = (RectTransform)go.transform;

        var dim = Img("Dim", root, null, new Color(0.10f, 0.07f, 0.05f, 0.72f));   // takes every tap
        dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one; dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;

        var paper = Resources.Load<Sprite>("Map/day_paper");
        var panel = Img("Panel", root, paper, paper != null ? Color.white : new Color(1f, 0.96f, 0.88f));
        panel.rectTransform.sizeDelta = new Vector2(820f, 820f);
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/beachday SDF"); var hand = Resources.Load<TMP_FontAsset>("Fonts & Materials/Delius SDF");
        var plain = Resources.Load<Material>("Fonts & Materials/beachday SDF - Plain");
        Color ink = new Color(0.36f, 0.20f, 0.09f), soft = new Color(0.47f, 0.31f, 0.20f);

        var title = Text("Title", panel.rectTransform, font, plain, 84f, ink, "NEW VERSION!");
        title.rectTransform.anchoredPosition = new Vector2(0f, 210f); title.rectTransform.sizeDelta = new Vector2(700f, 130f);
        var line = Text("Line", panel.rectTransform, hand != null ? hand : font, hand != null ? null : plain, 42f, soft, "Food Truck Cafe has an update.\nPlease update to keep playing.");
        line.rectTransform.anchoredPosition = new Vector2(0f, 40f); line.rectTransform.sizeDelta = new Vector2(660f, 170f);

        var button = Img("Update", panel.rectTransform, Rounded(), new Color(0.98f, 0.76f, 0.26f)); button.type = Image.Type.Sliced;
        button.rectTransform.anchoredPosition = new Vector2(0f, -170f); button.rectTransform.sizeDelta = new Vector2(430f, 136f);
        var edge = button.gameObject.AddComponent<Outline>(); edge.effectColor = new Color(0.62f, 0.38f, 0.10f, 0.9f); edge.effectDistance = new Vector2(3f, -5f);
        var word = Text("Word", button.rectTransform, font, plain, 66f, new Color(0.45f, 0.25f, 0.06f), "UPDATE");
        word.rectTransform.anchorMin = Vector2.zero; word.rectTransform.anchorMax = Vector2.one; word.rectTransform.offsetMin = word.rectTransform.offsetMax = Vector2.zero;
        button.gameObject.AddComponent<Button>().onClick.AddListener(() => Application.OpenURL(StoreUrl));
    }

    private static Image Img(string name, RectTransform parent, Sprite sprite, Color colour)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>(); img.sprite = sprite; img.color = colour; return img;
    }

    private static TextMeshProUGUI Text(string name, RectTransform parent, TMP_FontAsset font, Material material, float size, Color colour, string words)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>(); if (font != null) t.font = font; if (material != null) t.fontSharedMaterial = material;
        t.fontSize = size; t.color = colour; t.text = words; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; return t;
    }

    private static Sprite rounded;
    private static Sprite Rounded()
    {
        if (rounded != null) return rounded;
        const int S = 96, R = 40;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false); var px = new Color[S * S];
        for (var y = 0; y < S; y++)
            for (var x = 0; x < S; x++)
            {
                var cx = Mathf.Clamp(x, R, S - 1 - R); var cy = Mathf.Clamp(y, R, S - 1 - R);
                px[y * S + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(R - Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) + 0.5f));
            }
        tex.SetPixels(px); tex.Apply();
        rounded = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
        return rounded;
    }
}
