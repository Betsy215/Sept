// r33: the tutorial's caption now, and whether it waits for a tap
var dir = UnityEngine.Object.FindObjectOfType<TutorialDirector>(); if (dir == null) return "no director";
var FL = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var cap = typeof(TutorialDirector).GetField("caption", FL).GetValue(dir) as TMPro.TextMeshProUGUI;
var swipe = (int)typeof(TutorialDirector).GetField("awaitingSwipe", FL).GetValue(dir);
return (cap != null ? cap.text : "") + " | waiting " + (typeof(TutorialDirector).GetField("awaitingHit", FL).GetValue(dir) != null) + " swipe " + swipe + " | apples " + AppleInKitchen.Count;
