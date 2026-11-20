using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// The arranging lesson on a real thing, at the moment it can be used: the first arrangement a player meets (day 2).
/// A hand takes the thing (the coffee machine on the table, the coffee bags in the kitchen), carries it over, puts it
/// down and carries it back, once (owner, 19 November 2026: the day-1 tutorial showed this in the middle of its lesson
/// and "player would think they can do that anytime": "let's do option A, take out the arrangement tutorial from the
/// initial tutorial"; "remove the previous arrangement animation with the hand and bread basket, but keep the banner").
/// A touch anywhere, Ready, or the other place opening ends it at once and puts the thing back where it was.
public class ArrangeLesson : MonoBehaviour
{
    private static ArrangeLesson running;
    private Transform thing; private Vector3 from; private RectTransform hand;

    public static bool Running => running != null;

    /// `to`: where the thing is carried, in the world. `cam`: the camera that shows the thing (null for an overlay
    /// canvas). `canvas`: where the hand is drawn. `handLook`: the hand's picture (the arrangement panel's own).
    public static void Play(Transform thing, Vector3 to, Camera cam, RectTransform canvas, Image handLook)
    {
        Cancel();
        if (thing == null || canvas == null || handLook == null) return;
        running = new GameObject("ArrangeLesson").AddComponent<ArrangeLesson>();
        running.StartCoroutine(running.Run(thing, to, cam, canvas, handLook));
    }

    /// Ends a running lesson and puts its thing back. True when one was running (it had not finished).
    public static bool Cancel()
    {
        if (running == null) return false;
        var r = running; running = null;
        if (r.thing != null) { r.thing.position = r.from; var f = r.thing.GetComponent<DraggableFood>(); if (f != null) f.LessonHold(false); }
        if (r.hand != null) Destroy(r.hand.gameObject);
        Destroy(r.gameObject);
        return true;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0) Cancel();   // the player's own finger comes first
    }

    private void OnDestroy() { if (running == this) { running = null; if (thing != null) { thing.position = from; var f = thing.GetComponent<DraggableFood>(); if (f != null) f.LessonHold(false); } if (hand != null) Destroy(hand.gameObject); } }

    private IEnumerator Run(Transform target, Vector3 to, Camera cam, RectTransform canvas, Image handLook)
    {
        thing = target; from = target.position;
        var food = target.GetComponent<DraggableFood>(); if (food != null) food.LessonHold(true);   // a table item wiggles in place while arranging: that waits
        var go = new GameObject("DemoHand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvas, false); go.transform.SetAsLastSibling();
        hand = (RectTransform)go.transform; hand.anchorMin = hand.anchorMax = new Vector2(0.5f, 0.5f);
        var img = go.GetComponent<Image>(); img.sprite = handLook.sprite; img.preserveAspect = true; img.raycastTarget = false;
        hand.sizeDelta = handLook.rectTransform.rect.size * 1.6f; hand.pivot = handLook.rectTransform.pivot; hand.localRotation = handLook.rectTransform.localRotation;
        var root = canvas.GetComponentInParent<Canvas>(); root = root != null ? root.rootCanvas : null;
        var uiCam = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;
        System.Action follow = () =>
        {
            if (hand == null || thing == null) return;
            var screen = cam != null ? (Vector2)cam.WorldToScreenPoint(thing.position) : RectTransformUtility.WorldToScreenPoint(null, thing.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, uiCam, out var local);
            hand.anchoredPosition = local + new Vector2(40f, -30f);   // the finger on the thing
        };
        hand.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.9f);                        // the place is looked at first
        hand.gameObject.SetActive(true); follow();
        yield return new WaitForSeconds(0.6f);
        for (var leg = 0; leg < 2; leg++)
        {
            var a = leg == 0 ? from : to; var b = leg == 0 ? to : from;
            hand.localScale = Vector3.one * 0.92f;                    // pressed
            for (var t = 0f; t < 1f; t += Time.deltaTime / 1.1f) { thing.position = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t)); follow(); yield return null; }
            thing.position = b; follow();
            hand.localScale = Vector3.one;                            // let go
            yield return new WaitForSeconds(leg == 0 ? 0.9f : 0.4f);
        }
        Cancel();
    }
}
