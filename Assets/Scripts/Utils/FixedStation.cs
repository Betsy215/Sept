using UnityEngine;

/// The chapter's fixed food has its own place on the table (owner, 16 October): the coffee machine and its cups sit
/// on a small wooden station at the bottom left, with some air from the left and the bottom edges, and cannot be
/// dragged. Chapter 2's ice cream will use the same station once it exists. Built at runtime by GamePhaseManager at
/// the start of every arrangement phase; saved positions never apply to the fixed items.
public static class FixedStation
{
    public const float MarginLeft = 0.22f;     // world units beyond the table's own padding
    public const float MarginBottom = 0.18f;
    public const float Gap = 0.12f;            // between the machine and the cups

    /// True for the table objects that belong to the chapter's fixed food
    public static bool IsFixed(Component c)
    {
        if (c == null) return false;
        var fixedFood = SessionManager.FixedFood.TryGetValue(SessionManager.CurrentChapter, out var f) ? f : null;
        if (string.IsNullOrEmpty(fixedFood)) return false;
        var item = c as ServeableItem; if (item != null) return item.GetFoodType() == fixedFood;
        return fixedFood == "Coffee" && c is CoffeeMachine;
    }

    /// Places the fixed food's objects at the bottom left of the table and puts the station under them
    public static void Place(TableLayer table, ServeableItem[] items)
    {
        if (table == null) return;
        var fixedFood = SessionManager.FixedFood.TryGetValue(SessionManager.CurrentChapter, out var f) ? f : null;
        if (string.IsNullOrEmpty(fixedFood)) return;
        var bounds = table.InteractiveBounds; if (bounds.size.x <= 0f) return;

        ServeableItem food = null;
        if (items != null) foreach (var it in items) if (it != null && it.gameObject.activeInHierarchy && it.GetFoodType() == fixedFood) food = it;
        var machine = fixedFood == "Coffee" ? Object.FindObjectOfType<CoffeeMachine>() : null;
        if (food == null && machine == null) return;

        // the objects in a row from the left: the machine first, then the food; each stands on the station's floor
        var x = bounds.min.x + MarginLeft;
        var floorY = bounds.min.y + MarginBottom;
        var left = x; var right = x; var top = floorY;
        foreach (var t in new[] { machine != null ? machine.transform : null, food != null ? food.transform : null })
        {
            if (t == null) continue;
            var sr = t.GetComponent<SpriteRenderer>(); var b = sr != null ? sr.bounds : new Bounds(t.position, Vector3.one);
            var halfW = b.extents.x; var halfH = b.extents.y;
            var dx = t.position.x - b.center.x; var dy = t.position.y - b.center.y;   // the pivot's offset from the picture's centre
            t.position = new Vector3(x + halfW + dx, floorY + halfH + dy, t.position.z);
            right = x + 2f * halfW; top = Mathf.Max(top, floorY + 2f * halfH);
            x = right + Gap;
        }

        // the station: a wooden tray under the row, a little wider than it, its floor at the row's floor
        var sprite = Resources.Load<Sprite>("Table/coffee_station");
        if (sprite == null) return;
        var go = GameObject.Find("FixedStation"); if (go == null) go = new GameObject("FixedStation");
        var r = go.GetComponent<SpriteRenderer>(); if (r == null) r = go.AddComponent<SpriteRenderer>();   // never ?? on a Unity object: a missing component is not C# null
        r.sprite = sprite; r.sortingLayerName = "FoodItems"; r.sortingOrder = -1;
        var width = (right - left) + 0.5f; var scale = width / sprite.bounds.size.x;
        go.transform.localScale = Vector3.one * scale;
        var h = sprite.bounds.size.y * scale;
        go.transform.position = new Vector3((left + right) / 2f, floorY + h * 0.30f, 0.06f);   // the tray's top edge sits behind the objects' feet
    }
}
