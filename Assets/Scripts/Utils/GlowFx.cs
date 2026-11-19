using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// The three glows the owner picked on 18 November 2026 (report card "Kitchen glows", drafts A, B, C):
///   Gold  (A): a gold glow beating once a second, no hop. For the kitchen thing to act on NEXT (not wired yet: the
///              owner will say what counts as the next action).
///   New   (B): a bright white glow with twinkling stars round the thing; the thing itself does not move. On newly
///              bought things while arranging, table and kitchen, beside the NEW label.
///   Shine (C): a gold glow with a shine sweeping across. On the Ready button (table and kitchen) and on the map's
///              red circle (today).
/// The glow is painted at run time from the thing's own picture with the glow brush's rule (Docs/tools/glow_brush.py:
/// brightest at the edge, width 4 px + 15 % of the shorter side), so no glow picture has to be drawn per thing.
/// On UI things the glow is a child (it is empty inside the thing's shape, so it never covers it); on the table's
/// world sprites it is a separate object that follows the sprite, behind it, so nothing that measures or tints the
/// thing sees it.
public static class GlowFx
{
    public static readonly Color GoldColour = new Color(1f, 0.737f, 0.204f), WhiteColour = Color.white;
    private const string Name = "GlowFx";

    // ---- the glow picture ---------------------------------------------------------------------------------------

    private class Made { public Sprite glow; public float padX, padY; }   // pad as a share of the source picture's size
    private static readonly Dictionary<long, Made> cache = new Dictionary<long, Made>();
    // where the painted thing really is inside its picture's frame (0..1 of sprite.rect): many pictures have wide empty
    // margins (owner, 18 November 2026: "the new and glow effect needs to fit the size of the content")
    private static readonly Dictionary<int, Rect> contentOf = new Dictionary<int, Rect>();
    // the coffee bags' measured alpha by distance / brush width (glow_brush.py)
    private static readonly float[] CurveT = { 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 32, 34 };
    private static readonly float[] CurveA = { 215, 158, 131, 114, 102, 91, 82, 73, 64, 57, 50, 43, 37, 32, 28, 23, 20, 16, 13.5f, 11, 9, 7.4f, 6, 5, 3.9f, 3.1f, 2.5f, 1.9f, 1.5f, 0.8f, 0.4f, 0 };

    private static float Curve(float t)
    {
        t *= 30f;
        if (t <= 0f) return CurveA[0]; if (t >= CurveT[CurveT.Length - 1]) return 0f;
        var i = 0; while (i < CurveT.Length - 2 && t > CurveT[i + 1]) i++;
        return Mathf.Lerp(CurveA[i], CurveA[i + 1], Mathf.InverseLerp(CurveT[i], CurveT[i + 1], t));
    }

