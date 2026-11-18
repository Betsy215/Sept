using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// The red "NEW" label (owner, 18 November 2026: "show the new label on the book if a new cookie is collected";
/// "show a new label on items, for both game screen and kitchen if something is newly added"). The same look as the
/// shop cards' tag (ShopFlourish), as one picture (Resources/UI/new_tag) so it also works on the table's world
/// sprites. What counts as new is kept in the save (SessionManager.NoteNew).
public static class NewTag
{
    private const string Name = "NewTag";
    private static Sprite sprite;
    private static Sprite Picture { get { if (sprite == null) sprite = Resources.Load<Sprite>("UI/new_tag"); return sprite; } }
    private static readonly List<GameObject> world = new List<GameObject>();

    /// On a UI thing: at its top right corner, tilted like the shop's.
    public static void OnUI(RectTransform owner)
    {
        if (owner == null || Picture == null || owner.Find(Name) != null) return;
        var go = new GameObject(Name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(NewTagPulse));
        go.layer = owner.gameObject.layer;
        var rt = (RectTransform)go.transform; rt.SetParent(owner, false); rt.SetAsLastSibling();
        var w = Mathf.Clamp(Mathf.Min(owner.rect.width, owner.rect.height) * 0.62f, 84f, 140f);
        // at the screen's right edge the label goes to the thing's top left instead, so it is never cut off
        var corners = new Vector3[4]; owner.GetWorldCorners(corners);
        var canvas = owner.GetComponentInParent<Canvas>(); var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
        var x0 = RectTransformUtility.WorldToScreenPoint(cam, corners[1]).x; var x1 = RectTransformUtility.WorldToScreenPoint(cam, corners[2]).x;
        var perUnit = owner.rect.width > 0f ? (x1 - x0) / owner.rect.width : 1f;
        var left = x1 + 0.2f * w * perUnit > Screen.width * 0.94f;   // the label's own right edge
        rt.anchorMin = rt.anchorMax = new Vector2(left ? 0f : 1f, 1f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, w * Picture.rect.height / Picture.rect.width);
        rt.anchoredPosition = new Vector2(left ? w * 0.30f : -w * 0.30f, -w * 0.16f);
        rt.localRotation = Quaternion.Euler(0f, 0f, left ? -12f : 12f);
        var img = go.GetComponent<Image>(); img.sprite = Picture; img.raycastTarget = false;
    }

    public static void Remove(RectTransform owner)
    {
        var t = owner != null ? owner.Find(Name) : null;
        if (t != null) Object.Destroy(t.gameObject);
    }

    /// On a table thing (world sprites): above its top right, following it when it is dragged. Not a child of the
    /// thing, so nothing that measures or tints the thing's own sprites sees the label.
    public static void OnWorld(Transform owner)
    {
        if (owner == null || Picture == null) return;
        var has = false; var b = new Bounds(owner.position, Vector3.zero); var order = 0; var layer = 0;
        foreach (var r in owner.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!r.enabled || r.sprite == null) continue;
            if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            if (r.sortingOrder >= order) { order = r.sortingOrder; layer = r.sortingLayerID; }
        }
        if (!has) return;
        var go = new GameObject(Name, typeof(SpriteRenderer), typeof(NewTagPulse), typeof(NewTagFollow));
        var sr = go.GetComponent<SpriteRenderer>(); sr.sprite = Picture; sr.sortingLayerID = layer; sr.sortingOrder = order + 500;
        var width = Mathf.Clamp(Mathf.Min(b.size.x, b.size.y) * 0.6f, 0.55f, 0.95f);
        go.transform.localScale = Vector3.one * (width / Picture.bounds.size.x);
        var view = Camera.main;
        var left = view != null && view.WorldToViewportPoint(b.max + Vector3.right * (0.2f * width)).x > 0.94f;   // the label's own right edge at the screen's: on the top left, never cut off
        go.transform.rotation = Quaternion.Euler(0f, 0f, left ? -12f : 12f);
        var at = new Vector3(left ? b.min.x + width * 0.30f : b.max.x - width * 0.30f, b.max.y - width * 0.10f, owner.position.z - 0.5f);
        go.transform.position = at;
        var f = go.GetComponent<NewTagFollow>(); f.owner = owner; f.offset = at - owner.position;
        world.Add(go);
    }

    public static void ClearWorld()
    {
        foreach (var g in world) if (g != null) Object.Destroy(g);
        world.Clear();
    }
}

/// A slow, small breathing so the label catches the eye.
public class NewTagPulse : MonoBehaviour
{
    private Vector3 size; private bool got;
    private void LateUpdate()
    {
        if (!got) { size = transform.localScale; got = true; }
        transform.localScale = size * (1f + 0.06f * Mathf.Sin(Time.unscaledTime * 4f));
    }
}

public class NewTagFollow : MonoBehaviour
{
    public Transform owner; public Vector3 offset;
    private void LateUpdate()
    {
        if (owner == null || !owner.gameObject.activeInHierarchy) { Destroy(gameObject); return; }
        transform.position = owner.position + offset;
    }
}
