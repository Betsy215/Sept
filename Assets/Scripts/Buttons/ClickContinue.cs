using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ClickContinue : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Image _img;
    public Sprite _default, _pressed;
    public AudioClip _compressClip, _uncompressClip;
    public AudioSource _source;
    public string _sceneName;

    private bool launching; // Set on the first valid tap so a double-tap cannot continue the session twice

    private void Start()
    {
        UpdateButtonState();
    }

    private void OnEnable()
    {
        // Check button state when returning to main menu
        Invoke("UpdateButtonState", 0.1f);
    }

    private void UpdateButtonState()
    {
        var canContinue = false;

        if (SessionManager.Instance != null && SessionManager.Instance.HasActiveSession())
        {
            var session = SessionManager.Instance.GetCurrentSession();
            if (session != null)
                // Enable continue only if player finished at least 1 level
                canContinue = session.levelsCompleted >= 1;
        }

        // Enable/disable button
        var button = GetComponent<Button>();
        if (button != null) button.interactable = canContinue;

        // Update visual appearance
        if (_img != null)
        {
            if (canContinue)
            {
                _img.color = Color.white;
            }
            else
            {
                var disabledColor = Color.gray;
                disabledColor.a = 0.5f;
                _img.color = disabledColor;
            }
        }

        Debug.Log($"Continue button: {(canContinue ? "ENABLED" : "DISABLED")}");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (launching) return;
        var button = GetComponent<Button>();
        if (button != null && !button.interactable) return;

        if (_img != null && _pressed != null) _img.sprite = _pressed;
        if (_source != null && _compressClip != null)
            _source.PlayOneShot(_compressClip);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (launching) return;
        var button = GetComponent<Button>();
        if (button != null && !button.interactable) return;

        if (_img != null && _default != null) _img.sprite = _default;
        if (_source != null && _uncompressClip != null)
            _source.PlayOneShot(_uncompressClip);

        launching = true;
        StartCoroutine(WaitForDelay(2));
    }

    private IEnumerator WaitForDelay(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);

        // Ensure SessionManager exists
        if (SessionManager.Instance == null)
        {
            var sessionManagerGO = new GameObject("SessionManager");
            sessionManagerGO.AddComponent<SessionManager>();
        }

        // Continue session
        if (SessionManager.Instance.HasActiveSession())
        {
            Debug.Log("ClickContinue: Continuing existing session...");
            SessionManager.Instance.ContinueSession();
        }
        else
        {
            Debug.Log("ClickContinue: No session found, starting new session...");
            SessionManager.Instance.StartNewSession();
        }

        // Stop main menu music
        if (AudioManager.Instance != null) AudioManager.Instance.StopMusic();

        SceneTransitionManager.Instance.TransitionToScene(_sceneName);
    }
}