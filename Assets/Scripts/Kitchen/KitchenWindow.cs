using UnityEngine;
using UnityEngine.UI;

/// The kitchen window's view as its own object (owner, 16 October): in chapter 2 the glass shows the beach, simpler
/// and smaller than the table scene, with the coconut palm where the apple tree stands in chapter 1. Built at
/// kitchen load, anchored to the background Image's rectangle (the same glass anchors DayNightTint uses), inside a
/// RectMask2D, so on a wider or narrower phone the view is cropped at the glass instead of stretched. Chapter 1 is
/// untouched (the painted window and the apple tree stay).
public static class KitchenWindow
{
    static readonly Vector2 GlassMin = new Vector2(0.502f, 0.556f), GlassMax = new Vector2(0.922f, 0.803f);
    const float CoconutX = 0.55f, CoconutY = 0.60f;   // the cluster's place on the palm picture (measured on palm.png)

    static Sprite glowSprite;
    /// A soft round glow made once at runtime (like the map's three-star glow)
    static Sprite GlowSprite()
    {
        if (glowSprite != null) return glowSprite;
        const int n = 64; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color[n * n];
        for (var y = 0; y < n; y++) for (var x = 0; x < n; x++)
        {
            var dx = (x + 0.5f - n / 2f) / (n / 2f); var dy = (y + 0.5f - n / 2f) / (n / 2f);
            var a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy)); px[y * n + x] = new Color(1f, 1f, 1f, a * a * a);
        }
        tex.SetPixels(px); tex.Apply(); glowSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f)); return glowSprite;
    }

    public static void Apply()
    {
        if (SessionManager.CurrentChapter < 2) return;
        var beach = Chapter.Art<Sprite>(2, "bg_day"); if (beach == null) return;
        Transform bg = null;
        foreach (var canvas in Object.FindObjectsOfType<Canvas>())
            if (canvas.gameObject.scene.name == "KitchenScene") { bg = canvas.transform.Find("bg"); if (bg != null) break; }
        if (bg == null || bg.Find("WindowView") != null) return;

        var go = new GameObject("WindowView", typeof(RectTransform), typeof(RectMask2D));
        go.transform.SetParent(bg, false); go.transform.SetSiblingIndex(0);          // over the painted window, under the shelf and ovens
        var rt = (RectTransform)go.transform;
        rt.anchorMin = GlassMin; rt.anchorMax = GlassMax; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        // the beach: the picture's sky and sea band through the glass, cropped by the mask, never stretched
        var view = new GameObject("Beach", typeof(RectTransform), typeof(Image)); view.transform.SetParent(rt, false);
        var vrt = (RectTransform)view.transform; vrt.anchorMin = new Vector2(0.5f, 0.5f); vrt.anchorMax = new Vector2(0.5f, 0.5f);
        var glassW = rt.rect.width; var glassH = rt.rect.height;
        var aspect = beach.rect.width / beach.rect.height;
        var h = glassH * 2.6f; var w = h * aspect; if (w < glassW * 1.1f) { w = glassW * 1.1f; h = w / aspect; }
        vrt.sizeDelta = new Vector2(w, h); vrt.anchoredPosition = new Vector2(0f, -h * 0.18f);   // the horizon a little above the glass's middle
        var vi = view.GetComponent<Image>(); vi.sprite = beach; vi.preserveAspect = true; vi.raycastTarget = false;

        // the palm, on the right of the glass, its crown in view (the coconut cluster is its own pick object later)
        var palm = Chapter.Art<Sprite>(2, "palm");
        if (palm != null)
        {
            var p = new GameObject("Palm", typeof(RectTransform), typeof(Image)); p.transform.SetParent(rt, false);
            // along the left side of the window (owner, 16 October), its crown in the glass
            var prt = (RectTransform)p.transform; prt.anchorMin = new Vector2(0f, 0f); prt.anchorMax = new Vector2(0f, 0f); prt.pivot = new Vector2(0f, 0f);
            var ph = glassH * 1.45f; prt.sizeDelta = new Vector2(ph * palm.rect.width / palm.rect.height, ph); prt.anchoredPosition = new Vector2(-glassW * 0.16f, -glassH * 0.25f);
            var pi = p.GetComponent<Image>(); pi.sprite = palm; pi.preserveAspect = true; pi.raycastTarget = false;
            // the coconuts glow softly like the apples, so they read as something to pick (the pick object comes with the Coconut cluster)
            var glow = new GameObject("CoconutGlow", typeof(RectTransform), typeof(Image)); glow.transform.SetParent(prt, false); glow.transform.SetAsFirstSibling();
            var grt = (RectTransform)glow.transform; grt.anchorMin = grt.anchorMax = new Vector2(CoconutX, CoconutY); grt.sizeDelta = new Vector2(prt.sizeDelta.x * 0.42f, prt.sizeDelta.x * 0.42f);
            var gi = glow.GetComponent<Image>(); gi.sprite = GlowSprite(); gi.color = new Color(1f, 0.95f, 0.6f, 0.85f); gi.raycastTarget = false;
            glow.AddComponent<MapGlowPulse>();
        }

        // the apple tree is chapter 1's
        var tree = bg.Find("tree"); if (tree != null) tree.gameObject.SetActive(false);
    }
}
