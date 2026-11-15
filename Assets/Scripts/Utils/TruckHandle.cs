using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// The kitchen truck as a pull handle (owner, 15 November 2026: "for the truck icon, i want to make it look like a
/// pulling handle with a stick on the left, when clicked, it has a bounce effect"; earlier that day: "Instead of having
/// the truck park on the table, I want it to looks like a sticker sticking out from left of the screen, so it would be
/// above the table"). The truck floats above the cloth's edge on a wooden stick that comes out of the left edge of the
/// screen (Resources/UI/truck_stick, Store/ui/truck_handle_2026-11-15). A tap pulls it out and lets it spring back, and
/// the kitchen slides in from the left as it does on a swipe. Added by KitchenButtonOverTable.Setup.
public class TruckHandle : MonoBehaviour
{
    public const float RestHeight = 112f;        // canvas units from the cloth's edge to the middle of the button (it is 130 high)
    private const float StickHigh = 42f, StickPastEdge = 80f, StickBelowMiddle = 12f;
    private const float PullOut = 46f, PullSeconds = 0.07f, SpringSeconds = 0.36f, SpringFade = 8f, SpringTurns = 4.2f;
    // The kitchen opens once the handle has sprung back and swung out once more: loading the kitchen stops the
    // picture for a moment and then covers the truck, so a bounce that ran alongside it was never seen (measured in
    // the editor, 15 November 2026). The last small swings finish under the kitchen.
    private const float OpenAfter = 0.26f;

    private RectTransform rt, stick; private bool pulling;

    public static void Attach(RectTransform button)
    {
        if (button == null || button.GetComponent<TruckHandle>() != null) return;
        var h = button.gameObject.AddComponent<TruckHandle>(); h.rt = button;
        var art = Resources.Load<Sprite>("UI/truck_stick");
        if (art == null) return;
        var go = new GameObject("TruckStick", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(button.parent, false); go.transform.SetSiblingIndex(button.GetSiblingIndex());   // drawn under the truck
        h.stick = (RectTransform)go.transform;
        h.stick.anchorMin = h.stick.anchorMax = button.anchorMin; h.stick.pivot = new Vector2(1f, 0.5f);          // its right end stays behind the truck
        var img = go.GetComponent<Image>(); img.sprite = art; img.raycastTarget = false;
        h.Follow();
    }

    private void LateUpdate() { Follow(); }

    private void Follow()
    {
        if (stick == null || rt == null) return;
        stick.sizeDelta = new Vector2(rt.anchoredPosition.x + StickPastEdge + PullOut, StickHigh);
        stick.anchoredPosition = rt.anchoredPosition + new Vector2(0f, -StickBelowMiddle);
    }

    /// The tap's bounce. True when the handle opens the kitchen itself (a moment later, with the slide); false when
    /// the caller should open it as before (paused, or in the tutorial, which runs its own steps).
    public bool Pull()
    {
        if (pulling) return true;
        if (Time.timeScale == 0f) return false;
        var tutorial = FindObjectOfType<TutorialDirector>() != null;
        StartCoroutine(Bounce(!tutorial));
        return !tutorial;
    }

    private IEnumerator Bounce(bool open)
    {
        pulling = true;
        var rest = rt.anchoredPosition; var scale = rt.localScale; var opened = !open;
        for (var t = 0f; t < PullSeconds + SpringSeconds; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))   // a slow frame does not skip the bounce
        {
            float x;
            if (t < PullSeconds) { var k = 1f - t / PullSeconds; x = PullOut * (1f - k * k); }
            else { var u = t - PullSeconds; x = PullOut * Mathf.Exp(-SpringFade * u) * Mathf.Cos(2f * Mathf.PI * SpringTurns * u) * (1f - u / SpringSeconds); }
            var stretch = x / PullOut;   // a little longer and flatter while pulled out, the other way on the way back
            rt.anchoredPosition = rest + new Vector2(x, 0f);
            rt.localScale = new Vector3(scale.x * (1f + 0.07f * stretch), scale.y * (1f - 0.05f * stretch), scale.z);
            if (!opened && t >= OpenAfter) { opened = true; OpenKitchen(); }
            yield return null;
        }
        rt.anchoredPosition = rest; rt.localScale = scale;
        if (!opened) OpenKitchen();
        pulling = false;
    }

    private void OpenKitchen()
    {
        var ksm = KitchenSceneManager.Instance;
        if (ksm == null || ksm.IsKitchenOpen) return;
        StartCoroutine(KitchenSwipe.SlideOpen());
    }
}
