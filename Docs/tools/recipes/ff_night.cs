var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); if (lm == null) return "no LevelManager";
var f = typeof(LevelManager).GetField("dayTimeLeft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
f.SetValue(lm, lm.DayDuration * 0.12f); return "night: " + ((float)f.GetValue(lm)).ToString("F1");
