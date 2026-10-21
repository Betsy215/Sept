var co = UnityEngine.Object.FindObjectOfType<CoconutInKitchen>(); if (co == null) return "no coconuts";
var img = co.GetComponent<UnityEngine.UI.Image>();
return "V|coconuts|count=" + CoconutInKitchen.Count + "|sprite=" + (img != null && img.sprite != null ? img.sprite.name : "none") + "|shown=" + (img != null && img.enabled);
