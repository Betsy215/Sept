var b = UnityEngine.Object.FindObjectOfType<BlenderAnim>(); if (b == null) return "no blender";
var F = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
foreach (var n in new[]{"Blend","StartBlend","OnTap","Tap","OnPointerClick","Play","Run"}) { var m = typeof(BlenderAnim).GetMethod(n, F); if (m != null && m.GetParameters().Length == 0) { m.Invoke(b, null); return "called " + n; } }
return "no blend method: " + string.Join(",", System.Array.ConvertAll(typeof(BlenderAnim).GetMethods(F), x => x.Name));
