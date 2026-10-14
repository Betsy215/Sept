using UnityEngine;
using UnityEngine.UI;

/// The kitchen button (the little truck at the left) sits a third of its height on the tablecloth (owner,
/// 12 October: "put the truck icon 1/3 on the table cloth"). The main Canvas draws under the cloth (the cloth is on
/// the Table sorting layer), so the button moves into its own canvas on the FoodItems layer, with the same camera
/// and scaler, and is lowered so its bottom third overlaps the cloth's top edge (canvas y 0 = screen middle = cloth top).
public static class KitchenButtonOverTable
{
    const float OverlapShare = 0.56f;   // its bottom on the hen and pots' line (owner, 13 October)

    public static void Setup()
    {
        var button = GameObject.Find("KitchenButton");
        if (button == null) return;
        var rt = button.GetComponent<RectTransform>();
        var srcCanvas = button.GetComponentInParent<Canvas>();
        if (rt == null || srcCanvas == null || button.transform.parent.name == "KitchenButtonCanvas") return;

        var go = new GameObject("KitchenButtonCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = srcCanvas.renderMode;
        canvas.worldCamera = srcCanvas.worldCamera;
        canvas.planeDistance = srcCanvas.planeDistance;
        canvas.sortingLayerName = "FoodItems";
        canvas.sortingOrder = 3;                                   // over the cloth (Table) and its edge line
        var srcScaler = srcCanvas.GetComponent<CanvasScaler>();
        var scaler = go.GetComponent<CanvasScaler>();
        if (srcScaler != null)
        {
            scaler.uiScaleMode = srcScaler.uiScaleMode;
            scaler.referenceResolution = srcScaler.referenceResolution;
            scaler.screenMatchMode = srcScaler.screenMatchMode;
            scaler.matchWidthOrHeight = srcScaler.matchWidthOrHeight;
            scaler.referencePixelsPerUnit = srcScaler.referencePixelsPerUnit;
        }
        var anchored = rt.anchoredPosition;
        var anchorMin = rt.anchorMin; var anchorMax = rt.anchorMax; var size = rt.sizeDelta; var pivot = rt.pivot;
        rt.SetParent(go.transform, false);
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.sizeDelta = size; rt.pivot = pivot;
        // anchored to the left middle: y 0 is the cloth's top edge; bottom third of the button below it
        rt.anchoredPosition = new Vector2(anchored.x, size.y * (0.5f - OverlapShare));
        var wobble = button.GetComponent<BalatroWobble>();   // it writes its rest position every frame
        if (wobble != null) wobble.Rebase();
    }
}
