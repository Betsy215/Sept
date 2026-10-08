using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Main-menu Play button. Starts a new session.
/// If a session with progress already exists, the first tap only shows a warning and
/// a second tap within a few seconds actually starts over, so a mis-tap cannot wipe
/// a saved game. Use Continue to resume.
/// </summary>
public class ClickPlay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Image _img;
    public Sprite _default, _pressed;
    public AudioClip _compressClip, _uncompressClip;
    public AudioSource _source;
    public string _sceneName;

    [Header("Start-over confirmation")]
    [Tooltip("Optional label shown when a saved game would be erased. Created at runtime if left empty.")]
    public TextMeshProUGUI confirmLabel;

    public string confirmMessage = "This erases your saved game.\nTap PLAY again to start over.";
    public float confirmWindowSeconds = 4f;

    private bool armed;
    private bool launching;
    private Coroutine confirmRoutine;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (launching) return;
        if (_img != null && _pressed != null) _img.sprite = _pressed;
        if (_source != null && _compressClip != null) _source.PlayOneShot(_compressClip);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (launching) return;
        if (_img != null && _default != null) _img.sprite = _default;
        if (_source != null && _uncompressClip != null) _source.PlayOneShot(_uncompressClip);

        if (HasSavedProgress() && !armed)
        {
            ArmConfirmation();
            return;
        }

        launching = true;
        HideConfirmation();
        StartCoroutine(WaitForDelay(2));
    }

    private static bool HasSavedProgress()
    {
        var sm = SessionManager.Instance;
        if (sm == null || !sm.HasActiveSession()) return false;
        var session = sm.GetCurrentSession();
        return session != null && (session.levelsCompleted >= 1 || session.totalScore > 0f);
    }

    private void ArmConfirmation()
    {
        armed = true;
        if (confirmLabel == null) confirmLabel = CreateLabel();
        if (confirmLabel != null)
        {
            confirmLabel.text = confirmMessage;
            confirmLabel.gameObject.SetActive(true);
        }

        if (confirmRoutine != null) StopCoroutine(confirmRoutine);
        confirmRoutine = StartCoroutine(DisarmAfter(confirmWindowSeconds));
    }

    private IEnumerator DisarmAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        confirmRoutine = null;
        HideConfirmation();
    }

    private void HideConfirmation()
    {
        armed = false;
        if (confirmRoutine != null)
        {
            StopCoroutine(confirmRoutine);
            confirmRoutine = null;
        }

        if (confirmLabel != null) confirmLabel.gameObject.SetActive(false);
    }

    /// Builds a small warning label under the button when none was assigned in the Inspector.
    private TextMeshProUGUI CreateLabel()
    {
        var rect = GetComponent<RectTransform>();
        if (rect == null) return null;

        var go = new GameObject("StartOverLabel", typeof(RectTransform));
        go.transform.SetParent(transform, false);

        var r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 0f);
        r.anchorMax = new Vector2(0.5f, 0f);
        r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0f, -12f);
        r.sizeDelta = new Vector2(Mathf.Max(rect.rect.width, 700f), 120f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 36f;
        tmp.alignment = TextAlignmentOptions.Top;
        tmp.color = new Color(0.85f, 0.15f, 0.15f);
        tmp.raycastTarget = false;

        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/beachday SDF");
        if (font != null) tmp.font = font;

        return tmp;
    }

    private IEnumerator WaitForDelay(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);

        if (SessionManager.Instance == null)
        {
            var sessionManagerGO = new GameObject("SessionManager");
            sessionManagerGO.AddComponent<SessionManager>();
        }

        SessionManager.Instance.StartNewSession();

        if (AudioManager.Instance != null) AudioManager.Instance.StopMusic();

        SceneTransitionManager.Instance.TransitionToScene(_sceneName);
    }
}
