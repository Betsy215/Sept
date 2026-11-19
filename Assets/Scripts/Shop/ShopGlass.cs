using UnityEngine;
using UnityEngine.UI;

/// The shop's panel as liquid glass (owner, 19 November 2026: "change shop panel to liquid glass style, make the corners
/// rounded"). The panel becomes a rounded pane: the shop's own background shows through it blurred, under a thin white
/// veil, with a bright rim and a soft shadow round it. The tabs become a glass pill above the pane, with a brighter
/// lens that slides to the open tab. Pieces: Store/ui/shop_glass_2026-11-19/glass.py (Resources/UI/glass_*). The
/// panel's group is drawn squashed (scale 1 x 0.8), and the pieces are painted taller by the same amount, so the corners
/// are round on screen. Without the pieces the shop keeps its flat panel and folder tabs.
public static class ShopGlass
{
    public const float BarGap = 22f;      // between the pane's top and the tab pill, in the group's units
    public const float BarHeight = 112f;  // the pill's height in the group's units (0.8 of that on screen)
    private const float CornerPx = 75f;   // the pieces' corner height in pixels (their top and bottom border)

    private static Sprite Piece(string name) => Resources.Load<Sprite>("UI/" + name);
    public static bool Ready => Piece("glass_fill") != null && Piece("glass_rim") != null;

    public class Parts
    {
        public RectTransform bar, thumb;
        public float barBottom;   // the pill's lower edge, measured from the group's middle
    }

    /// Dresses the panel (the masked viewport inside its group) and builds the tab pill above it.
    public static Parts Build(RectTransform group, RectTransform viewport, Image panelImage)
    {
        var fill = Piece("glass_fill"); var rim = Piece("glass_rim"); var shadow = Piece("glass_shadow");
        var bg = Background(group);
        var blurred = bg != null && bg.sprite != null ? Blur(bg.sprite.texture) : null;
        if (blurred != null) group.gameObject.AddComponent<GlassOwner>().texture = blurred;
        var w = group.rect.width; var h = group.rect.height;

        // the pane: the panel's own image is the rounded mask; behind the cards lie the blurred background, a veil of
        // white and the light on its top
        panelImage.sprite = fill; panelImage.type = Image.Type.Sliced; panelImage.pixelsPerUnitMultiplier = 1f;
        panelImage.color = new Color(1f, 1f, 1f, 0.02f);
        var at = 0;
        if (blurred != null) Backdrop(viewport, bg, blurred).SetSiblingIndex(at++);
        var veil = NewImage("GlassVeil", viewport, null, new Color(1f, 1f, 1f, blurred != null ? 0.22f : 0.62f)); Stretch(veil.rectTransform); veil.transform.SetSiblingIndex(at++);
        var sheenSprite = Piece("glass_sheen");
        if (sheenSprite != null) { var sheen = NewImage("GlassSheen", viewport, sheenSprite, Color.white); Stretch(sheen.rectTransform); sheen.transform.SetSiblingIndex(at++); }
        var paneRim = NewImage("GlassRim", group, rim, Color.white); paneRim.type = Image.Type.Sliced;
        Box(paneRim.rectTransform, 0f, 0f, w, h); paneRim.transform.SetSiblingIndex(viewport.GetSiblingIndex() + 1);

        // the tab pill: the same glass, its ends half circles
        var parts = new Parts { barBottom = h * 0.5f + BarGap };
        var barY = parts.barBottom + BarHeight * 0.5f; var barM = CornerPx / (BarHeight * 0.5f);
        var bar = NewImage("GlassBar", group, fill, new Color(1f, 1f, 1f, 0.02f)); bar.type = Image.Type.Sliced; bar.pixelsPerUnitMultiplier = barM;
        Box(bar.rectTransform, 0f, barY, w, BarHeight);
        bar.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        if (blurred != null) Backdrop(bar.rectTransform, bg, blurred);
        var barVeil = NewImage("GlassVeil", bar.rectTransform, null, new Color(1f, 1f, 1f, blurred != null ? 0.16f : 0.50f)); Stretch(barVeil.rectTransform);
        parts.bar = bar.rectTransform;
        var barRim = NewImage("GlassBarRim", group, rim, Color.white); barRim.type = Image.Type.Sliced; barRim.pixelsPerUnitMultiplier = barM;
        Box(barRim.rectTransform, 0f, barY, w, BarHeight);
        // the lens over the open tab
        var thumbH = BarHeight - 20f; var thumbM = CornerPx / (thumbH * 0.5f);
        var thumb = NewImage("GlassThumb", group, fill, new Color(1f, 1f, 1f, 0.46f)); thumb.type = Image.Type.Sliced; thumb.pixelsPerUnitMultiplier = thumbM;
        Box(thumb.rectTransform, 0f, barY, 200f, thumbH);
        var thumbRim = NewImage("Rim", thumb.rectTransform, rim, Color.white); thumbRim.type = Image.Type.Sliced; thumbRim.pixelsPerUnitMultiplier = thumbM; Stretch(thumbRim.rectTransform);
        thumb.gameObject.AddComponent<GlassThumb>();
        parts.thumb = thumb.rectTransform;

        // back to front in the group: shadows, the pill, its rim, the lens, then the tabs, the pane and its rim
        thumb.transform.SetAsFirstSibling(); barRim.transform.SetAsFirstSibling(); bar.transform.SetAsFirstSibling();
        if (shadow != null)
        {
            var barShadow = NewImage("GlassBarShadow", group, shadow, Color.white); barShadow.type = Image.Type.Sliced; barShadow.pixelsPerUnitMultiplier = barM;
            var pad = 40f / barM; Box(barShadow.rectTransform, 0f, barY, w + 2f * pad, BarHeight + 2f * pad / 0.8f); barShadow.transform.SetAsFirstSibling();
            var paneShadow = NewImage("GlassShadow", group, shadow, Color.white); paneShadow.type = Image.Type.Sliced;
            Box(paneShadow.rectTransform, 0f, 0f, w + 80f, h + 100f); paneShadow.transform.SetAsFirstSibling();
        }
        return parts;
    }

