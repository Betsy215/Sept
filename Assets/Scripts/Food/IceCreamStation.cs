using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// The ice cream station on the table (owner, 16 and 17 October 2026): chapter 2's fixed food. A metal box with four
/// flavour sections (vanilla, strawberry, chocolate, mint) and a stack of cones in a holder. Each section holds three
/// scoops and goes down one per scoop taken; the kitchen blender refills every section to full. The player taps the
/// holder (or a section straight away): an empty cone floats up over the box; each tap on a section adds a scoop of
/// that flavour, three at most; the red cross clears the cone (the scoops taken are gone); a tap on the cone serves it
/// to the oldest open order that wants that count and those flavours, in any order (owner, 17 October).
/// The object carries a ServeableItem (foodType "IceCream") for the level, order and menu lists, but its taps are
/// handled here, not by it. The pictures: Assets/Images/icecream (ChatGPT's station and cones, cut by
/// Store/chapter2/icecream_station/cut_station.py).
/// Cones run out too (owner, 22 October 2026): the holder stacks four; every cone started takes one (served or
/// cleared, it is gone like the scoops), an empty holder refuses with a shake, and the kitchen waffle machine
/// (KitchenWaffleMachine) refills the stack to four. The holder picture per count: Resources/IceCream/holder_0..4,
/// holderSprite when a level picture is missing.
public class IceCreamStation : MonoBehaviour
{
    public const int Flavours = 4, PerSection = 3, MaxScoops = 3, ConesFull = 4;
    public static readonly string Letters = "vscm";                 // vanilla, strawberry, chocolate, mint: the order letters
    public static readonly string[] Names = { "vanilla", "strawberry", "chocolate", "mint" };
    public const int BaseFlavours = 2;                              // the station opens with two flavours; the shop's "Two more flavours" upgrade opens all four (owner, 22 October 2026)
    /// How many flavours are open today: two until the shop's ice cream upgrade (IceCream level 2), then four
    public static int ActiveFlavours =>
        SessionManager.Instance != null && SessionManager.Instance.GetFoodUpgradeLevel("IceCream") >= 2 ? Flavours : BaseFlavours;
    public static bool IsOpen(int flavour) => flavour >= 0 && flavour < ActiveFlavours;

    [Header("Pictures")] public Sprite[] sectionLevels = new Sprite[Flavours * PerSection];   // [flavour * 3 + (level - 1)]
    public Sprite coneSprite; public Sprite[] scoopSprites = new Sprite[Flavours]; public Sprite crossSprite; public Sprite holderSprite;
    public Sprite lidSprite;                                        // the lid over a well that is not open yet (22 October)
    [Header("Layout, fractions of the box picture (0..1 from its bottom left)")]
    public Rect[] sectionRects = new Rect[Flavours];        // where each section's picture sits
    public Rect holderRect;
    public Rect[] lidRects = new Rect[Flavours];            // where a closed well's lid sits; an empty rect means over the section's own rect
    public Vector2 buildPoint = new Vector2(0.5f, 1.15f);    // where the floating cone stands
    [Header("Build")] public float scoopStep = 0.62f;        // each scoop this much of a scoop's height above the one below
    public float firstScoopDrop = 0.22f;                      // the first scoop sits this much of its height into the cone
    public AudioClip scoopSound, serveSound;

    private readonly int[] left = { PerSection, PerSection, PerSection, PerSection };
    private int cones = ConesFull; private SpriteRenderer holderRenderer;
    private readonly List<int> cone = new List<int>();        // flavours on the floating cone, bottom first
    private SpriteRenderer[] sections = new SpriteRenderer[Flavours];
    private SpriteRenderer coneRenderer, crossRenderer; private SpriteRenderer[] scoopRenderers = new SpriteRenderer[MaxScoops];
    private Transform build; private bool building;
    private OrderSystem orderSystem; private ServeableItem item; private SpriteRenderer box;
    private Coroutine shake;
    private SpriteRenderer[] lids = new SpriteRenderer[Flavours];   // the closed wells' lids (22 October)

