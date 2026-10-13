using System.Collections;
using TMPro;
using UnityEngine;

/// The money and the day's stars on the table (owner, 12-13 October): a small cream board next to the truck button,
/// a little longer and a little shorter than the truck. The wallet is one number in the game's own font and text
/// style (black with the green glow, like the receipt), with a small bounce when it changes; the three stars sit on
/// top and turn gold as the day passes each threshold. Replaces the sign at the top of the screen (LevelManager hides
/// it). Built at runtime from procedural sprites and the game font. (The split-flap cards of 12 October were dropped
/// on 13 October: the flipping looked odd.)
public class MoneyFlipClock : MonoBehaviour
{
    public static Vector3 Position = new Vector3(-0.12f, 0.12f, 0f);   // just right of the truck button
    public const float Width = 1.4f, Height = 0.62f;                  // world units; the truck button is about 1.2 x 0.7

    static Sprite roundRect, starSprite;
    ScoreManager scoreManager;
    TextMeshPro money;
    Coroutine bounce;
    readonly SpriteRenderer[] stars = new SpriteRenderer[3];
    int shownMoney = -1, shownStars = -1;

    public static void Setup(ScoreManager sm)
    {
        var old = GameObject.Find("MoneyFlipClock");
        if (old != null) Destroy(old);
        var go = new GameObject("MoneyFlipClock");
        go.transform.position = Position;
        var c = go.AddComponent<MoneyFlipClock>();
        c.scoreManager = sm;
        c.Build();
    }

