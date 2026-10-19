var shop = UnityEngine.Object.FindObjectOfType<ShopManager>(); if (shop == null) return "no shop";
var info = shop.popupItemInfo;
var lines = info.text.Split('\n');
lines[0] = "<font=\"beachday SDF\"><size=72>" + System.Text.RegularExpressions.Regex.Replace(lines[0], "<[^>]+>", "") + "</size></font>";
info.text = "<color=#9FD2FF>" + lines[0] + "</color>\n" + string.Join("\n", lines, 1, lines.Length - 1);
return "mixed";
