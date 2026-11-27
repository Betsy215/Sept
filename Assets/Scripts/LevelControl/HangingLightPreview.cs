#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hanging night light drafts, editor preview only (owner, 27 November 2026: "instead of a lamp on the table, let's do a
/// string floor light that drops from top center of screen when night comes, try other styles of light"; then "i don't
/// want the light too much over the customers, just over the table", then "i think the position of the lamp is good, can
/// we just adjust the light it sheds"). Not in any build (the whole file is UNITY_EDITOR) and nothing in the game creates
/// it: a capture script calls HangingLightPreview.Show(style) in play mode; Show(0) removes it and restores the colours.
///
/// What it does, so it can be built for real:
///  - the light hangs from the top centre on a short cord, above the order bubbles and between the two top buttons.
///  - by day the light is out of sight above the screen. When night comes (DayNightTint.CurrentStage past 1.5) it drops:
///    the cord lengthens to its rest length with a damped spring (one small bounce), the pendant swings a few degrees and
///    settles, then the light switches on with a short flicker and fades in over 0.6 s. Unscaled time, so it also runs
///    while the editor captures pause the game; AnimTime >= 0 freezes the drop at that moment (frame-by-frame GIF).
///  - the light on the table: lift = 0.85 * GlowStrength * sum of exp(-Falloff d) over the pools (d in an ellipse
///    2.0 x 1.7 pool radii; the table lamp's light A curve). A navy sheet over the cloth (alpha Dark * night * (1 - lift))
///    and a tint on everything on the table keep the rest dark; one additive warm layer covers exactly the cloth
///    (WarmPeak * lift), fading in over the first 0.4 units below the cloth's back edge so no line shows there. Above the
///    table only a small halo round the shade and an almost invisible glow (AirPeak 0.04) on the customers and background
///    (27 November: the first round's 0.18 glow over the customers was too much).
/// Paintings and capture scripts: Store/lamp_2026-11-26 (hang_<n>_off/on/emit.png).
/// </summary>
public class HangingLightPreview : MonoBehaviour
{
    public static int Style = 0;                  // 0 = off (the game as it is), 1..5 = the drafts
    public static float AnimTime = -1f;           // >= 0: show the drop at this many seconds (frame captures)
    public static float GlowRadius = 1.4f, GlowStrength = 1f, Falloff = 1.6f, Dark = 0.45f, WarmPeak = 0.22f;
    public static bool SoftTail = true;
    public static float PoolDrop = 0.9f;          // the pool's centre this far below the cloth's back edge
    public static float AirPeak = 0.04f;          // the glow over the customers and the background round the light (first round 0.18)
    public static float HaloPeak = 0.4f;          // the small halo round the shade
    public static string ArtDir = "/Users/betzthebest/Desktop/Sept/Store/lamp_2026-11-26/";

    // per style: painted height (pendants) or width (fairy lights) in world units, the attach point's resting y,
    // the light's centre below the attach point as a share of the painted height
    private static readonly float[] Size = { 0f, 0.75f, 0.75f, 0.75f, 0.75f, 3.0f };
    private static readonly float[] RestY = { 0f, 4.45f, 4.45f, 4.45f, 4.45f, 5.0f };
    private static readonly float[] LightAt = { 0f, 0.8f, 0.85f, 0.62f, 0.55f, 0.75f };
    private static readonly Color NightTint = new Color(0.58f, 0.60f, 0.74f);
    private static readonly Color Navy = new Color(0.07f, 0.08f, 0.21f);
    private static readonly Color Warm = new Color(1f, 0.62f, 0.26f);
    private const float TopY = 5.05f;             // the cord's top: just above the screen (camera size 5)
    private const float TopFade = 0.4f;

    private class Hang { public Transform pivot; public SpriteRenderer fixture, emit, cord, halo; public float rest, start, delay; }
    private readonly List<Hang> hangs = new List<Hang>();
    private readonly List<Vector3> pools = new List<Vector3>();   // x, y, radius

    private static HangingLightPreview instance;
    private SpriteRenderer sheet, warm, air;
    private Sprite off, on, emitSprite;
    private float height, clothTop, dropStart = -1f, lastOn = -1f;
    private Bounds sheetBounds;
    private readonly Dictionary<Object, Color> original = new Dictionary<Object, Color>();
    private float nextScan;

