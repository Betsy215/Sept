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
/// </summary>
public class AdsInitializer : MonoBehaviour, IUnityAdsInitializationListener
{
    [SerializeField] private string _iOSGameId = "6074412";
    [SerializeField] private bool _testMode = false;

    private static bool sequenceRunning;

    private void Awake()
    {
        if (Advertisement.isInitialized || sequenceRunning) return;
        sequenceRunning = true;
        InterstitialAdService.Instance.StartCoroutine(RequestTrackingThenInitialize(_iOSGameId, _testMode, this));
    }

    private static IEnumerator RequestTrackingThenInitialize(string gameId, bool testMode,
        IUnityAdsInitializationListener listener)
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

        if (!Advertisement.isInitialized && Advertisement.isSupported)
            Advertisement.Initialize(gameId, testMode, listener);

        sequenceRunning = false;
    }

    public void InitializeAds()
    {
        if (!Advertisement.isInitialized && Advertisement.isSupported)
            Advertisement.Initialize(_iOSGameId, _testMode, this);
    }

    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads successfully initialized!");

        // Have the first between-level interstitial ready before the player finishes Day 1
        InterstitialAdService.Instance.Preload();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
        sequenceRunning = false;
    }
}
