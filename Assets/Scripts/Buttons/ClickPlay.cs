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

    /// One guard for Play and Continue together (review R3, 26 November 2026): once either has been accepted the other
    /// ignores taps until the scene has changed, so Continue then Play inside the two-second wait cannot start a new
    /// session over the save. Static, so it is cleared when the main menu loads again (Awake) and if the wait is cut short.
    public static bool MenuLaunchAccepted;

    /// True when the finger was lifted over this button; a press that slides off is cancelled, like a normal button.
    public static bool ReleasedOver(Component button, PointerEventData eventData)
    {
        var rt = button.transform as RectTransform;
        return rt == null || RectTransformUtility.RectangleContainsScreenPoint(rt, eventData.position, eventData.pressEventCamera);
    }

    private void Awake() { MenuLaunchAccepted = false; }

    private void OnDisable()
    {
        if (launching) { launching = false; MenuLaunchAccepted = false; }   // the wait stops with the object: free the menu
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (launching || MenuLaunchAccepted) return;
        if (_img != null && _pressed != null) _img.sprite = _pressed;
        if (_source != null && _compressClip != null) _source.PlayOneShot(_compressClip);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (launching || MenuLaunchAccepted) return;
        if (_img != null && _default != null) _img.sprite = _default;
        if (_source != null && _uncompressClip != null) _source.PlayOneShot(_uncompressClip);

        if (!ReleasedOver(this, eventData)) return;   // slid off the button: no new game

        launching = true;
        MenuLaunchAccepted = true;
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

        // A new game opens with the tutorial (day 0) before the map ever shows; the tutorial ends on the map
        var scene = TutorialDirector.IsDue(SessionManager.Instance) ? TutorialDirector.GameScene : _sceneName;
        SceneTransitionManager.Instance.TransitionToScene(scene);
    }
}
