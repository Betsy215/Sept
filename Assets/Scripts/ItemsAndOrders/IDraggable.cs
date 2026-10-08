using UnityEngine;

public interface IDraggable
{
    // Default implementations - children don't need to implement these
    void SetDraggingEnabled(bool enabled)
    {
        var gameObject = ((MonoBehaviour)this).gameObject;
        var draggable = gameObject.GetComponent<DraggableFood>();

        if (draggable == null && enabled)
            draggable = gameObject.AddComponent<DraggableFood>();

        if (draggable != null)
            draggable.SetDraggingEnabled(enabled);
    }

    bool HasOverlap()
    {
        var draggable = ((MonoBehaviour)this).GetComponent<DraggableFood>();
        return draggable?.HasOverlap() ?? false;
    }

    void InitializeDragging(TableLayer tableLayer, GamePhaseManager gamePhaseManager, ServeableItem[] allFoodItems)
    {
        // DraggableFood is added at runtime (never in a scene or prefab). GamePhaseManager calls
        // InitializeDragging before SetDraggingEnabled, so create the component here too; otherwise the
        // first arrangement phase ran with no table bounds and no overlap list.
        var gameObject = ((MonoBehaviour)this).gameObject;
        var draggable = gameObject.GetComponent<DraggableFood>();
        if (draggable == null) draggable = gameObject.AddComponent<DraggableFood>();

        draggable.Initialize(tableLayer, gamePhaseManager, allFoodItems);
    }
}