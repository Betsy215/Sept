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
    // the owner's new blender (23 October 2026, Store/artkit/blender/owner_blender_layers.py): the same spot and height as the
    // cut-out's box, the width follows the new drawing's proportions (290 x 355 instead of 191 x 285)
    static readonly Vector2 Min = new Vector2(0.0296f, 0.5005f), Max = new Vector2(0.2453f, 0.649f);
    const float GlowPad = 40f;   // Resources/Kitchen/blender_glow is blender.png with 40 px of room on every side, painted by Docs/tools/glow_brush.py
    static readonly Vector2 FluidMin = new Vector2(0.2034f, 0.3662f), FluidMax = new Vector2(0.7172f, 0.7549f); // the drink inside the jug (layout.json)
    // the blade from the owner's rest picture of 18 November 2026 (Store/redesign/blender_2026-11-18/build_layers.py): a box
    // centred on the shaft (x 128..147 of 290), so the squeeze turns it about its own axis; its foot reaches into the ice
    static readonly Vector2 BladeMin = new Vector2(0.31724f, 0.63944f), BladeMax = new Vector2(0.63103f, 0.87042f);
    public const int ChurnFrames = 12;   // Resources/Kitchen/blender_churn_00..11: the ice turning, one turn per loop
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
        // the owner's own cream blender as drawn, cut from the background, with a blade added (owner, 17 October:
        // "keep the original blender, only add a blade"); its drink blends the four ice cream colours
        var ch2 = SessionManager.CurrentChapter >= 2;
        // no ice cream, no blender (owner, 15 November 2026: "without ice cream purchase, the blender should not be
        // there"); the kitchen picture itself no longer has one painted in (Store/kitchen_blender_patch_2026-11-15)
        var sm = SessionManager.Instance;
        if (sm != null && sm.HasActiveSession() && !sm.IsFoodItemPurchased("IceCream")) return;
        var body = Resources.Load<Sprite>("Kitchen/blender"); if (body == null) return;
        Transform bg = null;
        foreach (var canvas in Object.FindObjectsOfType<Canvas>())
            if (canvas.gameObject.scene.name == "KitchenScene") { bg = canvas.transform.Find("bg"); if (bg != null) break; }
        if (bg == null || bg.Find("Blender") != null || bg.Find("Arrange/Blender") != null) return;   // KitchenArrange moves it under "Arrange"

        var go = new GameObject("Blender", typeof(RectTransform), typeof(Image), typeof(BlenderAnim));
        go.transform.SetParent(bg, false); go.transform.SetAsLastSibling();      // over the window view, under the shelf items (they are elsewhere)
        var rt = (RectTransform)go.transform; rt.anchorMin = Min; rt.anchorMax = Max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>(); img.sprite = body; img.preserveAspect = false; img.raycastTarget = ch2;   // stretched exactly like the background it was cut from
        // the "you can tap this" halo, made the way the coffee bags' is (owner, 15 November 2026: "add this glow effect to
        // other items that is intractable, for now, it is just the blender"; "double check the glow effect on coffee bag,
        // make blender the same way"). The bags' halo is painted into their own picture: no animation, it moves and pops
        // with them. Here it is part of the blender too, its first child, so it shakes with the blend; it is clear inside
        // the blender's shape, so lying over the body hides nothing. Only where the blender takes taps (chapter 2).
        var halo = ch2 ? Resources.Load<Sprite>("Kitchen/blender_glow") : null;
        if (halo != null)
        {
            var hg = new GameObject("Glow", typeof(RectTransform), typeof(Image)); hg.transform.SetParent(rt, false); hg.transform.SetAsFirstSibling();
            var px = GlowPad / body.rect.width; var py = GlowPad / body.rect.height;
            var hrt = (RectTransform)hg.transform; hrt.anchorMin = new Vector2(-px, -py); hrt.anchorMax = new Vector2(1f + px, 1f + py); hrt.offsetMin = Vector2.zero; hrt.offsetMax = Vector2.zero;
            var hi = hg.GetComponent<Image>(); hi.sprite = halo; hi.preserveAspect = false; hi.raycastTarget = false;
        }
        // the blending element the owner drew, its own layer so the blend can spin it (owner, 23 October: "that blending
        // thing turns on and swirls"); at rest it simply sits where it was drawn, in every chapter
        Image bladeImage = null;
        var bladeSprite = Resources.Load<Sprite>("Kitchen/blender_blade");
        if (bladeSprite != null)
        {
            var blade = new GameObject("Blade", typeof(RectTransform), typeof(Image)); blade.transform.SetParent(rt, false);
            var brt = (RectTransform)blade.transform; brt.anchorMin = BladeMin; brt.anchorMax = BladeMax; brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
            bladeImage = blade.GetComponent<Image>(); bladeImage.sprite = bladeSprite; bladeImage.preserveAspect = false; bladeImage.raycastTarget = false;
        }
        if (!ch2) return;                                                                 // chapter 1: the picture alone

        // the ice while blending (owner, 18 November 2026: "redo the animation of blender ... pay attention to the
        // details"): twelve pictures of the owner's own frost carried round the shaft, the surface climbing the glass,
        // ice chips going round; near white, so each flavour tints it and the frost's grain shows through
        var churn = new Sprite[ChurnFrames];
        for (var i = 0; i < ChurnFrames; i++) churn[i] = Resources.Load<Sprite>("Kitchen/blender_churn_" + i.ToString("00"));
        Image Full(string name, Sprite sprite)
        {
            var g = new GameObject(name, typeof(RectTransform), typeof(Image)); g.transform.SetParent(rt, false);
            var r = (RectTransform)g.transform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            var im = g.GetComponent<Image>(); im.sprite = sprite; im.preserveAspect = false; im.raycastTarget = false; im.color = Rest;
            return im;
        }
        var fi = Full("Contents", churn[0] != null ? churn[0] : Resources.Load<Sprite>("Kitchen/blender_fill"));
        if (bladeImage != null) bladeImage.transform.SetAsLastSibling();                 // the blade turns inside the ice, in front of it
        var marks = Full("Marks", Resources.Load<Sprite>("Kitchen/blender_scale"));       // the measuring marks printed on the glass, in front of all
        var button = Full("ButtonGlow", Resources.Load<Sprite>("Kitchen/blender_button_glow"));   // the snowflake button lights while the motor runs

        var anim = go.GetComponent<BlenderAnim>(); anim.contents = fi; anim.body = rt; anim.blade = bladeImage;
        anim.churn = churn; anim.marks = marks; anim.buttonGlow = button;
    }
}

