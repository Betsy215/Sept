using UnityEngine;
using UnityEngine.UI;

/// The kitchen's "drag to arrange" hint: a hand moving a kitchen thing (owner, 18 November 2026: "instead of just
/// banner, let's add animation of item moving, with a hand icon, that is more direct ... do that during tutorial").
/// The table's hint is an Animator clip (Animation/Clips/drag) on the game's own canvas, which the kitchen covers;
/// this plays the same four-second move, with the same hand, on the arranging canvas that is drawn over the kitchen.
public class ArrangeHintAnim : MonoBehaviour
{
    private RectTransform item, hand;
    // drag.anim's keys: time, the item's place, the hand's place (scaled up from the clip's 100-unit food)
    private static readonly float[] T = { 0f, 0.5167f, 1f, 2.0167f, 4f };
    private static readonly Vector2[] ItemAt = { new Vector2(-150f, 0f), new Vector2(-150f, 0f), new Vector2(7.6f, 34.9f), new Vector2(103.6f, 34.9f), new Vector2(103.6f, 34.9f) };
    private static readonly Vector2[] HandAt = { new Vector2(-90f, -2f), new Vector2(-90f, -2f), new Vector2(90f, 34.9f), new Vector2(199.6f, 23.9f), new Vector2(199.6f, 23.9f) };
    private const float Scale = 1.3f;

    public static ArrangeHintAnim Make(RectTransform parent, Vector2 at, Sprite itemSprite, Image handLook)
    {
        var go = new GameObject("KitchenHintAnim", typeof(RectTransform), typeof(ArrangeHintAnim));
        go.layer = parent.gameObject.layer;
        var rt = (RectTransform)go.transform; rt.SetParent(parent, false); rt.anchoredPosition = at; rt.sizeDelta = new Vector2(600f, 100f);
        var a = go.GetComponent<ArrangeHintAnim>();
        a.item = Picture(rt, "Item", itemSprite, new Vector2(100f, 100f) * Scale);
        a.hand = Picture(rt, "Hand", handLook != null ? handLook.sprite : null, (handLook != null ? handLook.rectTransform.rect.size : new Vector2(100f, 150f)) * Scale);
        if (handLook != null) { a.hand.pivot = handLook.rectTransform.pivot; a.hand.localRotation = handLook.rectTransform.localRotation; }
        a.Update();
        return a;
    }

    private static RectTransform Picture(RectTransform parent, string name, Sprite sprite, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = parent.gameObject.layer;
        var rt = (RectTransform)go.transform; rt.SetParent(parent, false); rt.sizeDelta = size;
        var img = go.GetComponent<Image>(); img.sprite = sprite; img.preserveAspect = true; img.raycastTarget = false; img.enabled = sprite != null;
        return rt;
    }

    private void Update()
    {
        var t = Mathf.Repeat(Time.unscaledTime, 4f);
        var i = 0; while (i < T.Length - 2 && t >= T[i + 1]) i++;
        var k = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(T[i], T[i + 1], t));
        if (item != null) item.anchoredPosition = Vector2.Lerp(ItemAt[i], ItemAt[i + 1], k) * Scale;
        if (hand != null) hand.anchoredPosition = Vector2.Lerp(HandAt[i], HandAt[i + 1], k) * Scale;
    }
}
