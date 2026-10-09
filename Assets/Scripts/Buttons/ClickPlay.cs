using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Main-menu Play button. Starts a new session on one tap (the owner removed the
/// two-tap "erases your saved game" notice on 9 October). Use Continue to resume.
/// </summary>
public class ClickPlay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Image _img;
    public Sprite _default, _pressed;
    public AudioClip _compressClip, _uncompressClip;
    public AudioSource _source;
    public string _sceneName;

    private bool launching;

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

        launching = true;
        StartCoroutine(WaitForDelay(2));
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
