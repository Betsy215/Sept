"""Coffee machine upgrade check (14 November 2026): level 2 machine on day 1, brew with the tray empty and with one cup on it."""
import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t); return m.group(1) if m else t[-300:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
CUPS = '''var cm = UnityEngine.Object.FindObjectOfType<CoffeeMachine>(); if (cm == null) return "no machine";
var F = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var r = (RefillableItem)typeof(CoffeeMachine).GetField("coffeeRefillableItem", F).GetValue(cm);
var sr = cm.GetComponent<SpriteRenderer>();
return "cups " + r.GetCurrentCount() + "/" + r.GetMaxCount() + " level " + typeof(CoffeeMachine).GetField("currentUpgradeLevel", F).GetValue(cm) + " active " + typeof(CoffeeMachine).GetField("activeBrewingLevel", F).GetValue(cm) + " brewing " + typeof(CoffeeMachine).GetField("isBrewing", F).GetValue(cm) + " frame " + (sr.sprite != null ? sr.sprite.name : "none") + " waiting " + (typeof(CoffeeMachine).GetField("cupWaiting", F) != null ? typeof(CoffeeMachine).GetField("cupWaiting", F).GetValue(cm) : "n/a");'''
SETCUPS = '''var cm = UnityEngine.Object.FindObjectOfType<CoffeeMachine>(); var F = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var r = (RefillableItem)typeof(CoffeeMachine).GetField("coffeeRefillableItem", F).GetValue(cm);
int n = 0; while (r.GetCurrentCount() > KEEP && n < 10) { r.OnItemServed(true); n++; } return "cups " + r.GetCurrentCount() + "/" + r.GetMaxCount();'''
TAP = '''var cm = UnityEngine.Object.FindObjectOfType<CoffeeMachine>(); cm.OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)); return "tapped";'''
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex('var sm = SessionManager.Instance; var ok = sm.UpgradeFood("CoffeeMachine", 0); return "upgrade " + ok + " level " + sm.GetFoodUpgradeLevel("CoffeeMachine");'))
print(ex(open(A + "goday.cs").read().replace("DAY", "0"))); time.sleep(7)
print(ex(open(A + "startday.cs").read())); time.sleep(1)
ex('var cmg = UnityEngine.Object.FindObjectOfType<CustomerManager>(); if (cmg != null) cmg.enabled = false; return "no customers";')
print("start:", ex(CUPS))
for keep in (0, 1):
    print("keep", keep, ex(SETCUPS.replace("KEEP", str(keep)))); print(ex(TAP))
    last = ""; t0 = time.time()
    while time.time() - t0 < 12:
        c = ex(CUPS)
        if c != last: print("  %4.1fs %s" % (time.time() - t0, c), flush=True); last = c
        if "brewing False" in c and time.time() - t0 > 2: break
        time.sleep(0.3)
    if keep == 1:
        time.sleep(0.5); U.call("manage_camera", {"action": "screenshot", "screenshot_file_name": "machine2_" + sys.argv[1] + "_tray1", "properties": {"include_image": False}}); time.sleep(1)
        print("  second tap while waiting:", ex(TAP), ex(CUPS))
        print("  serve one:", ex(SETCUPS.replace("KEEP", "1"))); time.sleep(0.6); print("  then:", ex(CUPS))
U.call("manage_editor", {"action": "stop"})
