using System.Collections.Generic;
using UnityEngine;

public class KitchenButtonGlowController : MonoBehaviour
{
    [Header("Debug")] public bool enableDebugLogs = true;

    private ButtonGlowEffect glowEffect;

    // Exactly the items we subscribed to, so OnDestroy unsubscribes from the same set even if
    // some were deactivated (FindObjectsOfType skips inactive objects) in between.
    private readonly List<RefillableItem> subscribedItems = new();

    private void Awake()
    {
        glowEffect = GetComponent<ButtonGlowEffect>();
        if (glowEffect == null)
            Debug.LogWarning("[KitchenButtonGlowController] No ButtonGlowEffect on this object; glow disabled.");
    }

    private void Start()
    {
        // Condition 1: refillable hit 0. Include inactive items: foods the player has not bought yet
        // are inactive at Start and activated later by LevelManager.
        var refillables = FindObjectsOfType<RefillableItem>(true);
        foreach (var item in refillables)
        {
            if (item == null) continue;
            item.OnCountChanged += OnRefillableCountChanged;
            subscribedItems.Add(item);
        }

        // Condition 2: new food item purchased
        if (SessionManager.Instance != null)
            SessionManager.Instance.OnFoodItemPurchased += OnFoodItemPurchased;
    }

    private void OnFoodItemPurchased(string foodType)
    {
        DebugLog($"New food item purchased ({foodType}) — enabling glow.");
        if (glowEffect != null) glowEffect.EnableGlow(3);
    }

    private void OnDestroy()
    {
        foreach (var item in subscribedItems)
            if (item != null)
                item.OnCountChanged -= OnRefillableCountChanged;
        subscribedItems.Clear();

        if (SessionManager.Instance != null)
            SessionManager.Instance.OnFoodItemPurchased -= OnFoodItemPurchased;
    }

    private void OnRefillableCountChanged(int currentCount, int maxCount)
    {
        if (currentCount == 0)
        {
            DebugLog("Refillable hit 0 — enabling glow.");
            if (glowEffect != null) glowEffect.EnableGlow(3);
        }
    }


    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"[KitchenButtonGlowController] {message}");
    }
}