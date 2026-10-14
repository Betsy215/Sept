using UnityEngine;

/// <summary>
/// Attach to any kitchen scene GameObject that should only be visible
/// when a specific food type has been purchased.
///
/// Works on KitchenItemWithTimer, OvenKitchenBase, UI panels, decorations —
/// anything. Leave associatedFoodType empty to always show.
///
/// Mirrors the LevelManager.ApplyServeableItemSettings() pattern.
/// </summary>
public class KitchenFoodGate : MonoBehaviour
{
    [Tooltip("Food type string as it appears in purchasedFoodItems (e.g. 'Burger'). Empty = always visible.")]
    public string associatedFoodType;

    [Tooltip("Ovens (owner, 14 October 2026): stay visible when the food is off; only the sticky note goes and the oven stops baking.")]
    public bool keepVisible = false;

    [Header("Debug")] public bool enableDebugLogs = true;

    private void Start()
    {
        ApplyVisibility();
    }

    public void ApplyVisibility()
    {
        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession())
        {
            DebugLog("No active session — staying visible (fail open).");
            return;
        }

        // Inspector strings pick up stray whitespace easily; purchases are matched by exact string.
        var foodType = associatedFoodType?.Trim() ?? "";
        var purchased = SessionManager.Instance.IsFoodItemActive(foodType);   // bought and switched on in the menu book
        if (keepVisible)
        {
            // The oven stays on the wall: its note shows only when the food is on, and its baking script sleeps when off
            foreach (Transform child in transform)
                if (child.name.StartsWith("sticker") && child.GetComponent<KitchenFoodGate>() == null) child.gameObject.SetActive(purchased);
            foreach (var b in GetComponents<MonoBehaviour>())
                if (b != this && !(b is UnityEngine.UI.Graphic)) b.enabled = purchased;
            DebugLog($"'{associatedFoodType}' on={purchased} → note {(purchased ? "shown" : "hidden")}, baking {(purchased ? "on" : "off")}");
            return;
        }
        gameObject.SetActive(purchased);
        DebugLog($"'{associatedFoodType}' purchased={purchased} → {(purchased ? "shown" : "hidden")}");
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"[KitchenFoodGate: {gameObject.name}] {message}");
    }
}