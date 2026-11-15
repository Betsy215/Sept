"""Truck window light and patience flower check (15 November 2026): day 1; the truck at rest (wiggle off, light off), a
bake started in the kitchen (light blinking), the night stage forced (steady warm light), and the flowers' new size."""
import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-700:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
def snap(n): ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/%s.png"); return "snap";' % n); time.sleep(1.0)
TRUCK = '''var b = GameObject.Find("KitchenButton"); var rt = (RectTransform)b.transform; var w = b.GetComponent<BalatroWobble>(); var sb = new System.Text.StringBuilder();
sb.Append("wobble " + (w == null ? "none" : (w.enabled ? "ON" : "off")) + " pos " + rt.anchoredPosition.ToString("F2") + " rot " + rt.localEulerAngles.z.ToString("F2") + " scale " + rt.localScale.x.ToString("F3") + " lights");
foreach (Transform t in rt) if (t.name == "WindowLight") sb.Append(" " + t.GetComponent<UnityEngine.UI.Image>().color.a.ToString("F2"));
sb.Append(" baking " + OvenKitchenBase.AnyBaking() + " stage " + DayNightTint.CurrentStage.ToString("F2")); return sb.ToString();'''
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex(open(A + "goday.cs").read().replace("DAY", "0"))); time.sleep(8)
print(ex(open(A + "startday.cs").read())); time.sleep(5.0)
for i in range(3): print("idle:", ex(TRUCK)); time.sleep(0.7)
snap("truck_idle")
print(ex('KitchenSceneManager.Instance.OpenKitchen(); return "kitchen open";')); time.sleep(1.5)
print(ex('''foreach (var o in UnityEngine.Object.FindObjectsOfType<OvenKitchenBase>()) { o.StartBake("Bread"); if (o.IsBaking) return "baking in " + o.name + " for " + o.BakeTime + " s"; } return "no oven started";''')); time.sleep(0.8)
print(ex('KitchenSceneManager.Instance.CloseKitchen(); return "kitchen closed";')); time.sleep(1.0)
vals = []
for i in range(10):
    r = ex(TRUCK); vals.append(r.split("lights")[1].split(" baking")[0].strip()); 
    if i in (0, 9): print("cooking:", r)
    time.sleep(0.13)
print("light over 10 reads:", " | ".join(vals))
for i in range(8):
    r = ex('var b = GameObject.Find("KitchenButton"); float a = 0f; foreach (Transform t in b.transform) if (t.name == "WindowLight") a = Mathf.Max(a, t.GetComponent<UnityEngine.UI.Image>().color.a); if (a %s) { ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/truck_cook_%s.png"); return "snap at " + a.ToString("F2"); } return "wait " + a.ToString("F2");' % (("> 0.85f", "on") if i < 4 else ("< 0.2f", "off")))
    if r.startswith("snap"): print(r); time.sleep(1.0); 
    else: time.sleep(0.11)
print(ex('''var dn = UnityEngine.Object.FindObjectOfType<DayNightTint>(); var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
typeof(DayNightTint).GetField("stage", F).SetValue(dn, 2f); typeof(DayNightTint).GetField("kitchenCooking", F).SetValue(dn, false); typeof(DayNightTint).GetField("cookingCheckAt", F).SetValue(dn, Time.unscaledTime + 5f);
typeof(DayNightTint).GetMethod("UpdateNightLights", F).Invoke(dn, null); var b = GameObject.Find("KitchenButton"); var sb = new System.Text.StringBuilder("night, not cooking: lights");
foreach (Transform t in b.transform) if (t.name == "WindowLight") sb.Append(" " + t.GetComponent<UnityEngine.UI.Image>().color.a.ToString("F2")); return sb.ToString();'''))
U.call("manage_editor", {"action": "stop"})
