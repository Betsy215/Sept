using UnityEngine;
using UnityEngine.UI;

public class SettingsButtonsController : MonoBehaviour
{
    [Header("Settings Animation")]
    public Animator settingsAnimator;
    public string showTrigger = "Show"; // Single trigger that toggles show/hide
    
    [Header("Toggle Buttons")]
    public Button audioButton;
    public Button musicButton;
    
    [Header("Button Images (for color changes)")]
    public Image audioButtonImage;
    public Image musicButtonImage;
    
    [Header("Color Settings")]
    [Range(0.3f, 1f)]
    public float fadedAlpha = 0.5f; // How faded the "off" state should be
    
    // Private variables
    private bool isAudioOn = true;
    private bool isMusicOn = true;
    
    // Store original colors
    private Color originalAudioColor;
    private Color originalMusicColor;
    
    // PlayerPrefs keys
    private const string AUDIO_STATE_KEY = "AudioButtonState";
    private const string MUSIC_STATE_KEY = "MusicButtonState";
    
    void Start()
    {
        InitializeSettings();
        SetupButtonListeners();
        LoadButtonStates();
        UpdateButtonVisuals();
    }
    
    void InitializeSettings()
    {
        // Auto-find components if not assigned
        if (settingsAnimator == null)
        {
            settingsAnimator = GetComponent<Animator>();
        }
        
        if (audioButtonImage == null && audioButton != null)
        {
            audioButtonImage = audioButton.GetComponent<Image>();
        }
        
        if (musicButtonImage == null && musicButton != null)
        {
            musicButtonImage = musicButton.GetComponent<Image>();
        }
        
        // Store original colors
        if (audioButtonImage != null)
        {
            originalAudioColor = audioButtonImage.color;
        }
        
        if (musicButtonImage != null)
        {
            originalMusicColor = musicButtonImage.color;
        }
        
        Debug.Log("SettingsButtonsController: Initialized");
    }
    
    void SetupButtonListeners()
    {
        // Setup audio button
        if (audioButton != null)
        {
            audioButton.onClick.AddListener(ToggleAudio);
        }
        else
        {
            Debug.LogWarning("SettingsButtonsController: Audio button not assigned!");
        }
        
        // Setup music button
        if (musicButton != null)
        {
            musicButton.onClick.AddListener(ToggleMusic);
        }
        else
        {
            Debug.LogWarning("SettingsButtonsController: Music button not assigned!");
        }
    }
    
    void LoadButtonStates()
    {
        // Load saved states (default to ON if no save exists)
        isAudioOn = PlayerPrefs.GetInt(AUDIO_STATE_KEY, 1) == 1;
        isMusicOn = PlayerPrefs.GetInt(MUSIC_STATE_KEY, 1) == 1;
        
        // Apply loaded states to AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXEnabled(isAudioOn);
            AudioManager.Instance.SetMusicEnabled(isMusicOn);
        }
        
        Debug.Log($"SettingsButtonsController: Loaded states - Audio: {isAudioOn}, Music: {isMusicOn}");
    }
    
    void SaveButtonStates()
    {
        PlayerPrefs.SetInt(AUDIO_STATE_KEY, isAudioOn ? 1 : 0);
        PlayerPrefs.SetInt(MUSIC_STATE_KEY, isMusicOn ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    // === BUTTON TOGGLE METHODS ===
    
    public void ToggleAudio()
    {
        isAudioOn = !isAudioOn;
        
        // Update AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXEnabled(isAudioOn);
            
            // Play click sound if audio is being turned ON
            if (isAudioOn)
            {
                AudioManager.Instance.PlayButtonClick();
            }
        }
        
        UpdateButtonVisuals();
        SaveButtonStates();
        
        Debug.Log($"SettingsButtonsController: Audio toggled to {(isAudioOn ? "ON" : "OFF")}");
    }
    
    public void ToggleMusic()
    {
        isMusicOn = !isMusicOn;
        
        // Update AudioManager
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicEnabled(isMusicOn);
        }
        
        UpdateButtonVisuals();
        SaveButtonStates();
        
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
                // Make it faded when OFF
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
                // Make it faded when OFF
                targetColor.a = fadedAlpha;
            }
            
            musicButtonImage.color = targetColor;
        }
    }
    
    // === SETTINGS PANEL CONTROL ===
    
    public void ToggleSettings()
    {
        if (settingsAnimator != null && !string.IsNullOrEmpty(showTrigger))
        {
            settingsAnimator.SetTrigger(showTrigger);
            
            // Play button click sound
            if (AudioManager.Instance != null && AudioManager.Instance.IsSFXEnabled())
            {
                AudioManager.Instance.PlayButtonClick();
            }
            
            Debug.Log("SettingsButtonsController: Settings panel toggled");
        }
    }
    
    // === PUBLIC GETTERS ===
    
    public bool IsAudioOn()
    {
        return isAudioOn;
    }
    
    public bool IsMusicOn()
    {
        return isMusicOn;
    }
    
    // === MANUAL STATE SETTING (if needed) ===
    
    public void SetAudioState(bool enabled)
    {
        isAudioOn = enabled;
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXEnabled(isAudioOn);
        }
        
        UpdateButtonVisuals();
        SaveButtonStates();
    }
    
    public void SetMusicState(bool enabled)
    {
        isMusicOn = enabled;
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicEnabled(isMusicOn);
        }
        
        UpdateButtonVisuals();
        SaveButtonStates();
    }
}