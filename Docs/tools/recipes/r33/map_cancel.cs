// r33: closes the map's day card (it opens by itself)
var m = UnityEngine.Object.FindObjectOfType<MapScreen>(); if (m == null) return "no map";
var FL = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var f = typeof(MapScreen).GetField("popup", FL); var p = f.GetValue(m) as GameObject; if (p == null) return "no card";
UnityEngine.Object.Destroy(p); f.SetValue(m, null); return "card closed";