    private static Made Make(Sprite s, float widthMul, float strength)
    {
        var key = ((long)s.GetInstanceID() << 16) ^ (long)Mathf.RoundToInt(widthMul * 100f) ^ ((long)Mathf.RoundToInt(strength * 100f) << 8);
        if (cache.TryGetValue(key, out var have) && have.glow != null) return have;

        // The picture's shape, in its full frame (sprite.rect), small. Some pictures keep their shape in alpha, others
        // (the table's, stored without alpha) only in their outline mesh: the shape is alpha AND inside the mesh.
        var r = s.rect; var tr = s.textureRect; var off = s.textureRectOffset;
        var k = Mathf.Min(1f, 224f / Mathf.Max(r.width, r.height));
        int w = Mathf.Max(8, Mathf.RoundToInt(r.width * k)), h = Mathf.Max(8, Mathf.RoundToInt(r.height * k));
        var src = s.texture;
        int tw = Mathf.Max(1, Mathf.RoundToInt(tr.width * k)), th = Mathf.Max(1, Mathf.RoundToInt(tr.height * k));
        var rtx = RenderTexture.GetTemporary(tw, th, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(src, rtx, new Vector2(tr.width / src.width, tr.height / src.height), new Vector2(tr.x / src.width, tr.y / src.height));
        var before = RenderTexture.active; RenderTexture.active = rtx;
        var small = new Texture2D(tw, th, TextureFormat.RGBA32, false);
        small.ReadPixels(new Rect(0, 0, tw, th), 0, 0); small.Apply(false);
        RenderTexture.active = before; RenderTexture.ReleaseTemporary(rtx);
        var tpx = small.GetPixels32(); Object.Destroy(small);
        var inMesh = new bool[w * h];
        var verts = s.vertices; var tris = s.triangles; var ppu = s.pixelsPerUnit; var piv = s.pivot;
        for (var t = 0; t + 2 < tris.Length; t += 3)
        {
            var p0 = (verts[tris[t]] * ppu + piv) * k; var p1 = (verts[tris[t + 1]] * ppu + piv) * k; var p2 = (verts[tris[t + 2]] * ppu + piv) * k;
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x)))), maxX = Mathf.Min(w - 1, Mathf.CeilToInt(Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x))));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(p0.y, Mathf.Min(p1.y, p2.y)))), maxY = Mathf.Min(h - 1, Mathf.CeilToInt(Mathf.Max(p0.y, Mathf.Max(p1.y, p2.y))));
            var area = (p1.x - p0.x) * (p2.y - p0.y) - (p2.x - p0.x) * (p1.y - p0.y); if (Mathf.Abs(area) < 1e-6f) continue;
            for (var y = minY; y <= maxY; y++)
                for (var x = minX; x <= maxX; x++)
                {
                    var q = new Vector2(x + 0.5f, y + 0.5f);
                    var w0 = ((p1.x - q.x) * (p2.y - q.y) - (p2.x - q.x) * (p1.y - q.y)) / area;
                    var w1 = ((p2.x - q.x) * (p0.y - q.y) - (p0.x - q.x) * (p2.y - q.y)) / area;
                    if (w0 >= -0.01f && w1 >= -0.01f && 1f - w0 - w1 >= -0.01f) inMesh[y * w + x] = true;
                }
        }
        var px = new Color32[w * h];
        int ox = Mathf.RoundToInt(off.x * k), oy = Mathf.RoundToInt(off.y * k);
        for (var y = 0; y < th; y++)
            for (var x = 0; x < tw; x++)
            {
                int X = x + ox, Y = y + oy; if (X < 0 || Y < 0 || X >= w || Y >= h) continue;
                px[Y * w + X] = inMesh[Y * w + X] ? tpx[y * tw + x] : new Color32(0, 0, 0, 0);
            }
        int cx0 = w, cy0 = h, cx1 = -1, cy1 = -1;
        for (var y = 0; y < h; y++) for (var x = 0; x < w; x++) if (px[y * w + x].a >= 200) { if (x < cx0) cx0 = x; if (x > cx1) cx1 = x; if (y < cy0) cy0 = y; if (y > cy1) cy1 = y; }
        if (cx1 < cx0) { cx0 = 0; cy0 = 0; cx1 = w - 1; cy1 = h - 1; }
        contentOf[s.GetInstanceID()] = Rect.MinMaxRect(cx0 / (float)w, cy0 / (float)h, (cx1 + 1) / (float)w, (cy1 + 1) / (float)h);
        // the brush follows the painted thing's size, not its frame's
        var brush = (4f * k + 0.151f * Mathf.Min(cx1 - cx0 + 1, cy1 - cy0 + 1)) * widthMul;
        var pad = Mathf.CeilToInt(brush) + 2; int W = w + 2 * pad, H = h + 2 * pad;
        // distance from the shape, two chamfer passes
        var d = new float[W * H]; const float Far = 1e6f, Diag = 1.41421f;
        for (var i = 0; i < d.Length; i++) d[i] = Far;
        for (var y = 0; y < h; y++) for (var x = 0; x < w; x++) if (px[y * w + x].a >= 200) d[(y + pad) * W + x + pad] = 0f;   // the solid picture, not its soft painted shadow
        for (var y = 0; y < H; y++)
            for (var x = 0; x < W; x++)
            {
                var i = y * W + x; var v = d[i]; if (v == 0f) continue;
                if (x > 0) v = Mathf.Min(v, d[i - 1] + 1f);
                if (y > 0) { v = Mathf.Min(v, d[i - W] + 1f); if (x > 0) v = Mathf.Min(v, d[i - W - 1] + Diag); if (x < W - 1) v = Mathf.Min(v, d[i - W + 1] + Diag); }
                d[i] = v;
            }
        for (var y = H - 1; y >= 0; y--)
            for (var x = W - 1; x >= 0; x--)
            {
                var i = y * W + x; var v = d[i]; if (v == 0f) continue;
                if (x < W - 1) v = Mathf.Min(v, d[i + 1] + 1f);
                if (y < H - 1) { v = Mathf.Min(v, d[i + W] + 1f); if (x < W - 1) v = Mathf.Min(v, d[i + W + 1] + Diag); if (x > 0) v = Mathf.Min(v, d[i + W - 1] + Diag); }
                d[i] = v;
            }
        var o = new Color32[W * H];
        for (var i = 0; i < o.Length; i++)
        {
            var a = d[i] <= 0f ? 0f : Mathf.Clamp(Curve((d[i] - 0.5f) / brush) * strength, 0f, 255f);
            o[i] = new Color32(255, 255, 255, (byte)a);
        }
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(o); tex.Apply(false);
        // as a sprite it lies exactly over the source picture's full frame, bigger by the pad on every side
        var pivotN = new Vector2(s.pivot.x / s.rect.width, s.pivot.y / s.rect.height);
        var units = s.rect.width / s.pixelsPerUnit * (1f + 2f * pad / (float)w);
        var glow = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2((pad + pivotN.x * w) / W, (pad + pivotN.y * h) / H), W / Mathf.Max(units, 1e-4f), 0, SpriteMeshType.FullRect);
        var made = new Made { glow = glow, padX = pad / (float)w, padY = pad / (float)h };
        cache[key] = made; return made;
    }

    /// The painted part of a picture, as a share of its frame (0..1).
    public static Rect ContentRect(Sprite s)
    {
        if (s == null) return new Rect(0f, 0f, 1f, 1f);
        if (!contentOf.TryGetValue(s.GetInstanceID(), out var r)) { Make(s, 1.7f, 2f); contentOf.TryGetValue(s.GetInstanceID(), out r); }
        return r.width > 0f ? r : new Rect(0f, 0f, 1f, 1f);
    }

    /// The painted part of a world sprite, in world space.
    public static Bounds WorldContent(SpriteRenderer r)
    {
        var s = r.sprite; var c = ContentRect(s); var ppu = s.pixelsPerUnit; var rect = s.rect; var piv = s.pivot;
        var b = new Bounds(); var first = true;
        foreach (var u in new[] { new Vector2(c.xMin, c.yMin), new Vector2(c.xMax, c.yMin), new Vector2(c.xMin, c.yMax), new Vector2(c.xMax, c.yMax) })
        {
            var local = new Vector3((u.x * rect.width - piv.x) / ppu * (r.flipX ? -1f : 1f), (u.y * rect.height - piv.y) / ppu * (r.flipY ? -1f : 1f), 0f);
            var w = r.transform.TransformPoint(local);
            if (first) { b = new Bounds(w, Vector3.zero); first = false; } else b.Encapsulate(w);
        }
        return b;
    }

    /// The painted part of a UI picture, in its own rect's space.
    public static Rect UiContent(Image owner)
    {
        if (owner == null || owner.sprite == null) return owner != null ? owner.rectTransform.rect : new Rect();
        var d = Drawn(owner); var c = ContentRect(owner.sprite);
        return Rect.MinMaxRect(d.xMin + c.xMin * d.width, d.yMin + c.yMin * d.height, d.xMin + c.xMax * d.width, d.yMin + c.yMax * d.height);
    }

    // ---- small pictures made once: a four-point star, a soft band ------------------------------------------------

    private static Sprite star, band;
    private static Sprite Star
    {
        get
        {
            if (star != null) return star;
            const int n = 48; var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var p = new Color32[n * n];
            for (var y = 0; y < n; y++)
                for (var x = 0; x < n; x++)
                {
                    float u = Mathf.Abs((x + 0.5f) / n * 2f - 1f), v = Mathf.Abs((y + 0.5f) / n * 2f - 1f);
                    var f = Mathf.Pow(u, 0.6f) + Mathf.Pow(v, 0.6f);          // an astroid: four points
                    p[y * n + x] = new Color32(255, 255, 255, (byte)(255f * Mathf.Clamp01((1f - f) * 6f)));
                }
            t.SetPixels32(p); t.Apply(false);
            return star = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        }
    }
    private static Sprite Band
    {
        get
        {
            if (band != null) return band;
            const int n = 32; var t = new Texture2D(n, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var p = new Color32[n * 2];
            for (var x = 0; x < n; x++) { var a = (byte)(255f * Mathf.Clamp01(1f - Mathf.Abs((x + 0.5f) / n * 2f - 1f))); p[x] = p[n + x] = new Color32(255, 255, 255, a); }
            t.SetPixels32(p); t.Apply(false);
            return band = Sprite.Create(t, new Rect(0, 0, n, 2), new Vector2(0.5f, 0.5f), 100f);
        }
    }

    // ---- on UI things ----------------------------------------------------------------------------------------------

    /// Where the picture is really drawn inside its rect (a preserve-aspect picture does not fill it).
    private static Rect Drawn(Image owner)
    {
        var r = owner.rectTransform.rect; var sp = owner.sprite.rect.size;
        if (!owner.preserveAspect || sp.x <= 0f || sp.y <= 0f) return r;
        var k = Mathf.Min(r.width / sp.x, r.height / sp.y); var size = sp * k; var pivot = owner.rectTransform.pivot;
        return new Rect(r.x + (r.width - size.x) * pivot.x, r.y + (r.height - size.y) * pivot.y, size.x, size.y);
    }

    private static RectTransform Child(Image owner, string name, Rect at)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = owner.gameObject.layer;
        var rt = (RectTransform)go.transform; rt.SetParent(owner.transform, false);
        var r = owner.rectTransform.rect;
        rt.anchorMin = new Vector2((at.xMin - r.xMin) / r.width, (at.yMin - r.yMin) / r.height);
        rt.anchorMax = new Vector2((at.xMax - r.xMin) / r.width, (at.yMax - r.yMin) / r.height);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().raycastTarget = false;
        return rt;
    }

    private static GlowPulse UiGlow(Image owner, Color colour, float widthMul, float strength, float low, float high)
    {
        var made = Make(owner.sprite, widthMul, strength);
        var d = Drawn(owner);
        var rt = Child(owner, Name, Rect.MinMaxRect(d.xMin - made.padX * d.width, d.yMin - made.padY * d.height, d.xMax + made.padX * d.width, d.yMax + made.padY * d.height));
        rt.SetAsFirstSibling();
        var img = rt.GetComponent<Image>(); img.sprite = made.glow; img.color = colour;
        var pulse = rt.gameObject.AddComponent<GlowPulse>(); pulse.low = low; pulse.high = high; pulse.gate = owner.GetComponent<Selectable>();
        return pulse;
    }

    private static bool Usable(Image owner) { return owner != null && owner.sprite != null && owner.transform.Find(Name) == null; }

    /// The kitchen's default (owner, 18 November 2026: "pick a glow state in B, a middle state, but without the stars,
    /// default glow is the static glow for actionable items in kitchen"): B's white glow, still, at the middle of its beat.
    public static void Default(Image owner)
    {
        if (!Usable(owner)) return;
        // toned down the same evening ("the glow is too much"): narrower, softer, fainter
        var p = UiGlow(owner, WhiteColour, 1.4f, 1.5f, 0.5f, 0.5f); p.gate = null;
    }

    /// A: gold, beating once a second. For the next thing to act on.
    public static void Gold(Image owner) { if (Usable(owner)) UiGlow(owner, GoldColour, 2.4f, 2.2f, 0.4f, 1f); }

    /// B: bright white with stars. For a newly bought thing.
    public static void New(Image owner)
    {
        if (!Usable(owner)) return;
        UiGlow(owner, WhiteColour, 2.2f, 2f, 0.5f, 1f);
        var d = UiContent(owner); var size = Mathf.Clamp(Mathf.Min(d.width, d.height) * 0.26f, 26f, 60f);
        var root = Child(owner, Name + "Stars", d);
        Object.Destroy(root.GetComponent<Image>());
        for (var i = 0; i < Spots.Length; i++)
        {
            var go = new GameObject("Star", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(GlowTwinkle));
            go.layer = owner.gameObject.layer;
            var rt = (RectTransform)go.transform; rt.SetParent(root, false);
            rt.anchorMin = rt.anchorMax = Spots[i]; rt.sizeDelta = new Vector2(size, size); rt.anchoredPosition = Vector2.zero;
            var img = go.GetComponent<Image>(); img.sprite = Star; img.raycastTarget = false;
            go.GetComponent<GlowTwinkle>().phase = i / (float)Spots.Length * 2.6f % 1f;
        }
    }

    /// C: gold with a shine sweeping across every second and a half. For Ready and for the map's red circle.
    public static void Shine(Image owner)
    {
        if (!Usable(owner)) return;
        var gate = owner.GetComponent<Selectable>();
        UiGlow(owner, GoldColour, 1.5f, 1.9f, 0.7f, 1f);
        var d = Drawn(owner);
        var maskRt = Child(owner, Name + "Shine", owner.rectTransform.rect);
        var maskImg = maskRt.GetComponent<Image>(); maskImg.sprite = owner.sprite; maskImg.preserveAspect = owner.preserveAspect; maskImg.type = owner.type;
        var mask = maskRt.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var go = new GameObject("Band", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(GlowSweep));
        go.layer = owner.gameObject.layer;
        var rt = (RectTransform)go.transform; rt.SetParent(maskRt, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(Mathf.Max(40f, d.width * 0.22f), d.height * 2.2f);
        rt.localRotation = Quaternion.Euler(0f, 0f, -24f);
        var img = go.GetComponent<Image>(); img.sprite = Band; img.raycastTarget = false; img.color = new Color(1f, 1f, 1f, 0.6f);
        var sweep = go.GetComponent<GlowSweep>(); sweep.travel = d.width * 0.5f + rt.sizeDelta.x + d.height * 0.5f; sweep.gate = gate;
    }

    public static void Remove(Image owner)
    {
        if (owner == null) return;
        foreach (var n in new[] { Name, Name + "Stars", Name + "Shine" }) { var t = owner.transform.Find(n); if (t != null) Object.Destroy(t.gameObject); }
    }

    // the stars' places round a thing, as shares of its box (a little outside it)
    private static readonly Vector2[] Spots =
    {
        new Vector2(-0.06f, 0.78f), new Vector2(0.30f, 1.08f), new Vector2(0.80f, 1.05f), new Vector2(1.07f, 0.52f),
        new Vector2(0.62f, -0.08f), new Vector2(0.10f, -0.04f), new Vector2(-0.07f, 0.30f), new Vector2(1.04f, 0.04f),
    };

    // ---- on the table's world sprites ------------------------------------------------------------------------------

    private static readonly List<GameObject> world = new List<GameObject>();

    private static void WorldGlow(Transform owner, Color colour, float widthMul, float strength, float low, float high, bool stars)
    {
        if (owner == null) return;
        var renderers = new List<SpriteRenderer>(); var has = false; var b = new Bounds(owner.position, Vector3.zero); var minOrder = int.MaxValue; var maxOrder = int.MinValue; var layer = 0;
        foreach (var r in owner.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!r.enabled || r.sprite == null || r.color.a < 0.05f) continue;
            var cb = WorldContent(r);
            renderers.Add(r); if (!has) { b = cb; has = true; } else b.Encapsulate(cb);
            if (r.sortingOrder < minOrder) minOrder = r.sortingOrder;
            if (r.sortingOrder >= maxOrder) { maxOrder = r.sortingOrder; layer = r.sortingLayerID; }
        }
        if (!has) return;
        foreach (var r in renderers)
        {
            var made = Make(r.sprite, widthMul, strength);
            var go = new GameObject(Name, typeof(SpriteRenderer), typeof(GlowPulse), typeof(GlowFollow));
            var sr = go.GetComponent<SpriteRenderer>(); sr.sprite = made.glow; sr.color = colour; sr.sortingLayerID = r.sortingLayerID; sr.sortingOrder = minOrder - 1;
            var f = go.GetComponent<GlowFollow>(); f.target = r; f.LateUpdate();
            var pulse = go.GetComponent<GlowPulse>(); pulse.low = low; pulse.high = high;
            world.Add(go);
        }
        if (!stars) return;
        var size = Mathf.Clamp(Mathf.Min(b.size.x, b.size.y) * 0.26f, 0.22f, 0.5f);
        for (var i = 0; i < Spots.Length; i++)
        {
            var go = new GameObject("Star", typeof(SpriteRenderer), typeof(GlowTwinkle), typeof(NewTagFollow));
            var sr = go.GetComponent<SpriteRenderer>(); sr.sprite = Star; sr.sortingLayerID = layer; sr.sortingOrder = maxOrder + 400;
            go.transform.localScale = Vector3.one * (size / Star.bounds.size.x);
            var at = new Vector3(b.min.x + Spots[i].x * b.size.x, b.min.y + Spots[i].y * b.size.y, owner.position.z - 0.4f);
            go.transform.position = at;
            var follow = go.GetComponent<NewTagFollow>(); follow.owner = owner; follow.offset = at - owner.position;
            go.GetComponent<GlowTwinkle>().phase = i / (float)Spots.Length * 2.6f % 1f;
            world.Add(go);
        }
    }

    public static void GoldWorld(Transform owner) { WorldGlow(owner, GoldColour, 2.4f, 2.2f, 0.4f, 1f, false); }
    public static void NewWorld(Transform owner) { WorldGlow(owner, WhiteColour, 1.7f, 2.0f, 0.5f, 1f, true); }   // narrower on the table: the cloth is light and busy

    public static void ClearWorld()
    {
        foreach (var g in world) if (g != null) Object.Destroy(g);
        world.Clear();
    }
}

