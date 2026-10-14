var map = UnityEngine.Object.FindObjectOfType<MapScreen>(); if (map == null) return "no map";
var m = typeof(MapScreen).GetMethod("OpenDay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
m.Invoke(map, new object[] { 2 }); return "day 3 board open";
