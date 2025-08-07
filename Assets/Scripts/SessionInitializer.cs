using UnityEngine;

public class SessionInitializer : MonoBehaviour
{
    void Awake()
    {
        // Ensure SessionManager exists in ANY scene
        if (SessionManager.Instance == null)
        {
            Debug.Log($"Creating SessionManager in scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            GameObject sessionManagerGO = new GameObject("SessionManager");
            sessionManagerGO.AddComponent<SessionManager>();
        }
    }
}