/// The glow's beat: once a second between `low` and `high` of its own brightness. Off while its button cannot be tapped.
public class GlowPulse : MonoBehaviour
{
    public float low = 0.5f, high = 1f; public Selectable gate;
    private Graphic graphic; private SpriteRenderer sprite; private float full = -1f;
    private void LateUpdate()
    {
        if (full < 0f) { graphic = GetComponent<Graphic>(); sprite = GetComponent<SpriteRenderer>(); full = graphic != null ? graphic.color.a : sprite != null ? sprite.color.a : 1f; }
        var a = gate != null && !gate.IsInteractable() ? 0f : full * Mathf.Lerp(low, high, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f));
        if (graphic != null) { var c = graphic.color; c.a = a; graphic.color = c; }
        else if (sprite != null) { var c = sprite.color; c.a = a; sprite.color = c; }
    }
}

/// A star that swells and fades in turn with the others.
public class GlowTwinkle : MonoBehaviour
{
    public float phase;
    private Graphic graphic; private SpriteRenderer sprite; private Vector3 size; private bool got;
    private void LateUpdate()
    {
        if (!got) { graphic = GetComponent<Graphic>(); sprite = GetComponent<SpriteRenderer>(); size = transform.localScale; got = true; }
        var q = Mathf.Repeat(Time.unscaledTime / 1.1f + phase, 1f);
        var a = q < 0.6f ? Mathf.Sin(Mathf.PI * q / 0.6f) : 0f;
        transform.localScale = size * (0.45f + 0.55f * a);
        if (graphic != null) { var c = graphic.color; c.a = a; graphic.color = c; }
        else if (sprite != null) { var c = sprite.color; c.a = a; sprite.color = c; }
    }
}