    public bool IsBuilding => building;
    public int Left(int flavour) => left[Mathf.Clamp(flavour, 0, Flavours - 1)];
    public int ConesLeft => cones;

    private void Awake()
    {
        box = GetComponent<SpriteRenderer>();
        item = GetComponent<ServeableItem>(); if (item != null) item.inputHandledElsewhere = true;
        Build();
    }

    private void Start() { orderSystem = FindObjectOfType<OrderSystem>(); RefreshSections(); RefreshHolder(); }

    /// The children: the sections, the holder, the floating cone with its scoops and the cross; built once
    private void Build()
    {
        if (transform.Find("Sec0") != null) return;
        var layer = box != null ? box.sortingLayerName : "Default"; var order = box != null ? box.sortingOrder : 0;
        var b = box != null && box.sprite != null ? box.sprite.bounds : new Bounds(Vector3.zero, Vector3.one);   // local units of this object
        Vector3 At(Vector2 frac) => new Vector3(b.min.x + frac.x * b.size.x, b.min.y + frac.y * b.size.y, 0f);
        for (var i = 0; i < Flavours; i++)
        {
            var r = sectionRects[i];
            var go = new GameObject("Sec" + i, typeof(SpriteRenderer), typeof(BoxCollider), typeof(IceCreamTap)); go.transform.SetParent(transform, false);
            go.transform.localPosition = At(r.center) + Vector3.back * 0.02f;
            var sr = go.GetComponent<SpriteRenderer>(); sr.sortingLayerName = layer; sr.sortingOrder = order + 1; sr.sprite = sectionLevels[i * PerSection + PerSection - 1];
            var col = go.GetComponent<BoxCollider>(); col.size = new Vector3(r.width * b.size.x * 0.9f, r.height * b.size.y * 0.9f, 0.2f);
            var tap = go.GetComponent<IceCreamTap>(); tap.station = this; tap.kind = IceCreamTap.Kind.Section; tap.index = i;
            sections[i] = sr;
            if (i >= BaseFlavours && lidSprite != null)            // a lid over the wells the upgrade opens; the section's collider under it takes the tap (refused while closed)
            {
                var lr = lidRects != null && i < lidRects.Length && lidRects[i].width > 0f ? lidRects[i] : r;
                var lid = new GameObject("Lid" + i, typeof(SpriteRenderer)); lid.transform.SetParent(transform, false);
                lid.transform.localPosition = At(lr.center) + Vector3.back * 0.025f;
                lids[i] = lid.GetComponent<SpriteRenderer>(); lids[i].sortingLayerName = layer; lids[i].sortingOrder = order + 3; lids[i].sprite = lidSprite;
            }
        }
        var h = new GameObject("Holder", typeof(SpriteRenderer), typeof(BoxCollider), typeof(IceCreamTap)); h.transform.SetParent(transform, false);
        h.transform.localPosition = At(holderRect.center) + Vector3.back * 0.02f;
        var hr = h.GetComponent<SpriteRenderer>(); hr.sortingLayerName = layer; hr.sortingOrder = order + 2; hr.sprite = holderSprite; holderRenderer = hr;
        h.GetComponent<BoxCollider>().size = new Vector3(holderRect.width * b.size.x, holderRect.height * b.size.y, 0.2f);
        var ht = h.GetComponent<IceCreamTap>(); ht.station = this; ht.kind = IceCreamTap.Kind.Holder;

        build = new GameObject("Build").transform; build.SetParent(transform, false); build.localPosition = At(buildPoint) + Vector3.back * 0.03f;
        var c = new GameObject("Cone", typeof(SpriteRenderer), typeof(BoxCollider), typeof(IceCreamTap)); c.transform.SetParent(build, false);
        coneRenderer = c.GetComponent<SpriteRenderer>(); coneRenderer.sortingLayerName = layer; coneRenderer.sortingOrder = order + 5; coneRenderer.sprite = coneSprite;
        var ct = c.GetComponent<IceCreamTap>(); ct.station = this; ct.kind = IceCreamTap.Kind.Cone;
        var coneH = coneSprite != null ? coneSprite.bounds.size.y : 1f; var scoopH = scoopSprites[0] != null ? scoopSprites[0].bounds.size.y : 0.6f;
        c.GetComponent<BoxCollider>().size = new Vector3(Mathf.Max(coneSprite != null ? coneSprite.bounds.size.x : 0.5f, scoopSprites[0] != null ? scoopSprites[0].bounds.size.x : 0.6f), coneH + MaxScoops * scoopStep * scoopH, 0.2f);
        c.GetComponent<BoxCollider>().center = new Vector3(0f, MaxScoops * scoopStep * scoopH * 0.5f, 0f);
        for (var i = 0; i < MaxScoops; i++)
        {
            var s = new GameObject("Scoop" + i, typeof(SpriteRenderer)); s.transform.SetParent(build, false);
            s.transform.localPosition = new Vector3(0f, coneH * 0.5f - firstScoopDrop * scoopH + i * scoopStep * scoopH, -0.001f * (i + 1));
            scoopRenderers[i] = s.GetComponent<SpriteRenderer>(); scoopRenderers[i].sortingLayerName = layer; scoopRenderers[i].sortingOrder = order + 6 + i; scoopRenderers[i].enabled = false;
        }
        var x = new GameObject("Cross", typeof(SpriteRenderer), typeof(BoxCollider), typeof(IceCreamTap)); x.transform.SetParent(build, false);
        x.transform.localPosition = new Vector3((coneSprite != null ? coneSprite.bounds.size.x : 0.5f) * 0.5f + 0.35f, coneH * 0.25f, 0f);
        crossRenderer = x.GetComponent<SpriteRenderer>(); crossRenderer.sortingLayerName = layer; crossRenderer.sortingOrder = order + 9; crossRenderer.sprite = crossSprite;
        x.GetComponent<BoxCollider>().size = crossSprite != null ? new Vector3(crossSprite.bounds.size.x * 1.4f, crossSprite.bounds.size.y * 1.4f, 0.2f) : new Vector3(0.3f, 0.3f, 0.2f);
        var xt = x.GetComponent<IceCreamTap>(); xt.station = this; xt.kind = IceCreamTap.Kind.Cross;
        build.gameObject.SetActive(false);
    }

