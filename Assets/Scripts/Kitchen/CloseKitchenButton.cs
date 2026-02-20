using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to a button to make it close the kitchen scene
/// Automatically finds KitchenSceneManager singleton
/// </summary>
public class CloseKitchenButton : MonoBehaviour
{
    private Button button;

    private void Start()
    {
        // Get the button component on this GameObject
        button = GetComponent<Button>();

        if (button == null)
        {
            Debug.LogError("CloseKitchenButton: No Button component found on this GameObject!");
            return;
        }

        // Wire up the button click event
        button.onClick.AddListener(OnButtonClick);

        Debug.Log("CloseKitchenButton: Successfully wired to button");
    }

    /// <summary>
    /// Called when button is clicked
    /// </summary>
    private void OnButtonClick()
    {
        // Find the singleton manager
        if (KitchenSceneManager.Instance != null)
        {
            Debug.Log("CloseKitchenButton: Closing kitchen...");
            KitchenSceneManager.Instance.CloseKitchen();
        }
        else
        {
            Debug.LogError("CloseKitchenButton: KitchenSceneManager.Instance is null!");
        }
    }

    /// <summary>
    /// Cleanup when destroyed
    /// </summary>
    private void OnDestroy()
    {
        // Remove listener to prevent memory leaks
        if (button != null) button.onClick.RemoveListener(OnButtonClick);
    }
}