var os = UnityEngine.Object.FindObjectOfType<OrderSystem>(); if (os == null || !os.IsOrderActive()) return "no order";
var types = os.GetCurrentOrderTypes(); if (types.Count == 0) return "nothing to serve";
var food = types[0];
foreach (var it in UnityEngine.Object.FindObjectsOfType<ServeableItem>())
{
    if (it.GetFoodType() != food || !it.gameObject.activeInHierarchy) continue;
    var ev = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
    it.OnPointerDown(ev); it.OnPointerUp(ev); it.OnPointerClick(ev);
    return "served " + food;
}
return "no item for " + food;
