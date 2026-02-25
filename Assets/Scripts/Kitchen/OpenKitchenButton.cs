using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the KitchenButton in the game scene.
/// Unity's Button handles the pressed sprite via Sprite Swap transition.
/// This script adds click sound.
///
/// SETUP:
/// - Button component Transition = Sprite Swap
/// - Assign your pressed sprite in Button's Pressed Sprite field
/// - Assign clickSound in Inspector
/// </summary>
public class OpenKitchenButton : MonoBehaviour
{
    [Header("Audio")] public AudioClip clickSound;

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