import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-700:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex(open(A + "goday.cs").read().replace("DAY", "0"))); time.sleep(8)
print(ex(open(A + "startday.cs").read()))
for i in range(60):
    r = ex('''var os = UnityEngine.Object.FindObjectOfType<OrderSystem>(); var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var list = (System.Collections.IList)typeof(OrderSystem).GetField("orders", F).GetValue(os); int fresh = 0; foreach (OrderSystem.ActiveOrder o in list) if (o.active && o.timer > o.displayTime - 1.2f) fresh++;
if (list.Count >= 2 && fresh >= 1) { os.freezeTimer = true; foreach (OrderSystem.ActiveOrder o in list) o.timer = o.displayTime; return "ready " + list.Count; } return "wait " + list.Count;''')
    if r.startswith("ready"): print(r); break
    time.sleep(0.3)
time.sleep(1.2)
ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/flowers_new.png"); return "snap";'); time.sleep(1.2)
U.call("manage_editor", {"action": "stop"})
