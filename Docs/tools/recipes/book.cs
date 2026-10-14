var mb = UnityEngine.Object.FindObjectOfType<MenuBook>(); if (mb == null) return "no MenuBook";
var a = System.IO.File.ReadAllText("/tmp/ap/book_arg.txt").Trim();
if (a == "open") { mb.Open(); return "open " + mb.IsOpen; }
if (a == "close") { mb.Close(); return "closed"; }
if (a == "state") return "open " + mb.IsOpen;
if (a.StartsWith("tap:")) return "tap " + a.Substring(4) + ": " + mb.TapSticker(a.Substring(4));
return "toggle " + a + ": " + mb.Toggle(a);
