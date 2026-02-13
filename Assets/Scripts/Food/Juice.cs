using UnityEngine;

/// <summary>
/// Handles juice audio effect when served.
/// Permanent item - no count tracking, always available.
/// 
/// SETUP REQUIREMENTS:
/// - RefillableItem.enableRefill = FALSE (permanent item)
/// - RefillableItem.customMaxCount = -1 (no count limit)
/// - ServeableItem must be attached for serving functionality
/// - Assign juiceServedSound in Inspector
/// 
/// COMPATIBILITY:
/// - Works with ServeableItem (foodType = "Juice")
/// - Works with OrderSystem for orders
/// </summary>
public class Juice : MonoBehaviour
{
    [Header("Audio Settings")] [Tooltip("Sound to play when juice is served")]
    public AudioClip juiceServedSound;

    [Tooltip("Volume for the served sound (0.0 to 1.0)")] [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Debug")] public bool enableDebugLogs = false;

    // References
    private ServeableItem serveableItem;
    private AudioSource audioSource;

    private void Start()
    {
        // Get components
        serveableItem = GetComponent<ServeableItem>();

        // Get or create AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (serveableItem == null)
            Debug.LogWarning("Juice: ServeableItem component not found!");

        DebugLog("Juice initialized");
    }

    /// <summary>
    /// Called by ServeableItem when item is successfully served
    /// </summary>
    public void OnServedSuccessfully()
    {
        DebugLog("Playing served sound");
        PlayServedSound();
    }

    private void PlayServedSound()
    {
        if (juiceServedSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(juiceServedSound, soundVolume);
            DebugLog("Playing juice served sound");
        }
        else
        {
            if (juiceServedSound == null)
                DebugLog("WARNING: juiceServedSound not assigned! Assign in Inspector.");
            if (audioSource == null)
                DebugLog("WARNING: AudioSource not found!");
        }
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[Juice] {message}");
    }

    #region Editor Testing

#if UNITY_EDITOR
    [ContextMenu("Test: Served Sound")]
    private void TestServedSound()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Juice: Test must be run in Play Mode!");
            return;
        }

        PlayServedSound();
    }
#endif

    #endregion
}