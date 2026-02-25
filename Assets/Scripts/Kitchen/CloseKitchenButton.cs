using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the backButton in the KitchenScene.
/// Unity's Button handles the pressed sprite via Sprite Swap transition.
/// This script adds click sound.
///
/// SETUP:
/// - Button component Transition = Sprite Swap
/// - Assign your pressed sprite in Button's Pressed Sprite field
/// - Assign clickSound in Inspector
/// </summary>
public class CloseKitchenButton : MonoBehaviour
{
    [Header("Audio")] public AudioClip clickSound;

    [Header("Debug")] public bool enableDebugLogs = true;

    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();

        if (button == null)
        {
            Debug.LogError("CloseKitchenButton: No Button component found!");
            return;
        }

        button.onClick.AddListener(OnButtonClick);
        DebugLog("CloseKitchenButton initialized");
    }

    private void OnButtonClick()
    {
        if (clickSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clickSound);

        if (KitchenSceneManager.Instance != null)
        {
            DebugLog("Closing kitchen...");
            KitchenSceneManager.Instance.CloseKitchen();
        }
        else
        {
            Debug.LogError("CloseKitchenButton: KitchenSceneManager.Instance is null!");
        }
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"[CloseKitchenButton] {message}");
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnButtonClick);
    }
}