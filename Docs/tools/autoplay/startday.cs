var g = UnityEngine.Object.FindObjectOfType<GamePhaseManager>(); if (g == null) return "no GamePhaseManager";
// two Ready buttons since 18 November 2026 (table, then kitchen): the test answers both at once
var f = typeof(GamePhaseManager).GetField("kitchenReady", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); if (f != null) f.SetValue(g, true);
g.OnDoneButtonClicked(); return "day started";
