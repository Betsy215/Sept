using UnityEngine;

/// Table decorations that make customers more patient (12 October 2026). Each one owned adds one second to every
/// customer's patience (one petal on the daisy). They are Shop items of type Upgrade with these keys, saved like the
/// other upgrades (level 2 = owned). Sprites live in Resources/Decor; they are placed on the table's back row by
/// LevelManager.ApplyLevelSettings, outside the food slots, so they never block a food.
public static class PatienceDecor
{
    public const float SecondsEach = 1f;

    // key, sprite, world position, scale (sprites are 680 px at 100 px per unit)
    static readonly (string key, string sprite, Vector3 pos, float scale)[] Items =
    {
        ("Decor_Vase", "Decor/vase", new Vector3(-1.85f, -0.75f, 0f), 0.184f),
        ("Decor_Bouquet", "Decor/bouquet", new Vector3(1.8f, -0.6f, 0f), 0.213f),
    };

    public static bool Owned(string key)
    {
        var sm = SessionManager.Instance;
        return sm != null && sm.HasActiveSession() && sm.GetFoodUpgradeLevel(key) > 1;
    }

    /// Extra seconds of patience from the decorations owned.
    public static float BonusSeconds()
    {
        var n = 0;
        foreach (var it in Items) if (Owned(it.key)) n++;
        return n * SecondsEach;
    }

    /// Puts the owned decorations on the table (removes any placed earlier in this scene).
    public static void Place()
    {
        var old = GameObject.Find("PatienceDecor");
        if (old != null) Object.Destroy(old);
        var root = new GameObject("PatienceDecor");
        foreach (var it in Items)
        {
            if (!Owned(it.key)) continue;
            var sprite = Resources.Load<Sprite>(it.sprite);
            if (sprite == null) { Debug.LogWarning($"PatienceDecor: no sprite {it.sprite}"); continue; }
            var go = new GameObject(it.key);
            go.transform.SetParent(root.transform, false);
            go.transform.position = it.pos;
            go.transform.localScale = Vector3.one * it.scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "FoodItems";
            sr.sortingOrder = 1;                       // under the foods (order 2) if they are dragged over it
        }
    }
}
