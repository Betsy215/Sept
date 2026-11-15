using System.Collections;
using UnityEngine;
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
public class PullHandle : MonoBehaviour
{
    // all lengths in the table canvas's own units (the truck button's scene size is the measure); the kitchen's canvas
    // has another scale, so there they are converted to give the same size on the screen
    public const float TableHeight = 112f;       // from the cloth's edge (the screen's middle) up to the knob's middle
    public const float KitchenHeight = 112f;     // the same height on the other side
    // Owner, 15 November 2026, on the first version (a 130 knob on a 250 stick): "use option L, make it smaller, about
    // half the size, make the stick half the lenth too". The knob is the vanilla donut with red drizzle.
    private const float Knob = 65f, FromEdge = 78f;                    // the knob's size, and its middle from the screen's edge (45 of stick show)
    private const float TapWide = 170f, TapHigh = 130f;                // the area that takes the tap: bigger than the small knob, out to the edge
    public const float TableHenAnchor = 262f;                          // where the truck button's right edge was: the hen keeps its place by it (MoneyFlipClock)
    private const float StickHigh = 24f, StickLong = 125f, StickEndsBeforeMiddle = 19f;   // it ends under the ring, not in the donut's hole
    private const float PullOut = 46f, PullSeconds = 0.07f, SpringSeconds = 0.36f, SpringFade = 8f, SpringTurns = 4.2f;
    // The kitchen moves once the handle has sprung back and swung out once more: loading the kitchen stops the
    // picture for a moment and then covers the table, so a bounce that ran alongside it was never seen (measured in
    // the editor, 15 November 2026). The last small swings finish during the slide.
    private const float ActAfter = 0.26f;

    private RectTransform rt, stick; private bool pulling; private int side = 1; private float unit = 1f;
    private Vector2 rest; private Vector3 restScale = Vector3.one; private bool hasRest;
    private static Canvas tableCanvas;           // remembered from the table's handle for the kitchen's (read later: its scaler has not run yet when the handle is made)

    /// side +1: out of the left edge, pulled to the right (the table); -1: out of the right edge (the kitchen).
    /// kitchen: true for the kitchen's button, which is first moved out of the mat to the screen's edge.
    public static PullHandle Attach(RectTransform button, int side, bool kitchen, float height)
    {
        if (button == null) return null;
        var old = button.GetComponent<PullHandle>(); if (old != null) return old;
        var canvas = button.GetComponentInParent<Canvas>(); if (canvas == null) return null;
        canvas = canvas.rootCanvas;
        var h = button.gameObject.AddComponent<PullHandle>(); h.rt = button; h.side = side;
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

    private void LateUpdate() { Follow(); }

    private void Follow()
    {
        if (stick == null || rt == null) return;
        stick.anchoredPosition = rt.anchoredPosition + new Vector2(-side * StickEndsBeforeMiddle * unit, 0f);
    }

    private void OnDestroy() { if (stick != null) Destroy(stick.gameObject); }

    /// The tap's bounce, then `then`. False when nothing is started and the caller should act at once as it always
    /// did: while paused, and in the tutorial, which runs its own steps (the bounce still shows there).
    public bool Pull(System.Action then)
    {
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
