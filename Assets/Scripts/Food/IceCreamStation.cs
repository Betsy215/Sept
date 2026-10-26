using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// The ice cream station on the table (owner, 16 and 17 October 2026): chapter 2's fixed food. A box with flavour wells
/// (vanilla, strawberry, chocolate, mint; two open until the shop's "Two more flavours" card). Each well holds three
/// scoops and goes down one per scoop taken; the kitchen blender refills every well.
/// The glass-dish flow (owner, 26 October 2026): a row of glass dishes stands under the box, four at first and six
/// once four flavours are open. The waffle machine sits to the right of the box; a bake (8 s) drops a waffle bowl
/// into up to two empty dishes. A tap on a dish lifts it (bigger, in front, a trash can beside it); taps on the
/// wells drop scoops into the lifted dish (three at most; without a lifted dish, into the first dish with room);
/// the trash can empties the dish; a tap anywhere else puts the dish down with whatever is in it; a second tap on a
/// lifted dish serves it to the oldest open order wanting that count, those flavours and the same waffle choice.
/// Orders ask with or without a waffle (variant letter 'w' after the sorted flavour letters).
/// The object carries a ServeableItem (foodType "IceCream") for the level, order and menu lists, but its taps are
/// handled here, not by it. Pictures: Assets/Images/icecream (the box and wells) and Resources/IceCream (dish_back,
/// dish_front_empty, dish_front_bowl, bowl, scoop_small_<f>, trash, waffle_closed/open; cones_holder/game_parts.py).
public class IceCreamStation : MonoBehaviour
{
    public const int Flavours = 4, PerSection = 3, MaxScoops = 3, ConesFull = 6;
    public const int DishesBase = 4, DishesFull = 6;                // dishes under the box: four with two flavours, six with four (owner, 26 October 2026)
    public const char WaffleLetter = 'w';                           // the variant's last letter when the order wants a waffle bowl
    public static readonly string Letters = "vscm";                 // vanilla, strawberry, chocolate, mint: the order letters
    public static readonly string[] Names = { "vanilla", "strawberry", "chocolate", "mint" };
    public const int BaseFlavours = 2;                              // the station opens with two flavours; the shop's "Two more flavours" upgrade opens all four (owner, 22 October 2026)
    /// How many flavours are open today: two until the shop's ice cream upgrade (IceCream level 2), then four
    public static int ActiveFlavours =>
        SessionManager.Instance != null && SessionManager.Instance.GetFoodUpgradeLevel("IceCream") >= 2 ? Flavours : BaseFlavours;
    public static bool IsOpen(int flavour) => flavour >= 0 && flavour < ActiveFlavours;
    public static int Dishes => ActiveFlavours >= Flavours ? DishesFull : DishesBase;
    /// The scoops in a variant (the waffle letter is not a scoop)
    public static int ScoopCount(string variant) { if (variant == null) return 0; var n = 0; foreach (var ch in variant) if (ch != WaffleLetter) n++; return n; }

