// r29 (18 November 2026): two Ready buttons since today (table, then kitchen); the captures answer both at once.
var g = UnityEngine.Object.FindObjectOfType<GamePhaseManager>(); if (g == null) return "no GamePhaseManager";
if (g.GetCurrentPhase() != GamePhase.ARRANGEMENT) return "phase " + g.GetCurrentPhase();
var f = typeof(GamePhaseManager).GetField("kitchenReady", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); if (f != null) f.SetValue(g, true);
g.OnDoneButtonClicked(); return "phase " + g.GetCurrentPhase();
