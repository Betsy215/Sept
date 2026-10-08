using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")] public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Background Music")] public AudioClip mainMenuMusic;
    public AudioClip arrangementPhaseMusic; // NEW: Music for arrangement phase
    public AudioClip[] gameplayPlaylist = new AudioClip[2]; // NEW: 2 tracks to play back-to-back
    public AudioClip levelCompleteMusic;
    public AudioClip shopMusic;

    [Header("Sound Effects")] public AudioClip buttonClickSFX;
    public AudioClip orderCompleteSFX;
    public AudioClip itemPickupSFX;
    public AudioClip levelWinSFX;
    public AudioClip wrongItemSFX;
    public AudioClip customerWalkInSFX;

    [Header("Order Complete Sounds - Defaults")]
    public AudioClip defaultPerfectOrderSound;

    public AudioClip defaultOrderDoneSound;

    [Header("Audio Settings")] [Range(0f, 1f)]
    public float musicVolume = 0.7f;

    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    [Header("Auto-Start Settings")] public bool autoStartMainMenuMusic = true;
    [Tooltip("Main-menu music starts automatically whenever this scene is loaded.")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Money/Counting Sounds")] public AudioClip moneyCountSound;
    public AudioClip moneyCompleteSound;

    // Audio enable/disable states
    private bool musicEnabled = true;
    private bool sfxEnabled = true;

    // NEW: Playlist variables
    private int currentPlaylistIndex = 0;
    private bool isPlaylistActive = false;
    private Coroutine playlistCoroutine;
    private string currentMusicType = "";

    private void Awake()
    {
        Debug.Log("AudioManager: Awake called!");

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

    private void Start()
    {
        if (Instance != this) return;

        // Start is only called once on the persistent instance, so scene-driven music
        // (main menu) is handled through sceneLoaded instead.
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        if (autoStartMainMenuMusic && scene.name == mainMenuSceneName) PlayMainMenuMusic();
    }

    private void InitializeAudioManager()
    {
        Debug.Log("AudioManager: Initializing...");

        if (musicSource == null)
        {
            Debug.Log("AudioManager: Creating Music Source");
            var musicGO = new GameObject("Music Source");
            musicGO.transform.SetParent(transform);
            musicSource = musicGO.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume;
        }

        if (sfxSource == null)
        {
            Debug.Log("AudioManager: Creating SFX Source");
            var sfxGO = new GameObject("SFX Source");
            sfxGO.transform.SetParent(transform);
            sfxSource = sfxGO.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume;
        }

        Debug.Log("AudioManager: Initialization complete!");
    }

    // Core music methods
    public void PlayMusic(AudioClip musicClip, bool shouldLoop = true)
    {
        if (musicClip == null || musicSource == null || !musicEnabled)
        {
            Debug.Log(
                $"AudioManager: Cannot play music. Clip: {musicClip != null}, Source: {musicSource != null}, Enabled: {musicEnabled}");
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
            Debug.Log(
                $"AudioManager: Cannot play SFX. Clip: {sfxClip != null}, Source: {sfxSource != null}, Enabled: {sfxEnabled}");
            return;
        }

        Debug.Log($"AudioManager: Playing SFX: {sfxClip.name}");
        sfxSource.PlayOneShot(sfxClip);
    }

    // NEW: Arrangement phase music
    public void PlayArrangementMusic()
    {
        if (arrangementPhaseMusic != null)
        {
            StopPlaylist();
            currentMusicType = "arrangement";
            PlayMusic(arrangementPhaseMusic, true);
            Debug.Log("AudioManager: Playing arrangement phase music");
        }
        else
        {
            Debug.LogWarning("AudioManager: No arrangement phase music assigned!");
            // Fallback to main menu music if arrangement music not assigned
            PlayMainMenuMusic();
        }
    }

    public void PlayCustomerWalkIn()
    {
        PlaySFX(customerWalkInSFX);
    }

    // NEW: Gameplay playlist functionality
    public void PlayGameplayPlaylist()
    {
        if (gameplayPlaylist == null || gameplayPlaylist.Length < 2 ||
            gameplayPlaylist[0] == null || gameplayPlaylist[1] == null)
        {
            Debug.LogWarning("AudioManager: Gameplay playlist needs 2 valid tracks assigned! Using fallback music.");
            // Fallback to first available track
            if (gameplayPlaylist != null && gameplayPlaylist.Length > 0 && gameplayPlaylist[0] != null)
            {
                StopPlaylist();
                currentMusicType = "gameplay";
                PlayMusic(gameplayPlaylist[0], true);
            }
            else if (mainMenuMusic != null)
            {
                PlayMainMenuMusic();
            }

            return;
        }

        StopPlaylist();
        currentMusicType = "gameplay";
        isPlaylistActive = true;
        currentPlaylistIndex = Random.Range(0, gameplayPlaylist.Length);
        playlistCoroutine = StartCoroutine(PlaylistCoroutine());
        Debug.Log("AudioManager: Starting gameplay playlist");
    }

    private IEnumerator PlaylistCoroutine()
    {
        while (isPlaylistActive && musicEnabled)
        {
            var currentTrack = gameplayPlaylist[currentPlaylistIndex];

            if (currentTrack != null)
            {
                Debug.Log($"AudioManager: Playing playlist track {currentPlaylistIndex + 1}: {currentTrack.name}");
                PlayMusic(currentTrack, false); // Don't loop individual tracks

                // Wait for track to finish
                yield return new WaitForSeconds(currentTrack.length);
            }
            else
            {
                Debug.LogWarning($"AudioManager: Playlist track {currentPlaylistIndex + 1} is null!");
                yield return new WaitForSeconds(1f);
            }

            // Move to next track (cycles between 0 and 1)
            currentPlaylistIndex = (currentPlaylistIndex + 1) % gameplayPlaylist.Length;
        }
    }

    public void StopPlaylist()
    {
        if (isPlaylistActive)
        {
            isPlaylistActive = false;
            if (playlistCoroutine != null)
            {
                StopCoroutine(playlistCoroutine);
                playlistCoroutine = null;
            }

            Debug.Log("AudioManager: Playlist stopped");
        }
    }

    public void StopMusic()
    {
        StopPlaylist();
        if (musicSource != null)
        {
            musicSource.Stop();
            Debug.Log("AudioManager: Music stopped");
        }

        currentMusicType = "";
    }

    // Convenience music methods
    public void PlayMainMenuMusic()
    {
        StopPlaylist();
        currentMusicType = "mainmenu";
        PlayMusic(mainMenuMusic, true);
    }

    public void PlayGameplayMusic()
    {
        PlayGameplayPlaylist();
    }

    public void PlayShopMusic()
    {
        StopPlaylist();
        currentMusicType = "shop";
        PlayMusic(shopMusic, true);
    }

    public void PlayLevelCompleteMusic()
    {
        StopPlaylist();
        currentMusicType = "levelcomplete";
        PlayMusic(levelCompleteMusic, false);
    }

    // SFX convenience methods
    public void PlayButtonClick()
    {
        PlaySFX(buttonClickSFX);
    }

    public void PlayOrderComplete()
    {
        if (orderCompleteSFX == null || sfxSource == null || !sfxEnabled) return;
        sfxSource.PlayOneShot(orderCompleteSFX, sfxVolume * 0.7f);
    }

    public void PlayPurchaseSound()
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

    public void PlayWrongItemSFX()
    {
        PlaySFX(wrongItemSFX);
    }

    public void PlayMoneyCount()
    {
        PlaySFX(moneyCountSound);
    }

    public void PlayMoneyTransferComplete()
    {
        PlaySFX(moneyCompleteSound);
    }

    // Settings methods
    public void SetMusicEnabled(bool enabled)
    {
        // Settings re-apply on every scene load; ignore no-op calls so the current track is not restarted.
        if (enabled == musicEnabled) return;

        musicEnabled = enabled;
        Debug.Log($"AudioManager: Music {(enabled ? "enabled" : "disabled")}");

        if (!enabled)
        {
            // Stop playback but keep currentMusicType so re-enabling resumes the right music.
            StopPlaylist();
            if (musicSource != null) musicSource.Stop();
        }
        else
            // Resume appropriate music based on what was playing
            switch (currentMusicType)
            {
                case "gameplay":
                    PlayGameplayPlaylist();
                    break;
                case "arrangement":
                    PlayArrangementMusic();
                    break;
                case "mainmenu":
                    PlayMainMenuMusic();
                    break;
                case "shop":
                    PlayShopMusic();
                    break;
                case "levelcomplete":
                    PlayLevelCompleteMusic();
                    break;
                default:
                    if (musicSource != null && musicSource.clip != null)
                    {
                        musicSource.Play();
                        Debug.Log($"AudioManager: Resumed music - {musicSource.clip.name}");
                    }

                    break;
            }
    }

    public void SetSFXEnabled(bool enabled)
    {
        sfxEnabled = enabled;
    }

    public bool IsSFXEnabled()
    {
        return sfxEnabled;
    }
}