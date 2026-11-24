// r34 (23 Nov 2026): the editor has no home bar; give the table the iPhone 6.9-inch bounds (34 pt home bar = 0.356 world
// more at the bottom) and run the start layout again, as a phone would
var tl = UnityEngine.Object.FindObjectOfType<TableLayer>(); if (tl == null) return "no TableLayer";
tl.bottomPadding = 0.1f + 0.3556f;
typeof(TableLayer).GetMethod("UpdateTableBounds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(tl, null);
UnityEngine.Object.FindObjectOfType<GamePhaseManager>().StartArrangementPhase();
var b = tl.InteractiveBounds; return "bounds y " + b.min.y.ToString("F3") + " .. " + b.max.y.ToString("F3");
