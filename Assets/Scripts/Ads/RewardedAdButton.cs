using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.UI;

/// <summary>
/// "Watch an ad to earn coins" button in the Shop. Loads a Unity Ads rewarded placement,
/// retries with backoff if loading fails, and only reports a reward when coins were really added.
/// </summary>
public class RewardedAdButton : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [Header("Ad Settings")] [SerializeField]
    private string _adUnitId = "Rewarded_iOS";

    [SerializeField] private AudioClip _rewardSFX;

    [Tooltip("Seconds before the first retry after a failed load. Doubles each time up to the max.")]
    [SerializeField] private float _retryDelaySeconds = 3f;

    [SerializeField] private float _maxRetryDelaySeconds = 30f;

    [Header("Reward Settings")] [SerializeField]
    private float _coinsPerAd = 5f;

    [Header("UI References (auto-found if empty)")] [SerializeField]
    private Button _button;

    [SerializeField] private TextMeshProUGUI _buttonLabel;
    [SerializeField] private TextMeshProUGUI _infoLabel;
    [SerializeField] private ShopManager _shopManager;

    private bool _adReady;
    private float _nextRetryDelay;
    private Coroutine _loadRoutine;

    #region Lifecycle

    private void Start()
    {
        if (_button == null) _button = GetComponent<Button>();
        if (_shopManager == null) _shopManager = FindObjectOfType<ShopManager>();

        _button.onClick.AddListener(OnButtonClicked);
        SetButtonReady(false);

        if (_infoLabel != null)
            _infoLabel.text = $"Watch an ad to earn ${_coinsPerAd:F0} coins!";

        _nextRetryDelay = _retryDelaySeconds;
        LoadAd();
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnButtonClicked);
    }

    #endregion

    #region Ad Loading

    private void LoadAd()
    {
        _adReady = false;
        SetButtonReady(false);

        if (_loadRoutine != null) StopCoroutine(_loadRoutine);
        _loadRoutine = StartCoroutine(LoadWhenInitialized());
    }

    private IEnumerator LoadWhenInitialized()
    {
        // Ads are initialized in the main menu; wait briefly in case that is still in flight.
        var waited = 0f;
        while (!Advertisement.isInitialized && waited < 15f)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            waited += 0.5f;
        }

        _loadRoutine = null;

        if (!Advertisement.isInitialized)
        {
            Debug.LogWarning("[RewardedAdButton] Unity Ads is not initialized; button stays disabled.");
            SetButtonReady(false, "Ads unavailable");
            yield break;
        }

        Advertisement.Load(_adUnitId, this);
    }

    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        if (adUnitId != _adUnitId) return;
        _adReady = true;
        _nextRetryDelay = _retryDelaySeconds;
        SetButtonReady(true);
        Debug.Log("[RewardedAdButton] Ad ready.");
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        if (adUnitId != _adUnitId) return;
        Debug.LogWarning($"[RewardedAdButton] Load failed: {error} - {message}. Retrying in {_nextRetryDelay:F0}s.");
        SetButtonReady(false);

        if (_loadRoutine != null) StopCoroutine(_loadRoutine);
        _loadRoutine = StartCoroutine(RetryAfter(_nextRetryDelay));
        _nextRetryDelay = Mathf.Min(_nextRetryDelay * 2f, _maxRetryDelaySeconds);
    }

    private IEnumerator RetryAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        _loadRoutine = null;
        LoadAd();
    }

    #endregion

    #region Ad Showing

    private void OnButtonClicked()
    {
        if (!_adReady) return;
        if (_shopManager != null && _shopManager.IsPurchasePopupOpen) return;
        _adReady = false;
        SetButtonReady(false, "Showing ad...");
        Advertisement.Show(_adUnitId, this);
    }

    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState state)
    {
        if (adUnitId != _adUnitId) return;

        if (state == UnityAdsShowCompletionState.COMPLETED)
        {
            GrantReward();
        }
        else
        {
            Debug.Log("[RewardedAdButton] Skipped - no reward.");
            if (_infoLabel != null) _infoLabel.text = "Watch the whole ad to earn coins.";
        }

        LoadAd();
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        if (adUnitId != _adUnitId) return;
        Debug.LogWarning($"[RewardedAdButton] Show failed: {error} - {message}");
        if (_infoLabel != null) _infoLabel.text = "The ad couldn't play. Try again.";
        LoadAd();
    }

    public void OnUnityAdsShowStart(string adUnitId)
    {
    }

    public void OnUnityAdsShowClick(string adUnitId)
    {
    }

    #endregion

    #region Reward Logic

    private void GrantReward()
    {
        var granted = SessionManager.Instance != null && SessionManager.Instance.AddScoreImmediately(_coinsPerAd);

        if (granted)
        {
            if (_rewardSFX != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(_rewardSFX);

            if (_infoLabel != null)
                _infoLabel.text = $"+${_coinsPerAd:F0} coins added! Watch another?";

            Debug.Log($"[RewardedAdButton] +{_coinsPerAd} coins awarded.");
        }
        else
        {
            Debug.LogWarning("[RewardedAdButton] Ad completed but there is no active session to credit.");
            if (_infoLabel != null)
                _infoLabel.text = "Couldn't add coins: no active game.";
        }

        if (_shopManager != null)
            _shopManager.RefreshScoreDisplay();
    }

    #endregion

    #region UI Helpers

    private void SetButtonReady(bool ready, string notReadyText = "Loading ad...")
    {
        if (_button != null) _button.interactable = ready;
        if (_buttonLabel != null)
            _buttonLabel.text = ready ? $"Watch Ad - Earn ${_coinsPerAd:F0} Coins!" : notReadyText;
    }

    #endregion
}
