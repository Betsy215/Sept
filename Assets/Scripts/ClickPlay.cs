using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ClickPlay : MonoBehaviour, IPointerDownHandler,IPointerUpHandler
{
    public Image _img;
    public Sprite _default, _pressed;
    public AudioClip _compressClip,_uncompressClip;
    public AudioSource _source;
    public string _sceneName;
    // public string sceneName;
    
    public void OnPointerDown(PointerEventData eventData)
    {
        _img.sprite = _pressed;
        _source.PlayOneShot(_compressClip);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _img.sprite = _default;
        _source.PlayOneShot(_uncompressClip);
        StartCoroutine(WaitForDelay(2));
       
    }
    IEnumerator WaitForDelay(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);

        // NEW: Ensure SessionManager exists and start new session before loading game
        if (SessionManager.Instance == null)
        {
            Debug.Log("ClickPlay: Creating SessionManager...");
            GameObject sessionManagerGO = new GameObject("SessionManager");
            sessionManagerGO.AddComponent<SessionManager>();
        }
        
        // Start new session
        Debug.Log("ClickPlay: Starting new game session...");
        SessionManager.Instance.StartNewSession();

        // Load the game scene
        SceneManager.LoadScene(_sceneName);
    }
}