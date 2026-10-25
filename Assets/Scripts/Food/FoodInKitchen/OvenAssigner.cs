using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Sets the ovens up when the kitchen loads (owner, 25 October 2026: ovens are bought one by one, any oven bakes any
/// recipe, an oven shows a note only while it bakes). Runs once per kitchen load, from the first oven's Awake:
///   - the note pictures are collected from the scene's own stickers (Choux on oven1, Bread on oven2, Cake and the
///     Cookies note on oven3) before anything is hidden, with Resources/UI/stickers/<food> as the fallback;
///   - the ovens are ordered top to bottom (spot1, spot2, spot3); the first SessionManager.OvensOwned are owned, the rest
///     stand dark with a lock and ignore taps;
///   - every note is hidden (a baking oven shows its own again in CheckBakingStateOnWake), the old per-oven
///     KitchenFoodGates are switched off (they would put the oven to sleep when "its" food is off the menu), and the
///     Cookies plate's tap stays off: Cookies bake in an oven like the rest.
/// Before 25 October this handed each oven one menu food (shared ovens, 16 October).
/// </summary>
public static class OvenAssigner
{
    private static int assignedFrame = -1;
    private static readonly Dictionary<string, Sprite> notes = new Dictionary<string, Sprite>();
    public static readonly string[] OvenFoods = { "Bread", "Choux", "Cake", "Cookies" };   // the picker's order

    public static void EnsureAssigned()
    {
        if (assignedFrame == Time.frameCount) return;
        assignedFrame = Time.frameCount;
        try { Assign(); }
        catch (System.Exception ex) { Debug.LogWarning("[OvenAssigner] " + ex.Message); }
    }

    /// The note picture of a recipe: the kitchen's own sticker for it, else the menu sticker.
    public static Sprite NoteFor(string food)
    {
        if (string.IsNullOrEmpty(food)) return null;
        if (notes.TryGetValue(food, out var s) && s != null) return s;
        s = Resources.Load<Sprite>("UI/stickers/" + food);
        if (s != null) notes[food] = s;
        return s;
    }

    /// The oven recipes on today's menu (bought, switched on, in season), in the picker's order.
    public static List<string> RecipesOnTheMenu()
    {
        var list = new List<string>();
        var sm = SessionManager.Instance;
        if (sm == null || !sm.HasActiveSession()) { list.AddRange(OvenFoods); return list; }   // no session (the kitchen opened on its own in the editor)
        foreach (var f in OvenFoods)
            if (SessionManager.StationOf(f) == "Oven" && sm.IsFoodItemActive(f)) list.Add(f);
        return list;
    }

    private static void Assign()
    {
        var ovens = new List<OvenKitchenBase>(Object.FindObjectsOfType<OvenKitchenBase>(true));
        if (ovens.Count == 0) return;
        ovens.Sort((a, b) => string.CompareOrdinal(SpotName(a), SpotName(b)));

        // The note pictures, by the food each sticker was painted for
        foreach (var oven in ovens)
        {
            var gate = oven.GetComponent<KitchenFoodGate>();
            var ownFood = gate != null ? gate.associatedFoodType?.Trim() : null;
            foreach (Transform child in oven.transform)
            {
                if (!child.name.StartsWith("sticker")) continue;
                var img = child.GetComponent<Image>(); if (img == null || img.sprite == null) continue;
                var childGate = child.GetComponent<KitchenFoodGate>();
                var food = childGate != null ? childGate.associatedFoodType?.Trim() : ownFood;
                if (!string.IsNullOrEmpty(food) && OvenKitchenBase.BakeTimes.ContainsKey(food) && !notes.ContainsKey(food)) notes[food] = img.sprite;
            }
        }

        var sm = SessionManager.Instance;
        var ownedCount = sm != null && sm.HasActiveSession() ? sm.OvensOwned : ovens.Count;   // no session: every oven works, as painted

        for (var i = 0; i < ovens.Count; i++)
        {
            var oven = ovens[i];
            var owned = i < ownedCount;
            oven.owned = owned;
            oven.food = "";
            var gate = oven.GetComponent<KitchenFoodGate>();
            if (gate != null) gate.enabled = false;   // the oven no longer belongs to one food
            foreach (Transform child in oven.transform)
            {
                if (child.name.StartsWith("sticker"))
                {
                    var childGate = child.GetComponent<KitchenFoodGate>();
                    if (childGate != null) { childGate.enabled = false; childGate.associatedFoodType = "none"; }   // the old Cookies note
                    child.gameObject.SetActive(false);
                }
            }
            // an oven not bought shows nothing at all (owner, 25 October: "just show nothing instead of a locked oven")
            oven.gameObject.SetActive(owned);
            Debug.Log($"[OvenAssigner] {SpotName(oven)} {(owned ? "owned" : "not bought: hidden")}");
        }

        // Cookies bake in an oven now: the plate on the shelf is a decoration, its tap does nothing
        foreach (var plate in Object.FindObjectsOfType<CookiesInKitchen>(true)) plate.enabled = false;
    }

    private static string SpotName(OvenKitchenBase o) => o.transform.parent != null ? o.transform.parent.name + "/" + o.name : o.name;
}

/// <summary>
/// The recipe picker (owner, 25 October 2026): a tap on an idle oven shows the notes of the oven foods on today's
/// menu as big stickers on a centred panel in the shop panel's style (the afternoon's ask; the morning's "no card, next to
/// the oven" look is gone); a tap on a note starts the bake there, a tap outside the panel closes the picker and nothing bakes. A recipe
/// already baking in another oven is greyed out. One picker at a time; it lives in the kitchen's canvas and goes
/// with the kitchen scene.
/// </summary>
public class OvenPicker : MonoBehaviour
{
    private static OvenPicker open;
    private OvenKitchenBase oven;
    // Owner, 25 October 2026, afternoon: "make the stickers bigger, it's like an overlay effect ... the only thing the player
    // can interact with, clicking anywhere else cancels it ... a transparent overlay behind the stickers, like the shop panel".
    // So: a centred panel in the shop panel's style (the built-in rounded UI sprite, white at 70 %), big notes in a grid of
    // three to a row, a full-screen blocker behind it; a tap outside the panel closes, a tap on the panel does nothing.
    private const float Tile = 260f, Gap = 24f, Pad = 36f; private const int PerRow = 3;

    public static void Open(OvenKitchenBase forOven, List<string> recipes)
    {
        Close();
        Canvas canvas = null;
        foreach (var c in Object.FindObjectsOfType<Canvas>())
            if (c.gameObject.scene.name == "KitchenScene") { canvas = c; break; }
        if (canvas == null || forOven == null) return;

        // a see-through blocker over the whole kitchen: a tap on it closes the picker
        var root = new GameObject("OvenPicker", typeof(RectTransform), typeof(Image), typeof(OvenPicker));
        root.transform.SetParent(canvas.transform, false); root.transform.SetAsLastSibling();
        var rrt = (RectTransform)root.transform; rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one; rrt.offsetMin = rrt.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f);
        var picker = root.GetComponent<OvenPicker>(); picker.oven = forOven; open = picker;
        var closer = root.AddComponent<PickerCloser>(); closer.picker = picker;

        // the panel, centred on the screen, in the shop panel's style
        var n = recipes.Count; var cols = Mathf.Min(n, PerRow); var rows = (n + PerRow - 1) / PerRow;
        var card = new GameObject("Card", typeof(RectTransform), typeof(Image)); card.transform.SetParent(root.transform, false);
        var crt = (RectTransform)card.transform; crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(cols * Tile + (cols - 1) * Gap + 2 * Pad, rows * Tile + (rows - 1) * Gap + 2 * Pad);
        crt.anchoredPosition = Vector2.zero;
        var panel = card.GetComponent<Image>(); panel.sprite = RoundedPanel(); panel.type = Image.Type.Sliced;
        panel.color = new Color(1f, 1f, 1f, 0.698f);                  // the shop panel's white at 70 %

        for (var i = 0; i < n; i++)
        {
            var recipe = recipes[i]; var col = i % PerRow; var row = i / PerRow;
            var inRow = Mathf.Min(PerRow, n - row * PerRow); var rowW = inRow * Tile + (inRow - 1) * Gap;   // a short last row is centred
            var tile = new GameObject("Note_" + recipe, typeof(RectTransform), typeof(Image), typeof(Button)); tile.transform.SetParent(card.transform, false);
            var trt = (RectTransform)tile.transform; trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f); trt.pivot = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = new Vector2(Tile, Tile);
            trt.anchoredPosition = new Vector2(-rowW * 0.5f + Tile * 0.5f + col * (Tile + Gap), -(Pad + Tile * 0.5f + row * (Tile + Gap)));
            trt.localRotation = Quaternion.Euler(0f, 0f, ((i * 7 + 3) % 9 - 4) * 1.5f);   // a small tilt each, as in the carousel
            var img = tile.GetComponent<Image>(); img.sprite = OvenAssigner.NoteFor(recipe); img.preserveAspect = true;
            var busy = OvenKitchenBase.IsRecipeBaking(recipe);
            img.color = busy ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
            var btn = tile.GetComponent<Button>(); btn.interactable = !busy;
            var chosen = recipe;
            btn.onClick.AddListener(() => picker.Pick(chosen));
        }
    }

    public static void Close()
    {
        if (open != null) Object.Destroy(open.gameObject);
        open = null;
    }

    /// A white rounded rectangle drawn once at runtime (the editor's built-in UISprite is not loadable in a build), sliced
    /// so it stretches to any panel size with the corners kept: the shop panel's look.
    private static Sprite rounded;
    private static Sprite RoundedPanel()
    {
        if (rounded != null) return rounded;
        const int S = 64, R = 20;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false); var px = new Color[S * S];
        for (var y = 0; y < S; y++)
            for (var x = 0; x < S; x++)
            {
                var cx = Mathf.Clamp(x, R, S - 1 - R); var cy = Mathf.Clamp(y, R, S - 1 - R);
                var d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                px[y * S + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(R - d + 0.5f));
            }
        tex.SetPixels(px); tex.Apply(); tex.filterMode = FilterMode.Bilinear;
        rounded = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(R + 2, R + 2, R + 2, R + 2));
        return rounded;
    }

    private void Pick(string recipe)
    {
        if (oven != null) oven.StartBake(recipe);
        Close();
    }

    /// A tap on the blocker (not on a note) closes the picker.
    private class PickerCloser : MonoBehaviour, IPointerClickHandler
    {
        public OvenPicker picker;
        public void OnPointerClick(PointerEventData e) { if (e.pointerCurrentRaycast.gameObject == gameObject) Close(); }
    }
}
