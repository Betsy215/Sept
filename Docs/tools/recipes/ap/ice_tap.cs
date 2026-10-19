var st = UnityEngine.Object.FindObjectOfType<IceCreamStation>(); if (st == null) return "no station";
st.OnTap((IceCreamTap.Kind)System.Enum.Parse(typeof(IceCreamTap.Kind), "KIND"), IDX); return "tapped KIND IDX, cone " + st.Variant() + " building " + st.IsBuilding;