    [Header("Pictures")] public Sprite[] sectionLevels = new Sprite[Flavours * PerSection];   // [flavour * 3 + (level - 1)]
    public Sprite coneSprite; public Sprite[] scoopSprites = new Sprite[Flavours]; public Sprite crossSprite; public Sprite holderSprite;   // the cone flow's pictures (before 26 October); scoopSprites is the fallback for the small scoops
    [Header("Layout, fractions of the box picture (0..1 from its bottom left)")]
    public Rect[] sectionRects = new Rect[Flavours];        // where each section's picture sits
    public Rect holderRect;                                 // unused since the dish flow; kept for the scene's values
    public Vector2 buildPoint = new Vector2(0.5f, 1.15f);
    [Header("Two-flavour station (level 1, owner 23 October 2026: half the size, two wells, no lids; the same canvas)")]
    public Sprite smallBoxSprite;                             // the short box; null = the old look (the long box with two lids)
    public Sprite[] smallSectionLevels = new Sprite[BaseFlavours * PerSection];   // [flavour * 3 + (level - 1)] for the two wells
    public Rect[] smallSectionRects = new Rect[BaseFlavours];
    public Rect smallHolderRect;
    public Vector2 smallBuildPoint = new Vector2(0.27f, 1.22f);
    [Header("Waffle machine on the table (owner, 26 October 2026: to the right of the box)")]
    public float waffleHeight = 0.66f;                       // its height as a share of the box picture's height
    public float waffleGap = 0.02f;                           // between the box's right edge and the machine, a fraction of the box width
    public float waffleShift = -0.01f;                      // unused since 26 October; kept for the scene's value
    public const int WafflesPerSession = 2; public const float WaffleSeconds = 8f;   // one session bakes two bowls in 8 s (owner, 25 October)
    [Header("Dish row (owner, 26 October 2026)")]
    public float dishWidth = 0.15f;                           // one dish's width as a share of the box picture's width
    public float boxBottom = 0.12f;                           // where the painted box ends, a fraction of the canvas height
    public float rowGap = 0.0f;                              // between the box's bottom and the dishes' tops, a fraction of the canvas height
    public float liftScale = 1.5f;                            // a lifted dish grows this much
    public float liftRise = 0.1f;                            // and rises this share of its height
    public float scoopSpread = 0.72f, scoopTop = 0.669f, scoopZig = 0.014f;   // scoops side by side: step as a share of a scoop's width, the row's height in the dish, the odd scoops a little lower
    [Header("Build")] public float scoopStep = 0.62f;        // the cone flow's values, kept for the scene
    public float firstScoopDrop = 0.22f;
    public AudioClip scoopSound, serveSound;

    private class Dish
    {
        public Transform root; public SpriteRenderer back, bowl, front; public SpriteRenderer[] scoops = new SpriteRenderer[MaxScoops]; public GameObject trash;
        public bool waffle; public readonly List<int> flavours = new List<int>(); public Vector3 rest; public float scale; public int order;
        public bool Empty => !waffle && flavours.Count == 0;
    }

    private readonly int[] left = { PerSection, PerSection, PerSection, PerSection };
    private readonly List<Dish> dishes = new List<Dish>(); private int selected = -1;
    private SpriteRenderer[] sections = new SpriteRenderer[Flavours];
    private OrderSystem orderSystem; private ServeableItem item; private SpriteRenderer box;
    private Coroutine shake;
    private Sprite[] activeLevels;                                  // the section pictures of the box in use (small or long)
    private SpriteRenderer waffleRenderer; private Sprite waffleClosed, waffleOpen; private Coroutine waffleBake;
    private Sprite dishBack, dishFrontEmpty, dishFrontBowl, bowlSprite, trashSprite; private readonly Sprite[] smallScoops = new Sprite[Flavours];
    private string layer; private int order;

    public bool IsBuilding => selected >= 0;
    public int Left(int flavour) => left[Mathf.Clamp(flavour, 0, Flavours - 1)];
    /// Dishes holding a waffle bowl (the cone flow's count, kept for the editor driver)
    public int ConesLeft { get { var n = 0; foreach (var d in dishes) if (d.waffle) n++; return n; } }
    public int EmptyDishes { get { var n = 0; foreach (var d in dishes) if (d.Empty) n++; return n; } }

    private void Awake()
    {
        box = GetComponent<SpriteRenderer>();
        item = GetComponent<ServeableItem>(); if (item != null) item.inputHandledElsewhere = true;
        Build();
    }

    private void Start() { orderSystem = FindObjectOfType<OrderSystem>(); RefreshSections(); foreach (var d in dishes) RefreshDish(d); }

