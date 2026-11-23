using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Advertisements;

/// <summary>
/// Full-screen interstitial shown between a finished level and the Shop.
/// Lives on a persistent GameObject created on first use, so no scene wiring is needed.
///
/// Rules:
/// - The game never waits on an ad that is not ready: if nothing is loaded, the caller's
///   continuation runs at once and the player goes straight to the Shop.
/// - The continuation runs exactly once, whether the ad completed, was skipped, failed, or
///   never reported back (a watchdog fires after <see cref="ShowTimeoutSeconds"/>).
/// - The next ad is loaded right after one is shown, so it is ready by the next level.
/// - Ads are only counted when the ad unit exists on the Unity Ads dashboard; if it does not,
///   loads fail quietly and the game behaves as if ads were off.
/// </summary>
public class InterstitialAdService : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    public const string AdUnitId = "Interstitial_iOS";

    /// Show an ad after every N completed levels. 1 = after every level.
    public static int ShowEveryNLevels = 1;

    /// Never show two interstitials closer together than this, in real seconds.
    public static float MinSecondsBetweenAds = 45f;

    private const float ShowTimeoutSeconds = 12f;
    private const float RetryDelaySeconds = 10f;      // after the first failed load; doubles up to MaxRetryDelaySeconds
    private const float MaxRetryDelaySeconds = 120f;
    private const float LoadTimeoutSeconds = 30f;     // a load that never answers counts as failed

    private static InterstitialAdService instance;

    public static InterstitialAdService Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("[InterstitialAdService]");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<InterstitialAdService>();
            }

            return instance;
        }
    }

    private bool adReady;
    private bool loading;
    private int loadFailures;             // in a row: stretches the wait before the next load
    private int loadId;                   // the load that is out (an answer to an older one is not waited for any more)
    private int levelsSinceLastAd;
    private float lastAdShownAt = -999f;
    private Action pendingContinuation;
    private Coroutine watchdog;

    #region Loading

    /// Start loading an ad. Safe to call repeatedly; it waits for initialization.
    public void Preload()
    {
        if (adReady || loading) return;
        StartCoroutine(LoadWhenInitialized());
    }

    private IEnumerator LoadWhenInitialized()
    {
        loading = true;
        AdsInitializer.Ensure();          // Unity Ads not started yet (or its start failed): have it tried again now

        var waited = 0f;
        while (!Advertisement.isInitialized && waited < 20f)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            waited += 0.5f;
        }

        if (!Advertisement.isInitialized)
        {
            // Not for the whole visit any more (23 November 2026): AdsInitializer keeps trying, and calls Preload
            // itself when Unity Ads starts; every day's end asks again too.
            loading = false;
            Debug.LogWarning("[Interstitial] Unity Ads not started yet; the ad loads when it is.");
            yield break;
        }

        var id = ++loadId;
        Advertisement.Load(AdUnitId, this);

        // a load that never answers must not leave `loading` set for good (no further load would ever start)
        yield return new WaitForSecondsRealtime(LoadTimeoutSeconds);
        if (loading && id == loadId)
        {
            loading = false;
            Debug.LogWarning("[Interstitial] Load did not answer in time.");
            StartCoroutine(RetryLoad());
        }
    }

    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        if (adUnitId != AdUnitId) return;
        loading = false;
        adReady = true;
        loadFailures = 0;
        Debug.Log("[Interstitial] Ad ready.");
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        if (adUnitId != AdUnitId) return;
        loading = false;
        adReady = false;
        loadId++;                         // this load is answered: its timeout has nothing to do
        Debug.LogWarning($"[Interstitial] Load failed: {error} - {message}.");
        StartCoroutine(RetryLoad());
    }

    /// The next load after a failed one: 10 s, then 20, 40, 80, and every 120 s from there (it was every 10 s without
    /// end, which asks Unity for nothing useful while there is no ad or no network).
    private IEnumerator RetryLoad()
    {
        var wait = Mathf.Min(RetryDelaySeconds * Mathf.Pow(2f, Mathf.Min(loadFailures, 6)), MaxRetryDelaySeconds);
        loadFailures++;
        yield return new WaitForSecondsRealtime(wait);
        Preload();
    }

    /// Back in the game after a while in the background: Unity Ads is looked after again, and a missing ad is loaded.
    private void OnApplicationPause(bool paused)
    {
        if (paused) return;
        AdsInitializer.Ensure();
        if (Advertisement.isInitialized) Preload();
    }

    #endregion

    #region Showing

    /// <summary>
    /// Called when a level is completed and the player moves on. Shows an ad if one is due and
    /// ready, then runs <paramref name="continuation"/>. Otherwise runs it immediately.
    /// </summary>
    public void ShowAfterLevelThen(Action continuation)
    {
        // TestFlight builds show only test ads, which never count (sandbox receipt), so the between-level ad is
        // skipped there altogether (owner, 25 October 2026: "bypass the intermediate every level ads in TestFlight").
        // The rewarded coin ad is untouched. Store builds are unchanged.
        if (AdsInitializer.IsTestFlightBuild())
        {
            Debug.Log("[Interstitial] TestFlight build: between-level ad skipped.");
            continuation?.Invoke();
            return;
        }
        if (RemoveAds.Owned) { continuation?.Invoke(); return; }   // Remove Ads bought (9 November 2026)
        levelsSinceLastAd++;

        var due = levelsSinceLastAd >= Mathf.Max(1, ShowEveryNLevels);
        var spaced = Time.realtimeSinceStartup - lastAdShownAt >= MinSecondsBetweenAds;

        if (!due || !spaced || !adReady || pendingContinuation != null)
        {
            if (due && !adReady) Preload();
            continuation?.Invoke();
            return;
        }

        pendingContinuation = continuation;
        adReady = false;
        levelsSinceLastAd = 0;
        lastAdShownAt = Time.realtimeSinceStartup;

        watchdog = StartCoroutine(Watchdog());
        Advertisement.Show(AdUnitId, this);
    }

    private IEnumerator Watchdog()
    {
        yield return new WaitForSecondsRealtime(ShowTimeoutSeconds);
        Debug.LogWarning("[Interstitial] No show callback in time; continuing without the ad.");
        Finish();
    }

    public void OnUnityAdsShowStart(string adUnitId)
    {
        // The ad is on screen; the watchdog is no longer needed. Completion or failure will follow.
        if (watchdog != null)
        {
            StopCoroutine(watchdog);
            watchdog = null;
        }
    }

    public void OnUnityAdsShowClick(string adUnitId)
    {
    }

    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState state)
    {
        if (adUnitId != AdUnitId) return;
        AnalyticsLog.AdWatched("interstitial", state == UnityAdsShowCompletionState.COMPLETED);
        Finish();
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        if (adUnitId != AdUnitId) return;
        Debug.LogWarning($"[Interstitial] Show failed: {error} - {message}");
        Finish();
    }

    /// Runs the pending continuation exactly once and queues the next load.
    private void Finish()
    {
        if (watchdog != null)
        {
            StopCoroutine(watchdog);
            watchdog = null;
        }

        var continuation = pendingContinuation;
        pendingContinuation = null;

        Preload();

        continuation?.Invoke();
    }

    #endregion
}
