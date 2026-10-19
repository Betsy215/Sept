var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); if (lm == null) return "no LevelManager";
var ft = typeof(LevelManager).GetField("dayTimeLeft", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); ft.SetValue(lm, 1.5f); return "clock near end";
