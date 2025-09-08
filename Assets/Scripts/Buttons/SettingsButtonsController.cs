using UnityEngine;
using UnityEngine.UI;

public class SettingsButtonsController : MonoBehaviour
{
    [Header("Settings Animation")]
    public Animator settingsAnimator;
    public string showTrigger = "Show";
    
    [Header("Toggle Buttons")]
    public Button audioButton;
    public Button musicButton;
    
    [Header("Button Images (for color changes)")]
    public Image audioButtonImage;
    public Image musicButtonImage;
    
    [Header("Color Settings")]
    [Range(0.3f, 1f)]
    public float fadedAlpha = 0.5f;
    
    // Private variables
    private bool isAudioOn = true;
    private bool isMusicOn = true;
    
    // Store original colors
    private Color originalAudioColor;
    private Color originalMusicColor;
    
    // PlayerPrefs keys (shared across all scenes)
    private const string AUDIO_STATE_KEY = "GlobalAudioState";
    private const string MUSIC_STATE_KEY = "GlobalMusicState";
    
    void Start()
    {
        InitializeSettings();
        LoadGlobalSettings();
        SetupButtonListeners();
        UpdateButtonVisuals();
        
        // Delay AudioManager application to ensure it exists
        StartCoroutine(ApplySettingsAfterDelay());
    }
    
    System.Collections.IEnumerator ApplySettingsAfterDelay()
    {
        // Wait a frame to ensure all other Start() methods have run
        yield return null;
        ApplySettingsToAudioManager();
    }
    
    void InitializeSettings()
    {
       
            settingsAnimator = GetComponent<Animator>();
        
        
        
            audioButtonImage = audioButton.GetComponent<Image>();
        
        
        
            musicButtonImage = musicButton.GetComponent<Image>();
        
        
        
            originalAudioColor = audioButtonImage.color;
        
        
      
            originalMusicColor = musicButtonImage.color;
        
        
       
    }
    
    void LoadGlobalSettings()
    {
        // Load global settings that persist across all scenes
        isAudioOn = PlayerPrefs.GetInt(AUDIO_STATE_KEY, 1) == 1;
        isMusicOn = PlayerPrefs.GetInt(MUSIC_STATE_KEY, 1) == 1;
        
        Debug.Log($"SettingsButtonsController: Loaded global settings - Audio: {isAudioOn}, Music: {isMusicOn}");
    }
    
    void SaveGlobalSettings()
    {
        // Save global settings that persist across all scenes
        PlayerPrefs.SetInt(AUDIO_STATE_KEY, isAudioOn ? 1 : 0);
        PlayerPrefs.SetInt(MUSIC_STATE_KEY, isMusicOn ? 1 : 0);
        PlayerPrefs.Save();
        
        Debug.Log($"SettingsButtonsController: Saved global settings - Audio: {isAudioOn}, Music: {isMusicOn}");
    }
    
    void ApplySettingsToAudioManager()
    {
       
            AudioManager.Instance.SetSFXEnabled(isAudioOn);
            AudioManager.Instance.SetMusicEnabled(isMusicOn);
            Debug.Log("SettingsButtonsController: Applied settings to AudioManager");
        
    }
    
   
    
    void SetupButtonListeners()
    {
        if (audioButton != null)
        {
            audioButton.onClick.AddListener(ToggleAudio);
        }
        else
        {
            Debug.LogWarning("SettingsButtonsController: Audio button not assigned!");
        }
        
        if (musicButton != null)
        {
            musicButton.onClick.AddListener(ToggleMusic);
        }
        else
        {
            Debug.LogWarning("SettingsButtonsController: Music button not assigned!");
        }
    }
    
    // === BUTTON TOGGLE METHODS ===
    
    public void ToggleAudio()
    {
        // Play click sound BEFORE turning off (if currently on)
        if (isAudioOn && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClick();
        }
        
        isAudioOn = !isAudioOn;
        
        // Update AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXEnabled(isAudioOn);
            
            // Play click sound AFTER turning on (if just turned on)
            if (isAudioOn)
            {
                AudioManager.Instance.PlayButtonClick();
            }
        }
        
        UpdateButtonVisuals();
        SaveGlobalSettings();
        
        Debug.Log($"SettingsButtonsController: Audio toggled to {(isAudioOn ? "ON" : "OFF")}");
    }
    
    public void ToggleMusic()
    {
        isMusicOn = !isMusicOn;
        
        // Update AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicEnabled(isMusicOn);
            
            // Play click sound if SFX is enabled
            if (AudioManager.Instance.IsSFXEnabled())
            {
                AudioManager.Instance.PlayButtonClick();
            }
        }
        
        UpdateButtonVisuals();
        SaveGlobalSettings();
        
        Debug.Log($"SettingsButtonsController: Music toggled to {(isMusicOn ? "ON" : "OFF")}");
    }
    
    // === VISUAL UPDATE METHODS ===
    
    void UpdateButtonVisuals()
    {
        UpdateAudioButtonVisual();
        UpdateMusicButtonVisual();
    }
    
    void UpdateAudioButtonVisual()
    {
        if (audioButtonImage != null)
        {
            Color targetColor = originalAudioColor;
            
            if (!isAudioOn)
            {
                targetColor.a = fadedAlpha;
            }
            
            audioButtonImage.color = targetColor;
        }
    }
    
    void UpdateMusicButtonVisual()
    {
        if (musicButtonImage != null)
        {
            Color targetColor = originalMusicColor;
            
            if (!isMusicOn)
            {
                targetColor.a = fadedAlpha;
            }
            
            musicButtonImage.color = targetColor;
        }
    }
    
   
}