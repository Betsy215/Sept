import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-600:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex(open(A + "goday.cs").read().replace("DAY", "0"))); time.sleep(7)
print(ex(open(A + "startday.cs").read()))
T = '''var os = UnityEngine.Object.FindObjectOfType<OrderSystem>(); var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var list = (System.Collections.IList)typeof(OrderSystem).GetField("orders", F).GetValue(os); var sb = new System.Text.StringBuilder();
if (list.Count < 2) return "wait";
foreach (OrderSystem.ActiveOrder o in list) sb.Append(" order" + o.slot + " " + o.timer.ToString("F2") + "/" + o.displayTime.ToString("F1"));
var n = os.GiveFlowerToAll(); sb.Append(" -> gave " + n + ":");
foreach (OrderSystem.ActiveOrder o in list) sb.Append(" order" + o.slot + " " + o.timer.ToString("F2") + "/" + o.displayTime.ToString("F1"));
return sb.ToString();'''
for _ in range(40):
    r = ex(T)
    if r != "wait": print(r); break
    time.sleep(0.4)
U.call("manage_editor", {"action": "stop"})
