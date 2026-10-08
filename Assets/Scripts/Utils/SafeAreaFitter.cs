using UnityEngine;

/// <summary>
/// Keeps a UI panel inside the device safe area (below the notch or Dynamic Island, above the
/// home indicator). Put it on a full-stretch RectTransform that is a direct child of the Canvas,
/// then parent the edge-anchored buttons (pause, settings, shop bottom buttons, kitchen back)
/// under that panel. Re-applies when the screen or orientation changes.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    [Tooltip("Ignore the top inset (notch). Leave on for panels that hold top buttons.")]
    public bool applyTop = true;

    [Tooltip("Ignore the bottom inset (home indicator). Leave on for panels that hold bottom buttons.")]
    public bool applyBottom = true;

    private RectTransform rect;
    private Rect lastSafeArea;
    private Vector2Int lastScreen;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void Update()
    {
        // Cheap struct compares; only re-applies when something actually changed
        if (Screen.safeArea != lastSafeArea || Screen.width != lastScreen.x || Screen.height != lastScreen.y)
            Apply();
    }

    private void Apply()
    {
        if (rect == null) rect = GetComponent<RectTransform>();
        var safe = Screen.safeArea;
        lastSafeArea = safe;
        lastScreen = new Vector2Int(Screen.width, Screen.height);

        var min = safe.position;
        var max = safe.position + safe.size;
        if (!applyBottom) min.y = 0f;
        if (!applyTop) max.y = Screen.height;

        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
