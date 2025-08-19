using UnityEngine;

public class TransitionSetup : MonoBehaviour
{
    [Header("Transition Settings")]
    [Tooltip("How long the entire transition takes (fade out + fade in)")]
    public float transitionDuration = 1f;
    
    [Tooltip("Color to transition through (usually black)")]
    public Color transitionColor = Color.black;
    
    void Awake()
    {
        // Ensure SceneTransitionManager exists
        if (SceneTransitionManager.Instance == null)
        {
            Debug.Log("TransitionSetup: Creating SceneTransitionManager");
            
            // Create SceneTransitionManager
            GameObject transitionManagerGO = new GameObject("SceneTransitionManager");
            SceneTransitionManager transitionManager = transitionManagerGO.AddComponent<SceneTransitionManager>();
            
            // Apply settings
            transitionManager.transitionDuration = transitionDuration;
            transitionManager.fadeColor = transitionColor;
            transitionManager.autoCreateTransitionUI = true;
        }
        else
        {
            Debug.Log("TransitionSetup: SceneTransitionManager already exists");
        }
    }
}