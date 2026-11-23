using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// The kitchen's movable things (owner, 16 November 2026: "I want to make kitchen arrangeable just like game scene
/// table"; "don't redraw ... they need to be fully arrangeable"). Seven pieces: blender, cutting board, baskets, cake,
/// coffee bags, juice, whisk. The ovens stay ("oven doesn't need to be moved").
///
/// The owner's rules:
///  - The TABLE (the counter's top) is free: a piece may stand anywhere with its foot on it. Each piece has a box
///    that may not overlap another piece's box; outside the boxes pieces may cover each other. Blender and board:
///    the whole piece. Cake: the stand only, the mat may be covered. The others: the middle.
///  - The CABINETS (the shelf with the bags, the three openings under the counter) snap: a piece dropped in one jumps
///    to a fixed place on its floor. A piece too big for the cabinet shows red.
///  - Red as on the game table. "When kitchen is red, don't allow level to start": AnyRed keeps the table's Ready
///    button off (GamePhaseManager), and the table's kitchen handle pulses red to say where the trouble is.
///  - "We arrange kitchen same time as the game scene arrange": the pieces wiggle and can be dragged only while the
///    table is being arranged (never in the tutorial, never during the day; then their taps work as always).
///
/// Every place is the piece's FOOT (the bottom middle of its box) as a share of the kitchen picture, so a place means
/// the same on every screen. The day's arrangement is kept in `working` (it outlives the kitchen scene, which is
/// unloaded on every close) and goes into the save when the day starts.
public static class KitchenArrange
{
    private class Def
    {
        public string name; public Vector2 foot;      // the game's own place
        public Vector4 box, fit;                      // no-overlap box and visible bounds: x0, y0 (from the top), x1, y1 as shares of the picture
        public Vector2 stretch;                       // the blender is sized as a share of the kitchen picture; zero: the scene's fixed size
        public Vector2 size;                          // fixed size for a piece this file makes (the whisk)
        public float scale;                           // a scene-sized piece drawn smaller or larger than the scene has it (0 = as is); its boxes are shares, so they follow
        public bool flat;                             // lies flat on the counter: its whole box must be on the counter's top, not only its lower edge
        public float BoxMidX => (box.x + box.z) * 0.5f;
    }

