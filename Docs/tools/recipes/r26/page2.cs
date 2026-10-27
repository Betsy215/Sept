var map = UnityEngine.Object.FindObjectOfType<MapScreen>(); if (map == null) return "no map"; var FL = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
var f = typeof(MapScreen).GetField("page", FL); var info = f != null ? "page field " + f.FieldType.Name + " = " + f.GetValue(map) : "no page field";
foreach (var n in new[]{"NextPage","ShowPage","GoToPage","SetPage"}) { var m = typeof(MapScreen).GetMethod(n, FL); if (m != null) { if (m.GetParameters().Length == 0) m.Invoke(map, null); else m.Invoke(map, new object[] { 1 }); return info + " | called " + n; } }
return info;
