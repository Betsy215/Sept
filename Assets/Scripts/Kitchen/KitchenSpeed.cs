using UnityEngine;

/// The kitchen upgrade (owner, 19 November 2026: a star shop item, 15 stars, "kitchen upgrade", "kitchen cooks faster";
/// "kitchen item cook speed, for example oven, blender, and future mixer ... make sure this change will apply to
/// everything, the speed goes up, not too much, but slightly, but not too slightly"). Once it is bought every kitchen
/// wait is a quarter shorter: the ovens' bake times, the things that grow or come back (apples, coconuts, juice,
/// coffee bags, cookies), the blender's and the waffle machine's steps. A new kitchen machine joins by multiplying its
/// own time by Factor.
public static class KitchenSpeed
{
    public const string ItemName = "StarKitchen";   // starts with "Star": the shop files it under the star tab
    public const int StarPrice = 15;
    public const float Faster = 0.75f;              // a wait takes three quarters of its time

    public static bool Owned => SessionManager.Instance != null && SessionManager.Instance.OwnsStarItem(ItemName);
    /// What a kitchen wait is multiplied by: 1, or Faster once the upgrade is owned.
    public static float Factor => Owned ? Faster : 1f;
}
