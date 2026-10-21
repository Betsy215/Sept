ServeableItem tray = null; foreach (var it in UnityEngine.Object.FindObjectsOfType<ServeableItem>()) if (it.GetFoodType() == "Coconut") tray = it;
if (tray == null) return "no tray";
var ri = tray.GetComponent<RefillableItem>(); var f = typeof(RefillableItem).GetField("currentCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
f.SetValue(ri, 0);
var co = UnityEngine.Object.FindObjectOfType<CoconutInKitchen>(); if (co == null) return "no coconuts";
var a = co.Take(); var b = co.Take();
var img = co.GetComponent<UnityEngine.UI.Image>();
return "V|take|took=" + a + b + "|count=" + CoconutInKitchen.Count + "|sprite=" + img.sprite.name + "|tray=" + f.GetValue(ri);
