// r34: presses Let's go on day 3's candy gift card when it is up (the autoplay drivers)
var g = GameObject.Find("CandyGift"); if (g == null) return "no gift";
foreach (var b in g.GetComponentsInChildren<UnityEngine.UI.Button>()) if (b.name == "LetsGo") { b.onClick.Invoke(); return "lets go pressed"; }
return "gift up, no button yet";