    // Places measured on the 1080 x 1920 kitchen picture (Store/redesign/kitchen_movable/boxes_map.py), then set so no
    // two boxes touch on a phone (978 x 2120 canvas units) or a tablet (1247 x 1663).
    // Owner, 16 November 2026, after test build 36 (which had the blender moved up and the board under it): "since we
    // made kitchen arrangable, no need to move the location of the items on table, move them back to default
    // positions. for the whisk, the new location is good." So: blender, board, baskets and cake where the scene had
    // them, the whisk at the left edge. The scene's own places let the board's box lie on the blender's and on the
    // baskets' (they were drawn overlapping), which would start every kitchen red: the board stands about 30 px
    // further right and lower than it did, the baskets about 35 px further right; nothing else differs.
    private static readonly Def[] Defs =
    {
        new Def { name = "Blender",  foot = new Vector2(0.145f, 0.5035f), box = new Vector4(0.09f, 0.06f, 0.98f, 0.98f),   fit = new Vector4(0.09f, 0.06f, 0.98f, 0.98f), stretch = new Vector2(0.2157f * 0.88f, 0.1485f * 0.88f) },   // 0.88: "resize blender to be slightly smaller" (owner, 21 November 2026); its glow and parts are shares of it
        new Def { name = "cutboard", foot = new Vector2(0.385f, 0.428f),  box = new Vector4(0.02f, 0.01f, 0.98f, 0.98f),   fit = new Vector4(0.02f, 0.01f, 0.98f, 0.98f) },
        new Def { name = "whisk",    foot = new Vector2(0.0505f, 0.437f), box = new Vector4(0.20f, 0.20f, 0.80f, 0.95f),   fit = new Vector4(0.04f, 0.03f, 0.96f, 0.95f), size = new Vector2(98f, 128f) },
        new Def { name = "basket",   foot = new Vector2(0.56f, 0.5177f),  box = new Vector4(0.20f, 0.25f, 0.80f, 0.95f),   fit = new Vector4(0.01f, 0.03f, 0.98f, 0.95f), scale = 0.88f },   // "resize the basket to be slightly smaller" (owner, 21 November 2026)
        new Def { name = "cake",     foot = new Vector2(0.8078f, 0.451f), box = new Vector4(0.21f, 0.03f, 0.875f, 0.775f), fit = new Vector4(0.01f, 0.03f, 0.99f, 0.98f) },
        new Def { name = "coffee",   foot = new Vector2(0.2694f, 0.6859f),box = new Vector4(0.20f, 0.25f, 0.80f, 0.90f),   fit = new Vector4(0.07f, 0.10f, 0.92f, 0.90f) },
        // the choux plate (20 November 2026, KitchenFlow), made by this file: on the table at a free place (FirstCheck); this foot, the opening under the counter, is the fallback
        new Def { name = "chouxplate", foot = new Vector2(0.2861f, 0.2906f), box = new Vector4(0.02f, 0.02f, 0.97f, 0.95f), fit = new Vector4(0.02f, 0.02f, 0.98f, 0.98f), size = new Vector2(205f, 149f), flat = true },   // the baking tray, squarer, choux 2 x 2, with the fruit board's hard shadow (464 x 338; owner, 20-21 November 2026). It lies flat: its whole body is its footprint and must be on the counter's top (owner, 21 November: "the footage of tray is its whole body, i see the top of tray is out side of table top")
        // the mixer set on its stand (20 November 2026), made by this file when choux or cake is on the menu; it takes the whisk's place then.
        // Its box is its foot. On the table at a free place (FirstCheck); this foot, the opening under the counter's right place, is the fallback
        new Def { name = "mixer", foot = new Vector2(0.40f, 0.2906f), box = new Vector4(0.02f, 0.72f, 0.98f, 0.99f), fit = new Vector4(0.02f, 0.02f, 0.98f, 0.99f), size = new Vector2(123f * 1.22f, 173f * 1.22f) },   // 1.22 (was 1.12): "make the mixer set slightly larger" (owner, 23 November 2026)   // x1.12: "make the mixing set slightly larger" (owner, 21 November 2026, night)   // 21 November 2026 evening: the hand mixer (owner's body, the kitchen's whisk) resting in its empty bowl, running in the cream, the bowl with the basket's kind of shadow (353 x 499, compose_lean.py); bowl rim 106 units as before
        new Def { name = "juice",    foot = new Vector2(0.1662f, 0.1469f),box = new Vector4(0.15f, 0.30f, 0.85f, 0.98f),   fit = new Vector4(0.13f, 0.07f, 0.87f, 0.98f) },
    };

    /// Raised when the game's own places change: places kept under an older number are dropped once (2: 16 November 2026, the places above).
    public const int LayoutVersion = 2;

    private const float TableLow = 0.425f, TableHigh = 0.55f;   // the counter's top, as shares of the picture's height

    private class Cabinet { public float x0, x1, top, floor; public int places; }
    private static readonly Cabinet[] Cabinets =
    {
        new Cabinet { x0 = 0.1093f, x1 = 0.4352f, top = 0.7917f, floor = 0.6859f, places = 2 },   // the shelf with the coffee bags
        new Cabinet { x0 = 0.0583f, x1 = 0.5139f, top = 0.4031f, floor = 0.2906f, places = 2 },   // the big opening under the counter
        new Cabinet { x0 = 0.0620f, x1 = 0.2685f, top = 0.2661f, floor = 0.1469f, places = 1 },   // small, left (the juice's)
        new Cabinet { x0 = 0.3046f, x1 = 0.5130f, top = 0.2661f, floor = 0.1469f, places = 1 },   // small, right
    };

    private static readonly Color Red = new Color(1f, 0.5f, 0.5f, 1f);   // DraggableFood's overlap colour

    /// True while a piece in the kitchen is red; the day cannot start then.
    public static bool AnyRed { get; private set; }

    private static Dictionary<string, Vector2> working;   // this day's arrangement; null until the kitchen is first opened
    private static bool fresh;                            // `working` came straight from the save and has not been checked on this screen yet

    private class Piece { public Def def; public RectTransform rt; public Image image; public bool red; }
    private static readonly List<Piece> pieces = new List<Piece>();
    private static RectTransform area;                    // "Arrange": the pieces' common parent, the size of the kitchen picture
    private static Piece dragging;

