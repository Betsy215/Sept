// chapter 2 demo check (26 October 2026, owner: "the ice cream should get served, not it shakes"): glass 0's contents,
// whether an order is still open, the caption
var st = UnityEngine.Object.FindObjectOfType<IceCreamStation>(); var os = UnityEngine.Object.FindObjectOfType<OrderSystem>();
var FL = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
var s = "";
if (st != null) { var dishes = (System.Collections.IList)typeof(IceCreamStation).GetField("dishes", FL).GetValue(st); var d = dishes[0]; var t = d.GetType();
    s += "glass0 waffle " + t.GetField("waffle").GetValue(d) + " scoops " + ((System.Collections.ICollection)t.GetField("flavours").GetValue(d)).Count; }
if (os != null) s += " | order open " + os.IsOrderActive() + " " + string.Join(",", os.GetCurrentOrderTypesWithVariants());
var dir = UnityEngine.Object.FindObjectOfType<TutorialDirector>();
if (dir != null) { var cap = typeof(TutorialDirector).GetField("caption", FL).GetValue(dir) as TMPro.TextMeshProUGUI; var box = typeof(TutorialDirector).GetField("captionBox", FL).GetValue(dir) as RectTransform;
    s += " | caption '" + (cap != null ? cap.text : "") + "' shown " + (box != null && box.gameObject.activeSelf); }
return s;
