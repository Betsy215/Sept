var mb = UnityEngine.Object.FindObjectOfType<MenuBook>(); if (mb == null) return "no MenuBook";
var f = typeof(MenuBook).GetField("carouselContent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var cc = f != null ? f.GetValue(mb) as UnityEngine.Transform : null; if (cc == null) return "no carousel";
var names = new System.Text.StringBuilder(); foreach (UnityEngine.Transform t in cc) names.Append(t.name).Append(",");
return "V|carousel|" + names;