    /// The children: the wells, the dish row, the waffle machine; built once
    private void Build()
    {
        if (transform.Find("Sec0") != null) return;
        layer = box != null ? box.sortingLayerName : "Default"; order = box != null ? box.sortingOrder : 0;
        var small = ActiveFlavours < Flavours && smallBoxSprite != null;
        if (small && box != null) box.sprite = smallBoxSprite;
        activeLevels = small ? smallSectionLevels : sectionLevels;
        var rects = small ? smallSectionRects : sectionRects;
        var count = small ? BaseFlavours : Flavours;
        var b = box != null && box.sprite != null ? box.sprite.bounds : new Bounds(Vector3.zero, Vector3.one);   // local units of this object
        Vector3 At(Vector2 frac) => new Vector3(b.min.x + frac.x * b.size.x, b.min.y + frac.y * b.size.y, 0f);
        dishBack = Resources.Load<Sprite>("IceCream/dish_back"); dishFrontEmpty = Resources.Load<Sprite>("IceCream/dish_front_empty"); dishFrontBowl = Resources.Load<Sprite>("IceCream/dish_front_bowl");
        bowlSprite = Resources.Load<Sprite>("IceCream/bowl"); trashSprite = Resources.Load<Sprite>("IceCream/trash");
        for (var i = 0; i < Flavours; i++) { smallScoops[i] = Resources.Load<Sprite>("IceCream/scoop_small_" + Letters[i]); if (smallScoops[i] == null) smallScoops[i] = scoopSprites[i]; }
        waffleClosed = Resources.Load<Sprite>("IceCream/waffle_closed"); waffleOpen = Resources.Load<Sprite>("IceCream/waffle_open");

        for (var i = 0; i < count; i++)
        {
            var r = rects[i];
            var go = new GameObject("Sec" + i, typeof(SpriteRenderer), typeof(BoxCollider), typeof(IceCreamTap)); go.transform.SetParent(transform, false);
            go.transform.localPosition = At(r.center) + Vector3.back * 0.02f;
            var sr = go.GetComponent<SpriteRenderer>(); sr.sortingLayerName = layer; sr.sortingOrder = order + 1; sr.sprite = activeLevels[i * PerSection + PerSection - 1];
            var col = go.GetComponent<BoxCollider>(); col.size = new Vector3(r.width * b.size.x * 0.9f, r.height * b.size.y * 0.9f, 0.2f);
            var tap = go.GetComponent<IceCreamTap>(); tap.station = this; tap.kind = IceCreamTap.Kind.Section; tap.index = i;
            sections[i] = sr;
        }
        // the painted box: from the first well's left margin to the same margin past the last well
        var rowLeft = rects[0].x; var rowRight = rects[count - 1].xMax + rects[0].x; var rowCentre = (rowLeft + rowRight) * 0.5f;
        // the waffle machine to the right of the box, level with the wells
        var wRect = new Rect(0f, 0f, 0f, 0f);
        if (waffleClosed != null)
        {
            var wh = waffleHeight; var ww = wh * waffleClosed.rect.width / waffleClosed.rect.height * b.size.y / b.size.x;
            wRect = new Rect(rowRight + waffleGap, rects[0].center.y - wh * 0.5f, ww, wh);
            var wf = new GameObject("Waffle", typeof(SpriteRenderer), typeof(BoxCollider), typeof(IceCreamTap)); wf.transform.SetParent(transform, false);
            wf.transform.localPosition = At(wRect.center) + Vector3.back * 0.02f;
            var k = wRect.height * b.size.y / waffleClosed.bounds.size.y; wf.transform.localScale = new Vector3(k, k, 1f);
            waffleRenderer = wf.GetComponent<SpriteRenderer>(); waffleRenderer.sortingLayerName = layer; waffleRenderer.sortingOrder = order + 2; waffleRenderer.sprite = waffleClosed;
            wf.GetComponent<BoxCollider>().size = new Vector3(waffleClosed.bounds.size.x, waffleClosed.bounds.size.y, 0.2f);
            var wt = wf.GetComponent<IceCreamTap>(); wt.station = this; wt.kind = IceCreamTap.Kind.Waffle;
        }
        // the dish row under the box
        var n = Dishes; var rowBottom = 0f;
        if (dishBack != null)
        {
            var dw = dishWidth * b.size.x; var ks = dw / dishBack.bounds.size.x; var dh = dishBack.bounds.size.y * ks;
            var top = b.min.y + (boxBottom - rowGap) * b.size.y; var cy = top - dh * 0.5f; rowBottom = (top - dh - b.min.y) / b.size.y;
            for (var i = 0; i < n; i++)
            {
                var d = new Dish(); d.scale = ks; d.order = order + 3;
                var go = new GameObject("Dish" + i, typeof(SpriteRenderer), typeof(BoxCollider), typeof(IceCreamTap)); go.transform.SetParent(transform, false);
                d.rest = new Vector3(b.min.x + (rowCentre + (i - (n - 1) * 0.5f) * dishWidth) * b.size.x, cy, -0.03f - 0.001f * i);
                go.transform.localPosition = d.rest; go.transform.localScale = new Vector3(ks, ks, 1f); d.root = go.transform;
                d.back = go.GetComponent<SpriteRenderer>(); d.back.sortingLayerName = layer; d.back.sortingOrder = d.order; d.back.sprite = dishBack;
                go.GetComponent<BoxCollider>().size = new Vector3(dishBack.bounds.size.x, dishBack.bounds.size.y, 0.2f);
                var tap = go.GetComponent<IceCreamTap>(); tap.station = this; tap.kind = IceCreamTap.Kind.Dish; tap.index = i;
                d.bowl = Child(go.transform, "Bowl", d.order + 1); d.bowl.sprite = bowlSprite;
                for (var s = 0; s < MaxScoops; s++) d.scoops[s] = Child(go.transform, "Scoop" + s, d.order + 2 + s);
                d.front = Child(go.transform, "Front", d.order + 6);
                var t = new GameObject("Trash", typeof(SpriteRenderer), typeof(BoxCollider), typeof(IceCreamTap)); t.transform.SetParent(go.transform, false);
                var tr = t.GetComponent<SpriteRenderer>(); tr.sortingLayerName = layer; tr.sortingOrder = d.order + 7; tr.sprite = trashSprite;
                var tw = trashSprite != null ? trashSprite.bounds.size.x : 0.5f; var th = trashSprite != null ? trashSprite.bounds.size.y : 0.6f;
                t.transform.localPosition = new Vector3(dishBack.bounds.extents.x + tw * 0.7f, dishBack.bounds.size.y * 0.1f, -0.01f);
                t.GetComponent<BoxCollider>().size = new Vector3(tw * 1.5f, th * 1.3f, 0.2f);
                var tt = t.GetComponent<IceCreamTap>(); tt.station = this; tt.kind = IceCreamTap.Kind.Trash; tt.index = i;
                t.SetActive(false); d.trash = t;
                dishes.Add(d); RefreshDish(d);
            }
        }
        // the drag collider covers the box, the dish row and the machine, so the group moves as one in the arrangement
        var drag = GetComponent<BoxCollider>();
        if (drag != null)
        {
            var xMin = Mathf.Min(0f, rowCentre - n * 0.5f * dishWidth); var xMax = Mathf.Max(small ? rowRight : 1f, wRect.xMax) + 0.02f;
            var yMin = Mathf.Min(0f, rowBottom - 0.02f); var yMax = Mathf.Max(0.85f, wRect.yMax + 0.02f);
            drag.size = new Vector3(b.size.x * (xMax - xMin), b.size.y * (yMax - yMin), drag.size.z);
            drag.center = new Vector3(b.min.x + (xMin + xMax) * 0.5f * b.size.x, b.min.y + (yMin + yMax) * 0.5f * b.size.y, drag.center.z);
        }
    }

