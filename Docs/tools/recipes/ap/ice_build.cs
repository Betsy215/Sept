var st = UnityEngine.Object.FindObjectOfType<IceCreamStation>(); if (st == null) return "no station";
st.OnTap(IceCreamTap.Kind.Holder, 0); st.OnTap(IceCreamTap.Kind.Section, 1); st.OnTap(IceCreamTap.Kind.Section, 2); st.OnTap(IceCreamTap.Kind.Section, 0);
return "cone built: " + st.Variant();
