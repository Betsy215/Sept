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
        var floorY = bounds.min.y + MarginBottom;        // the tray's bottom edge (its front face and shadow included)
        var feetY = floorY + 0.16f;                       // where the objects stand; raised onto the board once the tray's size is known (18 October)
        var left = x; var right = x; var top = feetY;
        var placed = new System.Collections.Generic.List<(Transform t, float yOff)>();
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
            placed.Add((t, halfH + dy + nudge.y));
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
        go.transform.localScale = new Vector3(scale, scale, 1f);                               // drawn at the 40-degree view (18 October): no squash
        var h = sprite.bounds.size.y * scale;
        go.transform.position = new Vector3((left + right) / 2f, floorY + h / 2f, 0.06f);      // the tray's bottom edge on floorY
        // the objects stand on the board: its front edge is 0.324 of the picture up from the bottom (coffee_station40.py)
        feetY = floorY + 0.34f * h;
        foreach (var (t, yOff) in placed) t.position = new Vector3(t.position.x, feetY + yOff, t.position.z);
        // the station moves as one: the machine is the handle, the cups and the tray follow (owner, 16 October)
        var grp = go.GetComponent<StationGroup>(); if (grp == null) grp = go.AddComponent<StationGroup>();
        if (machine != null) grp.Bind(machine.transform, food != null ? food.transform : null, go.transform);
        else if (food != null) grp.Bind(food.transform, go.transform);                 // chapter 2: the ice cream station is its own handle
    }

    /// After the fixed food is placed, no other item may sit on it (owner, 18 October: the coconut tray overlapped the
    /// ice cream station). Every other live item whose circle (the drag system's own radius rule) touches a fixed item
    /// is moved to the nearest free spot on the table, searched on a grid from where it was.
    public static void PushOthersOff(TableLayer table, ServeableItem[] items)
    {
        if (table == null || items == null) return;
        var fixedFood = SessionManager.FixedFood.TryGetValue(SessionManager.CurrentChapter, out var f) ? f : null;
        if (string.IsNullOrEmpty(fixedFood)) return;
        var bounds = table.InteractiveBounds; if (bounds.size.x <= 0f) return;
        var live = new System.Collections.Generic.List<ServeableItem>();
        foreach (var it in items) if (it != null && it.gameObject.activeInHierarchy) live.Add(it);
        var machine = fixedFood == "Coffee" ? Object.FindObjectOfType<CoffeeMachine>() : null;
        var fixedParts = new System.Collections.Generic.List<Component>();
        foreach (var it in live) if (it.GetFoodType() == fixedFood) fixedParts.Add(it);
        if (machine != null) fixedParts.Add(machine);
        var tray = GameObject.Find("FixedStation"); if (tray != null && tray.activeInHierarchy) fixedParts.Add(tray.transform);
        if (fixedParts.Count == 0) return;
        foreach (var it in live)
        {
            if (it.GetFoodType() == fixedFood) continue;
            if (!Touches(it.transform, Radius(it), fixedParts, live, it)) continue;
            var start = it.transform.position; Vector3? best = null; var bestD = float.MaxValue;
            for (var y = bounds.min.y + 0.3f; y <= bounds.max.y - 0.3f; y += 0.25f)
                for (var x = bounds.min.x + 0.3f; x <= bounds.max.x - 0.3f; x += 0.25f)
                {
                    var p = new Vector3(x, y, start.z); var d = (p - start).sqrMagnitude; if (d >= bestD) continue;
                    it.transform.position = p;
                    if (!Touches(it.transform, Radius(it), fixedParts, live, it)) { best = p; bestD = d; }
                }
            it.transform.position = best ?? start;
            if (best != null) Debug.Log($"FixedStation: moved {it.GetFoodType()} off the station to {best.Value}");
        }
    }

    static float Radius(Component c)
    {
        var d = c.GetComponent<DraggableFood>(); if (d != null && d.ItemRadius > 0f) return d.ItemRadius;
        var sr = c.GetComponent<SpriteRenderer>(); if (sr != null && sr.sprite != null) return Mathf.Max(sr.bounds.size.x, sr.bounds.size.y) / 2f;
        return 0.4f;
    }

    static bool Touches(Transform t, float r, System.Collections.Generic.List<Component> fixedParts, System.Collections.Generic.List<ServeableItem> live, ServeableItem self)
    {
        foreach (var p in fixedParts)
        {
            var sr = p.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null) { var b = sr.bounds; b.Expand(new Vector3(0.1f, 0.1f, 10f)); if (b.Contains(new Vector3(t.position.x, t.position.y, b.center.z)) || Circle(b, t.position, r)) return true; }
            else if (Vector2.Distance(p.transform.position, t.position) < r + 0.5f) return true;
        }
        foreach (var o in live)
            if (o != self && o.GetFoodType() != null && Vector2.Distance(o.transform.position, t.position) < r + Radius(o) + 0.05f) return true;
        return false;
    }

    static bool Circle(Bounds b, Vector3 c, float r)
    {
        var nx = Mathf.Clamp(c.x, b.min.x, b.max.x); var ny = Mathf.Clamp(c.y, b.min.y, b.max.y);
        return (nx - c.x) * (nx - c.x) + (ny - c.y) * (ny - c.y) < r * r;
    }
}
