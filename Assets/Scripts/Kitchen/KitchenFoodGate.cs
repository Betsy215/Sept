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

        var purchased = SessionManager.Instance.IsFoodItemPurchased(associatedFoodType);
        gameObject.SetActive(purchased);
        DebugLog($"'{associatedFoodType}' purchased={purchased} → {(purchased ? "shown" : "hidden")}");
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"[KitchenFoodGate: {gameObject.name}] {message}");
    }
}