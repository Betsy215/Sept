// lifted-glass check (owner, 26 October: "multiple empty glass larged"): taps the glasses fast, mid-animation
var st = UnityEngine.Object.FindObjectOfType<IceCreamStation>(); if (st == null) return "no station";
var FL = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
var dishes = (System.Collections.IList)typeof(IceCreamStation).GetField("dishes", FL).GetValue(st);
System.Collections.IEnumerator Go()
{
    var n = dishes.Count;
    for (var k = 0; k < 24; k++) { st.OnTap(IceCreamTap.Kind.Dish, (k * 3) % n); yield return new WaitForSeconds(0.04f + (k % 3) * 0.03f); }
    st.OnTap(IceCreamTap.Kind.Section, 0); yield return new WaitForSeconds(0.05f); st.OnTap(IceCreamTap.Kind.Dish, 1); yield return new WaitForSeconds(0.05f); st.OnTap(IceCreamTap.Kind.Dish, 1);
}
st.StartCoroutine(Go()); return "tapping " + dishes.Count;
