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
    private const float RetryDelaySeconds = 10f;

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

        var waited = 0f;
        while (!Advertisement.isInitialized && waited < 20f)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            waited += 0.5f;
        }

        if (!Advertisement.isInitialized)
        {
            loading = false;
            Debug.LogWarning("[Interstitial] Unity Ads not initialized; no interstitials this session.");
            yield break;
        }

        Advertisement.Load(AdUnitId, this);
    }

    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        if (adUnitId != AdUnitId) return;
        loading = false;
        adReady = true;
        Debug.Log("[Interstitial] Ad ready.");
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        if (adUnitId != AdUnitId) return;
        loading = false;
        adReady = false;
        Debug.LogWarning($"[Interstitial] Load failed: {error} - {message}. Retrying in {RetryDelaySeconds:F0}s.");
        StartCoroutine(RetryLoad());
    }

    private IEnumerator RetryLoad()
    {
        yield return new WaitForSecondsRealtime(RetryDelaySeconds);
        Preload();
    }

    #endregion

    #region Showing

    /// <summary>
    /// Called when a level is completed and the player moves on. Shows an ad if one is due and
    /// ready, then runs <paramref name="continuation"/>. Otherwise runs it immediately.
    /// </summary>
    public void ShowAfterLevelThen(Action continuation)
    {
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
