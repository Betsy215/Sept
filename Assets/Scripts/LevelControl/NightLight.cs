using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The night on the table and the hanging night light (owner, 27 November 2026).
///  - Every night the table dims a little (owner: "we don't want the night to be too dark so just dim it a little when no
///    light"): a navy sheet over the tablecloth and a matching tint on everything standing on the table.
///  - The night light is a star shop item (3 stars, "StarLight"; owner: "I want the lamp to be a shop item, 3 stars").
///    Once bought, each night a light drops from the top centre of the screen on its cord, with one small bounce and a
///    swing, flickers on and fades in: chapter 1 a red mushroom pendant, chapter 2 a woven rattan dome, chapter 3 a string
///    of fairy lights (owner: "For chapter 1 use the mushroom light, Chapter 2 use the rattan dome, Chapter 3 use fairy
///    lights"). The tutorials, which play at night, show it too, bought or not. It casts a warm pool on the tablecloth
///    only, brightest in the middle of the cloth and fading gradually out to its edges, with a small halo round the light;
///    the customers and the background are left as they are.
/// Placement: the pendant's cord top sits just above the screen and the shade hangs 0.55 to 1.3 world units below the top
/// edge, between the settings and pause buttons and above the order bubbles. Nothing here takes taps (sprites only, no
/// colliders, no UI). Only the default sprite shader is used (normal blending), so it renders the same in an iOS build.
/// Night strength follows DayNightTint.CurrentStage (dusk 0.4, night 1); a day that starts already at night shows the light
/// hanging and lit, without the drop. Built at runtime by LevelManager next to DayNightTint; the scene reload of a new
/// day, a restart or the map removes it. Drafts and previews: Store/lamp_2026-11-26 (HangingLightPreview, editor only).
/// </summary>
public class NightLight : MonoBehaviour
{
    public const string ItemName = "StarLight";   // starts with "Star": the shop files it under the star tab
    public const int StarPrice = 3;
    public static bool Owned => SessionManager.Instance != null && SessionManager.Instance.OwnsStarItem(ItemName);

    /// The light's pictures per chapter (Resources/Lights/<name>_off, _on, _emit); null = no light in that chapter.
    public static string ArtFor(int chapter) => chapter == 1 ? "mushroom" : chapter == 2 ? "rattan" : chapter == 3 ? "fairy" : null;

    private const float Dim = 0.24f;                 // the navy sheet's alpha over the cloth at full night (owner: "a tiny bit darker")
    // the pool's brightest point is the middle of the tablecloth, fading slowly out to its edges (owner, 27 November 2026:
    // "make the brightest part in the middle of the table ... brighter and more gradient")
    private const float PoolRadius = 1.6f, Falloff = 1.3f, PoolLift = 0.9f;
    private const float WarmPeak = 0.42f, AirPeak = 0.04f, HaloPeak = 0.22f;
    private const float TopFade = 0.4f;              // the warm light fades in over this much below the cloth's back edge
    private const float TutorialDelay = 1.5f;        // the tutorial's drop waits this long after the scene starts
    private const float PendantDrop = 0.55f;         // the pendant's attach point rests this far below the screen's top edge
    private static readonly Color Navy = new Color(0.07f, 0.08f, 0.21f);
    private static readonly Color NightTint = new Color(0.58f, 0.60f, 0.74f);
    private static readonly Color WarmColour = new Color(1f, 0.78f, 0.48f);
    private static readonly Color HaloColour = new Color(1f, 0.86f, 0.58f);
    private static readonly Color CordColour = new Color(0.27f, 0.23f, 0.2f);
    // where the light's glow sits below the attach point, as a share of the painted height
    private static float LightAt(string art) => art == "mushroom" ? 0.8f : art == "rattan" ? 0.85f : 0.75f;

    private SpriteRenderer sheet, warm, air, fixture, emit, cord, halo;
    private Texture2D sheetTex;
    private Transform pivot;
    private Sprite off, on;
    private bool fairy, started, tutorial;
    private float clothTop, poolX, poolY, rest, start, height, painted, lightX, startedAt, dropStart = -1f, lastLit = -1f, lastK, nextScan;
    private Bounds cloth;
    private MaterialPropertyBlock block;
    private readonly Dictionary<SpriteRenderer, float> tinted = new Dictionary<SpriteRenderer, float>();
    private static Sprite glowSprite, cordSprite;