    private bool CanAcceptInput()
    {
        if (Time.timeScale == 0f) return false;
        if (item != null && !item.ServingEnabled) return false;      // arrangement: the station is dragged, not used
        if (item != null && item.popupCanvas != null && item.popupCanvas.activeInHierarchy) return false;   // the finish board is up (21 October)
        return true;
    }

    public void OnTap(IceCreamTap.Kind kind, int index)
    {
        if (!CanAcceptInput()) return;
        switch (kind)
        {
            case IceCreamTap.Kind.Holder: if (!building) StartCone(); break;
            case IceCreamTap.Kind.Section: if (!building && !StartCone()) break; AddScoop(index); break;
            case IceCreamTap.Kind.Cross: ClearCone(); break;
            case IceCreamTap.Kind.Cone: Serve(); break;
        }
    }

    /// A cone off the holder; false (and a shake) when the holder is empty: the waffle machine in the kitchen refills it
    private bool StartCone()
    {
        if (!TakeCone()) { Reject(); return false; }
        building = true; cone.Clear();
        foreach (var s in scoopRenderers) if (s != null) s.enabled = false;
        if (build != null) { build.gameObject.SetActive(true); StartCoroutine(Float(build)); }
        return true;
    }

    public bool TakeCone() { if (cones <= 0) return false; cones--; RefreshHolder(); return true; }

    /// The holder back to four cones: the kitchen waffle machine's job (owner, 22 October)
    public void RefillCones() { cones = ConesFull; RefreshHolder(); }

    private void RefreshHolder()
    {
        if (holderRenderer == null) return;
        var s = Resources.Load<Sprite>("IceCream/holder_" + Mathf.Clamp(cones, 0, ConesFull));
        holderRenderer.sprite = s != null ? s : holderSprite;
        holderRenderer.color = s == null && cones <= 0 ? new Color(0.6f, 0.6f, 0.6f, 0.7f) : Color.white;   // no level picture yet: an empty stack greys
    }

