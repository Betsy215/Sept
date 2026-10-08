using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Press Effect Settings")]
    [Range(0.5f, 1f)]
    public float pressedColorMultiplier = 0.7f;
    public float pressDepth = 10f;
    
    [Header("Audio Feedback")]
    public AudioClip _compressClip, _uncompressClip;
    
    [Header("Game Management")]
    public LevelManager levelManager;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;

    // Private components and state
    private Image buttonImage;
    private AudioSource audioSource;
    private RectTransform rectTransform;
    
    // Store original values
    private Color originalColor;
    private Vector3 originalPosition;
    private bool isPressed = false;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        // Auto-find components
        buttonImage = GetComponent<Image>();
        audioSource = GetComponent<AudioSource>();
        rectTransform = GetComponent<RectTransform>();
        
        // Store original values
        if (buttonImage != null)
        {
            originalColor = buttonImage.color;
        }
        
        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition3D;
        }
        
        // Auto-find LevelManager
        if (levelManager == null)
        {
            levelManager = FindObjectOfType<LevelManager>();
            if (levelManager == null)
            {
                Debug.LogError("PauseButton: LevelManager not found in scene!");
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log("PauseButton: Initialized successfully");
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isPressed) return;
        
        isPressed = true;
        
        // Visual feedback - darken color
        if (buttonImage != null)
        {
            Color pressedColor = originalColor * pressedColorMultiplier;
            pressedColor.a = originalColor.a;
            buttonImage.color = pressedColor;
        }
        
        // Visual feedback - move down in Z
        if (rectTransform != null)
        {
            Vector3 pressedPosition = originalPosition;
            pressedPosition.z -= pressDepth;
            rectTransform.anchoredPosition3D = pressedPosition;
        }
        
        // Audio feedback
        if (audioSource != null && _compressClip != null)
        {
            audioSource.PlayOneShot(_compressClip);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log("PauseButton: Pressed");
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed) return;
        
        isPressed = false;
        
        // Visual feedback - restore original color
        if (buttonImage != null)
        {
            buttonImage.color = originalColor;
        }
        
        // Visual feedback - restore original position
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition3D = originalPosition;
        }
        
        // Audio feedback
        if (audioSource != null && _uncompressClip != null)
        {
            audioSource.PlayOneShot(_uncompressClip);
        }
        
        // Execute pause functionality after brief delay
        StartCoroutine(WaitForDelay(0.1f));
        
        if (enableDebugLogs)
        {
            Debug.Log("PauseButton: Released - executing pause");
        }
    }
    
    // Finger slid off the button while held: cancel the press so the pressed look is not left stuck
    // and the release does not toggle pause (same semantics as a Button click).
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isPressed) return;

        isPressed = false;

        if (buttonImage != null)
            buttonImage.color = originalColor;

        if (rectTransform != null)
            rectTransform.anchoredPosition3D = originalPosition;

        if (enableDebugLogs)
            Debug.Log("PauseButton: Pointer left while pressed - press cancelled");
    }

    IEnumerator WaitForDelay(float delayTime)
    {
        yield return new WaitForSecondsRealtime(delayTime); // Use realtime so it works during pause
        
        // Call the pause/resume functionality
        if (levelManager != null)
        {
            levelManager.TogglePause();
            
            if (enableDebugLogs)
            {
                Debug.Log("PauseButton: Called LevelManager.TogglePause()");
            }
        }
        else
        {
            Debug.LogError("PauseButton: Cannot toggle pause - LevelManager reference is null!");
        }
    }
}