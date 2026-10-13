using System.Collections;
using TMPro;
using UnityEngine;

/// The money and the day's stars on the table (owner, 12-13 October): a small flip clock next to the truck button,
/// a little longer and a little shorter than the truck. Four split cards flip when the wallet changes; the three
/// stars sit on top and turn gold as the day passes each threshold. No text, nothing else. Replaces the sign at the
/// top of the screen (LevelManager hides it). Built at runtime from procedural sprites and the game font.
public class MoneyFlipClock : MonoBehaviour
{
    public static Vector3 Position = new Vector3(-0.12f, 0.12f, 0f);   // just right of the truck button
    public const float Width = 1.4f, Height = 0.62f;                  // world units; the truck button is about 1.2 x 0.7
    const int Digits = 4;
    const float FlipSeconds = 0.16f;

    static Sprite roundRect, starSprite;
    ScoreManager scoreManager;
    readonly Card[] cards = new Card[Digits];
    readonly SpriteRenderer[] stars = new SpriteRenderer[3];
    int shownMoney = -1, shownStars = -1;

    class Card { public Transform root; public TextMeshPro top, bottom; public string digit = ""; public Coroutine flip; }

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
        // shadow, box, wooden foot
        Rect("Shadow", new Vector3(-0.05f, -0.05f, 0f), Width + 0.06f, Height + 0.04f, new Color(0.13f, 0.125f, 0.22f, 0.45f), 0);
        Rect("Box", Vector3.zero, Width, Height, new Color(0.98f, 0.953f, 0.91f), 1);
        Rect("Line", Vector3.zero, Width - 0.03f, Height - 0.03f, new Color(0.667f, 0.588f, 0.47f), 1, hollow: true);
        Rect("Foot", new Vector3(0f, -Height / 2f + 0.03f, 0f), Width - 0.12f, 0.07f, new Color(0.69f, 0.52f, 0.36f), 2);
        // cards: numbers in the middle bottom
        var cardW = 0.2f; var cardH = 0.3f; var gap = 0.035f; var y = -0.07f;
        var x0 = -(Digits * cardW + (Digits - 1) * gap) / 2f + cardW / 2f;
        for (var i = 0; i < Digits; i++)
        {
            var cx = x0 + i * (cardW + gap);
            // a classic flip clock: dark warm cards, cream digits, a thin seam (owner 13 October: the old cream cards with
            // the green-glow font looked odd)
            Rect("Slot" + i, new Vector3(cx, y, 0f), cardW + 0.03f, cardH + 0.03f, new Color(0.42f, 0.35f, 0.29f), 3);
            var card = new Card { root = new GameObject("Card" + i).transform };
            card.root.SetParent(transform, false);
            card.root.localPosition = new Vector3(cx, y, 0f);
            Rect("Top", new Vector3(0f, cardH / 4f + 0.004f, 0f), cardW, cardH / 2f - 0.008f, new Color(0.3f, 0.26f, 0.23f), 4, card.root);
            Rect("Bottom", new Vector3(0f, -cardH / 4f - 0.004f, 0f), cardW, cardH / 2f - 0.008f, new Color(0.24f, 0.21f, 0.18f), 4, card.root);
            card.top = Glyph("TopGlyph", card.root, font, new Vector3(0f, 0.002f, 0f), cardH);
            card.bottom = Glyph("BottomGlyph", card.root, font, new Vector3(0f, 0.002f, 0f), cardH);
            // each glyph is clipped to its half by the seam drawn over the middle
            Rect("Seam", Vector3.zero, cardW + 0.03f, 0.01f, new Color(0.42f, 0.35f, 0.29f), 7, card.root);
            cards[i] = card;
        }
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
        foreach (var t in GetComponentsInChildren<TextMeshPro>()) { t.sortingLayerID = SortingLayer.NameToID("FoodItems"); t.sortingOrder = 6; }
        Refresh(true);
    }

    TextMeshPro Glyph(string name, Transform parent, TMP_FontAsset font, Vector3 pos, float cardH)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        var t = g.AddComponent<TextMeshPro>();
        if (font != null) t.font = font;
        t.fontSize = 2.8f;
        t.color = new Color(0.99f, 0.96f, 0.9f);
        // the game font's material has a black outline and a green glow; the clock digits are plain
        t.fontMaterial = new Material(t.fontSharedMaterial);
        t.fontMaterial.SetFloat("_OutlineWidth", 0f);
        t.fontMaterial.SetFloat("_FaceDilate", 0.05f);
        t.fontMaterial.DisableKeyword("UNDERLAY_ON");
        t.alignment = TextAlignmentOptions.Center;
        t.rectTransform.sizeDelta = new Vector2(0.3f, cardH);
        t.text = "";
        return t;
    }

    void Update() => Refresh(false);

    void Refresh(bool instant)
    {
        var money = SessionManager.Instance != null && SessionManager.Instance.HasActiveSession() ? Mathf.FloorToInt(SessionManager.Instance.GetTotalScore()) : 0;
        money = Mathf.Clamp(money, 0, 9999);
        if (money != shownMoney)
        {
            var text = money.ToString().PadLeft(Digits, ' ');
            for (var i = 0; i < Digits; i++)
            {
                var d = text[i] == ' ' ? "" : text[i].ToString();
                if (d == cards[i].digit) continue;
                if (instant) SetDigit(cards[i], d);
                else { if (cards[i].flip != null) StopCoroutine(cards[i].flip); cards[i].flip = StartCoroutine(Flip(cards[i], d)); }
            }
            shownMoney = money;
        }
        var earned = scoreManager != null && scoreManager.starProgressBar != null ? scoreManager.starProgressBar.GetEarnedStarsCount() : 0;
        if (earned != shownStars)
        {
            for (var k = 0; k < 3; k++) stars[k].color = k < earned ? new Color(1f, 0.78f, 0.3f) : new Color(0.78f, 0.74f, 0.68f);
            if (!instant && earned > shownStars && shownStars >= 0) StartCoroutine(Pop(stars[Mathf.Clamp(earned - 1, 0, 2)].transform));
            shownStars = earned;
        }
    }

    static void SetDigit(Card c, string d) { c.digit = d; c.top.text = d; c.bottom.text = d; }

    /// The split-flap fold: the card closes on the old digit and opens on the new one
    IEnumerator Flip(Card c, string d)
    {
        var t = 0f;
        while (t < FlipSeconds) { t += Time.deltaTime; c.root.localScale = new Vector3(1f, Mathf.Max(0.02f, 1f - t / FlipSeconds), 1f); yield return null; }
        SetDigit(c, d);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayItemPickup();
        t = 0f;
        while (t < FlipSeconds) { t += Time.deltaTime; c.root.localScale = new Vector3(1f, Mathf.Min(1f, t / FlipSeconds), 1f); yield return null; }
        c.root.localScale = Vector3.one;
        c.flip = null;
    }

    IEnumerator Pop(Transform star)
    {
        var t = 0f;
        while (t < 0.4f) { t += Time.deltaTime; star.localScale = Vector3.one * (0.95f + 0.5f * Mathf.Sin(Mathf.Clamp01(t / 0.4f) * Mathf.PI)); yield return null; }
        star.localScale = Vector3.one * 0.95f;
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
