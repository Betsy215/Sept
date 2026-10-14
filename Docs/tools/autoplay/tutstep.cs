// the tutorial waits for taps on its own targets: answer the one it is waiting for, or press Let's go on the end card
var td = UnityEngine.Object.FindObjectOfType<TutorialDirector>(); if (td == null) return "T|none";
var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var t = typeof(TutorialDirector);
var end = (GameObject)t.GetField("endCard", F).GetValue(td);
if (end != null && end.activeInHierarchy) { t.GetMethod("OnLetsGo", F).Invoke(td, null); return "T|letsgo"; }
var hit = t.GetField("awaitingHit", F);
if (hit.GetValue(td) != null) { hit.SetValue(td, null); t.GetField("tapped", F).SetValue(td, true); return "T|tapped"; }
var cap = (TMPro.TextMeshProUGUI)t.GetField("caption", F).GetValue(td);
return "T|wait|" + (cap != null ? cap.text : "");
