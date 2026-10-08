using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class BalatroWobble : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Button _button;

    [Header("Position (Jitter)")] [SerializeField]
    private float _posXIntensity = 2.0f;

    [SerializeField] private float _posYIntensity = 1.0f;
    [SerializeField] private float _posFreq = 15.0f;

    [Header("Rotation (Tilt)")] [SerializeField]
    private float _rotIntensity = 1.5f;

    [SerializeField] private float _rotFreq = 8.0f;

    [Header("Scale (Pulse)")] [SerializeField]
    private float _scaleIntensity = 0.02f;

    [SerializeField] private float _scaleFreq = 3.0f;

    private Vector2 _initialAnchoredPosition;
    private Vector3 _initialLocalEuler;
    private Vector3 _initialScale;
    private float _timeCounter;

    // Cached to avoid Unity's expensive null check every frame
    private bool _hasButton;

    // Tracks reset state so we don't write to the transform every frame when disabled
    private bool _isReset;

    // Wrap threshold — keeps _timeCounter small to avoid float precision loss
    private const float TIME_WRAP = 1000f;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _button = GetComponent<Button>();
        _hasButton = _button != null;

        _initialAnchoredPosition = _rectTransform.anchoredPosition;
        _initialLocalEuler = _rectTransform.localEulerAngles; // local, not world
        _initialScale = _rectTransform.localScale;

        _timeCounter = Random.Range(0f, TIME_WRAP);
    }

    private void Update()
    {
        if (_hasButton && !_button.interactable)
        {
            // Only write to transform once when first becoming disabled
            if (!_isReset)
            {
                ResetTransform();
                _isReset = true;
            }

            return;
        }

        _isReset = false;

        _timeCounter += Time.unscaledDeltaTime;

        // Wrap to prevent float precision loss over long sessions
        if (_timeCounter > TIME_WRAP)
            _timeCounter -= TIME_WRAP;

        // Position
        var posX = _initialAnchoredPosition.x + Mathf.Sin(_timeCounter * _posFreq) * _posXIntensity;
        var posY = _initialAnchoredPosition.y + Mathf.Cos(_timeCounter * (_posFreq * 1.1f)) * _posYIntensity;
        _rectTransform.anchoredPosition = new Vector2(posX, posY);

        // Rotation — localEulerAngles avoids world-space hierarchy traversal
        var rotZ = _initialLocalEuler.z + Mathf.Sin(_timeCounter * _rotFreq) * _rotIntensity;
        _rectTransform.localEulerAngles = new Vector3(_initialLocalEuler.x, _initialLocalEuler.y, rotZ);

        // Scale
        var scalePulse = 1.0f + Mathf.Sin(_timeCounter * _scaleFreq) * _scaleIntensity;
        _rectTransform.localScale = _initialScale * scalePulse;
    }

    // Don't leave the button frozen at a random jitter offset when it is hidden mid-wobble
    private void OnDisable()
    {
        ResetTransform();
    }

    public void ResetTransform()
    {
        _rectTransform.anchoredPosition = _initialAnchoredPosition;
        _rectTransform.localEulerAngles = _initialLocalEuler;
        _rectTransform.localScale = _initialScale;
    }
}