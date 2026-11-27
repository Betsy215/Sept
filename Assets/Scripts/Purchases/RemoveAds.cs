using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

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

/// 15 stars for the star shop, bought with money (owner, 15 November 2026: "i want to add another product in dollar
/// tab, use dollar to buy stars"; their pick in the report: one pack, 15 stars, $0.99). It can be bought again and again
/// (a consumable): every confirmed purchase adds the stars to the save (SessionManager.AddBoughtStars). "Restore
/// purchases" does not bring these back. The shop shows the card only while the App Store offers the product, so a
/// build is safe to ship before the product exists.
public static class StarPack
{
    public const string ProductId = "com.BetzzzGame.FoodTruckCafe.stars15";
    public const int Stars = 15;
    public const string FallbackPrice = "$0.99";
    public static string PriceText => RemoveAdsStore.StarPackPrice ?? FallbackPrice;
    public static bool Available => RemoveAdsStore.StarPackAvailable;
    public static void Buy(Action<bool, string> done) => RemoveAdsStore.Ensure().BuyStars(done);
}

/// The hidden store listener; one per app, survives scene loads.
/// 14 November 2026: the store code used to sit inside `#if UNITY_PURCHASING`. Nothing ever defined that symbol (the
/// purchasing package is in the manifest, but the symbol only comes with the editor's Services switch, which is off), so
/// every build up to 28 shipped the empty "Coming soon" stub and the $ tab could never show. The guard is gone: the
/// package is a plain dependency of the game.
public class RemoveAdsStore : MonoBehaviour, IDetailedStoreListener
{
    static RemoveAdsStore instance;
    public static string LocalizedPrice { get; private set; }
    public static bool Ready { get; private set; }
    public static bool ProductAvailable { get; private set; }
    public static string StarPackPrice { get; private set; }
    public static bool StarPackAvailable { get; private set; }
    Action<bool, string> pending, pendingStars;
    const string DoneKey = "ftc_starpack_done";   // the last transactions already paid out, so none is ever paid twice

    public static RemoveAdsStore Ensure()
    {
        if (instance != null) return instance;
        var go = new GameObject("RemoveAdsStore");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<RemoveAdsStore>();
        return instance;
    }

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
        builder.AddProduct(StarPack.ProductId, ProductType.Consumable);
        UnityPurchasing.Initialize(this, builder);
    }

    /// The store's price as the game's fonts can draw it. Delius and beachday have no ₱, ₫, ฿ (or ₹, ₩, ...) and no
    /// fallback font, so such a sign showed as a box; those prices are written with the currency code instead
    /// ("PHP 49", "THB 39", "VND 25,000"). $, €, £, ¥, Rp, RM and the rest stay exactly as the App Store sends them.
    static string Drawable(ProductMetadata m)
    {
        var s = m.localizedPriceString.Replace('\u202F', ' ').Replace('\u2009', ' ');
        foreach (char ch in s)
        {
            if (ch < 128 || "\u00A0€£¥".IndexOf(ch) >= 0) continue;
            if (string.IsNullOrEmpty(m.isoCurrencyCode)) return s;
            var v = m.localizedPrice;
            return m.isoCurrencyCode + " " + v.ToString(v == decimal.Truncate(v) ? "#,0" : "#,0.00", System.Globalization.CultureInfo.InvariantCulture);
        }
        return s;
    }

    public void OnInitialized(IStoreController c, IExtensionProvider e)
    {
        controller = c; extensions = e; Ready = true; initializing = false;
        var p = c.products.WithID(RemoveAds.ProductId);
        ProductAvailable = p != null && p.availableToPurchase;
        if (p != null)
        {
            if (!string.IsNullOrEmpty(p.metadata?.localizedPriceString)) LocalizedPrice = Drawable(p.metadata);
            if (p.hasReceipt) RemoveAds.SetOwned();     // bought on another device, or before a reinstall
        }
        var s = c.products.WithID(StarPack.ProductId);
        StarPackAvailable = s != null && s.availableToPurchase;
        if (s != null && !string.IsNullOrEmpty(s.metadata?.localizedPriceString)) StarPackPrice = Drawable(s.metadata);
        Debug.Log("[RemoveAds] store ready, price " + LocalizedPrice + ", star pack " + (StarPackAvailable ? StarPackPrice : "not offered"));
    }

    public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, null);
    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        initializing = false; Ready = false; ProductAvailable = false; StarPackAvailable = false;
        Debug.LogWarning($"[RemoveAds] store init failed: {error} {message}");
    }

    public void Buy(Action<bool, string> done)
    {
        if (RemoveAds.Owned) { done?.Invoke(true, "Already yours"); return; }
        if (controller == null) { done?.Invoke(false, "The store is not ready yet"); StartCoroutine(InitWhenServicesReady()); return; }
        pending = done;
        controller.InitiatePurchase(RemoveAds.ProductId);
    }

    public void BuyStars(Action<bool, string> done)
    {
        if (controller == null) { done?.Invoke(false, "The store is not ready yet"); StartCoroutine(InitWhenServicesReady()); return; }
        pendingStars = done;
        controller.InitiatePurchase(StarPack.ProductId);
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
        else if (args.purchasedProduct.definition.id == StarPack.ProductId)
        {
            // no save to put the stars in yet (a purchase finished while the game was closed): the store sends it again
            if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession()) return PurchaseProcessingResult.Pending;
            var id = args.purchasedProduct.transactionID ?? ""; var done = PlayerPrefs.GetString(DoneKey, "");
            if (id.Length == 0 || !("|" + done + "|").Contains("|" + id + "|"))
            {
                SessionManager.Instance.AddBoughtStars(StarPack.Stars);
                if (id.Length > 0)
                {
                    var list = new System.Collections.Generic.List<string>(done.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)) { id };
                    while (list.Count > 20) list.RemoveAt(0);
                    PlayerPrefs.SetString(DoneKey, string.Join("|", list)); PlayerPrefs.Save();
                }
            }
            var cb = pendingStars; pendingStars = null; cb?.Invoke(true, StarPack.Stars + " stars added. Thank you!");
        }
        return PurchaseProcessingResult.Complete;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason) => Fail(product, reason.ToString());
    public void OnPurchaseFailed(Product product, PurchaseFailureDescription d) => Fail(product, d != null ? d.reason.ToString() : "failed");
    void Fail(Product product, string why)
    {
        var stars = product != null && product.definition != null && product.definition.id == StarPack.ProductId;
        var cb = stars ? pendingStars : pending; if (stars) pendingStars = null; else pending = null;
        Debug.LogWarning("[RemoveAds] purchase failed: " + why);
        cb?.Invoke(false, why == "UserCancelled" ? "" : "Purchase failed");
    }
}