    /// A new day's load: yesterday's reds are gone with yesterday's unsaved moves.
    public static void BeginDay()
    {
        working = null; AnyRed = false;
        KitchenFlow.Reset();   // a new day: every plate empty
        var button = GameObject.Find("KitchenButton");
        if (button != null && button.GetComponent<KitchenRedHint>() == null) button.AddComponent<KitchenRedHint>();
    }

    /// The day starts: the arrangement goes into the save.
    public static void Commit()
    {
        if (working == null || AnyRed || SessionManager.Instance == null) return;
        SessionManager.Instance.SetKitchenPlaces(working);
    }

    public static bool Arranging()
    {
        if (Object.FindObjectOfType<TutorialDirector>() != null) return false;
        var phases = Object.FindObjectOfType<GamePhaseManager>();
        return phases != null && phases.GetCurrentPhase() == GamePhase.ARRANGEMENT && !phases.SkipsArrangement;
    }

    /// Once per kitchen load (called beside KitchenBlender.Apply, before the first picture): the pieces under one
    /// parent, each at its place.
    public static void Apply()
    {
        Transform bg = null;
        foreach (var canvas in Object.FindObjectsOfType<Canvas>())
            if (canvas.gameObject.scene.name == "KitchenScene") { bg = canvas.transform.Find("bg"); if (bg != null) break; }
        if (bg == null || bg.Find("Arrange") != null) return;

        var go = new GameObject("Arrange", typeof(RectTransform), typeof(KitchenArrangeRunner));
        go.layer = bg.gameObject.layer;
        area = (RectTransform)go.transform; area.SetParent(bg, false); area.SetAsLastSibling();
        area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one; area.offsetMin = Vector2.zero; area.offsetMax = Vector2.zero;

        if (working == null)
        {
            working = new Dictionary<string, Vector2>();
            var kept = SessionManager.Instance != null ? SessionManager.Instance.GetKitchenPlaces() : null;
            foreach (var d in Defs) working[d.name] = kept != null && kept.TryGetValue(d.name, out var p) ? p : d.foot;
            needSpot.Clear();   // never placed by the player: they start on the table
            foreach (var n in new[] { "chouxplate", "mixer" }) if (kept == null || !kept.ContainsKey(n)) needSpot.Add(n);   // the flat tray first: it needs the most room on the counter
            fresh = true;
        }

        pieces.Clear(); dragging = null;
        foreach (var d in Defs)
        {
            RectTransform rt = null;
            if (d.name == "chouxplate")
            {
                var sprite = Resources.Load<Sprite>("Kitchen/flow/choux_empty"); var sm = SessionManager.Instance;
                if (sprite == null || sm == null || !sm.IsFoodItemActive("Choux")) continue;   // only with choux on the menu
                var w = new GameObject("chouxplate", typeof(RectTransform), typeof(Image)); w.layer = go.layer;
                rt = (RectTransform)w.transform; rt.SetParent(area, false); rt.sizeDelta = d.size;
                var wi = w.GetComponent<Image>(); wi.sprite = sprite; wi.raycastTarget = false;
            }
            else if (d.name == "mixer")
            {
                var sprite = Resources.Load<Sprite>("Kitchen/flow/mixer_rest"); if (sprite == null || !DessertOnMenu()) continue;
                var w = new GameObject("mixer", typeof(RectTransform), typeof(Image)); w.layer = go.layer;
                rt = (RectTransform)w.transform; rt.SetParent(area, false); rt.sizeDelta = d.size;
                var wi = w.GetComponent<Image>(); wi.sprite = sprite; wi.raycastTarget = false; wi.preserveAspect = true;
            }
            else if (d.name == "whisk")
            {
                var sprite = Resources.Load<Sprite>("Kitchen/whisk"); if (sprite == null) continue;
                if (DessertOnMenu() && Resources.Load<Sprite>("Kitchen/flow/mixer_rest") != null) continue;   // the mixer holds the whisk now
                var w = new GameObject("whisk", typeof(RectTransform), typeof(Image)); w.layer = go.layer;
                rt = (RectTransform)w.transform; rt.SetParent(area, false); rt.sizeDelta = d.size;
                var wi = w.GetComponent<Image>(); wi.sprite = sprite; wi.raycastTarget = false;
            }
            else
            {
                foreach (var t in bg.GetComponentsInChildren<Transform>(true))
                    if (t.name == d.name && t is RectTransform found && t != area) { rt = found; break; }
                if (rt == null) continue;
                var size = rt.sizeDelta;                 // the scene's size (kept); the blender's comes from its share of the picture
                rt.SetParent(area, false);
                if (d.stretch == Vector2.zero) rt.sizeDelta = size * (d.scale > 0f ? d.scale : 1f);
            }
            rt.pivot = new Vector2(0.5f, 0.5f); rt.localRotation = Quaternion.identity; rt.localScale = Vector3.one;
            var piece = new Piece { def = d, rt = rt, image = rt.GetComponent<Image>() };
            pieces.Add(piece); Place(piece, working[d.name]);
        }
        Sort();
        KitchenFlow.Attach(area, Arranging());   // the plates' pictures, the mixer and the board (20 November 2026)
        if (Arranging())
        {
            foreach (var p in pieces) KitchenGrab.Attach(p.rt, p.def.name);
            TagNew(bg);
        }
    }