    public static string Show(int style)
    {
        Style = style;
        if (style == 0) { if (instance != null) { instance.Restore(); Destroy(instance.gameObject); } return "hanging light off"; }
        if (instance == null) instance = new GameObject("HangingLightPreview").AddComponent<HangingLightPreview>();
        instance.Build();
        return "hanging light " + style;
    }

    private Sprite Load(string file, float size, float ppuOverride = 0f)
    {
        var path = ArtDir + file; if (!File.Exists(path)) return null;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false); tex.LoadImage(File.ReadAllBytes(path)); tex.wrapMode = TextureWrapMode.Clamp;
        var px = tex.GetPixels32(); int w = tex.width, h = tex.height, x0 = w, x1 = -1, y0 = h;
        for (var y = 0; y < h; y++) for (var x = 0; x < w; x++) if (px[y * w + x].a >= 128) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; }
        var ppu = ppuOverride > 0f ? ppuOverride : (Style == 5 ? Mathf.Max(1, x1 - x0) : Mathf.Max(1, h - y0)) / size;
        if (ppuOverride <= 0f) height = (h - y0) / ppu;                       // attach point (top) to the painted bottom
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), ppu);   // pivot at the attach point
    }

    private SpriteRenderer MakeSr(string name, Transform parent, Sprite s, string layer, int order)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = s; sr.sortingLayerName = layer; sr.sortingOrder = order; return sr;
    }

    private Material additive;

    private void Build()
    {
        foreach (Transform c in transform) Destroy(c.gameObject);
        hangs.Clear(); pools.Clear(); air = null;
        additive = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        additive.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));   // the shader doubles: 0.5 = colour as given

        var cloth = GameObject.Find("TableCloth"); var front = GameObject.Find("ClothFront");
        var b = cloth != null ? cloth.GetComponent<SpriteRenderer>().bounds : new Bounds(new Vector3(0, -2.5f), new Vector3(4.6f, 5f));
        clothTop = b.max.y;
        if (front != null) b.Encapsulate(front.GetComponent<SpriteRenderer>().bounds);
        sheetBounds = b;

        // the lights and where they hang
        off = Load($"hang_{Style}_off.png", Size[Style]); var ppu = off.pixelsPerUnit;
        on = Load($"hang_{Style}_on.png", Size[Style], ppu); emitSprite = Load($"hang_{Style}_emit.png", Size[Style], ppu);
        AddHang(0f, RestY[Style], 0f);
        pools.Add(new Vector3(0f, clothTop - PoolDrop, GlowRadius));   // the pool on the table (a list, so more lights can add theirs)

        sheet = MakeSr("NightSheet", transform, null, "Table", 2);
        warm = MakeSr("WarmOnCloth", transform, null, "FoodItems", 60); warm.sharedMaterial = additive;
        foreach (var sr in new[] { sheet, warm }) sr.transform.position = new Vector3(b.center.x, b.center.y, 0f);
        BuildSheets(0f);
        air = MakeSr("AirGlow", transform, GlowSprite(1.5f, false), "FoodItems", 61); air.sharedMaterial = additive;
        air.transform.position = new Vector3(0f, (RestY[Style] + clothTop) * 0.5f, 0f); air.transform.localScale = new Vector3(1.7f, 2.0f, 1f);
        dropStart = -1f; lastOn = -1f;
        Apply(0f, 0f);
    }

    private void AddHang(float x, float restAttachY, float delay)
    {
        var h = new Hang { delay = delay };
        h.pivot = new GameObject("Pivot").transform; h.pivot.SetParent(transform, false); h.pivot.position = new Vector3(x, TopY, -0.5f);
        if (Style != 5)
        {
            var white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply();
            h.cord = MakeSr("Cord", h.pivot, Sprite.Create(white, new Rect(0, 0, 1, 1), new Vector2(0.5f, 1f), 1f), "FoodItems", 70);
            h.cord.color = new Color(0.27f, 0.23f, 0.2f);
        }
        h.fixture = MakeSr("Fixture", h.pivot, off, "FoodItems", 70);
        h.emit = MakeSr("FixtureEmit", h.fixture.transform, emitSprite, "FoodItems", 72);
        h.halo = MakeSr("Halo", h.fixture.transform, GlowSprite(1.5f, false), "FoodItems", 69); h.halo.sharedMaterial = additive;
        h.halo.transform.localPosition = new Vector3(0f, -height * LightAt[Style], 0f);
        h.halo.transform.localScale = Style == 5 ? new Vector3(1.5f, 0.35f, 1f) : new Vector3(0.5f, 0.5f, 1f);
        h.rest = TopY - restAttachY;                                           // cord length at rest (negative: attach above the screen)
        h.start = Style == 5 ? h.rest - 1.0f : Mathf.Min(h.rest, 0f) - (height + 0.3f);   // out of sight above the screen
        hangs.Add(h);
    }

    private static float Curve(float d2, float falloff, bool soft) => soft ? Mathf.Exp(-falloff * Mathf.Sqrt(d2)) : Mathf.Exp(-falloff * d2);
    private float Raw(float x, float y)
    {
        var sum = 0f;
        foreach (var p in pools) { var dx = (x - p.x) / (2.0f * p.z); var dy = (y - p.y) / (1.7f * p.z); sum += Curve(dx * dx + dy * dy, Falloff, SoftTail); }
        return sum;
    }
    private float Lift(float x, float y) => Mathf.Clamp01(0.85f * GlowStrength * Raw(x, y));

    private static readonly Dictionary<float, Sprite> glowSprites = new Dictionary<float, Sprite>();
    private static Sprite GlowSprite(float falloff, bool soft)
    {
        var key = falloff + (soft ? 1000f : 0f);
        if (glowSprites.TryGetValue(key, out var cached) && cached != null) return cached;
        const int n = 256; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n];
        for (var y = 0; y < n; y++) for (var x = 0; x < n; x++)
        {
            var dx = (x + 0.5f - n / 2f) / (n / 2f) * 2.2f; var dy = (y + 0.5f - n / 2f) / (n / 2f) * 2.2f;
            var g = Curve(dx * dx + dy * dy, falloff, soft) * Mathf.Clamp01((2.2f - Mathf.Sqrt(dx * dx + dy * dy)) * 2f);
            px[y * n + x] = new Color(1f, 1f, 1f, g);
        }
        tex.SetPixels(px); tex.Apply();
        var sp = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n / 2f / 2.2f); glowSprites[key] = sp; return sp;
    }

    /// the navy sheet (dark everywhere the light does not reach) and the warm additive layer, both exactly over the cloth
    private void BuildSheets(float lit)
    {
        const int n = 96; var b = sheetBounds;
        var dark = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var glow = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pd = new Color[n * n]; var pg = new Color[n * n];
        for (var y = 0; y < n; y++) for (var x = 0; x < n; x++)
        {
            var wx = b.min.x + (x + 0.5f) / n * b.size.x; var wy = b.min.y + (y + 0.5f) / n * b.size.y;
            var lift = Lift(wx, wy);
            pd[y * n + x] = new Color(Navy.r, Navy.g, Navy.b, 1f - lit * lift);   // (the dark is lifted to the cloth's edge: the cloth meets the night background there anyway)
            var fade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((clothTop - wy) / TopFade));     // no line at the cloth's back edge
            pg[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(lift / 0.85f) * fade);
        }
        dark.SetPixels(pd); dark.Apply(); glow.SetPixels(pg); glow.Apply();
        sheet.sprite = Sprite.Create(dark, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n / b.size.y);
        warm.sprite = Sprite.Create(glow, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n / b.size.y);
        foreach (var sr in new[] { sheet, warm }) sr.transform.localScale = new Vector3(b.size.x / b.size.y, 1f, 1f);
    }

    private void Update()
    {
        if (Style == 0 || sheet == null) return;
        var s = DayNightTint.CurrentStage;
        var k = s <= 1f ? 0.4f * s : 0.4f + 0.6f * Mathf.Clamp01(s - 1f);
        if (k > 0.75f && dropStart < 0f) dropStart = Time.unscaledTime;
        if (k <= 0.75f) dropStart = -1f;
        var t = AnimTime >= 0f ? AnimTime : dropStart < 0f ? -1f : Time.unscaledTime - dropStart;
        Apply(k, t);
    }

    private static float LitAt(float t)
    {
        if (t < 0f) return 0f;
        var lit = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 1.0f) / 0.6f));
        if (t > 1.0f && t < 1.3f) lit *= Mathf.Sin(t * 70f) > -0.3f ? 1f : 0.35f;                         // a short flicker as it catches
        return lit;
    }

    /// k: night strength; t: seconds since the drop started (negative = not dropped)
    private void Apply(float k, float t)
    {
        var lit = LitAt(t);
        foreach (var h in hangs)
        {
            var th = t < 0f ? -1f : Mathf.Max(0f, t - h.delay);
            float length = h.start, swing = 0f;
            if (th >= 0f)
            {
                length = h.rest + (h.start - h.rest) * Mathf.Exp(-5f * th) * Mathf.Cos(8f * th);      // one small bounce
                if (Style != 5) swing = 7f * Mathf.Exp(-2.2f * th) * Mathf.Sin(6.5f * th);
            }
            var hl = LitAt(th);
            h.pivot.localRotation = Quaternion.Euler(0f, 0f, swing);
            h.fixture.transform.localPosition = new Vector3(0f, -length, 0f);
            if (h.cord != null) { h.cord.enabled = length > 0f; h.cord.transform.localScale = new Vector3(0.022f, Mathf.Max(0f, length), 1f); }
            h.fixture.sprite = hl > 0.5f ? on : off;
            h.emit.enabled = hl > 0.01f; h.emit.color = new Color(1f, 1f, 1f, hl);
            var dim = Mathf.Lerp(1f, 0.8f, k * (1f - hl));
            h.fixture.color = new Color(dim, dim, dim * 1.05f);
            if (h.cord != null) h.cord.color = new Color(0.27f, 0.23f, 0.2f) * dim;
            if (h.halo != null) h.halo.color = new Color(1f, 0.8f, 0.45f, HaloPeak * hl * k);
        }

        if (Mathf.Abs(lit - lastOn) > 0.02f || (lit >= 1f && lastOn < 1f)) { BuildSheets(lit); lastOn = lit; }
        sheet.color = new Color(1f, 1f, 1f, Dark * k);
        warm.color = new Color(Warm.r, Warm.g, Warm.b, Mathf.Clamp01(WarmPeak * GlowStrength * lit * k));
        if (air != null) air.color = new Color(Warm.r, Warm.g, Warm.b, Mathf.Clamp01(AirPeak * lit * k));

        if (Time.unscaledTime >= nextScan || AnimTime >= 0f)
        {
            nextScan = Time.unscaledTime + 0.1f;
            foreach (var sr in FindObjectsOfType<SpriteRenderer>())
            {
                if (sr.sortingLayerName != "FoodItems" || sr.transform.IsChildOf(transform)) continue;
                if (!original.ContainsKey(sr)) original[sr] = sr.color;
                var bb = sr.bounds; var d = k * (1f - lit * Lift(bb.center.x, bb.min.y + 0.15f * bb.size.y));
                sr.color = original[sr] * Color.Lerp(Color.white, NightTint, d * Dark / 0.42f);
            }
            var truck = GameObject.Find("KitchenButton"); var img = truck != null ? truck.GetComponent<Image>() : null;
            if (img != null)
            {
                if (!original.ContainsKey(img)) original[img] = img.color;
                var c = new Vector3[4]; img.rectTransform.GetWorldCorners(c);
                var d = k * (1f - lit * Lift((c[0].x + c[2].x) / 2f, c[0].y));
                img.color = original[img] * Color.Lerp(Color.white, NightTint, d * Dark / 0.42f);
            }
        }
    }

    private void Restore()
    {
        foreach (var kv in original)
        {
            if (kv.Key is SpriteRenderer sr && sr != null) sr.color = kv.Value;
            else if (kv.Key is Image im && im != null) im.color = kv.Value;
        }
        original.Clear();
    }
}
#endif
