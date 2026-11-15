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
        // the way back as a pull handle on the right edge instead of the truck on the mat (owner, 15 November 2026)
        if (KitchenButtonOverTable.Handle) PullHandle.Attach((RectTransform)transform, -1, true, PullHandle.KitchenHeight);
        DebugLog("CloseKitchenButton initialized");
    }

    private void OnButtonClick()
    {
        if (clickSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(clickSound);

        // the pull handle bounces first, then the kitchen slides away as on a swipe
        var handle = GetComponent<PullHandle>();
        if (handle != null && handle.Pull(() =>
            {
                var ksm = KitchenSceneManager.Instance;
                if (ksm != null && ksm.IsKitchenOpen && KitchenSwipe.Swipe(-1) != "closing") ksm.CloseKitchen();
            })) return;

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