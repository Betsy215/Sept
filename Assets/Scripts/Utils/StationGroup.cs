using UnityEngine;

/// The coffee station moves as one piece (owner, 16 October): the machine is the handle the player drags, and the
/// tray and the cups follow it every frame at the offsets FixedStation gave them. Lives on the FixedStation object.
public class StationGroup : MonoBehaviour
{
    public Transform handle;          // the coffee machine
    public Transform[] followers;     // the cups and the tray (this object)
    private Vector3[] offsets;
    private Vector3 lastHandle;

    public void Bind(Transform handle, params Transform[] followers)
    {
        this.handle = handle; this.followers = followers;
        offsets = new Vector3[followers.Length];
        for (var i = 0; i < followers.Length; i++) offsets[i] = followers[i] != null ? followers[i].position - handle.position : Vector3.zero;
        lastHandle = handle.position;
    }

    private void LateUpdate()
    {
        if (handle == null || followers == null) return;
        if ((handle.position - lastHandle).sqrMagnitude < 1e-8f) return;
        for (var i = 0; i < followers.Length; i++)
            if (followers[i] != null) followers[i].position = new Vector3(handle.position.x + offsets[i].x, handle.position.y + offsets[i].y, followers[i].position.z);
        lastHandle = handle.position;
    }
}
