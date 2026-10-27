// one autoplay action with the glass-dish ice cream flow (26 October 2026)
var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); if (lm == null) return "S|noLM";
if (lm.levelCompletePanel != null && lm.levelCompletePanel.activeSelf) return "S|done";
var os = UnityEngine.Object.FindObjectOfType<OrderSystem>(); if (os == null) return "S|noOS";
var tv = os.GetCurrentOrderTypesWithVariants(); if (tv.Count == 0) return "S|idle";
var FL = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
var first = tv[0];
if (first.StartsWith("IceCream:"))
{
    var st = UnityEngine.Object.FindObjectOfType<IceCreamStation>(); var want = first.Substring(9);
    var dishes = (System.Collections.IList)typeof(IceCreamStation).GetField("dishes", FL).GetValue(st);
    System.Func<object, bool> bw = d => (bool)d.GetType().GetField("waffle").GetValue(d);
    System.Func<object, int> sc = d => ((System.Collections.ICollection)d.GetType().GetField("flavours").GetValue(d)).Count;
    var bowl = want.IndexOf(IceCreamStation.WaffleLetter) >= 0; var letters = want.Replace(IceCreamStation.WaffleLetter.ToString(), "");
    foreach (var ch in letters) if (st.Left(IceCreamStation.Letters.IndexOf(ch)) <= 0) { st.RefillAll(); return "S|blender"; }
    var pick = -1; for (var i = 0; i < dishes.Count; i++) if (sc(dishes[i]) == 0 && bw(dishes[i]) == bowl) { pick = i; break; }
    if (pick < 0 && bowl) { var empty = false; for (var i = 0; i < dishes.Count; i++) if (sc(dishes[i]) == 0 && !bw(dishes[i])) empty = true; if (empty) { if (!st.IsBakingWaffles) st.OnTap(IceCreamTap.Kind.Waffle, 0); return "S|bake"; } }
    if (pick < 0) { for (var i = 0; i < dishes.Count; i++) if (sc(dishes[i]) > 0 || bw(dishes[i]) != bowl) { st.OnTap(IceCreamTap.Kind.Dish, i); st.OnTap(IceCreamTap.Kind.Trash, i); return "S|trash"; } return "S|wait"; }
    st.OnTap(IceCreamTap.Kind.Dish, pick); foreach (var ch in letters) st.OnTap(IceCreamTap.Kind.Section, IceCreamStation.Letters.IndexOf(ch)); st.OnTap(IceCreamTap.Kind.Dish, pick);
    return "S|served|" + first;
}
var ev = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
foreach (var it in UnityEngine.Object.FindObjectsOfType<ServeableItem>())
{
    if (it.GetFoodType() != first || !it.gameObject.activeInHierarchy) continue;
    var ri = it.GetComponent<RefillableItem>();
    if (ri != null) { var n = (int)typeof(RefillableItem).GetField("currentCount", FL).GetValue(ri); if (n <= 0) { if (first == "Coffee") { var cm = UnityEngine.Object.FindObjectOfType<CoffeeMachine>(); if (cm != null) cm.OnPointerClick(ev); return "S|brew"; } ri.IncreaseCount(99); return "S|refill|" + first; } }
    it.OnPointerDown(ev); it.OnPointerUp(ev); it.OnPointerClick(ev); return "S|served|" + first;
}
return "S|missing|" + first;
