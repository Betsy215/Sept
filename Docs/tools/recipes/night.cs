var t = UnityEngine.Object.FindObjectOfType<DayNightTint>(); if (t == null) return "no tint";
var f = typeof(DayNightTint).GetField("forceProgress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
return f == null ? "no forceProgress field" : "has field";
