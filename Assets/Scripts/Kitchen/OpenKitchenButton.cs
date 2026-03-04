using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the KitchenButton in the game scene.
/// Unity's Button handles the pressed sprite via Sprite Swap transition.
/// This script adds click sound and dismisses the kitchen tutorial on click.
///
/// SETUP:
/// - Button component Transition = Sprite Swap
/// - Assign your pressed sprite in Button's Pressed Sprite field
/// - Assign clickSound in Inspector
/// - LevelManager is auto-found at Start (or assign manually in Inspector)
/// </summary>
public class OpenKitchenButton : MonoBehaviour
{
    [Header("Audio")] public AudioClip clickSound;

    [Header("References")] [Tooltip("Auto-found at Start if not assigned")]
    public LevelManager levelManager;

    [Header("Debug")] public bool enableDebugLogs = true;

    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();

        if (button == null)
        {
            Debug.LogError("OpenKitchenButton: No Button component found!");
            return;
        }

        // Auto-find LevelManager if not assigned in Inspector
        if (levelManager == null)
            levelManager = FindObjectOfType<LevelManager>();

        button.onClick.AddListener(OnButtonClick);
        DebugLog("OpenKitchenButton initialized");
    }

    private void OnButtonClick()
    {
        if (clickSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clickSound);


        if (KitchenSceneManager.Instance != null)
        {
            DebugLog("Opening kitchen...");
            KitchenSceneManager.Instance.OpenKitchen();
        }
        else
        {
            Debug.LogError("OpenKitchenButton: KitchenSceneManager.Instance is null!");
        }
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"[OpenKitchenButton] {message}");
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnButtonClick);
    }
}