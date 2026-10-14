var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); if (lm == null) return "no LevelManager";
var m = typeof(LevelManager).GetMethod("OnLevelComplete", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
if (m == null) return "no OnLevelComplete"; m.Invoke(lm, null); return "level complete forced";
