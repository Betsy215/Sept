public class CakeOvenInKitchen : OvenKitchenBase
{
    protected override void OnRefill()
    {
        var cake = FindObjectOfType<Cake>();
        if (cake != null)
        {
            cake.RefillToFull();
            DebugLog("Cake refilled to full!");
        }
        else
        {
            DebugLog("ERROR: Cake not found in game scene!");
        }
    }
}