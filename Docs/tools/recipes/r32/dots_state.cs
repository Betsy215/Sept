var os = UnityEngine.Object.FindObjectOfType<OrderSystem>(); if (os == null) return "none";
string r = "";
foreach (var d in GameObject.FindObjectsOfType<Transform>()) if (d.name == "ThinkDots") { var b = d.parent.parent; string bub = ""; foreach (Transform c in d.parent.parent) if (c.name.StartsWith("Bubble") && c.gameObject.activeSelf) bub += c.name; r += "dots in " + bub + " "; }
return r == "" ? "no dots" : r;
