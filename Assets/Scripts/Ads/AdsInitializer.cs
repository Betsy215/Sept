using System.Collections;
using UnityEngine;
using UnityEngine.Advertisements;

#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

/// <summary>
/// MainMenu: asks for App Tracking Transparency permission, then initialises Unity Ads.
///
/// The request waits until the app is active. In a release build Unity starts inside
/// didFinishLaunching, before iOS considers the app active, and iOS silently ignores a tracking
/// request made then, so the prompt never appeared on first launch. (In the March 2026 build the
/// prompt was skipped anyway because Info.plist had no NSUserTrackingUsageDescription;
/// Assets/Editor/IOSPostBuild.cs adds it now, so the timing matters.)
///
/// The sequence runs on the persistent InterstitialAdService object so leaving MainMenu early
/// cannot cut it short and leave ads uninitialised for the session.
///
/// Keeping Unity Ads started (23 November 2026, after a player's two visits without a single ad request: Unity Ads
/// had not started, and nothing tried again). Before, the start was one call; when it failed, the whole visit had no
/// ads until the start menu was opened again. Now the start is kept up for as long as the game runs:
///  - a failed start is tried again after 5, 15, 30, 60 and then every 120 seconds (every 5 minutes when Unity
///    reports an ad blocker); with no network no try is spent, the next one waits for the network;
///  - a start that never answers within 45 seconds counts as lost and may be tried again;
///  - anything that wants an ad (a day's end, the shop's coin ad, the Daily Treats ad, coming back to the game)
///    calls <see cref="Ensure"/>, which brings a waiting try forward, at most one every 5 seconds;
///  - the listener is a plain object that lives as long as the game, not this scene object;
///  - when the start succeeds, <see cref="OnReady"/> fires and the first between-days ad is loaded.
/// Unity's SDK (4.17) accepts a second Initialize after a failed one. Nothing here blocks or shows anything.
/// </summary>
public class AdsInitializer : MonoBehaviour, IUnityAdsInitializationListener
{
    [SerializeField] private string _iOSGameId = "6074412";
    [SerializeField] private bool _testMode = false;

    private static bool sequenceRunning;

#if UNITY_IOS && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern bool FTC_IsTestFlight();
#endif

    /// TestFlight and Xcode builds carry a sandbox receipt (Plugins/iOS/TestFlightCheck.mm). Store builds do not.
    internal static bool IsTestFlightBuild()
    {
#if UNITY_IOS && !UNITY_EDITOR
        try { return FTC_IsTestFlight(); } catch (System.Exception) { return false; }
#else
        return false;
#endif
    }

    /// Test mode: the Inspector flag, or any TestFlight build (owner, 14 October 2026: our own testing must not
    /// count as real impressions on the Unity dashboard). Test ads show instead of real ones; nothing is counted.
    private bool TestMode => _testMode || IsTestFlightBuild();

    private void Awake()
    {
        // settings for every later try (the start menu is the only place that has them)
        gameId = _iOSGameId; testMode = TestMode; haveSettings = true;
        if (Advertisement.isInitialized || sequenceRunning) return;
        if (firstTryMade) { Ensure(); return; }           // back at the start menu with ads still not started
        sequenceRunning = true;
        Debug.Log($"Unity Ads test mode: {testMode}");
        InterstitialAdService.Instance.StartCoroutine(RequestTrackingThenInitialize());
    }

    private static IEnumerator RequestTrackingThenInitialize()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
            ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
        {
            // Wait for the app to be active, then a moment more so the splash has gone.
            var waited = 0f;
            while (!Application.isFocused && waited < 5f)
            {
                yield return null;
                waited += Time.unscaledDeltaTime;
            }

            yield return new WaitForSecondsRealtime(0.5f);
            ATTrackingStatusBinding.RequestAuthorizationTracking();

            // ios-support has no callback; poll until the player answers (capped so ads still start).
            waited = 0f;
            while (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
                   ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED && waited < 15f)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                waited += 0.25f;
            }
        }
