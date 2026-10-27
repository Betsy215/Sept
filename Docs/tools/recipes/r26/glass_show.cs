// a staged glass row for store shots: bowls in the glasses, a scoop heap in the first, the third lifted with three scoops
var st = UnityEngine.Object.FindObjectOfType<IceCreamStation>(); if (st == null) return "no station";
var FL = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
st.RefillCones(); st.RefillAll();
var dishes = (System.Collections.IList)typeof(IceCreamStation).GetField("dishes", FL).GetValue(st);
st.OnTap(IceCreamTap.Kind.Dish, 0); st.OnTap(IceCreamTap.Kind.Section, 1); st.OnTap(IceCreamTap.Kind.Section, 0); st.OnTap(IceCreamTap.Kind.Cross, 0);
st.OnTap(IceCreamTap.Kind.Dish, 2); st.OnTap(IceCreamTap.Kind.Section, 2); st.OnTap(IceCreamTap.Kind.Section, 3); st.OnTap(IceCreamTap.Kind.Section, 1);
return "glasses staged (" + dishes.Count + " glasses)";
