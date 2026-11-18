using UnityEngine;
using UnityEngine.UI;

/// The kitchen's default glow on everything that can be tapped (owner, 18 November 2026: "pick a glow state in B, a
/// middle state, but without the stars, default glow is the static glow for actionable items in kitchen"): the ovens,
/// the blender, the juice, the coconuts, the waffle machine. It is off while the thing cannot be used: an oven that is
/// baking, locked or switched off in the menu book, a palm with no coconuts left, the waffle machine while it bakes.
/// The coffee bags and the apples keep the glow painted into their own pictures (the owner's own), so they get none.
/// Never while the kitchen is being arranged (the pieces are moved then, not used).
public static class KitchenGlow
{
    private static readonly string[] Things = { "oven1", "oven2", "oven3", "juice", "Coconuts" };

    /// Once per kitchen load, beside KitchenBlender.Apply.
    public static void Apply()
    {
        Transform bg = null;
        foreach (var canvas in Object.FindObjectsOfType<Canvas>())
            if (canvas.gameObject.scene.name == "KitchenScene") { bg = canvas.transform.Find("bg"); if (bg != null) break; }
        if (bg == null || bg.GetComponent<KitchenGlowRunner>() != null) return;
        bg.gameObject.AddComponent<KitchenGlowRunner>();
    }
}

public class KitchenGlowRunner : MonoBehaviour
{
    private struct Entry { public Image image; public Transform glow; public System.Func<bool> usable; }
    private readonly System.Collections.Generic.List<Entry> entries = new System.Collections.Generic.List<Entry>();
    private bool built, arranging;

    private System.Collections.IEnumerator Start()
    {
        yield return null;   // the blender, the arranged pieces and the ovens' owned / locked state are set by now
        arranging = KitchenArrange.Arranging();   // fixed for this visit: arranging ends only through the kitchen's Ready, which closes the kitchen
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            var img = t.GetComponent<Image>(); if (img == null || img.sprite == null) continue;
            System.Func<bool> usable = null;
            if (t.name == "Blender" && img.raycastTarget) usable = () => true;
            else if (t.name.StartsWith("oven") && t.GetComponent<OvenKitchenBase>() != null)
            {
                var oven = t.GetComponent<OvenKitchenBase>();
                usable = () => oven != null && oven.enabled && oven.owned && !oven.IsBaking;
            }
            else if (t.name == "juice" && t.GetComponent<JuiceInKitchen>() != null) usable = () => true;
            else if (t.GetComponent<CoconutInKitchen>() != null) { var c = t.GetComponent<CoconutInKitchen>(); usable = () => CoconutInKitchen.Count > 0 && c.GetComponent<Image>().enabled; }
            else if (t.GetComponent<WaffleAnim>() != null) { var w = t.GetComponent<WaffleAnim>(); usable = () => w != null && !w.IsBaking; }
            if (usable == null) continue;
            if (t.Find("GlowFxStars") != null) continue;   // a newly bought thing: it shows the "new" glow while arranging, not this one
            GlowFx.Default(img);
            var glow = t.Find("GlowFx"); if (glow == null) continue;
            var old = t.Find("Glow"); if (old != null && old != glow) old.gameObject.SetActive(false);   // the blender's painted halo of 15 November: replaced
            entries.Add(new Entry { image = img, glow = glow, usable = usable });
        }
        built = true;
    }

    private void Update()
    {
        if (!built) return;
        foreach (var e in entries)
            if (e.glow != null) e.glow.gameObject.SetActive(!arranging && e.image != null && e.image.isActiveAndEnabled && e.usable());
    }
}
