using UnityEngine;
using UnityEngine.Advertisements;

#if UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

public class AdsInitializer : MonoBehaviour, IUnityAdsInitializationListener
{
    [SerializeField] private string _iOSGameId = "6074412";
    [SerializeField] private bool _testMode = false;

    private void Awake()
    {
#if UNITY_IOS
        if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
            ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            ATTrackingStatusBinding.RequestAuthorizationTracking();
#endif
        InitializeAds();
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
    }
}