    /// Editor captures only: the night as it was before this (no dim, no light).
    public static bool Off;
    /// Editor captures only: >= 0 shows the drop at this many seconds (frame-by-frame recordings).
    public static float DebugTime = -1f;

    private void Start()
    {
        if (Off) { enabled = false; return; }
        var clothGo = GameObject.Find("TableCloth");
        var clothSr = clothGo != null ? clothGo.GetComponent<SpriteRenderer>() : null;
        var cam = Camera.main;
        if (clothSr == null || cam == null) { enabled = false; return; }
        cloth = clothSr.bounds; clothTop = cloth.max.y;
        var front = GameObject.Find("ClothFront"); var frontSr = front != null ? front.GetComponent<SpriteRenderer>() : null;
        if (frontSr != null) cloth.Encapsulate(frontSr.bounds);
        poolX = clothSr.bounds.center.x; poolY = clothSr.bounds.center.y;   // the middle of the cloth itself (not its front drop)

        sheet = MakeSr("NightSheet", transform, null, "Table", 2);   // over the cloth and its front drop, under everything on it
        sheet.transform.position = new Vector3(cloth.center.x, cloth.center.y, 0f);
        sheetTex = new Texture2D(48, 48, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        // FullRect: a Tight mesh is cut from the texture's alpha when the sprite is made, before the sheet is filled in
        sheet.sprite = Sprite.Create(sheetTex, new Rect(0, 0, 48, 48), new Vector2(0.5f, 0.5f), 48f / cloth.size.y, 0, SpriteMeshType.FullRect);
        sheet.transform.localScale = new Vector3(cloth.size.x / cloth.size.y, 1f, 1f);
        FillSheet(0f);
        sheet.color = new Color(1f, 1f, 1f, 0f);

        lightX = cam.transform.position.x;
        // the tutorials play at night and show the light even before it is bought (owner, 27 November 2026: "for tutorial,
        // add the light"): it drops in once, TutorialDelay seconds after the scene starts, so it does not land on the first note
        tutorial = FindObjectOfType<TutorialDirector>() != null;
        var art = Owned || tutorial ? ArtFor(SessionManager.CurrentChapter) : null;
        if (art != null) BuildLight(art, cam);
        started = true; startedAt = Time.unscaledTime;
    }

    private void BuildLight(string art, Camera cam)
    {
        off = Resources.Load<Sprite>("Lights/" + art + "_off"); on = Resources.Load<Sprite>("Lights/" + art + "_on");
        var emitSprite = Resources.Load<Sprite>("Lights/" + art + "_emit");
        if (off == null || on == null) return;
        fairy = art == "fairy";
        height = off.bounds.size.y;                  // the whole picture (pivot at the top: it hangs below its attach point)
        painted = fairy ? 0.69f : 0.75f;            // its painted part (Resources/Lights metas: ppu set so pendants are 0.75 tall, the string 3.0 wide)

        // the warm pool, exactly over the cloth, fading in below its back edge
        var tex = new Texture2D(48, 48, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[48 * 48];
        for (var y = 0; y < 48; y++) for (var x = 0; x < 48; x++)
        {
            var wx = cloth.min.x + (x + 0.5f) / 48f * cloth.size.x; var wy = cloth.min.y + (y + 0.5f) / 48f * cloth.size.y;
            var fade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((clothTop - wy) / TopFade));
            px[y * 48 + x] = new Color(1f, 1f, 1f, Pool(wx, wy) * fade);
        }
        tex.SetPixels(px); tex.Apply();
        warm = MakeSr("WarmPool", transform, Sprite.Create(tex, new Rect(0, 0, 48, 48), new Vector2(0.5f, 0.5f), 48f / cloth.size.y, 0, SpriteMeshType.FullRect), "FoodItems", 60);
        warm.transform.position = new Vector3(cloth.center.x, cloth.center.y, 0f);
        warm.transform.localScale = new Vector3(cloth.size.x / cloth.size.y, 1f, 1f);
        warm.color = Clear(WarmColour);

        var top = cam.transform.position.y + cam.orthographicSize;
        pivot = new GameObject("NightLightCord").transform; pivot.SetParent(transform, false);
        pivot.position = new Vector3(lightX, top + 0.05f, 0f);
        rest = fairy ? 0.05f : 0.05f + PendantDrop;                    // cord length at rest
        start = fairy ? rest - 1.0f : -(height + 0.3f);              // out of sight above the screen

        air = MakeSr("AirGlow", transform, GlowSprite(), "FoodItems", 61);
        air.transform.position = new Vector3(lightX, (top - PendantDrop - painted + clothTop) * 0.5f, 0f);
        air.transform.localScale = new Vector3(1.7f, 2.0f, 1f); air.color = Clear(WarmColour);
        if (!fairy)
        {
            cord = MakeSr("Cord", pivot, CordSprite(), "FoodItems", 70);
            cord.color = CordColour;
        }
        fixture = MakeSr("Light", pivot, off, "FoodItems", 70);
        halo = MakeSr("Halo", fixture.transform, GlowSprite(), "FoodItems", 69);
        halo.transform.localPosition = new Vector3(0f, -painted * LightAt(art), 0f);
        halo.transform.localScale = fairy ? new Vector3(1.5f, 0.35f, 1f) : new Vector3(0.5f, 0.5f, 1f);
        halo.color = Clear(HaloColour);
        if (emitSprite != null) { emit = MakeSr("LightOn", fixture.transform, emitSprite, "FoodItems", 72); emit.enabled = false; }
        Place(start, 0f);
    }

    private static Color Clear(Color c) => new Color(c.r, c.g, c.b, 0f);

    private SpriteRenderer MakeSr(string name, Transform parent, Sprite s, string layer, int order)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = s; sr.sortingLayerName = layer; sr.sortingOrder = order;
        return sr;
    }

