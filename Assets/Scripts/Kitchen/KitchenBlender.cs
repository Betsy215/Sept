using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// The kitchen blender as its own object (owner, 17 October 2026: the ice cream is made in the existing blender, not a
/// new machine). The painted blender was cut out of the kitchen background (Store/artkit/blender/blender_cut.py) and
/// comes back here as an Image anchored to the background's rectangle at the same spot, so every phone sees it where
/// it was. Over its drink sits a tinted shading picture: at rest the owner's drink colour, while blending the four ice
/// cream colours one by one with a swirl and a shake (BlenderAnim). Built once per kitchen load, every chapter.
public static class KitchenBlender
{
    static readonly Vector2 Min = new Vector2(0.0296f, 0.5005f), Max = new Vector2(0.2065f, 0.6646f);          // the cut-out's box on the background
    static readonly Vector2 FluidMin = new Vector2(0.1571f, 0.327f), FluidMax = new Vector2(0.6806f, 0.6254f); // the drink inside the cut-out
    public static readonly Color Rest = new Color(1f, 1f, 1f, 0f);                                             // at rest the overlay is invisible: the owner's drink shows as drawn
    public static readonly Color[] Flavours =
    {
        new Color(0.98f, 0.94f, 0.82f),   // vanilla
        new Color(0.96f, 0.66f, 0.70f),   // strawberry
        new Color(0.55f, 0.36f, 0.24f),   // chocolate
        new Color(0.69f, 0.86f, 0.70f),   // mint
    };

    public static void Apply()
    {
        var body = Resources.Load<Sprite>("Kitchen/blender"); if (body == null) return;
        Transform bg = null;
        foreach (var canvas in Object.FindObjectsOfType<Canvas>())
            if (canvas.gameObject.scene.name == "KitchenScene") { bg = canvas.transform.Find("bg"); if (bg != null) break; }
        if (bg == null || bg.Find("Blender") != null) return;

        var go = new GameObject("Blender", typeof(RectTransform), typeof(Image), typeof(BlenderAnim));
        go.transform.SetParent(bg, false); go.transform.SetAsLastSibling();      // over the window view, under the shelf items (they are elsewhere)
        var rt = (RectTransform)go.transform; rt.anchorMin = Min; rt.anchorMax = Max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>(); img.sprite = body; img.preserveAspect = false; img.raycastTarget = true;

        var fill = new GameObject("Contents", typeof(RectTransform), typeof(Image)); fill.transform.SetParent(rt, false);
        var frt = (RectTransform)fill.transform; frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
        var fi = fill.GetComponent<Image>(); fi.sprite = Resources.Load<Sprite>("Kitchen/blender_fill"); fi.raycastTarget = false; fi.color = Rest;

        var swirl = new GameObject("Swirl", typeof(RectTransform), typeof(Image)); swirl.transform.SetParent(rt, false);
        var srt = (RectTransform)swirl.transform; srt.anchorMin = FluidMin; srt.anchorMax = FluidMax; srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
        var si = swirl.GetComponent<Image>(); si.sprite = Resources.Load<Sprite>("Kitchen/blender_swirl"); si.preserveAspect = true; si.raycastTarget = false; si.color = new Color(1f, 1f, 1f, 0f);

        var anim = go.GetComponent<BlenderAnim>(); anim.contents = fi; anim.swirl = si; anim.body = rt;
    }
}

/// The blend: a shake, the swirl turning, the drink's colour stepping through the four flavours, then the rest colour again.
public class BlenderAnim : MonoBehaviour, IPointerClickHandler
{
    public Image contents, swirl; public RectTransform body;
    public static float StepSeconds = 0.7f;      // per flavour; a test recipe can slow it down for captures
    public bool IsBlending { get; private set; }
    private Vector2 restPos;

    private void Awake() { restPos = ((RectTransform)transform).anchoredPosition; }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.timeScale == 0f) return;
        if (eventData.pointerCurrentRaycast.gameObject != gameObject) return;
        Blend();
    }

    public void Blend()
    {
        if (IsBlending) return;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        IsBlending = true;
        var rt = (RectTransform)transform;
        var flavours = KitchenBlender.Flavours;
        var from = new Color(flavours[0].r, flavours[0].g, flavours[0].b, 0f);
        for (var i = 0; i < flavours.Length; i++)
        {
            var t = 0f;
            while (t < StepSeconds)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / (StepSeconds * 0.35f));                        // a quick crossfade, then the colour holds
                if (contents != null) contents.color = Color.Lerp(from, flavours[i], k);
                Shake(rt, 1f);
                if (swirl != null) { swirl.color = new Color(1f, 1f, 1f, 0.55f); swirl.rectTransform.Rotate(0f, 0f, -540f * Time.unscaledDeltaTime); }
                yield return null;
            }
            from = flavours[i];
        }
        // settle: the shake dies, the swirl fades, the drink goes back to the owner's colour
        var s = 0f;
        while (s < 0.6f)
        {
            s += Time.unscaledDeltaTime; var k = Mathf.Clamp01(s / 0.6f);
            Shake(rt, 1f - k);
            if (swirl != null) { swirl.color = new Color(1f, 1f, 1f, 0.55f * (1f - k)); swirl.rectTransform.Rotate(0f, 0f, -540f * (1f - k) * Time.unscaledDeltaTime); }
            if (contents != null) contents.color = new Color(from.r, from.g, from.b, 1f - k);
            yield return null;
        }
        rt.anchoredPosition = restPos; rt.localRotation = Quaternion.identity;
        if (contents != null) contents.color = KitchenBlender.Rest;
        if (swirl != null) swirl.color = new Color(1f, 1f, 1f, 0f);
        IsBlending = false;
        // the blend refills every flavour of the table's ice cream station (owner, 17 October)
        var station = FindObjectOfType<IceCreamStation>(); if (station != null) station.RefillAll();
    }

    private void Shake(RectTransform rt, float amount)
    {
        var a = 2.2f * amount;
        rt.anchoredPosition = restPos + new Vector2(Random.Range(-a, a), Random.Range(-a, a) * 0.6f);
        rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-1.2f, 1.2f) * amount);
    }

    private void OnDisable()
    {
        var rt = (RectTransform)transform; rt.anchoredPosition = restPos; rt.localRotation = Quaternion.identity;
        if (contents != null) contents.color = KitchenBlender.Rest;
        if (swirl != null) swirl.color = new Color(1f, 1f, 1f, 0f);
        IsBlending = false;
    }
}
