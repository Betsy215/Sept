using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class BalatroRhythm : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private RectTransform _rectTransform;
    private Button _button;
    private bool _hasButton;

    [Header("Rhythm")] [Tooltip("Beats per minute. 80 = calm, 120 = energetic.")] [SerializeField]
    private float _bpm = 100f;

    [Header("Heartbeat")]
    [Tooltip("How much the button pops in scale on each beat. 0.08 = 8% bigger.")]
    [SerializeField]
    private float _beatScalePop = 0.08f;

    [Tooltip("How quickly the scale snaps back after the pop.")] [SerializeField]
    private float _beatScaleDecay = 10f;

    [Header("Press Sink")] [Tooltip("How much the button shrinks when pressed. 0.1 = 10% smaller.")] [SerializeField]
    private float _pressScaleDown = 0.1f;

    [Tooltip("How fast the button sinks on press.")] [SerializeField]
    private float _pressSpeed = 20f;

    [Tooltip("How fast the button springs back on release.")] [SerializeField]
    private float _releaseSpeed = 12f;

    private Vector3 _initialScale;

    // Heartbeat
    private float _beatTimer;
    private float _beatScaleExtra;

    // Press
    private bool _isPressed;
    private float _pressScale = 1f;
    private float _pressTarget = 1f;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _button = GetComponent<Button>();
        _hasButton = _button != null;

        _initialScale = _rectTransform.localScale;
        _beatTimer = 60f / _bpm * Random.Range(0.1f, 0.9f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_hasButton && !_button.interactable) return;
        _isPressed = true;
        _pressTarget = 1f - _pressScaleDown;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPressed = false;
        _pressTarget = 1f;
    }

    private void Update()
    {
        if (_hasButton && !_button.interactable)
        {
            if (_beatScaleExtra != 0f || _pressScale != 1f)
            {
                _beatScaleExtra = 0f;
                _pressScale = 1f;
                _rectTransform.localScale = _initialScale;
            }

            return;
        }

        // ── Heartbeat ─────────────────────────────
        _beatTimer -= Time.unscaledDeltaTime;
        if (_beatTimer <= 0f)
        {
            _beatTimer += 60f / _bpm;
            _beatScaleExtra = _beatScalePop;
        }

        if (_beatScaleExtra != 0f)
            _beatScaleExtra = Mathf.MoveTowards(_beatScaleExtra, 0f, _beatScaleDecay * Time.unscaledDeltaTime);

        // ── Press sink ────────────────────────────
        if (_pressScale != _pressTarget)
        {
            var speed = _isPressed ? _pressSpeed : _releaseSpeed;
            _pressScale = Mathf.MoveTowards(_pressScale, _pressTarget, speed * Time.unscaledDeltaTime);
        }

        // ── Apply both together ───────────────────
        // Only write if something actually changed
        if (_beatScaleExtra != 0f || _pressScale != 1f)
            _rectTransform.localScale = _initialScale * (_pressScale + _beatScaleExtra);
    }
}