/// The blend: a shake, the swirl turning, the drink's colour stepping through the four flavours, then the rest colour again.
public class BlenderAnim : MonoBehaviour, IPointerClickHandler
{
    public Image contents, swirl, blade, marks, buttonGlow; public RectTransform body; public Sprite[] churn;
    public static float ChurnFps = 12f;   // the ice's pictures per second, one turn per 12 (owner, 18 November 2026: "slower"; was 18)
    public static float BladeTurnsPerSecond = 1.6f;   // slower (owner, 18 November 2026: "slower"; was 2.5)   // the blending element spins about its vertical axis (seen as a width squeeze and flip)

    /// The blade seen turning: its width follows the cosine of the turn, flipping as it passes edge-on
    private void Spin(float amount)
    {
        if (blade == null) return;
        var c = Mathf.Cos(Time.unscaledTime * BladeTurnsPerSecond * 2f * Mathf.PI);
        var sx = Mathf.Lerp(1f, c, amount); if (Mathf.Abs(sx) < 0.08f) sx = sx < 0f ? -0.08f : 0.08f;
        blade.rectTransform.localScale = new Vector3(sx, 1f, 1f);
    }
    public static float StepSeconds = 1.1f;      // per flavour (owner, 18 November 2026: "slower"; was 0.7); a test recipe can slow it down for captures
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
        // the blend's own sound (owner, 18 October): a motor whirr spinning up and down, Resources/Kitchen/blender_blend
        if (blendSound == null) blendSound = Resources.Load<AudioClip>("Kitchen/blender_blend");
        if (blendSound != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(blendSound);
        // the blend refills every flavour of the table's ice cream station (owner, 17 October); done as the blend starts,
        // because closing the kitchen mid-blend unloads the scene and the coroutine with it (21 October)
        var station = FindObjectOfType<IceCreamStation>(); if (station != null) station.RefillAll();
        StartCoroutine(Run());
    }
    private AudioClip blendSound;

