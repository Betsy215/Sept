using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// The kitchen waffle machine (owner, 22 October 2026: "a refill system for the cones too, a waffle machine in
/// kitchen"). Chapter 2 only. It sits on the counter's front left, in front of the blender, built at kitchen load like the
/// blender and anchored to the background's rectangle so every phone sees it at the same spot. A tap refills the
/// table's cone holder to four (done as the tap lands, like the blender's refill: closing the kitchen mid-bake
/// unloads the scene and the coroutine with it) and plays the bake (WaffleAnim): the plate glows, steam rises,
/// the lid opens, a cone pops out and floats off, the lid shuts.
/// Pictures: Resources/Kitchen/waffle_closed, waffle_open (same 300 x 220 canvas), waffle_steam, and the cone
/// (Kitchen/waffle_cone, or the station's own cone picture when that is missing).
public static class KitchenWaffleMachine
{
    // the box on the 1080 x 1920 background: the counter's front-left corner, over the painted whisk (210 x 157 px, the 300 x 220
    // picture at 0.7). The counter's right is the strawberry cake's place (owner, 22 October night: "it needs to be placed somewhere else").
    static readonly Vector2 Min = new Vector2(0.03f, 0.418f), Max = new Vector2(0.225f, 0.50f);

    /// Since 25 October 2026 the machine stands on the table beside the cone stack (IceCreamStation); the kitchen has none.
    public static bool OnTheTable = true;

    public static void Apply()
    {
        if (OnTheTable) return;
        if (SessionManager.CurrentChapter < 2) return;
        var closed = Resources.Load<Sprite>("Kitchen/waffle_closed"); if (closed == null) return;
        Transform bg = null;
        foreach (var canvas in Object.FindObjectsOfType<Canvas>())
            if (canvas.gameObject.scene.name == "KitchenScene") { bg = canvas.transform.Find("bg"); if (bg != null) break; }
        if (bg == null || bg.Find("WaffleMachine") != null) return;

        var go = new GameObject("WaffleMachine", typeof(RectTransform), typeof(Image), typeof(WaffleAnim));
        go.transform.SetParent(bg, false); go.transform.SetAsLastSibling();
        var rt = (RectTransform)go.transform; rt.anchorMin = Min; rt.anchorMax = Max; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>(); img.sprite = closed; img.preserveAspect = false; img.raycastTarget = true;   // stretched like the background, as the blender is

        Image Child(string name, Sprite sprite, Vector2 aMin, Vector2 aMax, Vector2 size, bool aspect)
        {
            var c = new GameObject(name, typeof(RectTransform), typeof(Image)); c.transform.SetParent(rt, false);
            var crt = (RectTransform)c.transform; crt.anchorMin = aMin; crt.anchorMax = aMax; crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            if (size != Vector2.zero) { crt.sizeDelta = size; crt.anchoredPosition = Vector2.zero; }
            var ci = c.GetComponent<Image>(); ci.sprite = sprite; ci.preserveAspect = aspect; ci.raycastTarget = false; ci.color = new Color(1f, 1f, 1f, 0f);
            return ci;
        }
        var anim = go.GetComponent<WaffleAnim>(); anim.body = rt;
        anim.open = Child("Open", Resources.Load<Sprite>("Kitchen/waffle_open"), Vector2.zero, Vector2.one, Vector2.zero, false);

        // the cone: from the plate's middle up and away
        var coneSprite = Resources.Load<Sprite>("Kitchen/waffle_cone");
        if (coneSprite == null) { var station = Object.FindObjectOfType<IceCreamStation>(); if (station != null) coneSprite = station.coneSprite; }
        var h = rt.rect.height; var w = rt.rect.width;
        var coneH = h * 0.55f; var coneW = coneSprite != null ? coneH * coneSprite.rect.width / coneSprite.rect.height : coneH * 0.5f;
        anim.cone = Child("Cone", coneSprite, new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(coneW, coneH), true);

        // three steam puffs along the lid's seam
        var steam = Resources.Load<Sprite>("Kitchen/waffle_steam");
        for (var i = 0; i < anim.steam.Length; i++)
        {
            var x = 0.3f + 0.2f * i;
            anim.steam[i] = Child("Steam" + i, steam, new Vector2(x, 0.55f), new Vector2(x, 0.55f), new Vector2(w * 0.16f, w * 0.16f), true);
        }
    }
}

