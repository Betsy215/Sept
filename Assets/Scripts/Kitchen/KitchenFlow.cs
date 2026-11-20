using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// The kitchen flow for choux and cake (owner, 20 November 2026; placeholder pictures, "build the flow first"):
///   oven  ->  the plain bake shows on its plate in the kitchen (four choux on the choux plate, a sponge on the cake stand)
///         ->  the mixer glows; a tap, it runs, the bake gets its cream
///         ->  the fruit board glows; a tap, it cuts, the dessert is done
///         ->  the done picture shows for a moment, flies to the table, the plate is empty again and the table is refilled.
/// One dessert at a time at the mixer and at the board, the one that has waited longest first. The oven takes no new
/// bake of a dessert whose plate is still busy. The kitchen scene is unloaded when it closes, so the state lives here
/// and the timed steps run on the session's object; the pictures are refreshed whenever the kitchen is there.
/// The whisk stands in for the mixer. Pictures: Resources/Kitchen/flow (Store/redesign/kitchen_flow_2026-11-20/placeholders.py).
public static class KitchenFlow
{
    public enum Stage { Empty, Baked, Creamed, Done }
    public static float MixSeconds = 2.5f, CutSeconds = 2f, ShowSeconds = 0.9f, FlySeconds = 0.55f;   // the first two shorter with the kitchen upgrade
    public static readonly string[] Foods = { "Choux", "Cake" };

    private class Line { public Stage stage; public float since; public bool working; }
    private static readonly Dictionary<string, Line> lines = new Dictionary<string, Line>();
    private static string mixing, cutting;   // the dessert at the mixer, at the board
    private static int generation;           // a new day drops every running step

    private static Line Of(string food) { if (!lines.TryGetValue(food, out var l)) lines[food] = l = new Line(); return l; }
    public static bool Handles(string food) => food == "Choux" || food == "Cake";
    public static Stage StageOf(string food) => Handles(food) ? Of(food).stage : Stage.Empty;
    /// The dessert's plate is not free: no new bake of it.
    public static bool Busy(string food) => Handles(food) && Of(food).stage != Stage.Empty;
    public static bool Mixing => mixing != null;
    public static bool Cutting => cutting != null;
    public static bool MixerReady => mixing == null && Waiting(Stage.Baked) != null;
    public static bool BoardReady => cutting == null && Waiting(Stage.Creamed) != null;

    private static string Waiting(Stage s)
    {
        string best = null;
        foreach (var f in Foods) { var l = Of(f); if (l.stage != s || l.working) continue; if (best == null || l.since < Of(best).since) best = f; }
        return best;
    }

    /// A new day (or a new game): every plate empty.
    public static void Reset() { lines.Clear(); mixing = cutting = null; generation++; Refresh(); }

    /// The oven is done: the plain bake is on its plate.
    public static void Baked(string food)
    {
        if (!Handles(food)) return;
        var l = Of(food); l.stage = Stage.Baked; l.since = KitchenClock.Now; l.working = false;
        Refresh();
    }

    public static void TapMixer()
    {
        if (!MixerReady || Host == null) return;
        var f = Waiting(Stage.Baked); mixing = f; Of(f).working = true; Refresh();
        Host.StartCoroutine(Step(f, MixSeconds, Stage.Creamed, true));
    }

    public static void TapBoard()
    {
        if (!BoardReady || Host == null) return;
        var f = Waiting(Stage.Creamed); cutting = f; Of(f).working = true; Refresh();
        Host.StartCoroutine(Step(f, CutSeconds, Stage.Done, false));
    }

    private static MonoBehaviour Host => SessionManager.Instance;

    private static IEnumerator Step(string food, float seconds, Stage next, bool atMixer)
    {
        var gen = generation;
        yield return KitchenClock.Wait(seconds * KitchenSpeed.Factor);
        if (gen != generation) yield break;
        var l = Of(food); l.stage = next; l.since = KitchenClock.Now; l.working = false;
        if (atMixer) mixing = null; else cutting = null;
        Refresh();
        if (next != Stage.Done) yield break;
        yield return KitchenClock.Wait(ShowSeconds);                       // the done picture, for a moment
        if (gen != generation) yield break;
        yield return Fly(food);
        if (gen != generation) yield break;
        Deliver(food);
    }

    /// The table gets its dessert; the plate is free.
    private static void Deliver(string food)
    {
        Of(food).stage = Stage.Empty; Of(food).working = false;
        if (food == "Choux") { var f = Object.FindObjectOfType<Choux>(); if (f != null) f.RefillToFull(); }
        else { var f = Object.FindObjectOfType<Cake>(); if (f != null) f.RefillToFull(); }
        // the journal: the first strawberry cake, the first choux (owner, 19 November 2026), now when it is really done
        if (SessionManager.Instance != null) SessionManager.Instance.EarnJournal(food == "Cake" ? SessionManager.JournalCake : SessionManager.JournalChoux);
        Refresh();
    }

    // ---- the kitchen's pictures (only while the kitchen scene is loaded) ----

    private static Image chouxPlate, cakeStand, mixer, board;
    private static Sprite cakeDone;
    private static RectTransform flyTo;

    private static Sprite Pic(string name) => Resources.Load<Sprite>("Kitchen/flow/" + name);

