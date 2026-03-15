using UnityEngine;

public class KitchenButtonGlowController : MonoBehaviour
{
    [Header("Debug")] public bool enableDebugLogs = true;

    private ButtonGlowEffect glowEffect;

    private void Awake()
    {
        glowEffect = GetComponent<ButtonGlowEffect>();
    }

    private void Start()
    {
        // Condition 1: refillable hit 0
        var refillables = FindObjectsOfType<RefillableItem>();
        foreach (var item in refillables)
            item.OnCountChanged += OnRefillableCountChanged;

        // Condition 2: new food item purchased
        if (SessionManager.Instance != null)
            SessionManager.Instance.OnFoodItemPurchased += OnFoodItemPurchased;
    }

    private void OnFoodItemPurchased(string foodType)
    {
        DebugLog($"New food item purchased ({foodType}) — enabling glow.");
        glowEffect.EnableGlow(3);
    }

    private void OnDestroy()
    {
        var refillables = FindObjectsOfType<RefillableItem>();
        foreach (var item in refillables)
            item.OnCountChanged -= OnRefillableCountChanged;

        if (SessionManager.Instance != null)
            SessionManager.Instance.OnFoodItemPurchased -= OnFoodItemPurchased;
    }

    private void OnRefillableCountChanged(int currentCount, int maxCount)
    {
        if (currentCount == 0)
        {
            DebugLog("Refillable hit 0 — enabling glow.");
            glowEffect.EnableGlow(3);
        }
    }


    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"[KitchenButtonGlowController] {message}");
    }
}