using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Advertisements;
using TMPro;

public class RewardedAdButton : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [Header("Ad Settings")] [SerializeField]
    private string _adUnitId = "Rewarded_iOS";

    [SerializeField] private AudioClip _rewardSFX;

    [Header("Reward Settings")] [SerializeField]
    private float _coinsPerAd = 5f;

    [Header("UI References (auto-found if empty)")] [SerializeField]
    private Button _button;

    [SerializeField] private TextMeshProUGUI _buttonLabel;
    [SerializeField] private TextMeshProUGUI _infoLabel;
    [SerializeField] private ShopManager _shopManager;

    private bool _adReady = false;

    #region Lifecycle

    private void Start()
    {
        if (_button == null) _button = GetComponent<Button>();
        if (_shopManager == null) _shopManager = FindObjectOfType<ShopManager>();

        _button.onClick.AddListener(OnButtonClicked);
        SetButtonReady(false);

        if (_infoLabel != null)
            _infoLabel.text = $"Watch an ad to earn ${_coinsPerAd:F0} coins!";

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
        Advertisement.Load(_adUnitId, this);
    }

    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        if (adUnitId != _adUnitId) return;
        _adReady = true;
        SetButtonReady(true);
        Debug.Log("[RewardedAdButton] Ad ready.");
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.LogWarning($"[RewardedAdButton] Load failed: {error} - {message}");
        SetButtonReady(false);
    }

    #endregion

    #region Ad Showing

    private void OnButtonClicked()
    {
        if (!_adReady) return;
        SetButtonReady(false);
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
            LoadAd();
        }
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.LogWarning($"[RewardedAdButton] Show failed: {error} - {message}");
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
        if (_rewardSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(_rewardSFX);
        if (SessionManager.Instance != null && SessionManager.Instance.HasActiveSession())
        {
            SessionManager.Instance.AddScoreImmediately(_coinsPerAd);
            Debug.Log($"[RewardedAdButton] +{_coinsPerAd} coins awarded.");
        }

        if (_shopManager != null)
            _shopManager.RefreshScoreDisplay();

        if (_infoLabel != null)
            _infoLabel.text = $"+${_coinsPerAd:F0} coins added! Watch another?";

        LoadAd();
    }

    #endregion

    #region UI Helpers

    private void SetButtonReady(bool ready)
    {
        if (_button != null) _button.interactable = ready;
        if (_buttonLabel != null)
            _buttonLabel.text = ready ? $"Watch Ad - Earn ${_coinsPerAd:F0} Coins!" : "Loading ad...";
    }

    #endregion
}