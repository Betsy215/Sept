using UnityEngine;
using UnityEngine.UI;

/// The shop's panel as liquid glass (owner, 19 November 2026: "change shop panel to liquid glass style, make the corners
/// rounded"; then "the glass needs to be much more transparent, almost completely transparent, the tabs and the panel
/// needs to be a whole without space without divider line"). One rounded pane of nearly clear glass holds the tab row
/// and the cards: the shop's background shows through it almost untouched (a trace of blur and of white), with a bright
/// rim and a soft shadow round it. Each tab is its own glass button in the pane's top, the open one filled white
/// (owner, 19 November: "it's hard to tell all tab is clickable ... the two other tab is too invisible", their pick A
/// of four). Pieces:
/// Store/ui/shop_glass_2026-11-19/glass.py (Resources/UI/glass_*). The panel's group is drawn squashed (scale 1 x 0.8),
/// and the pieces are painted taller by the same amount, so the corners are round on screen. Without the pieces the
/// shop keeps its flat panel and folder tabs.
public static class ShopGlass
{
    public const float BarHeight = 112f;  // the tab row's height in the group's units (0.8 of that on screen)
    private const float CornerPx = 75f;   // the pieces' corner height in pixels (their top and bottom border)
    private const float Haze = 0.45f;     // how much of the blurred background is laid over the sharp one
    private const float Veil = 0.14f;     // white in the glass
    private const float PillHeight = 86f, OpenFill = 0.92f, ClosedFill = 0.27f;   // the tabs' buttons

    private static Sprite Piece(string name) => Resources.Load<Sprite>("UI/" + name);
    public static bool Ready => Piece("glass_fill") != null && Piece("glass_rim") != null;

    public class Parts
    {
        public Image[] pills = new Image[3];   // the tabs' glass buttons, in the tabs' order
        public float barBottom;   // the tab row's lower edge (the cards' upper edge), measured from the group's middle
    }

    /// Dresses the panel (the masked viewport inside its group) as the lower part of one pane and lays the tab row in
    /// the pane's top.
    public static Parts Build(RectTransform group, RectTransform viewport, Image panelImage)
    {
        var fill = Piece("glass_fill"); var rim = Piece("glass_rim"); var shadow = Piece("glass_shadow");
        var bg = Background(group);
        var blurred = bg != null && bg.sprite != null ? Blur(bg.sprite.texture) : null;
        if (blurred != null) group.gameObject.AddComponent<GlassOwner>().texture = blurred;
        var w = group.rect.width; var h = group.rect.height;
        var paneH = h + BarHeight; var paneY = BarHeight * 0.5f;   // the pane: the cards' window and the tab row above it

        // the cards' window keeps its mask (rounded like the pane) and shows nothing of its own
        panelImage.sprite = fill; panelImage.type = Image.Type.Sliced; panelImage.pixelsPerUnitMultiplier = 1f;
        panelImage.color = new Color(1f, 1f, 1f, 0.02f);

        // the glass, behind the cards and the tabs: a trace of the blurred background, a trace of white, light on its top
        var pane = NewImage("GlassPane", group, fill, new Color(1f, 1f, 1f, 0.02f)); pane.type = Image.Type.Sliced;
        Box(pane.rectTransform, 0f, paneY, w, paneH);
        pane.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        if (blurred != null) { var back = Backdrop(pane.rectTransform, bg, blurred).GetComponent<RawImage>(); var c = back.color; c.a = Haze; back.color = c; }
        var veil = NewImage("GlassVeil", pane.rectTransform, null, new Color(1f, 1f, 1f, Veil)); Stretch(veil.rectTransform);
        var sheenSprite = Piece("glass_sheen");
        if (sheenSprite != null) { var sheen = NewImage("GlassSheen", pane.rectTransform, sheenSprite, new Color(1f, 1f, 1f, 0.45f)); Stretch(sheen.rectTransform); }
        var paneRim = NewImage("GlassRim", group, rim, Color.white); paneRim.type = Image.Type.Sliced;
        Box(paneRim.rectTransform, 0f, paneY, w, paneH); paneRim.transform.SetSiblingIndex(viewport.GetSiblingIndex() + 1);

        // a glass button behind each tab, inside the pane's top
        var parts = new Parts { barBottom = h * 0.5f };
        var pillM = CornerPx / (PillHeight * 0.5f);
        for (var i = 0; i < parts.pills.Length; i++)
        {
            var pill = NewImage("GlassTab" + i, group, fill, new Color(1f, 1f, 1f, ClosedFill)); pill.type = Image.Type.Sliced; pill.pixelsPerUnitMultiplier = pillM;
            Box(pill.rectTransform, 0f, parts.barBottom + BarHeight * 0.5f - 3f, 200f, PillHeight);
            if (shadow != null)
            {
                var ps = NewImage("Shadow", pill.rectTransform, shadow, Color.white); ps.type = Image.Type.Sliced; ps.pixelsPerUnitMultiplier = pillM * 1.6f;
                var pad = 40f / (pillM * 1.6f); ps.rectTransform.anchorMin = Vector2.zero; ps.rectTransform.anchorMax = Vector2.one;
                ps.rectTransform.offsetMin = new Vector2(-pad, -pad / 0.8f - 5f); ps.rectTransform.offsetMax = new Vector2(pad, pad / 0.8f - 5f);
            }
            var pr = NewImage("Rim", pill.rectTransform, rim, Color.white); pr.type = Image.Type.Sliced; pr.pixelsPerUnitMultiplier = pillM; Stretch(pr.rectTransform);
            parts.pills[i] = pill;
        }

        // back to front in the group: the shadow, the glass, the tabs' buttons, then the cards' window, the rim and the tabs
        for (var i = parts.pills.Length - 1; i >= 0; i--) parts.pills[i].transform.SetAsFirstSibling();
        pane.transform.SetAsFirstSibling();
        if (shadow != null)
        {
            var paneShadow = NewImage("GlassShadow", group, shadow, Color.white); paneShadow.type = Image.Type.Sliced;
            Box(paneShadow.rectTransform, 0f, paneY, w + 80f, paneH + 100f); paneShadow.transform.SetAsFirstSibling();
        }
        return parts;
    }

    /// Places one tab's glass button (x and width of the tab's cell in the group's units) and fills it white when the
    /// tab is the open one.
    public static void PaintTab(Parts parts, int index, float x, float width, bool open, bool visible)
    {
        if (parts == null || index < 0 || index >= parts.pills.Length || parts.pills[index] == null) return;
        var pill = parts.pills[index]; pill.gameObject.SetActive(visible); if (!visible) return;
        var rt = pill.rectTransform; rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y); rt.sizeDelta = new Vector2(width - 36f, rt.sizeDelta.y);
        pill.color = new Color(1f, 1f, 1f, open ? OpenFill : ClosedFill);
        var sh = rt.Find("Shadow"); if (sh != null) sh.gameObject.SetActive(open);
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

/// Frees the blurred picture when the shop goes.
public class GlassOwner : MonoBehaviour
{
    public RenderTexture texture;
    private void OnDestroy() { if (texture != null) { texture.Release(); Destroy(texture); } }
}
