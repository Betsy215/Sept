// r33: every open order: is it thinking, does it have its dots, what is its first food
var os = UnityEngine.Object.FindObjectOfType<OrderSystem>(); if (os == null) return "none";
var FL = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var orders = (System.Collections.Generic.List<OrderSystem.ActiveOrder>)typeof(OrderSystem).GetField("orders", FL).GetValue(os);
var r = "";
foreach (var o in orders) r += (o.customer != null ? o.customer.name : "?") + " first=" + (o.items.Count > 0 ? o.items[0].foodType : "-") + " thinking=" + o.thinking + " dots=" + (o.thinkDots != null) + " ; ";
return r == "" ? "no orders" : r;