/// The bake: the plate warms (a brief glow), steam puffs rise and fade, the lid opens, a cone pops up and floats off, the lid shuts.
public class WaffleAnim : MonoBehaviour, IPointerClickHandler
{
    public RectTransform body; public Image open, cone; public Image[] steam = new Image[3];
    public static float BakeSeconds = 1.1f;      // the steam phase; a test recipe can slow it for captures
    public bool IsBaking { get; private set; }
    private Image closed; private AudioClip bakeSound;

    private void Awake() { closed = GetComponent<Image>(); }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.timeScale == 0f) return;
        if (eventData.pointerCurrentRaycast.gameObject != gameObject) return;
        Bake();
    }

    public void Bake()
    {
        if (IsBaking) return;
        if (bakeSound == null) bakeSound = Resources.Load<AudioClip>("Kitchen/waffle_bake");
        if (bakeSound != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(bakeSound);
        // the refill as the tap lands (see the class note): the holder is full again by the time the player is back at the table
        var station = FindObjectOfType<IceCreamStation>(); if (station != null) station.RefillCones();
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        IsBaking = true;
        var rt = (RectTransform)transform; var h = rt.rect.height;
        var warm = new Color(1f, 0.93f, 0.82f);
        // 1. the plate warms and steams
        var t = 0f;
        while (t < BakeSeconds)
        {
            t += Time.unscaledDeltaTime; var k = t / BakeSeconds;
            if (closed != null) closed.color = Color.Lerp(Color.white, warm, Mathf.Sin(k * Mathf.PI));
            for (var i = 0; i < steam.Length; i++)
            {
                var s = steam[i]; if (s == null) continue;
                var p = Mathf.Repeat(k * 1.6f + i * 0.33f, 1f);                       // each puff on its own cycle
                s.color = new Color(1f, 1f, 1f, 0.75f * Mathf.Sin(p * Mathf.PI));
                s.rectTransform.anchoredPosition = new Vector2(Mathf.Sin((p + i) * 6f) * h * 0.04f, p * h * 0.45f);
                s.rectTransform.localScale = Vector3.one * (0.6f + 0.7f * p);
            }
            yield return null;
        }
        foreach (var s in steam) if (s != null) s.color = new Color(1f, 1f, 1f, 0f);
        if (closed != null) closed.color = Color.white;
        // 2. the lid opens, the cone pops up and floats off
        t = 0f;
        while (t < 0.25f) { t += Time.unscaledDeltaTime; if (open != null) open.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t / 0.25f)); yield return null; }
        if (cone != null)
        {
            var start = cone.rectTransform.anchoredPosition; t = 0f;
            while (t < 0.8f)
            {
                t += Time.unscaledDeltaTime; var k = Mathf.Clamp01(t / 0.8f);
                var pop = k < 0.2f ? 1f + 0.35f * Mathf.Sin(k / 0.2f * Mathf.PI) : 1f;
                cone.rectTransform.localScale = Vector3.one * pop;
                cone.rectTransform.anchoredPosition = start + new Vector2(0f, Mathf.SmoothStep(0f, 1f, k) * h * 0.9f);
                cone.color = new Color(1f, 1f, 1f, k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f);
                yield return null;
            }
            cone.color = new Color(1f, 1f, 1f, 0f); cone.rectTransform.anchoredPosition = start; cone.rectTransform.localScale = Vector3.one;
        }
        // 3. the lid shuts
        t = 0f;
        while (t < 0.25f) { t += Time.unscaledDeltaTime; if (open != null) open.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01(t / 0.25f)); yield return null; }
        Reset();
    }

    private void Reset()
    {
        if (closed != null) closed.color = Color.white;
        if (open != null) open.color = new Color(1f, 1f, 1f, 0f);
        if (cone != null) { cone.color = new Color(1f, 1f, 1f, 0f); cone.rectTransform.localScale = Vector3.one; }
        foreach (var s in steam) if (s != null) s.color = new Color(1f, 1f, 1f, 0f);
        IsBaking = false;
    }

    private void OnDisable() { Reset(); }
}
