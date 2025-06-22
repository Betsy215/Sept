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
    
    [Header("Button State")]
    public bool checkSessionOnStart = true; // Auto-disable if no session
    
    void Start()
    {
        if (checkSessionOnStart)
        {
            UpdateButtonState();
        }
    }
    
    void UpdateButtonState()
    {
        // Check if there's an active session
        bool hasActiveSession = false;
        
        if (SessionManager.Instance != null)
        {
            hasActiveSession = SessionManager.Instance.HasActiveSession();
        }
        else
        {
            // Check PlayerPrefs directly if SessionManager doesn't exist yet
            hasActiveSession = PlayerPrefs.HasKey("FoodTruckSession");
        }
        
        // Enable/disable button based on session availability
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.interactable = hasActiveSession;
        }
        
        // Optional: Change visual appearance when disabled
        if (!hasActiveSession && _img != null)
        {
            Color disabledColor = Color.gray;
            disabledColor.a = 0.5f;
            _img.color = disabledColor;
        }
        else if (_img != null)
        {
            _img.color = Color.white;
        }
        
        Debug.Log($"Continue button: {(hasActiveSession ? "ENABLED" : "DISABLED")} - Has session: {hasActiveSession}");
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        // Only respond if button is interactable
        Button button = GetComponent<Button>();
        if (button != null && !button.interactable) return;
        
        _img.sprite = _pressed;
        if (_source != null && _compressClip != null)
            _source.PlayOneShot(_compressClip);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Only respond if button is interactable
        Button button = GetComponent<Button>();
        if (button != null && !button.interactable) return;
        
        _img.sprite = _default;
        if (_source != null && _uncompressClip != null)
            _source.PlayOneShot(_uncompressClip);
            
        StartCoroutine(WaitForDelay(2));
    }
    
    IEnumerator WaitForDelay(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);

        // Ensure SessionManager exists
        if (SessionManager.Instance == null)
        {
            Debug.Log("ClickContinue: Creating SessionManager...");
            GameObject sessionManagerGO = new GameObject("SessionManager");
            sessionManagerGO.AddComponent<SessionManager>();
        }
        
        // Continue existing session or start new if none exists
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

        // Load the game scene
        SceneManager.LoadScene(_sceneName);
    }
    
    // Public method to refresh button state (useful if called from other scripts)
    public void RefreshButtonState()
    {
        UpdateButtonState();
    }
}