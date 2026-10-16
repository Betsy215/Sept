using UnityEngine;

/// <summary>
/// A table decoration that shows only once it is bought in the shop (the flower pots, owner 16 October 2026).
/// The sprite keeps its place in the scene; this only switches the renderer on or off from the save.
/// </summary>
public class DecorGate : MonoBehaviour
{
    public string decorName;

    private void Start() => Apply();
    private void OnEnable() => Apply();

    public void Apply()
    {
        var on = SessionManager.Instance != null && SessionManager.Instance.IsDecorPurchased(decorName);
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = on;
    }
}
