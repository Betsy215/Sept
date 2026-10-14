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
    public static Vector3 Position = new Vector3(-0.12f, -0.02f, 0f);  // just right of the truck button, its bottom on the truck's line, a little lower (owner, 13 October)
    public const float Width = 1.15f, Height = 0.5f;                  // world units, the fallback board; the truck button is about 1.0 x 0.6
    public const float HenScale = 0.5f;                               // the owner's 236 x 222 hen (her kitchen layer export, 2x) at 100 px per unit: 1.18 x 1.11 units

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
        c.FollowTruck();
    }

    /// The truck button is anchored to the screen's left edge, so on a wider screen (iPad) it sits further left than on
    /// a phone. Keep the hen the same distance to the right of it everywhere (13 October review; the phone layout had a
    /// gap of about 0.5 units).
    const float GapAfterTruck = 0.5f;
    void FollowTruck()
    {
        var truck = GameObject.Find("KitchenButton");
        var rt = truck != null ? truck.GetComponent<RectTransform>() : null;
        if (rt == null) return;
        var corners = new Vector3[4]; rt.GetWorldCorners(corners);
        var truckRight = corners[2].x;
        var rs = GetComponentsInChildren<SpriteRenderer>();
        if (rs.Length == 0) return;
        var left = float.MaxValue;
        foreach (var r in rs) if (r.name == "Hen") left = r.bounds.min.x;
        if (left == float.MaxValue) foreach (var r in rs) left = Mathf.Min(left, r.bounds.min.x);
        var p = transform.position; p.x += truckRight + GapAfterTruck - left;
        // never past the screen middle (a very narrow truck rect would push it right)
        p.x = Mathf.Min(p.x, 0.6f);
        transform.position = p;
    }

    void Build()
    {
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/beachday SDF");
        // The board is the kitchen's little hen (owner, 13 October evening; Resources/UI/money_hen, drawn by
        // Store/artkit/money_hen.py with its contact shadow): the number sits on the white body, the three stars on
        // the red base. If the picture is missing, the old cream board is built instead.
        var hen = Resources.Load<Sprite>("UI/money_hen");
        // the hen is stretched: longer than tall, about the truck's height (owner, 13 October, late)
        var numberPos = new Vector3(0f, -0.02f, 0f); var starsY = -0.16f; var starGap = 0.2f; var starScale = 0.5f;   // the number just above the base's line (owner, 13 October)
        if (hen != null)
        {
            var h = new GameObject("Hen");
            h.transform.SetParent(transform, false);
            h.transform.localScale = new Vector3(HenScale * 0.75f, HenScale * 0.6f, 1f);   // 0.89 x 0.67 units: the truck's height, a quarter shorter again (owner, 13 October)
            var hsr = h.AddComponent<SpriteRenderer>(); hsr.sprite = hen; hsr.sortingOrder = 1;
        }
        else
        {
            Rect("Box", Vector3.zero, Width, Height, new Color(0.98f, 0.953f, 0.91f), 1);
            Rect("Line", Vector3.zero, Width - 0.03f, Height - 0.03f, new Color(0.667f, 0.588f, 0.47f), 1, hollow: true);
            numberPos = new Vector3(0f, -0.07f, 0f); starsY = Height / 2f - 0.11f; starGap = 0.26f; starScale = 0.8f;
        }
        // the number: warm brown with a cream outline, letters spaced out (owner, 13 October evening)
        var g = new GameObject("Money");
        g.transform.SetParent(transform, false);
        g.transform.localPosition = numberPos;
        money = g.AddComponent<TextMeshPro>();
        if (font != null) money.font = font;
        // the number in the filled star's yellow with a dark brown outline (owner, 13 October)
        money.fontSize = 1.7f;
        money.characterSpacing = 5f;
        money.color = new Color(1f, 0.78f, 0.3f);
        money.fontMaterial = new Material(money.fontSharedMaterial);
        money.fontMaterial.SetFloat("_OutlineWidth", 0.25f);
        money.fontMaterial.SetColor("_OutlineColor", new Color(0.27f, 0.15f, 0.05f));
        money.fontMaterial.DisableKeyword("UNDERLAY_ON");
        money.alignment = TextAlignmentOptions.Center;
        money.rectTransform.sizeDelta = new Vector2(1.2f, 0.36f);
        money.text = "";
        // stars on the base
        for (var k = 0; k < 3; k++)
        {
            var s = new GameObject("Star" + k);
            s.transform.SetParent(transform, false);
            s.transform.localPosition = new Vector3((k - 1) * starGap + numberPos.x, starsY, 0f);   // centred under the number
            s.transform.localScale = Vector3.one * starScale;
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
            if (!instant && earned > shownStars && shownStars >= 0) { var st = stars[Mathf.Clamp(earned - 1, 0, 2)].transform; StartCoroutine(Pop(st, 0.5f, st.localScale.x)); }
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

    /// The same five-point star for the level-complete panel (LevelManager)
    public static Sprite SharedStarSprite() => StarSprite();

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
