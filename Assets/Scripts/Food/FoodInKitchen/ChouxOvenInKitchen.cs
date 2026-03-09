using UnityEngine;

/// <summary>
/// Oven that bakes Choux.
/// Default bake time: 10s — adjust in Inspector.
/// </summary>
public class ChouxOvenInKitchen : OvenKitchenBase
{
    protected override void OnRefill()
    {
        var choux = FindObjectOfType<Choux>();
        if (choux != null)
        {
            choux.RefillToFull();
            DebugLog("Choux refilled to full!");
        }
        else
        {
            DebugLog("ERROR: Choux not found in game scene!");
        }
    }
}