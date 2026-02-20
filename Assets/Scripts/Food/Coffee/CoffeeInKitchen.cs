using UnityEngine;

public class CoffeeInKitchen : MonoBehaviour
{
    [Header("Audio")] public AudioClip refillSound;

    [Header("Debug")] public bool enableDebugLogs = true;

    private void OnMouseUpAsButton()
    {
        DebugLog("Coffee clicked - refilling beans...");

        // Play sound
        if (refillSound != null)
        {
            AudioManager.Instance.PlaySFX(refillSound);
            DebugLog("Playing refill sound");
        }

        // Find and refill BeanContainer
        var beanContainer = FindObjectOfType<BeanContainer>();

        if (beanContainer != null)
        {
            // Refill beans by calling its OnMouseUpAsButton (which sets beans to 6)
            beanContainer.SendMessage("OnMouseUpAsButton");
            DebugLog("Beans refilled to full!");
        }
        else
        {
            DebugLog("ERROR: BeanContainer not found!");
        }
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"[CoffeeController] {message}");
    }
}