#endif
        yield return null;

        sequenceRunning = false; firstTryMade = true;
        startedAt = Time.realtimeSinceStartup;
        nextTryAt = 0f;
        StartKeeping();                                    // the first try, and every later one
    }

    // ---- keeping Unity Ads started ----

    private static string gameId; private static bool testMode, haveSettings, firstTryMade;
    private static bool trying, keeping, announced, blocker;
    private static float tryStartedAt, nextTryAt, lastTryAt = -999f, startedAt;
    private static int failures, tries;
    private static readonly Listener listener = new Listener();
    private const float TryTimeout = 45f, MinSecondsBetweenTries = 5f;

    /// Unity Ads is started and ads can be loaded.
    public static bool Ready => Advertisement.isInitialized;
    /// Fires once, on the main thread, when Unity Ads has started (also when that happens late, after retries).
    public static event System.Action OnReady;

    /// Something wants an ad: if Unity Ads is not started and no try is out, the next try is brought forward (not more
    /// often than every 5 seconds). Cheap, and safe to call from anywhere, any number of times.
    public static void Ensure()
    {
        if (!haveSettings || !firstTryMade || sequenceRunning) return;   // the first try (after the tracking question) is still to come
        if (Advertisement.isInitialized) { Announce(); return; }
        var now = Time.realtimeSinceStartup;
        if (!trying && nextTryAt > now) nextTryAt = Mathf.Max(now, lastTryAt + MinSecondsBetweenTries);
        StartKeeping();
    }

    private static void StartKeeping()
    {
        if (keeping) return;
        keeping = true;
        InterstitialAdService.Instance.StartCoroutine(Keep());
    }

    /// One light loop for as long as Unity Ads is not started: a look once a second, a try when one is due.
    private static IEnumerator Keep()
    {
        if (!Advertisement.isSupported) { keeping = false; yield break; }   // no ads on this platform: nothing to keep
        while (!Advertisement.isInitialized)
        {
            var now = Time.realtimeSinceStartup;
            if (trying && now - tryStartedAt > TryTimeout) { trying = false; Failed("no answer"); }
            // no network: the try waits for it (a try then only fails), but never longer than a minute, in case the
            // phone reports no network wrongly; the very first try always goes
            var offline = Application.internetReachability == NetworkReachability.NotReachable && tries > 0 && now - lastTryAt < 60f;
            if (!trying && now >= nextTryAt && !offline)
            {
                trying = true; tryStartedAt = lastTryAt = now; tries++;
                try { Advertisement.Initialize(gameId, testMode, listener); }
                catch (System.Exception ex) { trying = false; Failed(ex.Message); }
            }
            yield return new WaitForSecondsRealtime(1f);
        }
        keeping = false; trying = false;
        Announce();                                        // also when the start succeeded without its callback reaching us
    }

    private static void Failed(string why)
    {
        failures++;
        var wait = blocker ? 300f : failures == 1 ? 5f : failures == 2 ? 15f : failures == 3 ? 30f : failures == 4 ? 60f : 120f;
        nextTryAt = Time.realtimeSinceStartup + wait;
        Debug.Log($"Unity Ads start failed ({why}); try {tries}, next in {wait:F0} s");
    }

    private static void Announce()
    {
        if (announced) return;
        announced = true;
        Debug.Log($"Unity Ads started after {tries} tries, {failures} failures");
        AnalyticsLog.AdsStarted(tries, Time.realtimeSinceStartup - startedAt);
        try { OnReady?.Invoke(); } catch (System.Exception ex) { Debug.LogWarning("Unity Ads ready listener: " + ex.Message); }
        // Have the first between-level interstitial ready before the player finishes Day 1
        InterstitialAdService.Instance.Preload();
    }

    private class Listener : IUnityAdsInitializationListener
    {
        public void OnInitializationComplete() { trying = false; Announce(); }
        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            blocker = error == UnityAdsInitializationError.AD_BLOCKER_DETECTED;
            trying = false; Failed(error + " - " + message);
        }
    }

    public void InitializeAds() { gameId = _iOSGameId; testMode = TestMode; haveSettings = true; firstTryMade = true; Ensure(); StartKeeping(); }

    // the scene object's own callbacks are no longer the ones Unity calls (the static listener is); kept for the interface
    public void OnInitializationComplete() { listener.OnInitializationComplete(); }
    public void OnInitializationFailed(UnityAdsInitializationError error, string message) { listener.OnInitializationFailed(error, message); }
}