    /// "NEW" on what was bought since the last day (owner, 18 November 2026): the food's own thing in the kitchen
    /// (juice, apples, cake, the choux board ...), the blender with the ice cream, a newly bought oven.
    private static void TagNew(Transform bg)
    {
        var sm = SessionManager.Instance; if (sm == null || !sm.HasActiveSession()) return;
        foreach (var gate in bg.GetComponentsInChildren<KitchenFoodGate>(true))
        {
            var food = gate.associatedFoodType != null ? gate.associatedFoodType.Trim() : "";
            if (gate.keepVisible || food == "" || !sm.IsNewToArrange("Food:" + food) || !sm.IsFoodItemActive(food)) continue;
            NewTag.OnUI(gate.transform as RectTransform); GlowFx.New(gate.GetComponent<Image>());
        }
        if (sm.IsNewToArrange("Food:IceCream") || sm.IsNewToArrange("Upgrade:IceCream")) { var blender = Find("Blender"); if (blender != null && blender.rt != null && blender.rt.gameObject.activeInHierarchy) { NewTag.OnUI(blender.rt); GlowFx.New(blender.image); } }
        if (sm.IsNewToArrange("Upgrade:Oven"))
            foreach (var t in bg.GetComponentsInChildren<Transform>(true))
                if (t.name == "spot" + sm.OvensOwned && t.childCount > 0) { NewTag.OnUI(t.GetChild(0) as RectTransform); GlowFx.New(t.GetChild(0).GetComponent<Image>()); break; }
    }