    private void AddScoop(int flavour)
    {
        if (!building || cone.Count >= MaxScoops) { Reject(); return; }
        if (!IsOpen(flavour) || left[flavour] <= 0) { Reject(); return; }
        left[flavour]--; cone.Add(flavour);
        var r = scoopRenderers[cone.Count - 1]; if (r != null) { r.sprite = scoopSprites[flavour]; r.enabled = true; StartCoroutine(Pop(r.transform)); }
        if (scoopSound != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(scoopSound);
        RefreshSections();
    }

    private void ClearCone()
    {
        building = false; cone.Clear();
        if (build != null) build.gameObject.SetActive(false);
    }

    /// The cone's flavours as the order letters, sorted (the order of the scoops does not matter: owner, 17 October)
    public string Variant() { var l = new List<char>(); foreach (var f in cone) l.Add(Letters[f]); l.Sort(); return new string(l.ToArray()); }

    public static string RandomVariant()
    {
        var n = Random.Range(1, MaxScoops + 1); var l = new List<char>();
        for (var i = 0; i < n; i++) l.Add(Letters[Random.Range(0, ActiveFlavours)]);   // orders ask only for open flavours
        l.Sort(); return new string(l.ToArray());
    }

    private void Serve()
    {
        if (!building || cone.Count == 0 || orderSystem == null) { Reject(); return; }
        if (orderSystem.TryServeItem("IceCream", Variant()))
        {
            if (serveSound != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(serveSound);
            ClearCone();
        }
        else Reject();
    }

    private void Reject()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayWrongItemSFX();
        if (shake != null) StopCoroutine(shake);
        shake = StartCoroutine(Shake(build != null && build.gameObject.activeSelf ? build : transform));
    }

    /// Every section back to three scoops: the kitchen blender's job
    public void RefillAll() { for (var i = 0; i < Flavours; i++) left[i] = PerSection; RefreshSections(); }

    private void RefreshSections()
    {
        for (var i = 0; i < Flavours; i++)
        {
            if (sections[i] == null) continue;
            var open = IsOpen(i); if (lids[i] != null) lids[i].enabled = !open;
            var lv = Mathf.Clamp(left[i], 0, PerSection);
            sections[i].enabled = open && lv > 0;
            if (lv > 0) sections[i].sprite = sectionLevels[i * PerSection + lv - 1];
        }
    }

    private IEnumerator Float(Transform t)
    {
        var from = t.localPosition + Vector3.down * 0.25f; var to = t.localPosition; var e = 0f;
        while (e < 0.18f) { e += Time.deltaTime; t.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, e / 0.18f)); yield return null; }
        t.localPosition = to;
    }

    private IEnumerator Pop(Transform t)
    {
        var s = t.localScale; var e = 0f;
        while (e < 0.16f) { e += Time.deltaTime; var k = Mathf.Sin(Mathf.Clamp01(e / 0.16f) * Mathf.PI); t.localScale = s * (1f + 0.25f * k); yield return null; }
        t.localScale = s;
    }

    private IEnumerator Shake(Transform t)
    {
        var rest = t.localPosition;
        for (var i = 0; i < 3; i++)
        {
            t.localPosition = rest + Vector3.right * 0.08f; yield return new WaitForSeconds(0.06f);
            t.localPosition = rest + Vector3.left * 0.08f; yield return new WaitForSeconds(0.06f);
        }
        t.localPosition = rest; shake = null;
    }

    private void OnDisable() { if (build != null) build.gameObject.SetActive(false); building = false; cone.Clear(); }
}

/// A tap target on the station: a section, the holder, the floating cone or the cross
public class IceCreamTap : MonoBehaviour, IPointerClickHandler
{
    public enum Kind { Section, Holder, Cone, Cross }
    public IceCreamStation station; public Kind kind; public int index;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.pointerCurrentRaycast.gameObject != gameObject) return;
        if (station != null) station.OnTap(kind, index);
    }
}
