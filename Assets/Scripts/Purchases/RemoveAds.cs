using System;
using System.Collections;
using UnityEngine;
#if UNITY_PURCHASING
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

/// Remove Ads, a one-time purchase (owner, 9 November 2026, $1.99): switches off the interstitial between days.
/// The rewarded ads (daily treat, the coin button) stay, the player asks for those. Ownership is a PlayerPrefs flag set
/// when the App Store confirms the purchase or a restore; InterstitialAdService checks it. The store side needs the
/// product created in App Store Connect under ProductId as a non-consumable and the Paid Apps Agreement signed.
/// Built on Unity IAP (com.unity.purchasing); without the package the code compiles to a shop card that says "soon".
public static class RemoveAds
{
    public const string ProductId = "com.BetzzzGame.FoodTruckCafe.removeads";
    const string OwnedKey = "ftc_ads_removed";
    public const string FallbackPrice = "$1.99";

    public static bool Owned => PlayerPrefs.GetInt(OwnedKey, 0) == 1;
    public static event Action Changed;
    /// Localized price from the store once it answered, else the fallback.
    public static string PriceText => RemoveAdsStore.LocalizedPrice ?? FallbackPrice;
    public static bool StoreReady => RemoveAdsStore.Ready;
    /// True when the App Store really offers the product (or it is already owned). The shop shows the $ tab, the card and
    /// the restore line only then, so a build is safe to ship before the product exists in App Store Connect (11 November
    /// 2026: the Paid Apps Agreement was not signed yet) and lights up by itself once it does.
    public static bool Available => Owned || RemoveAdsStore.ProductAvailable;

    public static void Init() => RemoveAdsStore.Ensure();
    public static void Buy(Action<bool, string> done) => RemoveAdsStore.Ensure().Buy(done);
    public static void Restore(Action<bool, string> done) => RemoveAdsStore.Ensure().Restore(done);

    internal static void SetOwned()
    {
        if (Owned) return;
        PlayerPrefs.SetInt(OwnedKey, 1); PlayerPrefs.Save();
        Debug.Log("[RemoveAds] owned");
        Changed?.Invoke();
    }
}

/// The hidden store listener; one per app, survives scene loads.
public class RemoveAdsStore : MonoBehaviour
#if UNITY_PURCHASING
    , IDetailedStoreListener
#endif
{
    static RemoveAdsStore instance;
    public static string LocalizedPrice { get; private set; }
    public static bool Ready { get; private set; }
    public static bool ProductAvailable { get; private set; }
    Action<bool, string> pending;

    public static RemoveAdsStore Ensure()
    {
        if (instance != null) return instance;
        var go = new GameObject("RemoveAdsStore");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<RemoveAdsStore>();
        return instance;
    }

#if UNITY_PURCHASING
    IStoreController controller;
    IExtensionProvider extensions;
    bool initializing;

    void Start() { StartCoroutine(InitWhenServicesReady()); }

    IEnumerator InitWhenServicesReady()
    {
        // Unity IAP wants Unity Gaming Services up first; AnalyticsLog starts them at launch
        var waited = 0f;
        while (Unity.Services.Core.UnityServices.State != Unity.Services.Core.ServicesInitializationState.Initialized && waited < 10f)
        { waited += Time.unscaledDeltaTime; yield return null; }
        if (initializing || controller != null) yield break;
        initializing = true;
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct(RemoveAds.ProductId, ProductType.NonConsumable);
        UnityPurchasing.Initialize(this, builder);
    }

    public void OnInitialized(IStoreController c, IExtensionProvider e)
    {
        controller = c; extensions = e; Ready = true; initializing = false;
        var p = c.products.WithID(RemoveAds.ProductId);
        ProductAvailable = p != null && p.availableToPurchase;
        if (p != null)
        {
            if (!string.IsNullOrEmpty(p.metadata?.localizedPriceString)) LocalizedPrice = p.metadata.localizedPriceString;
            if (p.hasReceipt) RemoveAds.SetOwned();     // bought on another device, or before a reinstall
        }
        Debug.Log("[RemoveAds] store ready, price " + LocalizedPrice);
    }

    public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, null);
    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        initializing = false; Ready = false; ProductAvailable = false;
        Debug.LogWarning($"[RemoveAds] store init failed: {error} {message}");
    }

    public void Buy(Action<bool, string> done)
    {
        if (RemoveAds.Owned) { done?.Invoke(true, "Already yours"); return; }
        if (controller == null) { done?.Invoke(false, "The store is not ready yet"); StartCoroutine(InitWhenServicesReady()); return; }
        pending = done;
        controller.InitiatePurchase(RemoveAds.ProductId);
    }

    public void Restore(Action<bool, string> done)
    {
        if (RemoveAds.Owned) { done?.Invoke(true, "Ads are already off"); return; }
        if (extensions == null) { done?.Invoke(false, "The store is not ready yet"); StartCoroutine(InitWhenServicesReady()); return; }
        var apple = extensions.GetExtension<IAppleExtensions>();
        if (apple == null) { done?.Invoke(false, "Restore is only on iPhone"); return; }
        apple.RestoreTransactions((ok, msg) =>
        {
            // a real restore arrives through ProcessPurchase and sets the flag; here only report
            done?.Invoke(ok && RemoveAds.Owned, RemoveAds.Owned ? "Ads are off again" : (ok ? "Nothing to restore" : "Restore failed: " + msg));
        });
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        if (args.purchasedProduct.definition.id == RemoveAds.ProductId)
        {
            RemoveAds.SetOwned();
            var cb = pending; pending = null; cb?.Invoke(true, "Ads are off. Thank you!");
        }
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason) => Fail(reason.ToString());
    public void OnPurchaseFailed(Product product, PurchaseFailureDescription d) => Fail(d != null ? d.reason.ToString() : "failed");
    void Fail(string why)
    {
        var cb = pending; pending = null;
        Debug.LogWarning("[RemoveAds] purchase failed: " + why);
        cb?.Invoke(false, why == "UserCancelled" ? "" : "Purchase failed");
    }
#else
    public void Buy(Action<bool, string> done) => done?.Invoke(false, "Coming soon");
    public void Restore(Action<bool, string> done) => done?.Invoke(false, "Coming soon");
#endif
}