    private static void Place(Piece p, Vector2 foot)
    {
        var d = p.def; var rt = p.rt;
        if (d.stretch != Vector2.zero)
        {
            rt.anchorMin = new Vector2(foot.x - d.BoxMidX * d.stretch.x, foot.y - (1f - d.box.w) * d.stretch.y);
            rt.anchorMax = new Vector2(foot.x + (1f - d.BoxMidX) * d.stretch.x, foot.y + d.box.w * d.stretch.y);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
        else
        {
            var size = rt.sizeDelta;
            rt.anchorMin = rt.anchorMax = foot;
            rt.anchoredPosition = new Vector2(-(d.BoxMidX - 0.5f) * size.x, (d.box.w - 0.5f) * size.y);
        }
    }

    // ---- geometry, in the picture's own units with (0, 0) at its bottom left ----

    private static Vector2 AreaSize => area != null ? area.rect.size : new Vector2(1080f, 1920f);

    private static Vector2 SizeOf(Piece p)
    {
        var a = AreaSize;
        return p.def.stretch != Vector2.zero ? new Vector2(p.def.stretch.x * a.x, p.def.stretch.y * a.y) : p.rt.sizeDelta;
    }

    /// A share-box of the piece (x0, y0 from the top, x1, y1) with the piece's foot at `foot`.
    private static Rect Part(Piece p, Vector2 foot, Vector4 part)
    {
        var a = AreaSize; var s = SizeOf(p); var d = p.def;
        var left = foot.x * a.x - d.BoxMidX * s.x; var bottom = foot.y * a.y - (1f - d.box.w) * s.y;
        return Rect.MinMaxRect(left + part.x * s.x, bottom + (1f - part.w) * s.y, left + part.z * s.x, bottom + (1f - part.y) * s.y);
    }

    private static Cabinet CabinetAt(Piece p, Vector2 foot)
    {
        var a = AreaSize; var mid = Part(p, foot, p.def.fit).center;
        foreach (var c in Cabinets)
            if (mid.x >= c.x0 * a.x && mid.x <= c.x1 * a.x && mid.y >= c.floor * a.y - 6f && mid.y <= c.top * a.y) return c;
        return null;
    }

    private static bool OnTable(Piece p, Vector2 foot)
    {
        if (foot.y < TableLow - 0.004f || foot.y > TableHigh + 0.004f) return false;
        var b = Part(p, foot, p.def.box);
        if (p.def.flat && b.yMax > TableHigh * AreaSize.y + 2f) return false;   // a flat thing's far edge stays on the counter too
        return b.xMin >= -2f && b.xMax <= AreaSize.x + 2f;
    }

    /// Where a piece dropped into this cabinet stands: on the floor, at the nearest of the cabinet's places; a piece
    /// wider than half the cabinet has the middle only.
    private static Vector2 Snap(Piece p, Cabinet c, Vector2 foot)
    {
        var a = AreaSize; var fit = Part(p, foot, p.def.fit); var wide = (c.x1 - c.x0) * a.x;
        var mid = (c.x0 + c.x1) * 0.5f * a.x; float x;
        if (c.places < 2 || fit.width > wide * 0.5f) x = mid;
        else { var l = (c.x0 + (c.x1 - c.x0) * 0.25f) * a.x; var r = (c.x0 + (c.x1 - c.x0) * 0.75f) * a.x; x = Mathf.Abs(fit.center.x - l) <= Mathf.Abs(fit.center.x - r) ? l : r; }
        return new Vector2((foot.x * a.x + (x - fit.center.x)) / a.x, c.floor);   // the visible middle onto the place
    }

    private static bool TooBig(Piece p, Cabinet c)
    {
        var a = AreaSize; var fit = Part(p, working[p.def.name], p.def.fit);
        return fit.width > (c.x1 - c.x0) * a.x || fit.height > (c.top - c.floor) * a.y;
    }

    /// Marks every piece that is too big for its cabinet, shares a cabinet place, or whose box lies on another's.
    private static void Check()
    {
        var live = new List<Piece>(); var cab = new Dictionary<Piece, Cabinet>();
        foreach (var p in pieces)
        {
            p.red = false;
            if (p.rt == null || !p.rt.gameObject.activeInHierarchy) continue;
            live.Add(p); cab[p] = CabinetAt(p, working[p.def.name]);
            if (cab[p] != null && TooBig(p, cab[p])) p.red = true;
        }
        for (var i = 0; i < live.Count; i++)
            for (var j = i + 1; j < live.Count; j++)
            {
                var a = live[i]; var b = live[j];
                if (cab[a] != cab[b]) continue;                                   // a cabinet and the table, or two cabinets, never meet
                var part = cab[a] != null;                                        // in a cabinet the whole visible piece counts
                var ra = Part(a, working[a.def.name], part ? a.def.fit : a.def.box);
                var rb = Part(b, working[b.def.name], part ? b.def.fit : b.def.box);
                if (ra.Overlaps(rb)) { a.red = true; b.red = true; }
            }
        var any = false;
        foreach (var p in pieces) { any |= p.red; if (p.image != null) p.image.color = p.red ? Red : Color.white; }
        if (any != AnyRed)
        {
            AnyRed = any;
            var phases = Object.FindObjectOfType<GamePhaseManager>(); if (phases != null) phases.OnItemOverlapChanged();
        }
    }

    /// The table's pieces are drawn back to front, as they stand; a dragged piece is over everything.
    private static void Sort()
    {
        var order = new List<Piece>(pieces);
        order.RemoveAll(p => p.rt == null);
        order.Sort((x, y) => working[y.def.name].y.CompareTo(working[x.def.name].y));
        for (var i = 0; i < order.Count; i++) order[i].rt.SetSiblingIndex(i);
        if (dragging != null && dragging.rt != null) dragging.rt.SetAsLastSibling();
    }

    /// One frame after the load, when the canvas has its size: the first check. An arrangement from the save that is
    /// red before the player touched anything (a newly bought piece stands where another was moved to) goes back to
    /// the game's own places, which never are.
    private static readonly List<string> needSpot = new List<string>();
    private static readonly float[] MiddleFirst = { 0.5f, 0.4f, 0.6f, 0.3f, 0.7f, 0.2f, 0.8f, 0.25f, 0.35f, 0.45f, 0.55f, 0.65f, 0.75f, 0.15f, 0.85f };
    private static readonly float[] LeftFirst = { 0.11f, 0.14f, 0.18f, 0.22f, 0.26f, 0.3f, 0.35f, 0.4f, 0.45f, 0.5f, 0.55f, 0.6f, 0.7f, 0.8f };   // the mixer: where the whisk lay, at the table's left end
    // a flat piece needs its whole body on the counter, so the few places that fit are narrow: every hundredth, the middle first
    private static readonly float[] FineMiddleFirst = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.OrderBy(System.Linq.Enumerable.Select(System.Linq.Enumerable.Range(8, 85), i => i / 100f), x => Mathf.Abs(x - 0.5f)));
    private static bool DessertOnMenu() { var sm = SessionManager.Instance; return sm != null && (sm.IsFoodItemActive("Choux") || sm.IsFoodItemActive("Cake")); }

