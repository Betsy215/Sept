using UnityEngine;

/// The chapter's fixed food has its own place on the table (owner, 16 October): the coffee machine and its cups sit
/// on a small wooden station at the bottom left, with some air from the left and the bottom edges, and cannot be
/// dragged. Chapter 2's ice cream will use the same station once it exists. Built at runtime by GamePhaseManager at
/// the start of every arrangement phase; saved positions never apply to the fixed items.
public static class FixedStation
{
    public const float MarginLeft = 0.22f;     // world units beyond the table's own padding
    public const float MarginBottom = 0.18f;
    public const float Gap = 0.18f;            // between the machine and the cups

    /// True for the table objects that belong to the chapter's fixed food
    public static bool IsFixed(Component c)
    {
        if (c == null) return false;
        var fixedFood = SessionManager.FixedFood.TryGetValue(SessionManager.CurrentChapter, out var f) ? f : null;
        if (string.IsNullOrEmpty(fixedFood)) return false;
        var item = c as ServeableItem; if (item != null) return item.GetFoodType() == fixedFood;
        return fixedFood == "Coffee" && c is CoffeeMachine;
    }

    /// The object the player drags to move the whole station: the coffee machine in chapter 1, the ice cream station itself in chapter 2
    public static bool IsHandle(Component c)
    {
        if (c == null) return false;
        if (c is CoffeeMachine) return true;
        var item = c as ServeableItem; return item != null && item.GetFoodType() == "IceCream" && IsFixed(c);
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

        // the objects in a row from the left: the machine first, then the cups; each stands with its drawn feet on the
        // tray's board. The pictures have empty padding, so the drawn part is measured (fractions of the sprite box:
        // machine x 0.44 y 0.485 w 0.77 h 0.89; cups x 0.51 y 0.53 w 0.55 h 0.29, from the png alphas, 16 October)
        var x = bounds.min.x + MarginLeft;
        var floorY = bounds.min.y + MarginBottom;        // the tray's bottom edge
        var feetY = floorY + 0.16f;                       // where the objects stand, inside the tray
        var left = x; var right = x; var top = feetY;
        var row = new[] { machine != null ? machine.transform : null, food != null ? food.transform : null };
        var frac = new[] { new Vector4(0.44f, 0.485f, 0.77f, 0.89f), new Vector4(0.51f, 0.53f, 0.55f, 0.29f) };
        if (fixedFood == "IceCream") frac[1] = new Vector4(0.5f, 0.5f, 1f, 1f);          // the station box picture has no padding (17 October)
        for (var i = 0; i < row.Length; i++)
        {
            var t = row[i]; if (t == null) continue;
            var sr = t.GetComponent<SpriteRenderer>(); var b = sr != null ? sr.bounds : new Bounds(t.position, Vector3.one);
            var fr = frac[i];
            var dc = new Vector2(b.min.x + fr.x * b.size.x, b.min.y + fr.y * b.size.y);        // the drawn part's centre
            var halfW = fr.z * b.size.x / 2f; var halfH = fr.w * b.size.y / 2f;
            var dx = t.position.x - dc.x; var dy = t.position.y - dc.y;                       // the pivot's offset from the drawn centre
            var nudge = i == 1 && machine != null ? new Vector2(-0.10f, -0.06f) : Vector2.zero;   // the cups a little down and left, deeper on the tray (owner, 16 October)
            t.position = new Vector3(x + halfW + dx + nudge.x, feetY + halfH + dy + nudge.y, t.position.z);
            right = x + 2f * halfW; top = Mathf.Max(top, feetY + 2f * halfH);
            x = right + Gap;
        }

        // the station: a wooden tray under the row, a little wider than it, its floor at the row's floor.
        // Chapter 2's ice cream box stands on its own (owner, 17 October: no board under it); an old tray object goes
        if (fixedFood == "IceCream") { var old = GameObject.Find("FixedStation"); if (old != null) old.SetActive(false); return; }
        var sprite = Resources.Load<Sprite>("Table/coffee_station");
        if (sprite == null) return;
        var go = GameObject.Find("FixedStation"); if (go == null) go = new GameObject("FixedStation");
        var r = go.GetComponent<SpriteRenderer>(); if (r == null) r = go.AddComponent<SpriteRenderer>();   // never ?? on a Unity object: a missing component is not C# null
        r.sprite = sprite; r.sortingLayerName = "FoodItems"; r.sortingOrder = -1;
        var width = (right - left) + 0.3f; var scale = width / sprite.bounds.size.x;          // hugs the machine and the cups with a little air
        go.transform.localScale = new Vector3(scale, scale * 0.62f, 1f);                       // a flat tray, not a board
        var h = sprite.bounds.size.y * scale * 0.62f;
        go.transform.position = new Vector3((left + right) / 2f, floorY + h / 2f, 0.06f);      // the tray's bottom edge on floorY; the objects' feet inside it
        // the station moves as one: the machine is the handle, the cups and the tray follow (owner, 16 October)
        var grp = go.GetComponent<StationGroup>(); if (grp == null) grp = go.AddComponent<StationGroup>();
        if (machine != null) grp.Bind(machine.transform, food != null ? food.transform : null, go.transform);
        else if (food != null) grp.Bind(food.transform, go.transform);                 // chapter 2: the ice cream station is its own handle
    }
}