    /// Called by KitchenArrange once the kitchen's pieces stand: the plates, the mixer and the board.
    public static void Attach(RectTransform area, bool arranging)
    {
        chouxPlate = Find(area, "chouxplate"); cakeStand = Find(area, "cake"); mixer = Find(area, "whisk"); board = Find(area, "cutboard");
        // every stage on the stand the owner approved, the table cake's own (20 November 2026: "use the one i approved");
        // the finished cake is the table's picture
        if (cakeStand != null) { cakeStand.preserveAspect = true; cakeDone = Pic("cake_done") ?? cakeStand.sprite; }
        var sm = SessionManager.Instance;
        // the fruit board also serves the cake: it shows when either dessert is on the menu
        if (board != null && sm != null && !sm.IsFoodItemActive("Choux") && sm.IsFoodItemActive("Cake"))
        {
            var gate = board.GetComponent<KitchenFoodGate>(); if (gate != null) gate.associatedFoodType = "Cake";   // its own gate asks for choux only
            board.gameObject.SetActive(true);
        }
        flyTo = null;
        var canvas = area.GetComponentInParent<Canvas>();
        if (canvas != null) foreach (var b in canvas.rootCanvas.GetComponentsInChildren<CloseKitchenButton>(true)) { flyTo = b.transform as RectTransform; break; }
        if (!arranging)
        {
            Tap(mixer, true); Tap(board, false);
        }
        Refresh();
    }

    private static Image Find(RectTransform area, string name) { var t = area.Find(name); return t != null ? t.GetComponent<Image>() : null; }

    private static void Tap(Image img, bool isMixer)
    {
        if (img == null) return;
        img.raycastTarget = true;
        var tap = img.GetComponent<KitchenFlowTap>(); if (tap == null) tap = img.gameObject.AddComponent<KitchenFlowTap>();
        tap.isMixer = isMixer;
    }

    /// The plates' pictures for their stage, and the glow on the next thing to tap.
    public static void Refresh()
    {
        if (chouxPlate != null) Show(chouxPlate, StageOf("Choux"), "choux_empty", "choux_plain", "choux_cream", Pic("choux_done"));
        if (cakeStand != null) Show(cakeStand, StageOf("Cake"), "cake_empty", "cake_sponge", "cake_cream", cakeDone);
        Glow(mixer, MixerReady); Glow(board, BoardReady);
    }

    private static void Show(Image img, Stage s, string empty, string baked, string creamed, Sprite done)
    {
        var sprite = s == Stage.Empty ? Pic(empty) : s == Stage.Baked ? Pic(baked) : s == Stage.Creamed ? Pic(creamed) : done;
        if (sprite != null) img.sprite = sprite;
    }

    private static void Glow(Image img, bool on)
    {
        if (img == null || !img.gameObject.activeInHierarchy) return;
        if (on) GlowFx.Gold(img); else GlowFx.Remove(img);
    }

    /// The done dessert leaves its plate for the table: a copy of the picture flies to the kitchen's handle and shrinks.
    private static IEnumerator Fly(string food)
    {
        var plate = food == "Choux" ? chouxPlate : cakeStand;
        if (plate == null || !plate.gameObject.activeInHierarchy || flyTo == null) yield break;   // the kitchen is closed: no flight
        var canvas = plate.canvas != null ? plate.canvas.rootCanvas.transform as RectTransform : null; if (canvas == null) yield break;
        var go = new GameObject("FlowFly", typeof(RectTransform), typeof(Image)); go.layer = plate.gameObject.layer;
        var rt = (RectTransform)go.transform; rt.SetParent(canvas, false); rt.SetAsLastSibling();
        var img = go.GetComponent<Image>(); img.sprite = plate.sprite; img.raycastTarget = false; img.preserveAspect = true;
        rt.sizeDelta = plate.rectTransform.rect.size; rt.position = plate.rectTransform.position;
        var from = rt.position; var scale0 = plate.rectTransform.lossyScale.x / Mathf.Max(0.0001f, canvas.lossyScale.x);
        Show(plate, Stage.Empty, food == "Choux" ? "choux_empty" : "cake_empty", null, null, null);        // the plate is empty under the flying copy
        for (var t = 0f; t < FlySeconds; t += KitchenClock.Delta)
        {
            if (rt == null || flyTo == null) yield break;                                        // the kitchen closed mid-flight
            var k = t / FlySeconds; var e = k * k;
            var p = Vector3.Lerp(from, flyTo.position, e); p.y += Mathf.Sin(k * Mathf.PI) * 0.6f;
            rt.position = p; rt.localScale = Vector3.one * scale0 * Mathf.Lerp(1f, 0.2f, e);
            yield return null;
        }
        if (go != null) Object.Destroy(go);
    }
}

/// A tap on the mixer or on the fruit board, and their little motion while they work (placeholder: the whisk rocks,
/// the board hops).
public class KitchenFlowTap : MonoBehaviour, IPointerClickHandler
{
    public bool isMixer;
    private Vector3 restScale; private bool kept;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.timeScale == 0f) return;   // paused
        if (isMixer) KitchenFlow.TapMixer(); else KitchenFlow.TapBoard();
    }

    private void Update()
    {
        if (!kept) { restScale = transform.localScale; kept = true; }
        var working = isMixer ? KitchenFlow.Mixing : KitchenFlow.Cutting;
        if (working && !KitchenClock.Paused)
        {
            var t = Time.unscaledTime;
            if (isMixer) transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 28f) * 14f);
            else transform.localScale = restScale * (1f + 0.05f * Mathf.Abs(Mathf.Sin(t * 14f)));
        }
        else if (!working) { transform.localRotation = Quaternion.identity; transform.localScale = restScale; }
    }
}