    /// The light's reach on the table: 1 in the middle of the cloth, fading smoothly toward its edges (exp(-Falloff d),
    /// d in an ellipse 2.0 x 1.7 pool radii round the cloth's centre).
    private float Pool(float x, float y)
    {
        var dx = (x - poolX) / (2.0f * PoolRadius); var dy = (y - poolY) / (1.7f * PoolRadius);
        return Mathf.Exp(-Falloff * Mathf.Sqrt(dx * dx + dy * dy));
    }
    private float Lift(float x, float y) => off == null ? 0f : Mathf.Clamp01(PoolLift * Pool(x, y));

    private void FillSheet(float lit)
    {
        var px = new Color[48 * 48];
        for (var y = 0; y < 48; y++) for (var x = 0; x < 48; x++)
        {
            var wx = cloth.min.x + (x + 0.5f) / 48f * cloth.size.x; var wy = cloth.min.y + (y + 0.5f) / 48f * cloth.size.y;
            px[y * 48 + x] = new Color(Navy.r, Navy.g, Navy.b, 1f - lit * Lift(wx, wy));
        }
        sheetTex.SetPixels(px); sheetTex.Apply();
    }

    private static Sprite GlowSprite()
    {
        if (glowSprite != null) return glowSprite;
        const int n = 64; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n];
        for (var y = 0; y < n; y++) for (var x = 0; x < n; x++)
        {
            var dx = (x + 0.5f - n / 2f) / (n / 2f) * 2.2f; var dy = (y + 0.5f - n / 2f) / (n / 2f) * 2.2f; var d2 = dx * dx + dy * dy;
            px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Exp(-1.5f * d2) * Mathf.Clamp01((2.2f - Mathf.Sqrt(d2)) * 2f));
        }
        tex.SetPixels(px); tex.Apply();
        glowSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n / 2f / 2.2f);   // 1 world unit = 1 radius
        return glowSprite;
    }

    private static Sprite CordSprite()
    {
        if (cordSprite != null) return cordSprite;
        var tex = new Texture2D(2, 2); tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); tex.Apply();
        cordSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 1f), 2f);
        return cordSprite;
    }

    private void Update()
    {
        if (!started) return;
        var s = DayNightTint.CurrentStage;
        var k = s <= 1f ? 0.4f * s : 0.4f + 0.6f * Mathf.Clamp01(s - 1f);
        float lit = 0f;
        if (fixture != null)
        {
            if (k > 0.75f && dropStart < 0f)
                dropStart = tutorial ? Mathf.Max(Time.unscaledTime, startedAt + TutorialDelay)                       // the tutorial: seen dropping in
                    : Time.unscaledTime - startedAt < 1f ? Time.unscaledTime - 10f : Time.unscaledTime;             // a day that starts at night: no drop
            if (k <= 0.75f) dropStart = -1f;
            var t = DebugTime >= 0f ? DebugTime : dropStart < 0f ? -1f : Time.unscaledTime - dropStart;
            float length = start, swing = 0f;
            if (t >= 0f)
            {
                length = rest + (start - rest) * Mathf.Exp(-5f * t) * Mathf.Cos(8f * t);          // one small bounce
                if (!fairy) swing = 7f * Mathf.Exp(-2.2f * t) * Mathf.Sin(6.5f * t);
                lit = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 1.0f) / 0.6f));
                if (t > 1.0f && t < 1.3f && Mathf.Sin(t * 70f) <= -0.3f) lit *= 0.35f;            // a short flicker as it catches
            }
            Place(length, swing);
            fixture.sprite = lit > 0.5f ? on : off;
            if (emit != null) { emit.enabled = lit > 0.01f; emit.color = new Color(1f, 1f, 1f, lit); }
            var dim = Mathf.Lerp(1f, 0.85f, k * (1f - lit));
            fixture.color = new Color(dim, dim, dim);
            if (cord != null) cord.color = CordColour * dim;
            warm.color = new Color(WarmColour.r, WarmColour.g, WarmColour.b, WarmPeak * lit * k);
            air.color = new Color(WarmColour.r, WarmColour.g, WarmColour.b, AirPeak * lit * k);
            halo.color = new Color(HaloColour.r, HaloColour.g, HaloColour.b, HaloPeak * lit * k);
        }
        if (Mathf.Abs(lit - lastLit) > 0.02f || (lit >= 1f && lastLit < 1f) || (lit <= 0f && lastLit > 0f)) { FillSheet(lit); lastLit = lit; }
        sheet.color = new Color(1f, 1f, 1f, Dim * k);

        // the same dim on everything standing on the table (a property block, so the game's own colours stay untouched)
        if (k <= 0.001f && lastK <= 0.001f) return;
        if (Time.unscaledTime < nextScan && Mathf.Abs(k - lastK) < 0.05f && DebugTime < 0f) return;
        nextScan = Time.unscaledTime + 0.25f; lastK = k;
        if (block == null) block = new MaterialPropertyBlock();
        foreach (var sr in FindObjectsOfType<SpriteRenderer>())
        {
            if (sr.sortingLayerName != "FoodItems" || sr.transform.IsChildOf(transform)) continue;
            var bb = sr.bounds;
            var d = k * (1f - lit * Lift(bb.center.x, bb.min.y + 0.15f * bb.size.y)) * Dim / 0.42f;
            d = Mathf.Round(d * 50f) / 50f;
            if (tinted.TryGetValue(sr, out var was) && Mathf.Approximately(was, d)) continue;
            tinted[sr] = d;
            sr.GetPropertyBlock(block);
            block.SetColor("_Color", Color.Lerp(Color.white, NightTint, d));
            sr.SetPropertyBlock(block);
        }
    }

    private void Place(float length, float swing)
    {
        pivot.localRotation = Quaternion.Euler(0f, 0f, swing);
        fixture.transform.localPosition = new Vector3(0f, -length, 0f);
        if (cord != null) { cord.enabled = length > 0f; cord.transform.localScale = new Vector3(0.022f, Mathf.Max(0f, length), 1f); }
    }
}
