// sets the four wells to 4, 3, 2 and 1 scoops so one shot shows every level (ChatGPT fills, 26 October 2026)
var st = UnityEngine.Object.FindObjectOfType<IceCreamStation>(); if (st == null) return "no station";
var FL = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var left = (int[])typeof(IceCreamStation).GetField("left", FL).GetValue(st);
for (var i = 0; i < left.Length; i++) left[i] = IceCreamStation.PerSection - i;
typeof(IceCreamStation).GetMethod("RefreshSections", FL).Invoke(st, null);
return "wells " + string.Join(",", left);
