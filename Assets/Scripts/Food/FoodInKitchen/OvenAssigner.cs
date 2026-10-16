using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared ovens (owner, 16 October 2026): "ovens are all the same, which oven doesn't matter; each recipe has its own
/// cooking time". When the kitchen loads, the oven foods that are on the menu (bought, switched on, in season, station
/// Oven, in the order they are on) are handed to the ovens top to bottom (spot1, spot2, spot3). Each oven's first
/// "sticker" child becomes its note and takes the picture of that food's note (the pictures are collected from the
/// scene's own stickers before anything moves: Choux on oven1, Bread on oven2, Cake and Cookies on oven3); the oven's
/// KitchenFoodGate is pointed at the food so the note shows and the baking wakes exactly as before. Spare stickers
/// and the Cookies plate's tap are switched off: Cookies bake in an oven like the rest. Runs once per kitchen load,
/// from the first oven's Awake.
/// </summary>
public static class OvenAssigner
{
    private static int assignedFrame = -1;

    public static void EnsureAssigned()
    {
        if (assignedFrame == Time.frameCount) return;
        assignedFrame = Time.frameCount;
        try { Assign(); }
        catch (System.Exception ex) { Debug.LogWarning("[OvenAssigner] " + ex.Message); }
    }

    private static void Assign()
    {
        var ovens = new List<OvenKitchenBase>(Object.FindObjectsOfType<OvenKitchenBase>(true));
        if (ovens.Count == 0) return;
        ovens.Sort((a, b) => string.CompareOrdinal(SpotName(a), SpotName(b)));

        // The note pictures, by the food each sticker was painted for
        var notes = new Dictionary<string, Sprite>();
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
                if (!string.IsNullOrEmpty(food) && !notes.ContainsKey(food)) notes[food] = img.sprite;
            }
        }

        // The oven foods that are on today, in the order they were switched on
        var sm = SessionManager.Instance;
        var on = new List<string>();
        if (sm != null && sm.HasActiveSession() && sm.GetCurrentSession() != null)
        {
            var list = sm.GetCurrentSession().enabledFoodItems ?? sm.GetCurrentSession().purchasedFoodItems;
            if (list != null)
                foreach (var f in list)
                    if (SessionManager.StationOf(f) == "Oven" && sm.IsFoodItemActive(f) && !on.Contains(f)) on.Add(f);
        }
        else return;   // no session (the kitchen opened on its own in the editor): leave the scene as painted

        for (var i = 0; i < ovens.Count; i++)
        {
            var oven = ovens[i];
            var food = i < on.Count ? on[i] : "";
            oven.food = food;
            var gate = oven.GetComponent<KitchenFoodGate>();
            if (gate != null) { gate.associatedFoodType = string.IsNullOrEmpty(food) ? "none" : food; gate.keepVisible = true; }
            var first = true;
            foreach (Transform child in oven.transform)
            {
                if (!child.name.StartsWith("sticker")) continue;
                var childGate = child.GetComponent<KitchenFoodGate>();
                if (childGate != null) { childGate.associatedFoodType = "none"; child.gameObject.SetActive(false); continue; }   // a spare note (the old Cookies note)
                if (!first) { child.gameObject.SetActive(false); continue; }
                first = false;
                var img = child.GetComponent<Image>();
                if (img != null && !string.IsNullOrEmpty(food) && notes.TryGetValue(food, out var sprite)) { img.sprite = sprite; img.preserveAspect = true; }
                child.gameObject.SetActive(!string.IsNullOrEmpty(food));
            }
            Debug.Log($"[OvenAssigner] {SpotName(oven)} bakes {(string.IsNullOrEmpty(food) ? "nothing" : food)}");
        }

        // Cookies bake in an oven now: the plate on the shelf is a decoration, its tap does nothing
        foreach (var plate in Object.FindObjectsOfType<CookiesInKitchen>(true)) plate.enabled = false;
    }

    private static string SpotName(OvenKitchenBase o) => o.transform.parent != null ? o.transform.parent.name + "/" + o.name : o.name;
}
