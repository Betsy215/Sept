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

        // Code to execute after the delay

        SceneManager.LoadScene(_sceneName);
    }

}
