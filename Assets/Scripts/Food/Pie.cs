using UnityEngine;
using System.Collections;

/// <summary>
/// Handles pie visual and audio effects when served.
/// Permanent item - no count tracking, always available.
/// 
/// SETUP REQUIREMENTS:
/// - RefillableItem.enableRefill = FALSE (permanent item)
/// - RefillableItem.customMaxCount = -1 (no count limit)
/// - ServeableItem must be attached for serving functionality
/// - Assign pieServedSound in Inspector (sound of taking item from basket)
/// 
/// COMPATIBILITY:
/// - Works with ServeableItem (foodType = "Pie")
/// - Works with OrderSystem for orders
/// </summary>
public class Pie : MonoBehaviour
{
    [Header("Served Effect Settings")] [Tooltip("How far down the pie sinks when served (in Unity units)")]
    public float sinkDistance = 0.2f;

    [Tooltip("How long it takes to sink down")]
    public float sinkDuration = 0.15f;

    [Tooltip("How long it takes to come back up")]
    public float riseDuration = 0.1f;

    [Tooltip("Ease curve for sinking animation")]
    public AnimationCurve sinkCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio Settings")] [Tooltip("Sound to play when pie is served (like taking item from basket)")]
    public AudioClip pieServedSound;

    [Tooltip("Volume for the served sound (0.0 to 1.0)")] [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Debug")] public bool enableDebugLogs = false;

    // References
    private ServeableItem serveableItem;
    private AudioSource audioSource;
    private Vector3 originalPosition;

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

        // Store original position for sink animation
        originalPosition = transform.position;

        if (serveableItem == null)
            Debug.LogWarning("Pie: ServeableItem component not found!");

        DebugLog("Pie initialized");
    }

    /// <summary>
    /// Called by ServeableItem when item is successfully served
    /// </summary>
    public void OnServedSuccessfully()
    {
        DebugLog("Playing served effect: sound + sink animation");

        // Play sound
        PlayServedSound();

        // Play sink animation
        StartCoroutine(SinkEffect());
    }

    private void PlayServedSound()
    {
        if (pieServedSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pieServedSound, soundVolume);
            DebugLog("Playing pie served sound");
        }
        else
        {
            if (pieServedSound == null)
                DebugLog("WARNING: pieServedSound not assigned! Assign in Inspector.");
            if (audioSource == null)
                DebugLog("WARNING: AudioSource not found!");
        }
    }

    private IEnumerator SinkEffect()
    {
        var startPosition = transform.position;
        var sinkPosition = startPosition + Vector3.down * sinkDistance;

        var elapsed = 0f;

        // Sink down
        while (elapsed < sinkDuration)
        {
            var t = elapsed / sinkDuration;
            var curveValue = sinkCurve.Evaluate(t);
            transform.position = Vector3.Lerp(startPosition, sinkPosition, curveValue);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure we're at the sink position
        transform.position = sinkPosition;

        // Rise back up
        elapsed = 0f;
        while (elapsed < riseDuration)
        {
            var t = elapsed / riseDuration;
            transform.position = Vector3.Lerp(sinkPosition, originalPosition, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure we're back at original position
        transform.position = originalPosition;

        DebugLog("Sink effect complete");
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[Pie] {message}");
    }

    #region Editor Testing

#if UNITY_EDITOR
    [ContextMenu("Test: Served Effect (Sound + Sink)")]
    private void TestServedEffect()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Pie: Test must be run in Play Mode!");
            return;
        }

        OnServedSuccessfully();
    }

    [ContextMenu("Test: Sound Only")]
    private void TestSound()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Pie: Test must be run in Play Mode!");
            return;
        }

        PlayServedSound();
    }
#endif

    #endregion
}