/// The shine: a slanted band of light crossing the picture in 0.6 s, every 1.5 s.
public class GlowSweep : MonoBehaviour
{
    public float travel = 200f; public Selectable gate;
    private Graphic graphic; private RectTransform rt; private float full = -1f;
    private void LateUpdate()
    {
        if (full < 0f) { graphic = GetComponent<Graphic>(); rt = (RectTransform)transform; full = graphic != null ? graphic.color.a : 1f; }
        var p = Mathf.Repeat(Time.unscaledTime, 1.5f) / 0.6f;
        var on = p < 1f && !(gate != null && !gate.IsInteractable());
        rt.anchoredPosition = new Vector2(Mathf.Lerp(-travel, travel, Mathf.Clamp01(p)), 0f);
        if (graphic != null) { var c = graphic.color; c.a = on ? full : 0f; graphic.color = c; }
    }
}

/// A world glow lies exactly on its sprite, wherever that goes.
public class GlowFollow : MonoBehaviour
{
    public SpriteRenderer target;
    public void LateUpdate()
    {
        if (target == null || !target.gameObject.activeInHierarchy || !target.enabled) { Destroy(gameObject); return; }
        var t = target.transform;
        transform.SetPositionAndRotation(t.position + new Vector3(0f, 0f, 0.01f), t.rotation);
        var s = t.lossyScale; transform.localScale = new Vector3(target.flipX ? -s.x : s.x, target.flipY ? -s.y : s.y, s.z);
    }
}
