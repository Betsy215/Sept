using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class KitchenSceneManager : MonoBehaviour
{
    public static KitchenSceneManager Instance { get; private set; }

    [Header("Kitchen Scene Settings")] public string kitchenSceneName = "KitchenScene";

    [Header("Debug")] public bool enableDebugLogs = true;

    private bool isKitchenOpen = false;

    // In-flight unload from the last CloseKitchen; a second additive load while it runs would
    // give two kitchen scenes (and two sets of timers re-attaching to the same cooldown keys).
    private AsyncOperation unloadOperation;

    // Game colliders (not Kitchen layer) that were ENABLED when the kitchen opened and that we
    // disabled. Rebuilt on every OpenKitchen so objects spawned later are covered, and only these
    // are re-enabled on close so colliders that were deliberately off stay off.
    private readonly List<Collider2D> disabledGameColliders = new();
    private readonly List<Collider> disabledGameColliders3D = new();

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

    public void OpenKitchen()
    {
        if (isKitchenOpen)
        {
            DebugLog("Kitchen already open");
            return;
        }

        if (unloadOperation != null && !unloadOperation.isDone)
        {
            DebugLog("Kitchen is still unloading - ignoring open");
            return;
        }

        if (SceneManager.GetSceneByName(kitchenSceneName).isLoaded)
        {
            DebugLog("Kitchen scene already loaded - ignoring open");
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

        // UnloadSceneAsync throws if the scene is not loaded (e.g. a Single scene load already
        // removed it), so check first.
        var kitchenScene = SceneManager.GetSceneByName(kitchenSceneName);
        if (kitchenScene.isLoaded)
            unloadOperation = SceneManager.UnloadSceneAsync(kitchenScene);

        isKitchenOpen = false;

        DebugLog("Kitchen scene unloaded");
    }

    /// <summary>
    /// Disable every currently-enabled game collider (not on the Kitchen layer), remembering
    /// exactly which ones we turned off. Scans the scene each time so later-spawned objects are included.
    /// </summary>
    private void DisableGameInteractions()
    {
        var startTime = Time.realtimeSinceStartup;

        disabledGameColliders.Clear();
        disabledGameColliders3D.Clear();

        var kitchenLayer = LayerMask.NameToLayer("Kitchen");

        foreach (var col in FindObjectsOfType<Collider2D>(true)) // true = include inactive
            if (col.enabled && col.gameObject.layer != kitchenLayer)
            {
                col.enabled = false;
                disabledGameColliders.Add(col);
            }

        foreach (var col in FindObjectsOfType<Collider>(true))
            if (col.enabled && col.gameObject.layer != kitchenLayer)
            {
                col.enabled = false;
                disabledGameColliders3D.Add(col);
            }

        var duration = (Time.realtimeSinceStartup - startTime) * 1000f;
        DebugLog(
            $"Disabled {disabledGameColliders.Count} 2D + {disabledGameColliders3D.Count} 3D colliders in {duration:F2}ms");
    }

    /// <summary>
    /// Re-enable only the colliders that DisableGameInteractions turned off.
    /// </summary>
    private void EnableGameInteractions()
    {
        var startTime = Time.realtimeSinceStartup;

        var enabledCount = 0;

        foreach (var col in disabledGameColliders)
            if (col != null) // Check for destroyed objects
            {
                col.enabled = true;
                enabledCount++;
            }

        foreach (var col in disabledGameColliders3D)
            if (col != null)
            {
                col.enabled = true;
                enabledCount++;
            }

        disabledGameColliders.Clear();
        disabledGameColliders3D.Clear();

        var duration = (Time.realtimeSinceStartup - startTime) * 1000f;
        DebugLog($"Re-enabled {enabledCount} colliders in {duration:F2}ms");
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"[KitchenSceneManager] {message}");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
        {
            isKitchenOpen = false;
            unloadOperation = null;
            disabledGameColliders.Clear();
            disabledGameColliders3D.Clear();
            DebugLog($"Scene '{scene.name}' loaded — kitchen state and collider list reset.");
        }
    }
}