using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    
    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    
    [Header("Background Music")]
    public AudioClip mainMenuMusic;
    public AudioClip gameplayMusic;
    public AudioClip levelCompleteMusic;
    
    [Header("Sound Effects")]
    public AudioClip buttonClickSFX;
    public AudioClip orderCompleteSFX;
    public AudioClip itemPickupSFX;
    public AudioClip levelWinSFX;
    
    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float musicVolume = 0.7f;
    [Range(0f, 1f)]
    public float sfxVolume = 0.8f;
    
    [Header("Auto-Start Settings")]
    public bool autoStartMainMenuMusic = true; // NEW: Toggle this in inspector
    
    // Audio enable/disable states
    private bool musicEnabled = true;
    private bool sfxEnabled = true;
    
    void Awake()
    {
        Debug.Log("AudioManager: Awake called!");
        
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioManager();
        }
        else
        {
            Debug.Log("AudioManager: Destroying duplicate instance");
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        Debug.Log("AudioManager: Start called!");
        
        // Auto-start main menu music if enabled
        if (autoStartMainMenuMusic)
        {
            PlayMainMenuMusic();
        }
    }
    
    void InitializeAudioManager()
    {
        Debug.Log("AudioManager: Initializing...");
        
        // Create audio sources if not assigned
        if (musicSource == null)
        {
            Debug.Log("AudioManager: Creating Music Source");
            GameObject musicGO = new GameObject("Music Source");
            musicGO.transform.SetParent(transform);
            musicSource = musicGO.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume;
        }
        
        if (sfxSource == null)
        {
            Debug.Log("AudioManager: Creating SFX Source");
            GameObject sfxGO = new GameObject("SFX Source");
            sfxGO.transform.SetParent(transform);
            sfxSource = sfxGO.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume;
        }
        
        Debug.Log("AudioManager: Initialization complete!");
    }
    
    // Music methods
    public void PlayMusic(AudioClip musicClip, bool shouldLoop = true)
    {
        if (musicClip == null || musicSource == null || !musicEnabled) 
        {
            Debug.Log($"AudioManager: Cannot play music. Clip: {musicClip != null}, Source: {musicSource != null}, Enabled: {musicEnabled}");
            return;
        }
        
        Debug.Log($"AudioManager: Playing music: {musicClip.name} (Loop: {shouldLoop})");
        musicSource.clip = musicClip;
        musicSource.loop = shouldLoop;
        musicSource.Play();
    }
    
    public void PlaySFX(AudioClip sfxClip)
    {
        if (sfxClip == null || sfxSource == null || !sfxEnabled) 
        {
            Debug.Log($"AudioManager: Cannot play SFX. Clip: {sfxClip != null}, Source: {sfxSource != null}, Enabled: {sfxEnabled}");
            return;
        }
        
        Debug.Log($"AudioManager: Playing SFX: {sfxClip.name}");
        sfxSource.PlayOneShot(sfxClip);
    }
    
    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
            Debug.Log("AudioManager: Music stopped");
        }
    }
    
    // Convenience methods
    public void PlayMainMenuMusic()
    {
        PlayMusic(mainMenuMusic, true); // Loop main menu music
    }
    
    public void PlayGameplayMusic()
    {
        PlayMusic(gameplayMusic, true); // Loop gameplay music
    }
    
    public void PlayLevelCompleteMusic()
    {
        PlayMusic(levelCompleteMusic, false); // DON'T loop level complete music
    }
    
    public void PlayButtonClick()
    {
        PlaySFX(buttonClickSFX);
    }
    
    public void PlayOrderComplete()
    {
        PlaySFX(orderCompleteSFX);
    }
    
    public void PlayItemPickup()
    {
        PlaySFX(itemPickupSFX);
    }
    
    public void PlayLevelWin()
    {
        PlaySFX(levelWinSFX);
    }
    
    // Settings methods
    public void SetMusicEnabled(bool enabled)
    {
        musicEnabled = enabled;
        Debug.Log($"AudioManager: Music {(enabled ? "enabled" : "disabled")}");
        
        if (!enabled && musicSource != null && musicSource.isPlaying)
        {
            // Turn OFF: Stop the music
            musicSource.Stop();
        }
        else if (enabled && musicSource != null)
        {
            // Turn ON: Resume or restart music
            if (musicSource.clip != null)
            {
                // If there's a clip assigned, play it
                musicSource.Play();
                Debug.Log($"AudioManager: Resumed music - {musicSource.clip.name}");
            }
            else
            {
                Debug.LogWarning("AudioManager: No music clip assigned to resume!");
            }
        }
    }
    
    public void SetSFXEnabled(bool enabled)
    {
        sfxEnabled = enabled;
        if (enabled && sfxSource != null && buttonClickSFX != null)
        {
            sfxSource.PlayOneShot(buttonClickSFX);
            Debug.Log("AudioManager: Played SFX confirmation sound");
        }
    }
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
        Debug.Log($"AudioManager: Music volume set to {musicVolume}");
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
        Debug.Log($"AudioManager: SFX volume set to {sfxVolume}");
    }
    
    // Getters
    public bool IsMusicEnabled() { return musicEnabled; }
    public bool IsSFXEnabled() { return sfxEnabled; }
    public float GetMusicVolume() { return musicVolume; }
    public float GetSFXVolume() { return sfxVolume; }
}