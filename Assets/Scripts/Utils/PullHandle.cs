using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// The way between the table and the kitchen as a pull handle: a round knob on a wooden stick that comes out of the
/// edge of the screen (owner, 15 November 2026: "i want to make it look like a pulling handle with a stick on the
/// left, when clicked, it has a bounce effect"; "let's not use truck, just change it to a pulling circle"; "same thng
/// in kitchen, remove the truck icon on the mat, add a pulling handle on the right side"). On the table it sits on
/// the left edge above the cloth and opens the kitchen; in the kitchen it sits on the right edge and goes back. A tap
/// pulls it out and lets it spring back, then the kitchen slides as it does on a swipe.
/// The pictures: Resources/UI/handle_knob (Store/ui/pull_circle_2026-11-15) and Resources/UI/handle_stick
/// (Store/ui/truck_handle_2026-11-15). The existing buttons are kept (KitchenButton, backButton), with their sounds
/// and the tutorial's pointers; only their look and place change. Switch: KitchenButtonOverTable.Handle.
/// The handle can be moved up and down its edge (owner, 15 November 2026: "only while arranging the table, make it's
/// collision box to fit it's image, so it won't over lap with food ... don't go up to the setting buttons and don;t go
/// down out side of screen ... the bottom can be same a food items"; "also allow this in kitchen"). On the table it
/// moves only in the arrangement phase, between the settings button and the bottom of the food area, and never onto a
/// food; food cannot be put onto it either (DraggableFood asks TableOverlaps). In the kitchen, which has no arranging,
/// it can be dragged at any time. Each height is kept in the save, as a world height.
public class PullHandle : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // all lengths in the table canvas's own units (the truck button's scene size is the measure); the kitchen's canvas
    // has another scale, so there they are converted to give the same size on the screen
    public const float TableHeight = 112f;       // from the cloth's edge (the screen's middle) up to the knob's middle
    public const float KitchenHeight = 112f;     // the same height on the other side
    // Owner, 15 November 2026, on the first version (a 130 knob on a 250 stick): "use option L, make it smaller, about
    // half the size, make the stick half the lenth too". The knob is the vanilla donut with red drizzle.
    // Their tick on that the same afternoon: "A little bigger": 65 became 80, the stick thicker with it. Then, after
    // build 33: "make the handles slightly larger": 80 became 92. After build 34: "make the handle bigger": 112.
    private const float Knob = 112f, FromEdge = 101f;                    // the knob's size, and its middle from the screen's edge (45 of stick show)
    private const float TapWide = 170f, TapHigh = 130f;                // the area that takes the tap: bigger than the small knob, out to the edge
    public const float TableHenAnchor = 262f;                          // where the truck button's right edge was: the hen keeps its place by it (MoneyFlipClock)
    private const float StickHigh = 40f, StickLong = 152f, StickEndsBeforeMiddle = 32f;   // it ends under the ring, not in the donut's hole
    private const float PullOut = 46f, PullSeconds = 0.07f, SpringSeconds = 0.36f, SpringFade = 8f, SpringTurns = 4.2f;
    // The kitchen moves once the handle has sprung back and swung out once more: loading the kitchen stops the
    // picture for a moment and then covers the table, so a bounce that ran alongside it was never seen (measured in
    // the editor, 15 November 2026). The last small swings finish during the slide.
    private const float ActAfter = 0.26f;

    private RectTransform rt, stick; private bool pulling; private int side = 1; private float unit = 1f;
    private Vector2 rest; private Vector3 restScale = Vector3.one; private bool hasRest;
    private bool inKitchen, dragging, dragged, checkedAtStart, red; private float grabbedAt, heightAtGrab, lastFreeY;
    // Red over a food, like a dragged food (owner, 15 November 2026: "During arrangement, if the handle overlaps with
    // item, nothing shows red, make it show red just like food items so player know why they can't move it there").
    // The handle follows the finger, turns red where it may not stay, and goes back to its last free place when let go.
    private static readonly Color Red = new Color(1f, 0.5f, 0.5f, 1f);
    private float savedWorldY = float.NaN;       // the player's own height; none: the game's
    private static PullHandle tableHandle;
    // It wiggles while the table is being arranged, like the food, to show it can be moved (owner, 15 November 2026:
    // "make it wiggle during arrangement"). The food's own speed; a wider swing, because the knob is small.
    private const float WiggleDegrees = 4f, WiggleSpeed = 20f;
    private GamePhaseManager phases; private bool wiggling;
    private static Canvas tableCanvas;           // remembered from the table's handle for the kitchen's (read later: its scaler has not run yet when the handle is made)

    /// side +1: out of the left edge, pulled to the right (the table); -1: out of the right edge (the kitchen).
    /// kitchen: true for the kitchen's button, which is first moved out of the mat to the screen's edge.
    public static PullHandle Attach(RectTransform button, int side, bool kitchen, float height)
    {
        if (button == null) return null;
        var old = button.GetComponent<PullHandle>(); if (old != null) return old;
        var canvas = button.GetComponentInParent<Canvas>(); if (canvas == null) return null;
        canvas = canvas.rootCanvas;
        var h = button.gameObject.AddComponent<PullHandle>(); h.rt = button; h.side = side; h.inKitchen = kitchen;
        if (!kitchen) tableHandle = h;
        if (SessionManager.Instance != null && SessionManager.Instance.TryGetHandleHeight(kitchen, out var keptY)) h.savedWorldY = keptY;
        if (!kitchen) tableCanvas = canvas;
        else if (tableCanvas != null && canvas.transform.lossyScale.x > 0f) h.unit = tableCanvas.transform.lossyScale.x / canvas.transform.lossyScale.x;
        if (kitchen)
        {
            // straight under the canvas's own child (the one the kitchen's slide moves)
            Transform top = button; while (top.parent != null && top.parent != canvas.transform) top = top.parent;
            if (top != button && button.parent != top) button.SetParent(top, false);
        }
        // on the screen's edge: the button is the knob itself, a see-through child around it takes the taps
        button.anchorMin = button.anchorMax = new Vector2(side > 0 ? 0f : 1f, 0.5f); button.pivot = new Vector2(0.5f, 0.5f);
        button.sizeDelta = new Vector2(Knob, Knob) * h.unit; button.localScale = Vector3.one; button.localRotation = Quaternion.identity;
        button.anchoredPosition = new Vector2(side * FromEdge, height) * h.unit;
        var tap = new GameObject("TapArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        tap.transform.SetParent(button, false);
        var trt = (RectTransform)tap.transform; trt.sizeDelta = new Vector2(TapWide, TapHigh) * h.unit;
        trt.anchoredPosition = new Vector2(-side * (FromEdge - TapWide * 0.5f + 8f) * h.unit, 0f);   // from just past the screen's edge inwards
        tap.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
        var wobble = button.GetComponent<BalatroWobble>();
        if (wobble != null) { wobble.Rebase(); wobble.ResetTransform(); wobble.enabled = false; }
        var knob = Resources.Load<Sprite>("UI/handle_knob"); var picture = button.GetComponent<Image>();
        if (knob != null && picture != null) { picture.sprite = knob; picture.preserveAspect = true; }
        var press = button.GetComponent<Button>(); if (press != null) press.transition = Selectable.Transition.None;   // its pressed picture was the truck
        var wood = Resources.Load<Sprite>("UI/handle_stick");
        if (wood != null)
        {
            var go = new GameObject("HandleStick", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(button.parent, false); go.transform.SetSiblingIndex(button.GetSiblingIndex());   // drawn under the knob
            h.stick = (RectTransform)go.transform;
            h.stick.anchorMin = h.stick.anchorMax = button.anchorMin; h.stick.pivot = new Vector2(side > 0 ? 1f : 0f, 0.5f);   // the end by the knob
            h.stick.sizeDelta = new Vector2(StickLong, StickHigh) * h.unit;
            var img = go.GetComponent<Image>(); img.sprite = wood; img.raycastTarget = false;
            h.Follow();
        }
        return h;
    }

    private void LateUpdate()
    {
        // the player's own height, re-applied every frame: the canvas is not scaled yet when the handle is made
        if (rt != null && !dragging && !float.IsNaN(savedWorldY))
        {
            if (!checkedAtStart && Time.timeSinceLevelLoad > 0.6f)
            {
                // a food now stands there (a new food, another screen): back to the game's own place, which no food can reach
                checkedAtStart = true;
                if (!inKitchen && OverlapsFood(ToAnchored(savedWorldY))) { savedWorldY = float.NaN; rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, TableHeight * unit); hasRest = false; }
            }
            if (!float.IsNaN(savedWorldY))
            {
                Limits(out var lo, out var hi); var y = Mathf.Clamp(ToAnchored(savedWorldY), lo, hi);
                if (Mathf.Abs(y - rt.anchoredPosition.y) > 0.01f) { rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y); if (!pulling) hasRest = false; }
            }
        }
        Wiggle();
        Follow();
    }

    private void Wiggle()
    {
        if (rt == null || inKitchen) return;
        if (phases == null) phases = FindObjectOfType<GamePhaseManager>();
        var on = phases != null && phases.GetCurrentPhase() == GamePhase.ARRANGEMENT && !pulling && !dragging && Time.timeScale != 0f;
        if (on) { rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * WiggleSpeed) * WiggleDegrees); wiggling = true; }
        else if (wiggling) { rt.localRotation = Quaternion.identity; wiggling = false; }
    }

    private void Follow()
    {
        if (stick == null || rt == null) return;
        stick.anchoredPosition = rt.anchoredPosition + new Vector2(-side * StickEndsBeforeMiddle * unit, 0f);
    }

    private void OnDestroy() { if (stick != null) Destroy(stick.gameObject); if (tableHandle == this) tableHandle = null; }

    // ---- moving it up and down ------------------------------------------------------------------------------------

    private RectTransform Parent => (RectTransform)rt.parent;
    /// A world height as the handle's own anchored height (it is anchored to the middle of its parent's side).
    private float ToAnchored(float worldY) => Parent.InverseTransformPoint(new Vector3(rt.position.x, worldY, rt.position.z)).y - Parent.rect.center.y;
    private float ToWorld(float anchoredY) => Parent.TransformPoint(new Vector3(0f, anchoredY + Parent.rect.center.y, 0f)).y;

    /// How low and how high the knob's middle may go, as anchored heights.
    private void Limits(out float lo, out float hi)
    {
        var half = rt.rect.height * 0.5f; var p = Parent.rect;
        lo = p.yMin - p.center.y + half + 60f * unit; hi = p.yMax - p.center.y - half - 150f * unit;   // the kitchen: clear of the screen's ends
        if (inKitchen) return;
        var table = FindObjectOfType<TableLayer>();
        if (table != null && table.InteractiveBounds.size.y > 0f) lo = ToAnchored(table.InteractiveBounds.min.y) + half;   // as low as the food may stand
        var gear = FindObjectOfType<SettingButton>(); var gearRt = gear != null ? gear.GetComponent<RectTransform>() : null;
        if (gearRt != null) { var c = new Vector3[4]; gearRt.GetWorldCorners(c); hi = ToAnchored(c[0].y) - half - 12f * unit; }   // under the settings button
        if (hi < lo) hi = lo;
    }

    /// The knob's picture as a world rectangle, at a given anchored height.
    private Rect BoxAt(float anchoredY)
    {
        var c = new Vector3[4]; rt.GetWorldCorners(c);
        var w = Mathf.Abs(c[2].x - c[0].x); var h = Mathf.Abs(c[2].y - c[0].y); var middleX = (c[0].x + c[2].x) * 0.5f;
        return new Rect(middleX - w * 0.5f, ToWorld(anchoredY) - h * 0.5f, w, h);
    }

    private bool OverlapsFood(float anchoredY)
    {
        var box = BoxAt(anchoredY);
        foreach (var food in FindObjectsOfType<DraggableFood>())
        {
            if (food == null || !food.gameObject.activeInHierarchy) continue;
            var middle = (Vector2)food.transform.position + food.CentreOffset; var half = food.HalfSize;
            if (Mathf.Abs(middle.x - box.center.x) < half.x + box.width * 0.5f && Mathf.Abs(middle.y - box.center.y) < half.y + box.height * 0.5f) return true;
        }
        // the coffee station's wooden tray reaches a little past the machine's own box
        var tray = GameObject.Find("FixedStation"); var wood = tray != null && tray.activeInHierarchy ? tray.GetComponent<SpriteRenderer>() : null;
        if (wood != null && wood.enabled && wood.sprite != null)
        {
            var b = wood.bounds;
            if (Mathf.Abs(b.center.x - box.center.x) < b.extents.x + box.width * 0.5f && Mathf.Abs(b.center.y - box.center.y) < b.extents.y + box.height * 0.5f) return true;
        }
        return false;
    }

    /// For the food's own drag: would a food rectangle (its middle and half size, in world units) lie on the table's handle?
    public static bool TableOverlaps(Vector2 middle, Vector2 half)
    {
        var h = tableHandle; if (h == null || h.rt == null || !h.isActiveAndEnabled) return false;
        var box = h.BoxAt(h.rt.anchoredPosition.y);
        return Mathf.Abs(middle.x - box.center.x) < half.x + box.width * 0.5f && Mathf.Abs(middle.y - box.center.y) < half.y + box.height * 0.5f;
    }

    private bool MayMove()
    {
        if (pulling || Time.timeScale == 0f) return false;
        // at any time, on the table as in the kitchen (owner, 19 November 2026: "the kitchen pulling handle can be moved
        // anytime, i like it, do that for the game scene handle too"); until then the table's only while arranging
        return FindObjectOfType<TutorialDirector>() == null;
    }

    /// Moves the knob to a world height if it may be there (inside the limits, on no food). False when it stayed.
    public bool TryMoveTo(float worldY)
    {
        Limits(out var lo, out var hi); var y = Mathf.Clamp(ToAnchored(worldY), lo, hi);
        if (!inKitchen && OverlapsFood(y)) return false;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y); hasRest = false; Follow();
        return true;
    }

    /// Keeps the height the knob has now, in the save.
    public void KeepHeight()
    {
        savedWorldY = rt.position.y; checkedAtStart = true;
        if (SessionManager.Instance != null) SessionManager.Instance.SetHandleHeight(inKitchen, savedWorldY);
    }

    public void OnPointerDown(PointerEventData e) { dragged = false; }

    public void OnBeginDrag(PointerEventData e)
    {
        if (!MayMove()) return;
        dragging = true; dragged = true; heightAtGrab = rt.position.y; grabbedAt = PointerWorldY(e); lastFreeY = rt.anchoredPosition.y;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!dragging) return;
        Limits(out var lo, out var hi); var y = Mathf.Clamp(ToAnchored(heightAtGrab + PointerWorldY(e) - grabbedAt), lo, hi);
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y); hasRest = false; Follow();
        var blocked = !inKitchen && OverlapsFood(y);
        if (!blocked) lastFreeY = y;
        Paint(blocked);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!dragging) return; dragging = false;
        if (red) { rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, lastFreeY); Follow(); Paint(false); }   // back to where it was free
        KeepHeight();
    }

    private void Paint(bool on)
    {
        if (red == on) return; red = on;
        var c = on ? Red : Color.white;
        var img = rt.GetComponent<Image>(); if (img != null) img.color = c;
        var wood = stick != null ? stick.GetComponent<Image>() : null; if (wood != null) wood.color = c;
    }

    private float PointerWorldY(PointerEventData e)
    {
        var canvas = rt.GetComponentInParent<Canvas>(); var cam = canvas != null ? canvas.rootCanvas.worldCamera : null;
        return RectTransformUtility.ScreenPointToWorldPointInRectangle(Parent, e.position, cam != null ? cam : Camera.main, out var world) ? world.y : rt.position.y;
    }

    /// The tap's bounce, then `then`. False when nothing is started and the caller should act at once as it always
    /// did: while paused, and in the tutorial, which runs its own steps (the bounce still shows there).
    public bool Pull(System.Action then)
    {
        if (dragged) { dragged = false; return true; }   // the finger moved the handle: that was not a tap
        if (pulling) return true;
        if (Time.timeScale == 0f) return false;
        var tutorial = FindObjectOfType<TutorialDirector>() != null;
        StartCoroutine(Bounce(tutorial ? null : then));
        return !tutorial;
    }

    private IEnumerator Bounce(System.Action then)
    {
        pulling = true; hasRest = false;
        for (var t = 0f; t < BounceSeconds; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))   // a slow frame does not skip the bounce
        {
            ShowAt(t);
            if (then != null && t >= ActAfter) { var act = then; then = null; act(); }
            yield return null;
        }
        ShowAt(BounceSeconds);
        if (then != null) then();
        pulling = false; hasRest = false;
    }

    public const float BounceSeconds = PullSeconds + SpringSeconds;

    /// The handle as it is `seconds` after a tap (0 and BounceSeconds: at rest). The bounce runs on this, and the
    /// report's pictures of the bounce are taken with it (Docs/tools/autoplay/pull_handle_gif.py).
    public void ShowAt(float seconds)
    {
        if (!hasRest) { rest = rt.anchoredPosition; restScale = rt.localScale; hasRest = true; }
        float x;
        if (seconds <= 0f || seconds >= BounceSeconds) x = 0f;
        else if (seconds < PullSeconds) { var k = 1f - seconds / PullSeconds; x = 1f - k * k; }
        else { var u = seconds - PullSeconds; x = Mathf.Exp(-SpringFade * u) * Mathf.Cos(2f * Mathf.PI * SpringTurns * u) * (1f - u / SpringSeconds); }
        rt.anchoredPosition = rest + new Vector2(side * PullOut * unit * x, 0f);
        rt.localScale = new Vector3(restScale.x * (1f + 0.07f * x), restScale.y * (1f - 0.05f * x), restScale.z);   // a little longer and flatter while pulled out
        Follow();
    }
}
