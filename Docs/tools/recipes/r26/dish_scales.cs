// reports each glass's scale against its resting scale and which one is selected
var st = UnityEngine.Object.FindObjectOfType<IceCreamStation>(); if (st == null) return "no station";
var FL = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
var dishes = (System.Collections.IList)typeof(IceCreamStation).GetField("dishes", FL).GetValue(st);
var sel = (int)typeof(IceCreamStation).GetField("selected", FL).GetValue(st); var s = "selected " + sel + ":";
foreach (var d in dishes) { var t = d.GetType(); var root = (Transform)t.GetField("root").GetValue(d); var sc = (float)t.GetField("scale").GetValue(d); s += " " + (root.localScale.x / sc).ToString("0.00"); }
return s;
