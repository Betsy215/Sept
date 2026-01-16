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
        var draggable = ((MonoBehaviour)this).GetComponent<DraggableFood>();
        if (draggable != null) draggable.Initialize(tableLayer, gamePhaseManager, allFoodItems);
    }
}