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
    const float ClusterX = 0.4035f, ClusterY = 0.6275f, ClusterW = 0.2953f, ClusterH = 0.1538f;   // the cluster's box on the palm picture (measured on palm.png)

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
            // the coconut cluster as its own picture with the apples' halo baked in (Resources/Chapter2/coconuts, cut
            // from the palm at its own spot), so it reads as something to pick; later the Coconut pick object
            var cluster = Chapter.Art<Sprite>(2, "coconuts");
            if (cluster != null)
            {
                var c = new GameObject("Coconuts", typeof(RectTransform), typeof(Image)); c.transform.SetParent(prt, false);
                var crt = (RectTransform)c.transform; crt.anchorMin = crt.anchorMax = new Vector2(ClusterX, ClusterY); crt.pivot = new Vector2(0.5f, 0.5f);
                crt.sizeDelta = new Vector2(prt.sizeDelta.x * ClusterW, prt.sizeDelta.y * ClusterH); crt.anchoredPosition = Vector2.zero;
                var ci = c.GetComponent<Image>(); ci.sprite = cluster; ci.preserveAspect = true; ci.raycastTarget = false;
            }
        }

        // night through the glass (owner, 16 October): the painted beach night fades in over the day picture as the
        // day's clock reaches dusk and night, and the palm darkens with it
        var night = Chapter.Art<Sprite>(2, "bg_night");
        if (night != null)
        {
            var nv = new GameObject("BeachNight", typeof(RectTransform), typeof(Image)); nv.transform.SetParent(rt, false); nv.transform.SetSiblingIndex(1);
            var nrt = (RectTransform)nv.transform; nrt.anchorMin = vrt.anchorMin; nrt.anchorMax = vrt.anchorMax; nrt.sizeDelta = vrt.sizeDelta; nrt.anchoredPosition = vrt.anchoredPosition;
            var ni = nv.GetComponent<Image>(); ni.sprite = night; ni.preserveAspect = true; ni.raycastTarget = false; ni.color = new Color(1f, 1f, 1f, 0f);
            var fade = go.AddComponent<KitchenWindowNight>(); fade.night = ni; fade.palm = rt.Find("Palm") != null ? rt.Find("Palm").GetComponent<Image>() : null;
        }

        // the apple tree is chapter 1's
        var tree = bg.Find("tree"); if (tree != null) tree.gameObject.SetActive(false);
    }
}

/// Fades the night picture in the chapter 2 kitchen window with the day's clock and dims the palm
public class KitchenWindowNight : MonoBehaviour
{
    public Image night, palm;
    private void Update()
    {
        var st = DayNightTint.CurrentStage;
        var a = st <= 1f ? 0.5f * st : 0.5f + 0.5f * (st - 1f);
        if (night != null) night.color = new Color(1f, 1f, 1f, a);
        if (palm != null) palm.color = Color.Lerp(Color.white, new Color(0.45f, 0.5f, 0.7f), a);
    }
}
