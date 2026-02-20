using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class KitchenSceneManager : MonoBehaviour
{
    public static KitchenSceneManager Instance { get; private set; }

    [Header("Kitchen Scene Settings")] public string kitchenSceneName = "KitchenScene";

    [Header("Performance")] [Tooltip("Cache colliders on start for maximum performance")]
    public bool cacheCollidersOnStart = true;

    [Header("Debug")] public bool enableDebugLogs = true;

    private bool isKitchenOpen = false;

    // CACHED list of all game colliders (not Kitchen layer)
    private List<Collider2D> cachedGameColliders = new();
    private List<Collider> cachedGameColliders3D = new();
    private bool isCached = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (cacheCollidersOnStart) CacheGameColliders();
    }

    /// <summary>
    /// Cache all game colliders ONCE on start (expensive but only once)
    /// </summary>
    private void CacheGameColliders()
    {
        var startTime = Time.realtimeSinceStartup;

        cachedGameColliders.Clear();
        cachedGameColliders3D.Clear();

        var kitchenLayer = LayerMask.NameToLayer("Kitchen");

        // Find all 2D colliders NOT on Kitchen layer
        var all2D = FindObjectsOfType<Collider2D>(true); // true = include inactive
        foreach (var col in all2D)
            if (col.gameObject.layer != kitchenLayer)
                cachedGameColliders.Add(col);

        // Find all 3D colliders NOT on Kitchen layer
        var all3D = FindObjectsOfType<Collider>(true);
        foreach (var col in all3D)
            if (col.gameObject.layer != kitchenLayer)
                cachedGameColliders3D.Add(col);

        isCached = true;

        var duration = (Time.realtimeSinceStartup - startTime) * 1000f;
        DebugLog(
            $"Cached {cachedGameColliders.Count} 2D + {cachedGameColliders3D.Count} 3D colliders in {duration:F2}ms");
    }

    public void OpenKitchen()
    {
        if (isKitchenOpen)
        {
            DebugLog("Kitchen already open");
            return;
        }

        DebugLog($"Opening kitchen scene: {kitchenSceneName}");

        // Disable game interactions (FAST if cached!)
        DisableGameInteractions();

        SceneManager.LoadScene(kitchenSceneName, LoadSceneMode.Additive);

        isKitchenOpen = true;

        DebugLog("Kitchen scene loaded");
    }

    public void CloseKitchen()
    {
        if (!isKitchenOpen)
        {
            DebugLog("Kitchen already closed");
            return;
        }

        DebugLog($"Closing kitchen scene: {kitchenSceneName}");

        // Re-enable game interactions (FAST!)
        EnableGameInteractions();

        SceneManager.UnloadSceneAsync(kitchenSceneName);

        isKitchenOpen = false;

        DebugLog("Kitchen scene unloaded");
    }

    /// <summary>
    /// Disable all game colliders (uses cache if available)
    /// </summary>
    private void DisableGameInteractions()
    {
        var startTime = Time.realtimeSinceStartup;

        // If not cached, cache now
        if (!isCached) CacheGameColliders();

        var disabledCount = 0;

        // Disable cached 2D colliders
        foreach (var col in cachedGameColliders)
            if (col != null && col.enabled) // Check for destroyed objects
            {
                col.enabled = false;
                disabledCount++;
            }

        // Disable cached 3D colliders
        foreach (var col in cachedGameColliders3D)
            if (col != null && col.enabled)
            {
                col.enabled = false;
                disabledCount++;
            }

        var duration = (Time.realtimeSinceStartup - startTime) * 1000f;
        DebugLog($"Disabled {disabledCount} colliders in {duration:F2}ms");
    }

    /// <summary>
    /// Re-enable all game colliders
    /// </summary>
    private void EnableGameInteractions()
    {
        var startTime = Time.realtimeSinceStartup;

        var enabledCount = 0;

        // Re-enable cached 2D colliders
        foreach (var col in cachedGameColliders)
            if (col != null)
            {
                col.enabled = true;
                enabledCount++;
            }

        // Re-enable cached 3D colliders
        foreach (var col in cachedGameColliders3D)
            if (col != null)
            {
                col.enabled = true;
                enabledCount++;
            }

        var duration = (Time.realtimeSinceStartup - startTime) * 1000f;
        DebugLog($"Re-enabled {enabledCount} colliders in {duration:F2}ms");
    }

    /// <summary>
    /// Call this if you spawn new objects at runtime
    /// </summary>
    public void RefreshColliderCache()
    {
        DebugLog("Refreshing collider cache...");
        CacheGameColliders();
    }

    public void ToggleKitchen()
    {
        if (isKitchenOpen)
            CloseKitchen();
        else
            OpenKitchen();
    }

    public bool IsKitchenOpen()
    {
        return isKitchenOpen;
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"[KitchenSceneManager] {message}");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}