    /// Slides the lens under a tab (x and width in the group's units); at once the first time.
    public static void MoveThumb(Parts parts, float x, float width)
    {
        if (parts == null || parts.thumb == null) return;
        var t = parts.thumb.GetComponent<GlassThumb>(); if (t != null) t.Go(x, width - 20f);
    }

    // ---- pieces ----

    /// The shop's background picture: the canvas' "bg", the image the pane lies on.
    private static Image Background(RectTransform group)
    {
        var canvas = group.GetComponentInParent<Canvas>(); if (canvas == null) return null;
        var t = canvas.rootCanvas.transform.Find("bg");
        return t != null ? t.GetComponent<Image>() : null;
    }

    /// A blurred copy of a picture at a quarter of its size: halved four times (each halving averages four pixels),
    /// then doubled twice, which leaves a smooth blur of about sixteen pixels. Works on pictures that cannot be read.
    private static RenderTexture Blur(Texture source)
    {
        if (source == null) return null;
        int w = Mathf.Max(8, source.width / 2), h = Mathf.Max(8, source.height / 2);
        var a = RenderTexture.GetTemporary(w, h, 0); a.filterMode = FilterMode.Bilinear; Graphics.Blit(source, a);
        for (var i = 0; i < 4; i++)
        {
            w = Mathf.Max(4, w / 2); h = Mathf.Max(4, h / 2);
            var b = RenderTexture.GetTemporary(w, h, 0); b.filterMode = FilterMode.Bilinear; Graphics.Blit(a, b); RenderTexture.ReleaseTemporary(a); a = b;
        }
        for (var i = 0; i < 2; i++)
        {
            w *= 2; h *= 2;
            var b = RenderTexture.GetTemporary(w, h, 0); b.filterMode = FilterMode.Bilinear; Graphics.Blit(a, b); RenderTexture.ReleaseTemporary(a); a = b;
        }
        var result = new RenderTexture(w * 2, h * 2, 0) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "ShopGlassBlur" };
        Graphics.Blit(a, result); RenderTexture.ReleaseTemporary(a);
        return result;
    }

    private static Transform Backdrop(RectTransform parent, Image bg, Texture blurred)
    {
        var go = new GameObject("GlassBack", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.transform.SetParent(parent, false);
        var raw = go.GetComponent<RawImage>(); raw.texture = blurred; raw.raycastTarget = false; raw.color = bg.color;
        var s = bg.sprite; var tr = s.textureRect; var tex = s.texture;
        raw.uvRect = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
        var follow = go.AddComponent<GlassBackdrop>(); follow.source = bg.rectTransform; follow.Align();
        return go.transform;
    }

    private static Image NewImage(string name, RectTransform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>(); img.sprite = sprite; img.color = color; img.raycastTarget = false;
        return img;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private static void Box(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
    }
}

/// Keeps the blurred picture exactly over the sharp one, wherever the pane is (it slides in, and it is squashed).
public class GlassBackdrop : MonoBehaviour
{
    public RectTransform source;
    private readonly Vector3[] corners = new Vector3[4];

    private void LateUpdate() { Align(); }

    public void Align()
    {
        var parent = transform.parent as RectTransform; if (source == null || parent == null) return;
        source.GetWorldCorners(corners);
        var a = parent.InverseTransformPoint(corners[0]); var b = parent.InverseTransformPoint(corners[2]);
        var rt = (RectTransform)transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
        rt.localPosition = new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, 0f);
    }
}

/// The lens of the tab pill: glides to the open tab.
public class GlassThumb : MonoBehaviour
{
    private float toX, toW; private bool placed;

    public void Go(float x, float width)
    {
        toX = x; toW = width;
        if (!placed) { placed = true; Set(x, width); }
    }

    private void Update()
    {
        var rt = (RectTransform)transform;
        var k = 1f - Mathf.Exp(-18f * Mathf.Min(Time.unscaledDeltaTime, 0.05f));
        Set(Mathf.Lerp(rt.anchoredPosition.x, toX, k), Mathf.Lerp(rt.sizeDelta.x, toW, k));
    }

    private void Set(float x, float width)
    {
        var rt = (RectTransform)transform;
        rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y); rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
    }
}

/// Frees the blurred picture when the shop goes.
public class GlassOwner : MonoBehaviour
{
    public RenderTexture texture;
    private void OnDestroy() { if (texture != null) { texture.Release(); Destroy(texture); } }
}
