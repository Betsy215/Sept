using UnityEngine;
using UnityEngine.Advertisements;

public class AdsInitializer : MonoBehaviour, IUnityAdsInitializationListener
{
    [SerializeField] private string _iOSGameId = "6074412";
    [SerializeField] private bool _testMode = true; // KEEP THIS TRUE WHILE TESTING!

    private void Awake()
    {
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
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
    }
}