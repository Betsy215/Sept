// one autoplay action: serve the oldest needed food; refill an empty item (as a kitchen trip would); report state
var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); if (lm == null) return "S|noLM";
if (lm.levelCompletePanel != null && lm.levelCompletePanel.activeSelf) return "S|done";
var os = UnityEngine.Object.FindObjectOfType<OrderSystem>(); if (os == null) return "S|noOS";
var types = os.GetCurrentOrderTypes();
if (types.Count == 0) return "S|idle|" + lm.DayTimeLeft.ToString("F1");
var food = types[0];
var ev = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
foreach (var it in UnityEngine.Object.FindObjectsOfType<ServeableItem>())
{
    if (it.GetFoodType() != food || !it.gameObject.activeInHierarchy) continue;
    var ri = it.GetComponent<RefillableItem>();
    if (ri != null) {
        var f = typeof(RefillableItem).GetField("currentCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var n = (int)f.GetValue(ri);
        if (n <= 0) {
            if (food == "Coffee") { var cm = UnityEngine.Object.FindObjectOfType<CoffeeMachine>(); if (cm != null) cm.OnPointerClick(ev); return "S|brew|" + food; }
            ri.IncreaseCount(99); return "S|refill|" + food;
        }
    }
    it.OnPointerDown(ev); it.OnPointerUp(ev); it.OnPointerClick(ev);
    return "S|served|" + food;
}
return "S|missing|" + food;
