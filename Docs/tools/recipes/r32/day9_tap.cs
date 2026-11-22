var m = UnityEngine.Object.FindObjectOfType<MapScreen>(); if (m == null) return "no map";
typeof(MapScreen).GetMethod("TapDay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(m, new object[] { 8, true });
return "day 9 tapped";
