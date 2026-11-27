#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Table lamp drafts, editor preview only (owner, 26 November 2026: "a table lamp on the table, in the fixed table items
/// area, in the middle. When night comes the table gets darker, this light turns on automatically and the table gets a
/// broad gentle warm radiance"). Not in any build (the whole file is UNITY_EDITOR) and nothing in the game creates it:
/// a capture script calls TableLampPreview.Show(design) in play mode; Design 0 removes it and restores the colours.
///
/// What it does, so it can be built for real:
///  - the lamp: a SpriteRenderer in the back row (FoodItems layer, order 0, behind the hen and the jar), its painted
///    bottom on the pots' line (TableBackRowAlign), in the gap between the hen and the candy jar; OFF picture by day, ON
///    picture at night with its glowing part (the emit picture) drawn again on top, untinted.
///  - night strength k from DayNightTint.CurrentStage (dusk 0.4, night 1).
///  - the table darkens: one navy sheet over the cloth (Table layer, order 2: over the cloth and its front drop, under
///    everything on it) whose alpha is Dark * k * (1 - pool); everything on the FoodItems layer (food, stations, hen,
///    jar, pots) and the truck button get a multiply tint toward NightTint by the same amount at their foot.
///  - pool = exp(-1.5 d^2), d the distance from the lamp's foot in an ellipse GlowRadius * (2.0 wide, 1.7 tall): the
///    lamp lifts the dark around itself, most near the lamp, nothing far away.
///  - the warm light: one large additive sprite (Legacy Shaders/Particles/Additive, a 256 px exp falloff texture made at
///    runtime) over the table and the back row, peak GlowStrength * 0.22 of warm orange; plus a small additive halo
///    round the shade or flame.
/// Captures: Store/lamp_2026-11-26/preview.
/// </summary>
public class TableLampPreview : MonoBehaviour
{
    public static int Design = 0;                 // 0 = off (the game as it is), 1..4 = the drafts
    public static float GlowRadius = 1f;          // size of the light pool (1 = broad: about the table's width)
    public static float GlowStrength = 1f;        // how much the lamp lifts the dark and how warm it gets
    public static float Dark = 0.42f;             // the navy sheet's alpha at full night, far from the lamp
    public static float LampHeight = 0.9f;        // painted height in world units (the candy jar is about 0.73)
    public static float LampX = 0.16f;            // the middle of the gap between the hen and the jar
    public static string ArtDir = "/Users/betzthebest/Desktop/Sept/Store/lamp_2026-11-26/";

    // the light's height in each painting, as a share of the painted height from the bottom (shade, flame or glass)
    private static readonly float[] LightY = { 0f, 0.44f, 0.72f, 0.66f, 0.79f };
    private static readonly Color NightTint = new Color(0.58f, 0.60f, 0.74f);
    private static readonly Color Navy = new Color(0.07f, 0.08f, 0.21f);
    private static readonly Color Warm = new Color(1f, 0.62f, 0.26f);

    private static TableLampPreview instance;
    private int built = -1; private float builtR = -1f;
    private SpriteRenderer lamp, emit, sheet, glow, halo;
    private Sprite off, on, emitSprite;
    private float lineY, lightY;
    private readonly Dictionary<Object, Color> original = new Dictionary<Object, Color>();
    private float nextScan;

    public static string Show(int design)
    {
        Design = design;
        if (design == 0) { if (instance != null) { instance.Restore(); Destroy(instance.gameObject); } return "lamp off"; }
        if (instance == null) instance = new GameObject("TableLampPreview").AddComponent<TableLampPreview>();
        instance.Build();
        return "lamp " + design + " at " + instance.lamp.transform.position.ToString("F2");
    }