    private IEnumerator Run()
    {
        IsBlending = true;
        var rt = (RectTransform)transform;
        var flavours = KitchenBlender.Flavours;
        var from = new Color(flavours[0].r, flavours[0].g, flavours[0].b, 0f);     // the first flavour fades in over the drawn drink
        for (var i = 0; i < flavours.Length; i++)
        {
            var t = 0f;
            while (t < StepSeconds)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / (StepSeconds * 0.35f));                        // a quick crossfade, then the colour holds
                if (contents != null) contents.color = Color.Lerp(from, flavours[i], k);
                Turn(i == 0 ? k : 1f); Spin(1f);
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
            Shake(rt, 1f - k); Turn(1f - k); Spin(1f - k);
            if (swirl != null) { swirl.color = new Color(1f, 1f, 1f, 0.55f * (1f - k)); swirl.rectTransform.Rotate(0f, 0f, -540f * (1f - k) * Time.unscaledDeltaTime); }
            if (contents != null) contents.color = new Color(from.r, from.g, from.b, 1f - k);           // the last flavour fades out to the drawn drink
            yield return null;
        }
        rt.anchoredPosition = restPos; rt.localRotation = Quaternion.identity;
        if (contents != null) { contents.color = KitchenBlender.Rest; contents.rectTransform.localScale = Vector3.one; contents.rectTransform.anchoredPosition = Vector2.zero; }
        SetAlpha(marks, 0f); SetAlpha(buttonGlow, 0f); turned = 0f;
        if (swirl != null) swirl.color = new Color(1f, 1f, 1f, 0f);
        if (blade != null) blade.rectTransform.localScale = Vector3.one;
        IsBlending = false;
    }

    private float turned;
    private static void SetAlpha(Image im, float a) { if (im == null) return; var c = im.color; c.a = a; im.color = c; }

    /// The ice turns (its pictures in a loop), the marks on the glass show over it as it shows, the button glows
    private void Turn(float amount)
    {
        turned += Time.unscaledDeltaTime * ChurnFps * Mathf.Max(0.25f, amount);
        if (churn != null && churn.Length > 0 && contents != null)
        {
            var s = churn[(int)turned % churn.Length]; if (s != null) contents.sprite = s;
        }
        var shown = contents != null ? contents.color.a : 0f;
        SetAlpha(marks, shown);
        SetAlpha(buttonGlow, amount * (0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 9f)));
    }

    /// The swirl churns: a quick pulse in height and a sway, as the blades push it round
    private void Churn(float amount)
    {
        if (contents == null) return;
        var t = Time.unscaledTime;
        contents.rectTransform.localScale = new Vector3(1f + 0.03f * amount * Mathf.Sin(t * 31f), 1f + 0.05f * amount * Mathf.Sin(t * 23f + 1f), 1f);
        contents.rectTransform.anchoredPosition = new Vector2(1.5f * amount * Mathf.Sin(t * 19f), 0f);
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
        if (blade != null) blade.rectTransform.localScale = Vector3.one;
        SetAlpha(marks, 0f); SetAlpha(buttonGlow, 0f);
        IsBlending = false;
    }
}
