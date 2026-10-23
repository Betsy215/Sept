// cones: four on the holder, one per cone started, none when empty, the waffle machine's Bake() refills (22 October)
var st = UnityEngine.Object.FindObjectOfType<IceCreamStation>(); if (st == null) return "no station";
var log = "start " + st.ConesLeft;
for (var i = 0; i < 5; i++) { st.OnTap(IceCreamTap.Kind.Cross, 0); st.OnTap(IceCreamTap.Kind.Holder, 0); log += " >" + st.ConesLeft + (st.IsBuilding ? "b" : "-"); }
st.OnTap(IceCreamTap.Kind.Cross, 0);
var wm = UnityEngine.Object.FindObjectOfType<WaffleAnim>(); if (wm == null) return log + " | no waffle machine (kitchen open?)";
wm.Bake(); log += " | baked " + st.ConesLeft + " baking " + wm.IsBaking;
return log;
