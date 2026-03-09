public class BreadOvenInKitchen : OvenKitchenBase
{
    protected override void OnRefill()
    {
        var bread = FindObjectOfType<Bread>();
        if (bread != null)
        {
            bread.RefillToFull();
            DebugLog("Bread refilled to full!");
        }
        else
        {
            DebugLog("ERROR: Bread not found in game scene!");
        }
    }
}