var st = UnityEngine.Object.FindObjectOfType<IceCreamStation>(); if (st == null) return "no station";
var bl = UnityEngine.Object.FindObjectOfType<BlenderAnim>(); if (bl == null) return "no blender";
st.OnTap(IceCreamTap.Kind.Holder, 0); st.OnTap(IceCreamTap.Kind.Section, 0); st.OnTap(IceCreamTap.Kind.Section, 0); st.OnTap(IceCreamTap.Kind.Cross, 0);
var before = st.Left(0);
bl.Blend();
var after = st.Left(0);
var co = UnityEngine.Object.FindObjectOfType<CoconutInKitchen>(); var took = co != null && co.Take() && co.Take();
return "V|blender|before=" + before + "|rightAfterBlend=" + after + "|blending=" + bl.IsBlending + "|coconutsTaken2=" + took + "|count=" + CoconutInKitchen.Count;
