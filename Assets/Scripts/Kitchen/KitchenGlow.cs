using UnityEngine;
using UnityEngine.UI;

/// The kitchen's default glow on everything that can be tapped (owner, 18 November 2026: "pick a glow state in B, a
/// middle state, but without the stars, default glow is the static glow for actionable items in kitchen"): the coffee
/// bags, the apples, the blender, the juice, the coconuts, the waffle machine. Later the same evening: "change the coffee
/// bag glow to the one i picked for default glow" (their painted glow is taken out of the pictures:
/// Store/glow_default_2026-11-18/strip_glow.py) and "oven does not need glow". It is off while the thing cannot be used:
/// no apples or coconuts left, the waffle machine while it bakes.
/// Never while the kitchen is being arranged (the pieces are moved then, not used).
public static class KitchenGlow
{
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
    private class Entry { public Image image; public Transform glow; public Sprite sprite; public System.Func<bool> usable; }
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
            else if (t.GetComponent<CoffeeInKitchen>() != null) usable = () => true;
            else if (t.GetComponent<AppleInKitchen>() != null) usable = () => AppleInKitchen.Count > 0;
            else if (t.name == "juice" && t.GetComponent<JuiceInKitchen>() != null) usable = () => true;
            else if (t.GetComponent<CoconutInKitchen>() != null) { var c = t.GetComponent<CoconutInKitchen>(); usable = () => CoconutInKitchen.Count > 0 && c.GetComponent<Image>().enabled; }
            else if (t.GetComponent<WaffleAnim>() != null) { var w = t.GetComponent<WaffleAnim>(); usable = () => w != null && !w.IsBaking; }
            if (usable == null) continue;
            if (t.Find("GlowFxStars") != null) continue;   // a newly bought thing: it shows the "new" glow while arranging, not this one
            GlowFx.Default(img);
            var glow = t.Find("GlowFx"); if (glow == null) continue;
            var old = t.Find("Glow"); if (old != null && old != glow) old.gameObject.SetActive(false);   // the blender's painted halo of 15 November: replaced
            entries.Add(new Entry { image = img, glow = glow, sprite = img.sprite, usable = usable });
        }
        built = true;
    }

    private void Update()
    {
        if (!built) return;
        foreach (var e in entries)
        {
            if (e.image != null && e.image.sprite != null && e.image.sprite != e.sprite)   // the apples change picture as they are picked: the glow follows
            {
                e.sprite = e.image.sprite;
                if (e.glow != null) { e.glow.name = "GlowFxOld"; Destroy(e.glow.gameObject); }
                GlowFx.Default(e.image); e.glow = e.image.transform.Find("GlowFx");
            }
            if (e.glow != null) e.glow.gameObject.SetActive(!arranging && e.image != null && e.image.isActiveAndEnabled && e.usable());
        }
    }
}
