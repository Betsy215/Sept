#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hanging night light drafts, editor preview only (owner, 27 November 2026: "instead of a lamp on the table, let's do a
/// string floor light that drops from top center of screen when night comes, try other styles of light"). Not in any
/// build (the whole file is UNITY_EDITOR) and nothing in the game creates it: a capture script calls
/// HangingLightPreview.Show(style) in play mode; Show(0) removes it and restores the colours.
///
/// What it does, so it can be built for real:
///  - a light on a cord from the top centre of the screen (styles 1-4 pendants, 5 a string of fairy lights whose own
///    picture carries its cord). By day it is out of sight above the screen.
///  - when night comes (DayNightTint.CurrentStage past 1.5, the same threshold as the table lamp) it drops: the cord
///    lengthens to its rest length with a damped spring (one small bounce), the pendant swings a few degrees and settles,
///    then the light switches on with a short flicker and its glow fades in over 0.6 s. Unscaled time, so it also runs
///    while the editor captures pause the game; AnimTime >= 0 freezes the drop at that moment (frame-by-frame GIF).
///  - the light: the table lamp's pool (TableLampPreview, light A: soft tail exp(-Falloff d), GlowRadius 1.4), now
///    centred on the table's upper middle (PoolDrop below the cloth's back edge); a navy sheet over the cloth and a tint
///    on everything on the table keep the rest of the table dark. Additive warm light: one pool over the table, one
///    softer glow over the customers and the background round the light, and a halo round the shade.
/// Paintings and capture scripts: Store/lamp_2026-11-26 (hang_<n>_off/on/emit.png).
/// </summary>
public class HangingLightPreview : MonoBehaviour
{
    public static int Style = 0;                  // 0 = off (the game as it is), 1..5 = the drafts
    public static float AnimTime = -1f;           // >= 0: show the drop at this many seconds (frame captures)
    public static float GlowRadius = 1.4f, GlowStrength = 1f, Falloff = 1.6f, Dark = 0.45f, WarmPeak = 0.22f;
    public static bool SoftTail = true;
    public static float PoolDrop = 0.9f;          // the pool's centre this far below the cloth's back edge (the table's upper middle)
    public static float AirPeak = 0.18f;          // the soft glow over the customers and the background round the light
    public static float HaloPeak = 0.4f;
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

    private static HangingLightPreview instance;
    private Transform pivot; private SpriteRenderer fixture, emit, cord, sheet, pool, air, halo;
    private Sprite off, on, emitSprite;
    private float height, clothTop, poolY, restLength, startLength, dropStart = -1f, lastOn = -1f;
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

