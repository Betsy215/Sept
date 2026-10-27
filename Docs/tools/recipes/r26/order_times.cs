// each open order's patience time against the level's base time (chapter 2 should be 1.2 x on top of customer and pots)
var os = UnityEngine.Object.FindObjectOfType<OrderSystem>(); if (os == null) return "no OrderSystem";
var FL = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
var orders = (System.Collections.IList)typeof(OrderSystem).GetField("orders", FL).GetValue(os);
var sm = SessionManager.Instance; var s = "chapter " + Chapter.Of(sm.GetCurrentLevelIndex()) + " base " + os.orderDisplayTime.ToString("0.0") + " pots x" + sm.PatienceMultiplier.ToString("0.00") + ":";
foreach (var o in orders) { var t = o.GetType(); var c = t.GetField("customer").GetValue(o) as CustomerController; var dt = (float)t.GetField("displayTime").GetValue(o);
    s += " " + (c != null ? c.name + " x" + c.OrderTimeMultiplier.ToString("0.00") : "?") + " -> " + dt.ToString("0.0") + "s (" + (dt / (os.orderDisplayTime * sm.PatienceMultiplier * (c != null ? c.OrderTimeMultiplier : 1f))).ToString("0.00") + ")"; }
return s;