    /// A free place on the kitchen table for a piece: its box on the table's top and on no other table piece's box,
    /// the front of the table first, the middle before the sides.
    private static bool FreeTableSpot(Piece p, out Vector2 foot)
    {
        foot = p.def.foot; if (AreaSize.x <= 0f) return false;
        // the mixer keeps to the counter's left end whatever the depth (it was its whisk's place); the rest go front first
        var spots = new List<Vector2>();
        if (p.def.name == "mixer") { foreach (var x in LeftFirst) for (var y = TableLow + 0.004f; y <= TableHigh; y += 0.01f) spots.Add(new Vector2(x, y)); }
        else for (var y = TableLow + 0.004f; y <= TableHigh; y += 0.01f) foreach (var x in p.def.flat ? FineMiddleFirst : MiddleFirst) spots.Add(new Vector2(x, y));
        foreach (var spot in spots)
            {
                var x = spot.x; var y = spot.y;
                var f = new Vector2(x, y); if (!OnTable(p, f)) continue;
                var r = Part(p, f, p.def.box); var free = true;
                foreach (var o in pieces)
                {
                    if (o == p || o.rt == null || !o.rt.gameObject.activeInHierarchy || CabinetAt(o, working[o.def.name]) != null) continue;
                    if (r.Overlaps(Part(o, working[o.def.name], o.def.box))) { free = false; break; }
                }
                if (free) { foot = f; return true; }
            }
        return false;
    }

    public static void FirstCheck()
    {
        // the choux plate, new in the kitchen: on the kitchen table (owner, 20 November 2026: "The default location ...
        // for choux plate is on the kitchen table"), at a free place; only when the table is full it keeps the
        // opening under the counter
        foreach (var n in needSpot)
        {
            var piece = Find(n);
            if (piece != null && piece.rt != null && piece.rt.gameObject.activeInHierarchy && FreeTableSpot(piece, out var spot))
            { piece.def.foot = spot; working[n] = spot; Place(piece, spot); Sort(); }
        }
        needSpot.Clear();
        Check();
        if (AnyRed && fresh)
        {
            foreach (var p in pieces) { working[p.def.name] = p.def.foot; Place(p, p.def.foot); }
            Sort(); Check();
        }
        fresh = false;
    }