    private Sprite Load(string file, float ppuOverride = 0f)
    {
        var path = ArtDir + file; if (!File.Exists(path)) return null;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false); tex.LoadImage(File.ReadAllBytes(path)); tex.wrapMode = TextureWrapMode.Clamp;
        var px = tex.GetPixels32(); int w = tex.width, h = tex.height, x0 = w, x1 = -1, y0 = h, y1 = -1;
        for (var y = 0; y < h; y++) for (var x = 0; x < w; x++) if (px[y * w + x].a >= 128) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
        var ppu = ppuOverride > 0f ? ppuOverride : (Style == 5 ? Mathf.Max(1, x1 - x0) : Mathf.Max(1, h - y0)) / Size[Style];
        if (ppuOverride <= 0f) height = (h - y0) / ppu;                       // attach point (top) to the painted bottom
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), ppu);   // pivot at the attach point
    }

    private SpriteRenderer MakeSr(string name, Transform parent, Sprite s, string layer, int order)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = s; sr.sortingLayerName = layer; sr.sortingOrder = order; return sr;
    }

    private void Build()
    {
        foreach (Transform c in transform) Destroy(c.gameObject);
        off = Load($"hang_{Style}_off.png"); var ppu = off.pixelsPerUnit;
        on = Load($"hang_{Style}_on.png", ppu); emitSprite = Load($"hang_{Style}_emit.png", ppu);

        var cloth = GameObject.Find("TableCloth"); var front = GameObject.Find("ClothFront");
        var b = cloth != null ? cloth.GetComponent<SpriteRenderer>().bounds : new Bounds(new Vector3(0, -2.5f), new Vector3(4.6f, 5f));
        clothTop = b.max.y;
        if (front != null) b.Encapsulate(front.GetComponent<SpriteRenderer>().bounds);
        sheetBounds = b; poolY = clothTop - PoolDrop;

        // the light on its cord, hung from a pivot just above the top centre of the screen
        pivot = new GameObject("Pivot").transform; pivot.SetParent(transform, false); pivot.position = new Vector3(0f, TopY, -0.5f);
        if (Style != 5)
        {
            var white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply();
            cord = MakeSr("Cord", pivot, Sprite.Create(white, new Rect(0, 0, 1, 1), new Vector2(0.5f, 1f), 1f), "FoodItems", 70);
            cord.color = new Color(0.27f, 0.23f, 0.2f);
        }
        fixture = MakeSr("Fixture", pivot, off, "FoodItems", 70);
        emit = MakeSr("FixtureEmit", fixture.transform, emitSprite, "FoodItems", 72);
        restLength = TopY - RestY[Style];
        startLength = Style == 5 ? restLength - 1.0f : -(height + 0.3f);     // out of sight above the screen

        sheet = MakeSr("NightSheet", transform, null, "Table", 2);
        sheet.transform.position = new Vector3(b.center.x, b.center.y, 0f);
        BuildSheet();

        var add = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        add.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
        pool = MakeSr("WarmPool", transform, GlowSprite(Falloff, SoftTail), "FoodItems", 60); pool.sharedMaterial = add;
        pool.transform.position = new Vector3(0f, poolY, 0f); pool.transform.localScale = new Vector3(Rx, Ry, 1f);
        air = MakeSr("AirGlow", transform, GlowSprite(1.5f, false), "FoodItems", 61); air.sharedMaterial = add;
        air.transform.position = new Vector3(0f, (RestY[Style] + clothTop) * 0.5f, 0f); air.transform.localScale = new Vector3(1.7f, 2.0f, 1f);
        halo = MakeSr("Halo", fixture.transform, GlowSprite(1.5f, false), "FoodItems", 69); halo.sharedMaterial = add;
        halo.transform.localPosition = new Vector3(0f, -height * LightAt[Style], 0f);
        halo.transform.localScale = Style == 5 ? new Vector3(1.5f, 0.35f, 1f) : new Vector3(0.5f, 0.5f, 1f);
        dropStart = -1f; lastOn = -1f;
        Apply(0f, 0f);
    }

    private float Rx => 2.0f * GlowRadius;
    private float Ry => 1.7f * GlowRadius;
    private static float Curve(float d2, float falloff, bool soft) => soft ? Mathf.Exp(-falloff * Mathf.Sqrt(d2)) : Mathf.Exp(-falloff * d2);
    private float Lift(float x, float y) { var dx = x / Rx; var dy = (y - poolY) / Ry; return Mathf.Clamp01(0.85f * GlowStrength * Curve(dx * dx + dy * dy, Falloff, SoftTail)); }

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

    private void BuildSheet(float on = 1f)
    {
        const int n = 96; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n]; var b = sheetBounds;
        for (var y = 0; y < n; y++) for (var x = 0; x < n; x++)
            px[y * n + x] = new Color(Navy.r, Navy.g, Navy.b, 1f - on * Lift(b.min.x + (x + 0.5f) / n * b.size.x, b.min.y + (y + 0.5f) / n * b.size.y));
        tex.SetPixels(px); tex.Apply();
        sheet.sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n / b.size.y);
        sheet.transform.localScale = new Vector3(b.size.x / b.size.y, 1f, 1f);
    }

    private void Update()
    {
        if (Style == 0 || fixture == null) return;
        var s = DayNightTint.CurrentStage;
        var k = s <= 1f ? 0.4f * s : 0.4f + 0.6f * Mathf.Clamp01(s - 1f);
        if (k > 0.75f && dropStart < 0f) dropStart = Time.unscaledTime;
        if (k <= 0.75f) dropStart = -1f;
        var t = AnimTime >= 0f ? AnimTime : dropStart < 0f ? -1f : Time.unscaledTime - dropStart;
        Apply(k, t);
    }

    /// k: night strength; t: seconds since the drop started (negative = not dropped)
    private void Apply(float k, float t)
    {
        float length, swing = 0f, lit = 0f;
        if (t < 0f) length = startLength;
        else
        {
            length = restLength + (startLength - restLength) * Mathf.Exp(-5f * t) * Mathf.Cos(8f * t);   // one small bounce
            if (Style != 5) swing = 7f * Mathf.Exp(-2.2f * t) * Mathf.Sin(6.5f * t);
            lit = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 1.0f) / 0.6f));
            if (t > 1.0f && t < 1.3f) lit *= Mathf.Sin(t * 70f) > -0.3f ? 1f : 0.35f;                     // a short flicker as it catches
        }
        pivot.localRotation = Quaternion.Euler(0f, 0f, swing);
        fixture.transform.localPosition = new Vector3(0f, -length, 0f);
        if (cord != null) { cord.enabled = length > 0f; cord.transform.localScale = new Vector3(0.022f, Mathf.Max(0f, length), 1f); }
        fixture.sprite = lit > 0.5f ? on : off;
        emit.enabled = lit > 0.01f; emit.color = new Color(1f, 1f, 1f, lit);
        var dim = Mathf.Lerp(1f, 0.8f, k * (1f - lit));
        fixture.color = new Color(dim, dim, dim * 1.05f);
        if (cord != null) cord.color = new Color(0.27f, 0.23f, 0.2f) * dim;

        if (Mathf.Abs(lit - lastOn) > 0.02f || (lit >= 1f && lastOn < 1f)) { BuildSheet(lit); lastOn = lit; }
        sheet.color = new Color(1f, 1f, 1f, Dark * k);
        pool.color = new Color(Warm.r, Warm.g, Warm.b, Mathf.Clamp01(WarmPeak * GlowStrength * lit * k));
        air.color = new Color(Warm.r, Warm.g, Warm.b, Mathf.Clamp01(AirPeak * lit * k));
        halo.color = new Color(1f, 0.8f, 0.45f, HaloPeak * lit * k);

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