    void Build()
    {
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/beachday SDF");
        // contact shadow under the foot (a soft band hugging the base, a little to the right: owner 13 October), box, wooden foot
        var shadow = new GameObject("Shadow");
        shadow.transform.SetParent(transform, false);
        shadow.transform.localPosition = new Vector3(0.03f, -Height / 2f + 0.005f, 0f);
        var ssr = shadow.AddComponent<SpriteRenderer>();
        ssr.sprite = ContactShadowSprite(); ssr.drawMode = SpriteDrawMode.Sliced; ssr.size = new Vector2(Width + 0.16f, 0.22f);
        ssr.color = new Color(0.27f, 0.2f, 0.17f, 0.55f); ssr.sortingOrder = 0;
        Rect("Box", Vector3.zero, Width, Height, new Color(0.98f, 0.953f, 0.91f), 1);
        Rect("Line", Vector3.zero, Width - 0.03f, Height - 0.03f, new Color(0.667f, 0.588f, 0.47f), 1, hollow: true);
        Rect("Foot", new Vector3(0f, -Height / 2f + 0.03f, 0f), Width - 0.12f, 0.07f, new Color(0.69f, 0.52f, 0.36f), 2);
        // the number, in the game's text style (the font's own material: black face, green glow)
        var g = new GameObject("Money");
        g.transform.SetParent(transform, false);
        g.transform.localPosition = new Vector3(0f, -0.09f, 0f);
        money = g.AddComponent<TextMeshPro>();
        if (font != null) money.font = font;
        money.fontSize = 3.4f;
        money.color = Color.black;
        money.alignment = TextAlignmentOptions.Center;
        money.rectTransform.sizeDelta = new Vector2(Width, 0.36f);
        money.text = "";
        // stars on top
        for (var k = 0; k < 3; k++)
        {
            var s = new GameObject("Star" + k);
            s.transform.SetParent(transform, false);
            s.transform.localPosition = new Vector3((k - 1) * 0.3f, Height / 2f - 0.13f + (k == 1 ? 0.02f : 0f), 0f);
            s.transform.localScale = Vector3.one * 0.95f;
            var sr = s.AddComponent<SpriteRenderer>();
            sr.sprite = StarSprite(); sr.sortingOrder = 4;
            stars[k] = sr;
        }
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>()) sr.sortingLayerName = "FoodItems";
        money.sortingLayerID = SortingLayer.NameToID("FoodItems"); money.sortingOrder = 6;
        Refresh(true);
    }

    void Update() => Refresh(false);

    void Refresh(bool instant)
    {
        var wallet = SessionManager.Instance != null && SessionManager.Instance.HasActiveSession() ? Mathf.FloorToInt(SessionManager.Instance.GetTotalScore()) : 0;
        wallet = Mathf.Clamp(wallet, 0, 99999);
        if (wallet != shownMoney)
        {
            money.text = "$" + wallet;
            if (!instant && shownMoney >= 0)
            {
                if (bounce != null) StopCoroutine(bounce);
                bounce = StartCoroutine(Pop(money.transform, 0.3f));
            }
            shownMoney = wallet;
        }
        var earned = scoreManager != null && scoreManager.starProgressBar != null ? scoreManager.starProgressBar.GetEarnedStarsCount() : 0;
        if (earned != shownStars)
        {
            for (var k = 0; k < 3; k++) stars[k].color = k < earned ? new Color(1f, 0.78f, 0.3f) : new Color(0.78f, 0.74f, 0.68f);
            if (!instant && earned > shownStars && shownStars >= 0) StartCoroutine(Pop(stars[Mathf.Clamp(earned - 1, 0, 2)].transform, 0.5f, 0.95f));
            shownStars = earned;
        }
    }

    /// A little bounce: up by `amount` and back over 0.4 s
    IEnumerator Pop(Transform t, float amount, float rest = 1f)
    {
        var time = 0f;
        while (time < 0.4f) { time += Time.deltaTime; t.localScale = Vector3.one * (rest + amount * Mathf.Sin(Mathf.Clamp01(time / 0.4f) * Mathf.PI)); yield return null; }
        t.localScale = Vector3.one * rest;
    }

    SpriteRenderer Rect(string name, Vector3 pos, float w, float h, Color col, int order, Transform parent = null, bool hollow = false)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent != null ? parent : transform, false);
        g.transform.localPosition = pos;
        var sr = g.AddComponent<SpriteRenderer>();
        sr.sprite = RoundRect(); sr.drawMode = SpriteDrawMode.Sliced; sr.size = new Vector2(w, h); sr.color = col; sr.sortingOrder = order;
        if (hollow) { sr.sprite = RoundRectHollow(); }
        return sr;
    }

    // a 32 px rounded square with an 8 px corner, sliced so any size keeps round corners
    static Sprite RoundRect()
    {
        if (roundRect != null) return roundRect;
        roundRect = MakeRound(false); return roundRect;
    }
    static Sprite hollowRect;
    static Sprite RoundRectHollow() { if (hollowRect == null) hollowRect = MakeRound(true); return hollowRect; }
    static Sprite MakeRound(bool hollow)
    {
        const int n = 32, r = 8;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color[n * n];
        for (var y = 0; y < n; y++)
        for (var x = 0; x < n; x++)
        {
            var cx = Mathf.Clamp(x + 0.5f, r, n - r); var cy = Mathf.Clamp(y + 0.5f, r, n - r);
            var d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
            var a = Mathf.Clamp01(r - d + 0.5f);
            if (hollow) a *= Mathf.Clamp01(d - (r - 2.2f) + 0.5f);     // a 2 px ring
            px[y * n + x] = new Color(1f, 1f, 1f, a);
        }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r + 2, r + 2, r + 2, r + 2));
    }

    // a soft-edged rounded blob, sliced: solid in the middle, fading out over 10 px all round
    static Sprite contactShadow;
    static Sprite ContactShadowSprite()
    {
        if (contactShadow != null) return contactShadow;
        const int n = 48, r = 16;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color[n * n];
        for (var y = 0; y < n; y++)
        for (var x = 0; x < n; x++)
        {
            var cx = Mathf.Clamp(x + 0.5f, r, n - r); var cy = Mathf.Clamp(y + 0.5f, r, n - r);
            var d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
            var a = Mathf.SmoothStep(1f, 0f, d / r);
            px[y * n + x] = new Color(1f, 1f, 1f, a);
        }
        tex.SetPixels(px); tex.Apply();
        contactShadow = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r + 1, r + 1, r + 1, r + 1));
        return contactShadow;
    }

    static Sprite StarSprite()
    {
        if (starSprite != null) return starSprite;
        const int n = 40;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color[n * n];
        for (var y = 0; y < n; y++)
        for (var x = 0; x < n; x++)
        {
            var dx = (x + 0.5f - n / 2f) / (n / 2f); var dy = (y + 0.5f - n / 2f) / (n / 2f);
            var ang = Mathf.Atan2(dy, dx); var rad = Mathf.Sqrt(dx * dx + dy * dy);
            // five-point star: radius alternates between 1 and 0.45 with the angle
            var k = Mathf.Abs(Mathf.Repeat(ang + Mathf.PI / 2f, 2f * Mathf.PI / 5f) / (2f * Mathf.PI / 5f) - 0.5f) * 2f;   // 0 at a tip, 1 at a valley
            var edge = Mathf.Lerp(1f, 0.45f, k);
            var a = Mathf.Clamp01((edge - rad) * 10f);
            px[y * n + x] = new Color(1f, 1f, 1f, a);
        }
        tex.SetPixels(px); tex.Apply();
        starSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f * 40f / 22f);   // about 0.22 units wide
        return starSprite;
    }
}
