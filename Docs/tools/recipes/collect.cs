var dt = UnityEngine.Object.FindObjectOfType<DailyTreats>(); if (dt == null) return "no treats";
var m = typeof(DailyTreats).GetMethod("Collect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
m.Invoke(dt, null); return "collected";
