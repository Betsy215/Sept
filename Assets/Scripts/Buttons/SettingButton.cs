using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SettingButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Visual Feedback Settings")]
    public Image targetImage; // The image to change color on
    [Range(0.5f, 1f)]
    public float pressedColorMultiplier = 0.7f; // How dark it gets when pressed
    
    [Header("Debug")]
    public bool showDebugMessages = true;
    
    // Store original color
    private Color originalColor;
    private bool isPressed = false;
    
    void Start()
    {
        // Auto-find the Image component if not assigned
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
        
        // Store the original color
        if (targetImage != null)
        {
            originalColor = targetImage.color;
            if (showDebugMessages)
            {
                Debug.Log($"VisualButtonFeedback: Original color stored for {gameObject.name}");
            }
        }
        else
        {
            Debug.LogWarning($"VisualButtonFeedback: No Image component found on {gameObject.name}");
        }
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (targetImage != null && !isPressed)
        {
            isPressed = true;
            
            // Make the color darker
            Color pressedColor = originalColor * pressedColorMultiplier;
            pressedColor.a = originalColor.a; // Keep original alpha
            targetImage.color = pressedColor;
            
            if (showDebugMessages)
            {
                Debug.Log($"VisualButtonFeedback: Button {gameObject.name} PRESSED - Color darkened");
            }
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (targetImage != null && isPressed)
        {
            isPressed = false;
            
            // Restore original color
            targetImage.color = originalColor;
            
            if (showDebugMessages)
            {
                Debug.Log($"VisualButtonFeedback: Button {gameObject.name} RELEASED - Color restored");
            }
        }
    }
    
    // Handle case where pointer leaves the button while pressed
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isPressed)
        {
            OnPointerUp(eventData);
            if (showDebugMessages)
            {
                Debug.Log($"VisualButtonFeedback: Pointer left {gameObject.name} while pressed - Color restored");
            }
        }
    }
    
    // Reset color if needed (useful for debugging)
    public void ResetColor()
    {
        if (targetImage != null)
        {
            targetImage.color = originalColor;
            isPressed = false;
            
            if (showDebugMessages)
            {
                Debug.Log($"VisualButtonFeedback: Color manually reset for {gameObject.name}");
            }
        }
    }
    
    // Method to change the original color (if button color changes during runtime)
    public void UpdateOriginalColor()
    {
        if (targetImage != null)
        {
            originalColor = targetImage.color;
            
            if (showDebugMessages)
            {
                Debug.Log($"VisualButtonFeedback: Original color updated for {gameObject.name}");
            }
        }
    }
}