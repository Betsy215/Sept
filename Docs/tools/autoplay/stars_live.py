"""Live star display check (14 November 2026): day 1, the FIRST customer is made to leave through the game's own
ExpireOrder, everyone after is served by the autoplay step. Logs what the hen's stars show after every change.
Usage: stars_live.py tag [K=1]"""
import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t); return m.group(1) if m else t[-200:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
tag = sys.argv[1]; K = int(sys.argv[2]) if len(sys.argv) > 2 else 1
EXPIRE = '''var os = UnityEngine.Object.FindObjectOfType<OrderSystem>(); if (os == null) return "no os";
var f = typeof(OrderSystem).GetField("orders", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var list = (System.Collections.IList)f.GetValue(os); if (list == null || list.Count == 0) return "none waiting";
var m = typeof(OrderSystem).GetMethod("ExpireOrder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
m.Invoke(os, new object[] { list[0] }); return "expired one";'''
COUNTS = '''var os = UnityEngine.Object.FindObjectOfType<OrderSystem>(); var sc = UnityEngine.Object.FindObjectOfType<ScoreManager>();
var lm = UnityEngine.Object.FindObjectOfType<LevelManager>();
return "served " + (os != null ? os.CustomersServed : -1) + " lost " + (os != null ? os.CustomersLost : -1) + " open " + (os != null ? os.OpenOrders : -1) + " shown " + (sc != null && sc.starProgressBar != null ? sc.starProgressBar.GetEarnedStarsCount() : -1) + " left " + (lm != null ? lm.DayTimeLeft.ToString("F0") : "?");'''
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex(open(A + "goday.cs").read().replace("DAY", "0"))); time.sleep(7)
print(ex(open(A + "startday.cs").read())); time.sleep(1)
t0 = time.time(); forced = 0; last = ""
while time.time() - t0 < 200:
    if forced < K:
        r = ex(EXPIRE)
        if r.startswith("expired"):
            forced += 1; time.sleep(0.7)
            U.call("manage_camera", {"action": "screenshot", "screenshot_file_name": "starslive_" + tag + "_lost%d" % forced, "properties": {"include_image": False}})
    r = ex(open(A + "step.cs").read()); kind = r.split("|")[1] if "|" in r else r
    c = ex(COUNTS)
    if c.rsplit(" left", 1)[0] != last: print("  %5.1fs %s" % (time.time() - t0, c), flush=True); last = c.rsplit(" left", 1)[0]
    if kind == "done": break
    time.sleep(0.5)
time.sleep(2.5)
print("END", tag, ex(COUNTS))
U.call("manage_camera", {"action": "screenshot", "screenshot_file_name": "starslive_" + tag + "_end", "properties": {"include_image": False}}); time.sleep(1.5)
U.call("manage_editor", {"action": "stop"})
