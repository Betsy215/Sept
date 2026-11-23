// r33 (23 Nov 2026): finishes the oldest open order with a given share of its patience left (__SHARE__), for the tip levels
var os = UnityEngine.Object.FindObjectOfType<OrderSystem>(); if (os == null) return "none";
var FL = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var orders = (System.Collections.Generic.List<OrderSystem.ActiveOrder>)typeof(OrderSystem).GetField("orders", FL).GetValue(os);
// the oldest open order: all its items but one are served, then every open order's patience is set, then the last
// item (each served item fills the meter to the next whole flower, so only the last serve decides the level)
OrderSystem.ActiveOrder first = null; foreach (var o in orders) if (o.active && !o.thinking && (first == null || o.startedAt < first.startedAt)) first = o;
if (first == null) return "no open order";
var left = first.items.FindAll(i => !i.isServed); if (left.Count == 0) return "nothing to serve";
for (var k = 0; k < left.Count - 1; k++) os.TryServeItem(left[k].foodType, left[k].variant);
foreach (var o in orders) if (o.active) o.timer = o.displayTime * __SHARE__f;
var last = left[left.Count - 1]; os.TryServeItem(last.foodType, last.variant);
return "served " + left.Count + " items, the last at share __SHARE__";
