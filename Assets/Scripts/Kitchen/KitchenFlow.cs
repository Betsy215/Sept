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
    /// The kitchen also finishes a dessert by itself (owner, 20 November 2026: "i want kitchen be able to cook the recipe
    /// all the way after we pick a recipe with oven ... if we wait for kitchen itself to automatically cook, between each
    /// step, it will wait for 3 seconds"): a dessert waiting at the mixer or the board starts that step on its own after
    /// this many seconds (kitchen clock: a pause stops it); a tap starts it at once.
    /// 21 November 2026, after playing it: "make kitchen cook automatically faster, no pause between steps": zero, so
    /// each step follows the one before as soon as its tool is free.
    public static float AutoWaitSeconds = 0f;
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
        if (Host != null) Host.StartCoroutine(AutoNext(food, Stage.Baked));
    }

    /// The step after `waiting` starts by itself once the dessert has waited AutoWaitSeconds and its tool is free.
    private static IEnumerator AutoNext(string food, Stage waiting)
    {
        var gen = generation;
        yield return KitchenClock.Wait(AutoWaitSeconds);
        while (gen == generation && Of(food).stage == waiting && !Of(food).working)
        {
            if (waiting == Stage.Baked && mixing == null) { StartMix(food); yield break; }
            if (waiting == Stage.Creamed && cutting == null) { StartCut(food); yield break; }
            yield return null;                                                  // the tool is busy with the other dessert
        }
    }

    private static void StartMix(string f) { mixing = f; Of(f).working = true; Refresh(); Sound("Kitchen/mixer_mix"); Host.StartCoroutine(Step(f, MixSeconds, Stage.Creamed, true)); }
    private static void StartCut(string f) { cutting = f; Of(f).working = true; Refresh(); Sound("Kitchen/board_cut"); Host.StartCoroutine(Step(f, CutSeconds, Stage.Done, false)); }

    /// The mixing and the cutting sound (owner, 21 November 2026: "find several sound effects on line for mixing and
    /// cutting, show me and i will pick"): silent until the picked sample is in Resources, then once per step.
    private static void Sound(string path)
    {
        var clip = Resources.Load<AudioClip>(path);
        if (clip != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clip);
    }

    public static void TapMixer()
    {
        if (!MixerReady || Host == null) return;
        StartMix(Waiting(Stage.Baked));
    }

    public static void TapBoard()
    {
        if (!BoardReady || Host == null) return;
        StartCut(Waiting(Stage.Creamed));
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
        if (next != Stage.Done) { Host.StartCoroutine(AutoNext(food, next)); yield break; }
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
    private static bool mixerIsSet;   // the mixer set on its stand (two pictures: at rest, running); else the old whisk stands in
    private static RectTransform flyTo;

    private static Sprite Pic(string name) => Resources.Load<Sprite>("Kitchen/flow/" + name);

    /// Called by KitchenArrange once the kitchen's pieces stand: the plates, the mixer and the board.
    public static void Attach(RectTransform area, bool arranging)
    {
        chouxPlate = Find(area, "chouxplate"); cakeStand = Find(area, "cake"); mixer = Find(area, "mixer"); if (mixer == null) mixer = Find(area, "whisk"); board = Find(area, "cutboard");
        mixerIsSet = mixer != null && mixer.name == "mixer";
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
        if (mixerIsSet && mixer != null) { var pic = Pic(Mixing ? "mixer_run" : "mixer_rest"); if (pic != null) mixer.sprite = pic; }   // head up and whisk out at rest; level, whisk in the bowl while it runs
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

/// A tap on the mixer or on the fruit board, and their motion while they work (owner, 21 November 2026: "add animation
/// for mixing, do that to the bowl and mixing blade ... the mixer handle ... won't need to be involved in the mixing
/// animation"; "also work on the cutting animation for fruit board").
///   Mixer: the running picture comes apart into its layers (Resources/Kitchen/flow/mixer_run_bowl, _whisk, _body, all on
///   the same canvas, from Store/redesign/mixer_hand_2026-11-20/compose_lean.py). The whisk spins about its own upright
///   axis (seen as a narrowing and a flip), the bowl rocks a little on its foot, and drops of cream jump from the whisk.
///   The body holds still.
///   Board: Gemini's knife (Resources/Kitchen/flow/knife) chops along the strawberries and the lemon, rocking on its
///   handle; each chop gives the board a small bump and throws a few bits of fruit.
/// Everything runs on the kitchen clock, so a pause stops it.
public class KitchenFlowTap : MonoBehaviour, IPointerClickHandler
{
    public bool isMixer;
    private Vector3 restScale; private bool kept;
    private float t;                       // seconds of work, kitchen clock

    // the running mixer's layers, as shares of the picture (compose_lean.py prints them): turning points
    private static readonly Vector2 WhiskAxis = new Vector2(0.2573f, 0.92f), BowlFoot = new Vector2(0.2565f, 0f), WhiskTip = new Vector2(0.2573f, 0.38f);   // with the owner's body (21 Nov)
    private RectTransform bowl, whisk, body; private Image own; private Color ownColour;
    private RectTransform knife; private float lastChop = -1f;
    private static Sprite dot;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.timeScale == 0f) return;   // paused
        if (isMixer) KitchenFlow.TapMixer(); else KitchenFlow.TapBoard();
    }

    private void Update()
    {
        if (!kept) { restScale = transform.localScale; own = GetComponent<Image>(); if (own != null) ownColour = own.color; kept = true; }
        var working = isMixer ? KitchenFlow.Mixing : KitchenFlow.Cutting;
        if (!working) { if (t > 0f) Stop(); return; }
        t += KitchenClock.Delta;
        if (isMixer) { if (name == "mixer" && Layers()) Mix(); else if (name != "mixer") transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 28f) * 14f); }
        else Cut();
    }

    private void Stop()
    {
        t = 0f; lastChop = -1f;
        transform.localRotation = Quaternion.identity; transform.localScale = restScale;
        foreach (var r in new[] { bowl, whisk, body }) if (r != null) Destroy(r.gameObject);
        bowl = whisk = body = null;
        if (own != null) own.color = ownColour;
        if (knife != null) { Destroy(knife.gameObject); knife = null; }
    }

    // ---- mixer ----

    private bool Layers()
    {
        if (bowl != null) return true;
        var b = Resources.Load<Sprite>("Kitchen/flow/mixer_run_bowl"); var w = Resources.Load<Sprite>("Kitchen/flow/mixer_run_whisk"); var h = Resources.Load<Sprite>("Kitchen/flow/mixer_run_body");
        if (b == null || w == null || h == null || own == null) return false;
        bowl = Layer("MixBowl", b, BowlFoot); whisk = Layer("MixWhisk", w, WhiskAxis); body = Layer("MixBody", h, new Vector2(0.5f, 0.5f));
        own.color = new Color(ownColour.r, ownColour.g, ownColour.b, 0f);   // the whole picture hides; it still takes the taps
        return true;
    }

    private RectTransform Layer(string n, Sprite s, Vector2 pivot)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image)); go.layer = gameObject.layer;
        var rt = (RectTransform)go.transform; rt.SetParent(transform, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = pivot; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>(); img.sprite = s; img.preserveAspect = true; img.raycastTarget = false;
        return rt;
    }

    private void Mix()
    {
        var turn = Mathf.Cos(t * 2f * Mathf.PI * 3.2f);                                      // three turns a second
        whisk.localScale = new Vector3(Mathf.Sign(turn) * Mathf.Max(0.22f, Mathf.Abs(turn)), 1f, 1f);
        whisk.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 9f) * 2.5f);              // and it stirs round the bowl a little
        bowl.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 13f) * 1.6f);
        bowl.localScale = new Vector3(1f + 0.012f * Mathf.Sin(t * 26f), 1f - 0.012f * Mathf.Sin(t * 26f), 1f);
        if (Mathf.Repeat(t, 0.22f) < KitchenClock.Delta && KitchenClock.Delta > 0f) StartCoroutine(Drop(WhiskTip, new Color(1f, 0.975f, 0.9f), Random.Range(-1f, 1f) * 55f, Random.Range(70f, 120f), 0.045f));
    }

    // ---- board ----

    private void Cut()
    {
        var rt = (RectTransform)transform; var size = rt.rect.size;
        if (knife == null)
        {
            var s = Resources.Load<Sprite>("Kitchen/flow/knife"); if (s == null) { transform.localScale = restScale * (1f + 0.05f * Mathf.Abs(Mathf.Sin(t * 14f))); return; }
            var go = new GameObject("Knife", typeof(RectTransform), typeof(Image)); go.layer = gameObject.layer;
            knife = (RectTransform)go.transform; knife.SetParent(transform, false);
            var img = go.GetComponent<Image>(); img.sprite = s; img.preserveAspect = true; img.raycastTarget = false;
            var w = size.x * 0.62f; knife.sizeDelta = new Vector2(w, w * s.rect.height / s.rect.width);
            knife.anchorMin = knife.anchorMax = new Vector2(0.5f, 0.5f); knife.pivot = new Vector2(0.92f, 0.5f);   // it rocks on its handle
        }
        // four chops a second; the knife works from the strawberries along to the lemon
        const float rate = 4f; var phase = Mathf.Repeat(t * rate, 1f); var lift = phase < 0.55f ? Mathf.Sin(phase / 0.55f * Mathf.PI) : 0f;
        var along = Mathf.PingPong(t * 0.18f, 0.22f);
        knife.anchoredPosition = new Vector2(size.x * (0.19f + along), size.y * (0.02f + 0.06f * lift));
        knife.localRotation = Quaternion.Euler(0f, 0f, -14f * lift);
        knife.localScale = Vector3.one * (1f + 0.06f * lift);                                  // nearer the eye when lifted
        var chop = Mathf.Floor(t * rate);
        if (phase >= 0.55f && chop != lastChop)
        {
            lastChop = chop;
            transform.localScale = restScale * 1.025f;
            var tip = new Vector2(0.5f + (0.19f + along - 0.92f * 0.62f + 0.08f) , 0.5f);      // where the blade meets the board, as shares
            for (var i = 0; i < 3; i++)
            {
                var c = Random.value < 0.5f ? new Color(0.98f, 0.82f, 0.25f) : new Color(0.9f, 0.25f, 0.22f);
                StartCoroutine(Drop(tip, c, Random.Range(-1f, 1f) * 70f, Random.Range(50f, 95f), 0.03f));
            }
        }
        else transform.localScale = Vector3.Lerp(transform.localScale, restScale, 0.35f);
    }

    // ---- bits ----

    /// A small round bit (cream or fruit) jumps from `at` (shares of this piece) and falls back, fading.
    private IEnumerator Drop(Vector2 at, Color colour, float vx, float vy, float sizeShare)
    {
        var rt0 = (RectTransform)transform; var size = rt0.rect.size;
        var go = new GameObject("Bit", typeof(RectTransform), typeof(Image)); go.layer = gameObject.layer;
        var rt = (RectTransform)go.transform; rt.SetParent(transform, false); rt.anchorMin = rt.anchorMax = at;
        var img = go.GetComponent<Image>(); img.sprite = Dot(); img.color = colour; img.raycastTarget = false;
        var d = Mathf.Max(4f, size.x * sizeShare * Random.Range(0.7f, 1.2f)); rt.sizeDelta = new Vector2(d, d * Random.Range(0.8f, 1f));
        var p = Vector2.zero; var v = new Vector2(vx, vy) * (size.x / 200f);
        for (var a = 0f; a < 0.45f; a += KitchenClock.Delta)
        {
            if (rt == null) yield break;
            var dt = KitchenClock.Delta; v.y -= 520f * (size.x / 200f) * dt; p += v * dt; rt.anchoredPosition = p;
            img.color = new Color(colour.r, colour.g, colour.b, 1f - a / 0.45f);
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    private static Sprite Dot()
    {
        if (dot != null) return dot;
        const int n = 32; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear }; var px = new Color[n * n];
        for (var y = 0; y < n; y++) for (var x = 0; x < n; x++) { var r = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f); var shade = Mathf.Lerp(1f, 0.86f, Mathf.Clamp01((x - y) / (float)n + 0.3f)); px[y * n + x] = new Color(shade, shade, shade, Mathf.Clamp01((1f - r) * 6f)); }
        tex.SetPixels(px); tex.Apply(); dot = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return dot;
    }
}