    private Sprite Load(string file, out int solidRow, out int top, float ppuOverride = 0f)
    {
        solidRow = 0; top = 0;
        var path = ArtDir + file; if (!File.Exists(path)) return null;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false); tex.LoadImage(File.ReadAllBytes(path));
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = tex.GetPixels32(); var w = tex.width; var h = tex.height; var lo = -1; var hi = -1;
        for (var y = 0; y < h; y++) for (var x = 0; x < w; x++) if (px[y * w + x].a >= 128) { if (lo < 0) lo = y; hi = y; break; }
        for (var y = 0; y < h; y++) { var solid = false; for (var x = 0; x < w; x++) if (px[y * w + x].a >= 230) { solid = true; break; } if (solid) { solidRow = y; break; } }
        top = hi;
        var ppu = ppuOverride > 0f ? ppuOverride : Mathf.Max(1, hi - solidRow) / LampHeight;   // ON and emit use OFF's scale
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), ppu);
    }

    private void Build()
    {
        foreach (Transform c in transform) Destroy(c.gameObject);
        int solid, top;
        off = Load($"lamp_{Design}_off.png", out solid, out top);
        var ppu = off.pixelsPerUnit;
        on = Load($"lamp_{Design}_on.png", out _, out _, ppu);
        emitSprite = Load($"lamp_{Design}_emit.png", out _, out _, ppu);

        // the back row's line: the flower pot's painted bottom (as TableBackRowAlign.Snap uses)
        var decor = GameObject.Find("TableDecor");
        var pot = decor != null ? decor.transform.Find("PotFlowers") : null;
        var potSr = pot != null ? pot.GetComponent<SpriteRenderer>() : null;
        lineY = potSr != null ? TableBackRowAlign.VisibleBottom(potSr) : -0.25f;
        var pos = new Vector3(LampX, lineY - solid / ppu, -0.02f);
        lightY = lineY + LampHeight * LightY[Design];

        lamp = MakeSr("Lamp", off, "FoodItems", 0, pos);
        emit = MakeSr("LampEmit", emitSprite, "FoodItems", 0, pos + new Vector3(0, 0, -0.01f));

        // the navy sheet over the cloth
        var cloth = GameObject.Find("TableCloth"); var front = GameObject.Find("ClothFront");
        var b = cloth != null ? cloth.GetComponent<SpriteRenderer>().bounds : new Bounds(new Vector3(0, -2.5f), new Vector3(4.6f, 5f));
        if (front != null) b.Encapsulate(front.GetComponent<SpriteRenderer>().bounds);
        sheet = MakeSr("NightSheet", null, "Table", 2, new Vector3(b.center.x, b.center.y, 0f));
        sheetBounds = b;

        // the warm light (additive), over the back row and the table
        var add = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        add.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));   // the shader doubles: 0.5 = colour as given
        glow = MakeSr("WarmGlow", GlowSprite(), "FoodItems", 60, new Vector3(LampX, lineY - 0.2f, 0f));
        glow.sharedMaterial = add;
        halo = MakeSr("ShadeHalo", GlowSprite(), "FoodItems", 61, new Vector3(LampX, lightY, 0f));
        halo.sharedMaterial = add;
        built = Design; builtR = -1f;
    }

    private Bounds sheetBounds;

    private SpriteRenderer MakeSr(string name, Sprite s, string layer, int order, Vector3 pos)
    {
        var go = new GameObject(name); go.transform.SetParent(transform, false); go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = s; sr.sortingLayerName = layer; sr.sortingOrder = order;
        return sr;
    }

    private static Sprite glowSprite;
    private static Sprite GlowSprite()
    {
        if (glowSprite != null) return glowSprite;
        const int n = 256; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n];
        for (var y = 0; y < n; y++) for (var x = 0; x < n; x++)
        {
            var dx = (x + 0.5f - n / 2f) / (n / 2f) * 2.2f; var dy = (y + 0.5f - n / 2f) / (n / 2f) * 2.2f;   // the texture spans +-2.2 radii
            var g = Mathf.Exp(-1.5f * (dx * dx + dy * dy)); g *= Mathf.Clamp01((2.2f - Mathf.Sqrt(dx * dx + dy * dy)) * 2f);
            px[y * n + x] = new Color(1f, 1f, 1f, g);   // additive: adds colour * alpha * g
        }
        tex.SetPixels(px); tex.Apply();
        glowSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n / 2f / 2.2f);   // 1 world unit = 1 radius
        return glowSprite;
    }

    private float Rx => 2.0f * GlowRadius;
    private float Ry => 1.7f * GlowRadius;
    private float Pool(float x, float y)
    {
        var dx = (x - LampX) / Rx; var dy = (y - (lineY - 0.2f)) / Ry; return Mathf.Exp(-1.5f * (dx * dx + dy * dy));
    }
    private float Lift(float x, float y) => Mathf.Clamp01(0.85f * GlowStrength * Pool(x, y));

    private void BuildSheet()
    {
        const int n = 128; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n]; var b = sheetBounds;
        for (var y = 0; y < n; y++) for (var x = 0; x < n; x++)
        {
            var wx = b.min.x + (x + 0.5f) / n * b.size.x; var wy = b.min.y + (y + 0.5f) / n * b.size.y;
            px[y * n + x] = new Color(Navy.r, Navy.g, Navy.b, 1f - Lift(wx, wy));
        }
        tex.SetPixels(px); tex.Apply();
        sheet.sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n / b.size.y);
        sheet.transform.localScale = new Vector3(b.size.x / b.size.y, 1f, 1f);
        glow.transform.localScale = new Vector3(Rx, Ry, 1f);
        halo.transform.localScale = new Vector3(0.32f, 0.32f, 1f);
        builtR = GlowRadius * 1000f + GlowStrength;
    }

    private void Update()
    {
        if (Design == 0 || lamp == null) return;
        if (built != Design) Build();
        if (builtR != GlowRadius * 1000f + GlowStrength) BuildSheet();
        var s = DayNightTint.CurrentStage;
        var k = s <= 1f ? 0.4f * s : 0.4f + 0.6f * Mathf.Clamp01(s - 1f);
        var lit = k > 0.3f;
        lamp.sprite = lit ? on : off;
        lamp.color = Color.Lerp(Color.white, new Color(0.86f, 0.84f, 0.9f), k);
        emit.enabled = lit;
        sheet.color = new Color(1f, 1f, 1f, Dark * k);
        var g = lit ? k : 0f;
        glow.color = new Color(Warm.r, Warm.g, Warm.b, Mathf.Clamp01(0.22f * GlowStrength * g));
        halo.color = new Color(1f, 0.8f, 0.45f, 0.35f * g);

        // night tint on everything standing on the table, by the dark left at its foot
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + 0.1f;
            foreach (var sr in FindObjectsOfType<SpriteRenderer>())
            {
                if (sr.sortingLayerName != "FoodItems" || sr.transform.IsChildOf(transform)) continue;
                if (!original.ContainsKey(sr)) original[sr] = sr.color;
                var bb = sr.bounds; var d = k * (1f - Lift(bb.center.x, bb.min.y + 0.15f * bb.size.y));
                sr.color = original[sr] * Color.Lerp(Color.white, NightTint, d * Dark / 0.42f);
            }
            var truck = GameObject.Find("KitchenButton"); var img = truck != null ? truck.GetComponent<Image>() : null;
            if (img != null)
            {
                if (!original.ContainsKey(img)) original[img] = img.color;
                var c = new Vector3[4]; img.rectTransform.GetWorldCorners(c);
                var d = k * (1f - Lift((c[0].x + c[2].x) / 2f, c[0].y));
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
