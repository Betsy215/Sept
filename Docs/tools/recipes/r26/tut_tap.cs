var dir = UnityEngine.Object.FindObjectOfType<TutorialDirector>(); if (dir == null) return "no director";
var FL = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var fh = typeof(TutorialDirector).GetField("awaitingHit", FL); var ft = typeof(TutorialDirector).GetField("tapped", FL);
var cap = typeof(TutorialDirector).GetField("caption", FL).GetValue(dir) as TMPro.TextMeshProUGUI;
if (fh.GetValue(dir) == null) return "not waiting: " + (cap != null ? cap.text : "");
fh.SetValue(dir, null); ft.SetValue(dir, true); return "tapped at: " + (cap != null ? cap.text : "");