    public static void Wiggle()
    {
        foreach (var p in pieces)
        {
            if (p.rt == null || p == dragging) continue;
            if (p.rt.GetComponentInChildren<KitchenGrab>() == null) continue;   // not arranging: the pieces' own animations own their turn (the blender shakes)
            p.rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin((Time.unscaledTime + p.def.foot.x) * 20f));   // the food's wiggle: one degree, 20 a second
        }
    }

    // ---- dragging ----

    private static Piece Find(string name) { foreach (var p in pieces) if (p.def.name == name) return p; return null; }
    private static Vector2 grabOffset, pickedFrom;

    private static bool PointerShare(PointerEventData e, out Vector2 share)
    {
        share = Vector2.zero; if (area == null) return false;
        var canvas = area.GetComponentInParent<Canvas>(); var cam = canvas != null ? canvas.rootCanvas.worldCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, cam, out var local)) return false;
        var r = area.rect; share = new Vector2((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);
        return true;
    }

    public static void BeginDrag(string name, PointerEventData e)
    {
        var p = Find(name); if (p == null || !PointerShare(e, out var at)) return;
        dragging = p; pickedFrom = working[name]; grabOffset = pickedFrom - at;
        p.rt.localEulerAngles = Vector3.zero; p.rt.localScale = Vector3.one * 1.05f; p.rt.SetAsLastSibling();
    }

    public static void Drag(string name, PointerEventData e)
    {
        var p = dragging; if (p == null || p.def.name != name || !PointerShare(e, out var at)) return;
        var foot = at + grabOffset;
        foot.x = Mathf.Clamp01(foot.x); foot.y = Mathf.Clamp(foot.y, 0.02f, 0.95f);
        working[name] = foot; Place(p, foot);
        var c = CabinetAt(p, foot);
        if (c == null && !OnTable(p, foot)) { if (p.image != null) p.image.color = Red; return; }   // nowhere it may stay
        var shown = foot; if (c != null) working[name] = Snap(p, c, foot);                           // judged where it would land
        Check(); working[name] = shown;
    }

    public static void EndDrag(string name)
    {
        var p = dragging; if (p == null || p.def.name != name) return;
        if (p.rt == null || area == null) { dragging = null; return; }   // the kitchen was closed under the finger
        var foot = working[name]; var c = CabinetAt(p, foot);
        if (c != null) foot = Snap(p, c, foot);
        else if (!OnTable(p, foot)) foot = pickedFrom;                                               // neither table nor cabinet: back where it was
        working[name] = foot; Place(p, foot);
        p.rt.localScale = Vector3.one; dragging = null;
        Sort(); Check();
    }
}

/// Lives in the kitchen scene with the pieces: the first check and the wiggle.
public class KitchenArrangeRunner : MonoBehaviour
{
    // While the kitchen is being arranged, the table's own arranging things are drawn over the kitchen. The small
    // menu is put away until the kitchen closes. The Ready button stays (owner, 18 November 2026: a Ready in the
    // kitchen too), moved down onto the rug so nothing lies on the counter; the hint shows only in the tutorial.
    private Canvas menu;
    private GamePhaseManager phases;

    private IEnumerator Start()
    {
        if (KitchenArrange.Arranging())
        {
            phases = FindObjectOfType<GamePhaseManager>();
            if (phases != null) phases.KitchenShown(true);
            var book = GameObject.Find("MenuBookCanvas"); var c = book != null ? book.GetComponent<Canvas>() : null;
            if (c != null && c.enabled) { c.enabled = false; menu = c; }
        }
        yield return null;
        KitchenArrange.FirstCheck();
    }

    private void OnDestroy()
    {
        if (phases != null) phases.KitchenShown(false);
        if (menu != null) menu.enabled = true;
    }

    private void Update() { KitchenArrange.Wiggle(); }
}

/// A see-through sheet over a piece while the kitchen is being arranged: it takes the drag, and it takes the tap, so
/// a piece does not do its day-time job (refilling beans, blending) while it is being moved.
public class KitchenGrab : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private string piece;

    public static void Attach(RectTransform owner, string name)
    {
        var go = new GameObject("Grab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(KitchenGrab));
        go.layer = owner.gameObject.layer;
        var rt = (RectTransform)go.transform; rt.SetParent(owner, false); rt.SetAsLastSibling();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
        go.GetComponent<KitchenGrab>().piece = name;
    }

    public void OnPointerClick(PointerEventData e) { }
    public void OnBeginDrag(PointerEventData e) { KitchenArrange.BeginDrag(piece, e); }
    public void OnDrag(PointerEventData e) { KitchenArrange.Drag(piece, e); }
    public void OnEndDrag(PointerEventData e) { KitchenArrange.EndDrag(piece); }
    private void OnDisable() { KitchenArrange.EndDrag(piece); }
}

/// On the table's kitchen handle: while the table is being arranged and something in the kitchen is red, the handle
/// pulses red, so the player sees why Ready is off and where to go.
public class KitchenRedHint : MonoBehaviour
{
    private Image image; private bool shown; private GamePhaseManager phases;

    private void Update()
    {
        if (image == null) image = GetComponent<Image>();
        if (image == null) return;
        if (phases == null) phases = FindObjectOfType<GamePhaseManager>();
        var on = KitchenArrange.AnyRed && phases != null && phases.GetCurrentPhase() == GamePhase.ARRANGEMENT;
        if (on) { var k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f); image.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.35f, 1f), k); shown = true; }
        else if (shown) { image.color = Color.white; shown = false; }
    }
}