    private SpriteRenderer Child(Transform parent, string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(SpriteRenderer)); go.transform.SetParent(parent, false); go.transform.localPosition = new Vector3(0f, 0f, -0.001f * (sortingOrder - order));
        var sr = go.GetComponent<SpriteRenderer>(); sr.sortingLayerName = layer; sr.sortingOrder = sortingOrder; sr.enabled = false; return sr;
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
            case IceCreamTap.Kind.Dish: if (selected == index) Serve(index); else Select(index); break;
            case IceCreamTap.Kind.Section: AddScoop(index); break;
            case IceCreamTap.Kind.Trash: if (selected >= 0) { EmptyDish(dishes[selected]); Deselect(); } break;
            case IceCreamTap.Kind.Waffle: BakeCones(); break;
            case IceCreamTap.Kind.Holder: { var i = FirstWithRoom(); if (i >= 0) Select(i); else Reject(); break; }   // the cone flow's taps, for the editor driver
            case IceCreamTap.Kind.Cone: if (selected >= 0) Serve(selected); else Reject(); break;
            case IceCreamTap.Kind.Cross: Deselect(); break;
        }
    }

    /// A tap anywhere but this station's dishes, wells and trash can puts the lifted dish down, with its contents
    private void Update()
    {
        if (selected < 0 || Time.timeScale == 0f || !Input.GetMouseButtonDown(0)) return;
        var cam = Camera.main; if (cam == null) return;
        if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out var hit, 100f))
        {
            var tap = hit.collider.GetComponent<IceCreamTap>();
            if (tap != null && tap.station == this && (tap.kind == IceCreamTap.Kind.Dish || tap.kind == IceCreamTap.Kind.Trash || tap.kind == IceCreamTap.Kind.Section)) return;
        }
        Deselect();
    }

    private int FirstWithRoom() { for (var i = 0; i < dishes.Count; i++) if (dishes[i].flavours.Count < MaxScoops) return i; return -1; }

    private void Select(int index)
    {
        if (index < 0 || index >= dishes.Count) return;
        if (selected >= 0) Deselect();
        selected = index; var d = dishes[index];
        d.root.localScale = new Vector3(d.scale * liftScale, d.scale * liftScale, 1f);
        d.root.localPosition = d.rest + Vector3.up * (dishBack != null ? dishBack.bounds.size.y * d.scale * liftRise : 0.3f) + Vector3.back * 0.05f;
        SetOrder(d, d.order + 20); d.trash.SetActive(true); StartCoroutine(Pop(d.root));
    }

    private void Deselect()
    {
        if (selected < 0) return;
        var d = dishes[selected]; selected = -1;
        d.root.localScale = new Vector3(d.scale, d.scale, 1f); d.root.localPosition = d.rest; SetOrder(d, d.order); d.trash.SetActive(false);
    }

    private void SetOrder(Dish d, int baseOrder)
    {
        d.back.sortingOrder = baseOrder; d.bowl.sortingOrder = baseOrder + 1;
        for (var s = 0; s < MaxScoops; s++) d.scoops[s].sortingOrder = baseOrder + 2 + s;
        d.front.sortingOrder = baseOrder + 6; var tr = d.trash.GetComponent<SpriteRenderer>(); if (tr != null) tr.sortingOrder = baseOrder + 7;
    }

    /// The waffle machine (owner, 25 and 26 October): a tap starts a session, 8 s closed, then the lid opens and up to two
    /// empty dishes get a waffle bowl; no empty dish or a running session refuses
    public bool IsBakingWaffles => waffleBake != null;
    private void BakeCones()
    {
        if (EmptyDishes == 0 || waffleBake != null) { Reject(); return; }
        var bake = Resources.Load<AudioClip>("Kitchen/waffle_bake"); if (bake != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(bake);
        waffleBake = StartCoroutine(WaffleBake());
    }

    private IEnumerator WaffleBake()
    {
        if (waffleRenderer != null && waffleClosed != null) waffleRenderer.sprite = waffleClosed;
        if (waffleRenderer != null) StartCoroutine(Pop(waffleRenderer.transform));
        var t = 0f;
        while (t < WaffleSeconds)
        {
            t += Time.deltaTime;
            if (waffleRenderer != null) waffleRenderer.color = Color.Lerp(Color.white, new Color(1f, 0.92f, 0.82f), 0.5f + 0.5f * Mathf.Sin(t * 5f));   // a warm pulse while it bakes
            yield return null;
        }
        if (waffleRenderer != null) waffleRenderer.color = Color.white;
        if (waffleRenderer != null && waffleOpen != null) waffleRenderer.sprite = waffleOpen;
        var given = 0;
        foreach (var d in dishes)
        {
            if (given >= WafflesPerSession) break;
            if (!d.Empty) continue;
            d.waffle = true; given++; RefreshDish(d);
            if (waffleRenderer != null) StartCoroutine(Fly(d.bowl.transform, waffleRenderer.transform.position)); else StartCoroutine(Pop(d.root));
        }
        yield return new WaitForSeconds(1.2f);
        if (waffleRenderer != null && waffleClosed != null) waffleRenderer.sprite = waffleClosed;
        waffleBake = null;
    }

    public bool TakeCone() => false;
    /// Every empty dish gets a bowl (the editor driver's refill)
    public void RefillCones() { foreach (var d in dishes) if (d.Empty) { d.waffle = true; RefreshDish(d); } }

    private void AddScoop(int flavour)
    {
        var target = selected >= 0 ? selected : FirstWithRoom();
        if (target < 0) { Reject(); return; }
        var d = dishes[target];
        if (d.flavours.Count >= MaxScoops) { Reject(); return; }
        if (!IsOpen(flavour) || left[flavour] <= 0) { Reject(); return; }
        left[flavour]--; d.flavours.Add(flavour);
        // the feel of a tap (owner, 25 October 2026: the slot is small, not enough feedback): the well pops and
        // flashes, and the scoop flies from the well into the dish instead of appearing there
        var sec = sections[flavour];
        if (sec != null) { StartCoroutine(Pop(sec.transform)); StartCoroutine(Flash(sec)); }
        RefreshDish(d);
        var r = d.scoops[d.flavours.Count - 1];
        if (r != null) { if (sec != null) StartCoroutine(Fly(r.transform, sec.transform.position)); else StartCoroutine(Pop(r.transform)); }
        if (scoopSound != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(scoopSound);
        RefreshSections();
    }

    private void EmptyDish(Dish d) { d.waffle = false; d.flavours.Clear(); RefreshDish(d); }

    /// The dish's pictures: the bowl when it has one, the scoops side by side, the right front wall over them
    private void RefreshDish(Dish d)
    {
        if (d.bowl != null) d.bowl.enabled = d.waffle;
        if (d.front != null) { d.front.sprite = d.waffle ? dishFrontBowl : dishFrontEmpty; d.front.enabled = d.front.sprite != null; }
        var n = d.flavours.Count; var dh = dishBack != null ? dishBack.bounds.size.y : 2f; var dmin = dishBack != null ? dishBack.bounds.min.y : -1f;
        for (var s = 0; s < MaxScoops; s++)
        {
            var r = d.scoops[s]; if (r == null) continue;
            r.enabled = s < n;
            if (s >= n) continue;
            r.sprite = smallScoops[d.flavours[s]];
            var sw = r.sprite != null ? r.sprite.bounds.size.x : 0.4f;
            r.transform.localPosition = new Vector3((s - (n - 1) * 0.5f) * sw * scoopSpread, dmin + scoopTop * dh - (s % 2 == 1 ? scoopZig * dh : 0f), -0.001f * (3 + s));
        }
    }

    /// The dish's contents as the order letters, sorted, then 'w' when it holds a waffle bowl
    public string Variant() => selected >= 0 ? Variant(dishes[selected]) : "";
    private static string Variant(Dish d) { var l = new List<char>(); foreach (var f in d.flavours) l.Add(Letters[f]); l.Sort(); var s = new string(l.ToArray()); return d.waffle ? s + WaffleLetter : s; }

    /// An order's ice cream: one to three scoops of open flavours, with a waffle bowl half the time
    public static string RandomVariant()
    {
        var n = Random.Range(1, MaxScoops + 1); var l = new List<char>();
        for (var i = 0; i < n; i++) l.Add(Letters[Random.Range(0, ActiveFlavours)]);   // orders ask only for open flavours
        l.Sort(); var s = new string(l.ToArray()); return Random.value < 0.5f ? s + WaffleLetter : s;
    }

    private void Serve(int index)
    {
        var d = dishes[index];
        if (d.Empty) { Deselect(); return; }                       // nothing in it: the tap just puts it down
        if (orderSystem == null) { Reject(); return; }
        if (orderSystem.TryServeItem("IceCream", Variant(d)))
        {
            if (serveSound != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(serveSound);
            EmptyDish(d); Deselect();
        }
        else Reject();
    }

    private void Reject()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayWrongItemSFX();
        if (shake != null) StopCoroutine(shake);
        shake = StartCoroutine(Shake(selected >= 0 ? dishes[selected].root : transform, selected >= 0 ? dishes[selected].root.localPosition : transform.localPosition));
    }

    /// Every section back to three scoops: the kitchen blender's job
    public void RefillAll() { for (var i = 0; i < Flavours; i++) left[i] = PerSection; RefreshSections(); }

    private void RefreshSections()
    {
        for (var i = 0; i < Flavours; i++)
        {
            if (sections[i] == null) continue;
            var open = IsOpen(i);
            var lv = Mathf.Clamp(left[i], 0, PerSection);
            sections[i].enabled = open && lv > 0;
            if (lv > 0) sections[i].sprite = (activeLevels ?? sectionLevels)[i * PerSection + lv - 1];
        }
    }

    private IEnumerator Pop(Transform t)
    {
        var s = t.localScale; var e = 0f;
        while (e < 0.16f) { e += Time.deltaTime; var k = Mathf.Sin(Mathf.Clamp01(e / 0.16f) * Mathf.PI); t.localScale = s * (1f + 0.25f * k); yield return null; }
        t.localScale = s;
    }

    /// A short white flash over a well: a copy of its picture, white, fading out in 0.2 s (owner, 25 October 2026)
    private IEnumerator Flash(SpriteRenderer over)
    {
        if (over == null || over.sprite == null) yield break;
        var go = new GameObject("Flash", typeof(SpriteRenderer)); go.transform.SetParent(over.transform, false);
        var sr = go.GetComponent<SpriteRenderer>(); sr.sprite = over.sprite; sr.sortingLayerName = over.sortingLayerName; sr.sortingOrder = over.sortingOrder + 1;
        var white = Shader.Find("GUI/Text Shader"); if (white != null) sr.material = new Material(white);   // draws the picture's shape in plain white
        var e = 0f;
        while (e < 0.2f && sr != null) { e += Time.deltaTime; sr.color = new Color(1f, 1f, 1f, 0.7f * (1f - e / 0.2f)); yield return null; }
        if (go != null) Destroy(go);
    }

    /// A part flies from a world point to its own place, in a small arc, 0.22 s (a scoop from its well, a bowl from the machine)
    private IEnumerator Fly(Transform t, Vector3 fromWorld)
    {
        var to = t.localPosition; var parent = t.parent;
        var from = parent != null ? parent.InverseTransformPoint(fromWorld) : fromWorld; from.z = to.z;
        var e = 0f; const float dur = 0.22f;
        while (e < dur) { e += Time.deltaTime; var k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / dur)); t.localPosition = Vector3.Lerp(from, to, k) + Vector3.up * (0.35f * Mathf.Sin(k * Mathf.PI)); yield return null; }
        t.localPosition = to;
        StartCoroutine(Pop(t));
    }

    private IEnumerator Shake(Transform t, Vector3 rest)
    {
        for (var i = 0; i < 3; i++)
        {
            t.localPosition = rest + Vector3.right * 0.08f; yield return new WaitForSeconds(0.06f);
            t.localPosition = rest + Vector3.left * 0.08f; yield return new WaitForSeconds(0.06f);
        }
        t.localPosition = rest; shake = null;
    }

    private void OnDisable()
    {
        Deselect();
        if (waffleBake != null) { StopCoroutine(waffleBake); waffleBake = null; if (waffleRenderer != null) { waffleRenderer.color = Color.white; if (waffleClosed != null) waffleRenderer.sprite = waffleClosed; } }
    }
}

/// A tap target on the station: a well, a dish, its trash can, the waffle machine (Holder, Cone and Cross are the cone flow's, kept for the editor driver)
public class IceCreamTap : MonoBehaviour, IPointerClickHandler
{
    public enum Kind { Section, Holder, Cone, Cross, Waffle, Dish, Trash }
    public IceCreamStation station; public Kind kind; public int index;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.pointerCurrentRaycast.gameObject != gameObject) return;
        if (station != null) station.OnTap(kind, index);
    }
}
