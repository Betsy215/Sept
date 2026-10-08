using UnityEngine;

/// <summary>
/// Drives the glow on the CloseKitchenButton.
/// Glows 3 times when a new customer walks in, nudging the player to go back.
/// Kitchen is loaded additively so CustomerManager from the game scene is reachable.
/// </summary>
public class CloseKitchenButtonGlowController : MonoBehaviour
{
    [Header("Debug")] public bool enableDebugLogs = true;

    private ButtonGlowEffect glowEffect;
    private CustomerManager customerManager;

    private void Awake()
    {
        glowEffect = GetComponent<ButtonGlowEffect>();
    }

    private void Start()
    {
        customerManager = FindObjectOfType<CustomerManager>();

        if (customerManager != null)
        {
            customerManager.OnCustomerSpawned += OnCustomerSpawned;
            DebugLog("Subscribed to CustomerManager.OnCustomerSpawned.");
        }
        else
        {
            DebugLog("CustomerManager not found — glow will not trigger.");
        }
    }

    private void OnCustomerSpawned(CustomerController customer)
    {
        DebugLog("Customer walked in — enabling glow.");
        if (glowEffect != null) glowEffect.EnableGlow(3);
    }

    private void OnDestroy()
    {
        if (customerManager != null)
            customerManager.OnCustomerSpawned -= OnCustomerSpawned;
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"[CloseKitchenButtonGlowController] {message}");
    }
}