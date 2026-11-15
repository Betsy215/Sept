"""The table handle's wiggle while arranging (owner, 15 November 2026). Day 2 in the arrangement phase: reads the
knob's turn a few times (it must change), then steps it through one swing by hand for a small film, starts the day and
reads the turn again (it must be still), and snaps the kitchen. Frames /tmp/ftc/popframes/hw_NN.png, hw_play.png, hw_kitchen.png."""
import sys, time, re, math
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-900:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
H = 'var h = GameObject.Find("KitchenButton").GetComponent<PullHandle>(); var rt = (RectTransform)h.transform; '
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex('var s = SessionManager.Instance.GetCurrentSession(); s.levelsCompleted = 1; s.levelStars = new System.Collections.Generic.List<int> { 3 }; return "day 1 done";'))
print(ex(open(A + "goday.cs").read().replace("DAY", "1"))); time.sleep(9)
for _ in range(12):
    r = ex(open(A + "tutstep.cs").read())
    if r == "T|none": break
    time.sleep(1.0)
print("phase", ex('return UnityEngine.Object.FindObjectOfType<GamePhaseManager>().GetCurrentPhase().ToString();'))
print("turns while arranging:", [ex(H + 'var z = rt.localEulerAngles.z; if (z > 180f) z -= 360f; return z.ToString("0.0");') for _ in range(5)])
print(ex(H + 'return "size " + rt.sizeDelta + " at " + rt.anchoredPosition;'))
ex(H + 'h.enabled = false; return "held";')
for i in range(10):
    deg = math.sin(2 * math.pi * i / 10.0) * 4.0
    ex(H + 'rt.localRotation = Quaternion.Euler(0f, 0f, %sf); ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/hw_%02d.png"); return "ok";' % (repr(round(deg, 3)), i)); time.sleep(0.9)
ex(H + 'h.enabled = true; return "free";')
print(ex(open(A + "startday.cs").read())); time.sleep(5)
print("turn in play:", ex(H + 'var z = rt.localEulerAngles.z; if (z > 180f) z -= 360f; return z.ToString("0.0");'))
ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/hw_play.png"); return "snap";'); time.sleep(1.0)
print(ex('GameObject.Find("KitchenButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); return "tapped";')); time.sleep(2.2)
print("kitchen open", ex('return KitchenSceneManager.Instance.IsKitchenOpen.ToString();'))
ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/hw_kitchen.png"); return "snap";'); time.sleep(1.0)
U.call("manage_editor", {"action": "stop"})
