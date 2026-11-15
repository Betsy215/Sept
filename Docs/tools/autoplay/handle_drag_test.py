"""The pull handles moved up and down by the player (owner, 15 November 2026). Day 2 (day 1 has no arranging step):
in the arrangement phase the table handle is dragged with real pointer events to the top, the bottom and a free spot on
the cloth, and a food is tested against its box; after the day starts a drag must do nothing; in the kitchen its handle
is dragged; then the day is loaded again to see both heights come back from the save.
Snaps /tmp/ftc/popframes/hdrag_<tag>.png."""
import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-900:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
def snap(tag): ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/hdrag_%s.png"); return "snap";' % tag); time.sleep(1.1)
def load_day():
    print(ex(open(A + "goday.cs").read().replace("DAY", "1"))); time.sleep(9)
    for _ in range(12):
        r = ex(open(A + "tutstep.cs").read())
        if r == "T|none": break
        time.sleep(1.0)
H = 'var h = GameObject.Find("KitchenButton").GetComponent<PullHandle>(); var rt = (RectTransform)h.transform; '
KH = 'var cb = UnityEngine.Object.FindObjectOfType<CloseKitchenButton>(); var h = cb.GetComponent<PullHandle>(); var rt = (RectTransform)h.transform; '
DRAG = '''var cam = Camera.main; var es = UnityEngine.EventSystems.EventSystem.current; var ped = new UnityEngine.EventSystems.PointerEventData(es);
var from = RectTransformUtility.WorldToScreenPoint(cam, rt.position); var to = RectTransformUtility.WorldToScreenPoint(cam, new Vector3(rt.position.x, TOYf, rt.position.z));
ped.position = from; h.OnPointerDown(ped); h.OnBeginDrag(ped);
for (var i = 1; i <= 8; i++) { ped.position = Vector2.Lerp(from, to, i / 8f); h.OnDrag(ped); }
var redMid = h.GetComponent<UnityEngine.UI.Image>().color.g < 0.9f; var yMid = rt.position.y; HOLD
h.OnEndDrag(ped); var s = SessionManager.Instance.GetCurrentSession();
return "asked " + TOYf + " -> while held at " + yMid.ToString("0.00") + (redMid ? " RED" : "") + ", let go at " + rt.position.y.ToString("0.00") + " (saved table " + (s.handleTableSet ? s.handleTableY.ToString("0.00") : "none") + ", kitchen " + (s.handleKitchenSet ? s.handleKitchenY.ToString("0.00") : "none") + ")";'''
def drag(find, y, hold=""): return ex(find + DRAG.replace("TOY", repr(float(y))).replace("HOLD", hold))
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex('var s = SessionManager.Instance.GetCurrentSession(); s.levelsCompleted = 1; s.levelStars = new System.Collections.Generic.List<int> { 3 }; return "day 1 done";'))   # day 1 has no arranging step
load_day()
print("phase:", ex('return UnityEngine.Object.FindObjectOfType<GamePhaseManager>().GetCurrentPhase().ToString();'))
print("start:", ex(H + 'var g = UnityEngine.Object.FindObjectOfType<SettingButton>(); var c = new Vector3[4]; ((RectTransform)g.transform).GetWorldCorners(c); var tb = UnityEngine.Object.FindObjectOfType<TableLayer>().InteractiveBounds; return "handle y " + rt.position.y.ToString("0.00") + "; settings button bottom " + c[0].y.ToString("0.00") + "; food area " + tb.min.y.ToString("0.00") + " to " + tb.max.y.ToString("0.00");'))
print("up:    ", drag(H, 9.0)); snap("top")
print("free:  ", drag(H, -2.0)); snap("mid")
print("food on the handle:", ex(H + '''var sb = new System.Text.StringBuilder(); var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
foreach (var d in UnityEngine.Object.FindObjectsOfType<DraggableFood>()) { if (!d.name.Contains("Bread")) continue;
  var m = typeof(DraggableFood).GetMethod("CheckOverlapAtPosition", F); var there = new Vector3(rt.position.x + 0.2f, rt.position.y, d.transform.position.z);
  sb.Append(d.name + " at the handle: overlap " + m.Invoke(d, new object[] { there }) + "; where it stands: overlap " + m.Invoke(d, new object[] { d.transform.position })); }
return sb.ToString();'''))
HOLDDRAG = DRAG.split("h.OnEndDrag(ped);")[0].replace("HOLD", "") + 'window.__ped = 0; return "held";'
print("held over the coffee:", ex(H + HOLDDRAG.replace("TOY", "-3.95").replace("window.__ped = 0; ", "System.AppDomain.CurrentDomain.SetData(\"ped\", ped); ")))
time.sleep(0.6); snap("red")
print("let go:", ex(H + 'var ped = (UnityEngine.EventSystems.PointerEventData)System.AppDomain.CurrentDomain.GetData("ped"); h.OnEndDrag(ped); return "at " + rt.position.y.ToString("0.00") + " red " + (h.GetComponent<UnityEngine.UI.Image>().color.g < 0.9f);'))
print("down:  ", drag(H, -9.0)); snap("bottom")
print("free:  ", drag(H, -2.0))
print(ex(open(A + "startday.cs").read())); time.sleep(5)
print("in play:", drag(H, 2.0))
print(ex('GameObject.Find("KitchenButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); return "tapped";')); time.sleep(2.0)
print("kitchen open:", ex('return KitchenSceneManager.Instance.IsKitchenOpen.ToString();'))
print("kitchen:", drag(KH, -3.0)); snap("kitchen")
print("kitchen up:", drag(KH, 9.0)); print("kitchen:", drag(KH, -3.0))
print(ex('KitchenSceneManager.Instance.CloseKitchen(); return "closed";')); time.sleep(1.5)
load_day(); time.sleep(1.0)
print("after reload:", ex(H + 'return "table handle y " + rt.position.y.ToString("0.00");')); snap("reload")
print(ex(open(A + "startday.cs").read())); time.sleep(5)
print(ex('KitchenSceneManager.Instance.OpenKitchen(); return "open";')); time.sleep(2.0)
print("after reload:", ex(KH + 'return "kitchen handle y " + rt.position.y.ToString("0.00");'))
U.call("manage_editor", {"action": "stop"})
