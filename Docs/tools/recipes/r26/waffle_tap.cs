var st = UnityEngine.Object.FindObjectOfType<IceCreamStation>(); if (st == null) return "no station";
var FL = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
var dishes = (System.Collections.IList)typeof(IceCreamStation).GetField("dishes", FL).GetValue(st);
foreach (var d in dishes) { var t = d.GetType(); t.GetField("waffle").SetValue(d, false); ((System.Collections.IList)t.GetField("flavours").GetValue(d)).Clear(); typeof(IceCreamStation).GetMethod("RefreshDish", FL).Invoke(st, new object[] { d }); }
st.OnTap(IceCreamTap.Kind.Waffle, 0); return "baking " + st.IsBakingWaffles;
