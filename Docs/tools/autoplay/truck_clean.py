import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-700:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
def snap(tag):
    ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/tk_%s.png"); return "snap";' % tag); time.sleep(1.2)
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex(open(A + "goday.cs").read().replace("DAY", "0"))); time.sleep(8)
for _ in range(12):
    r = ex(open(A + "tutstep.cs").read())
    if r == "T|none": break
    time.sleep(1.0)
print(ex(open(A + "startday.cs").read())); time.sleep(9)
ex('Time.timeScale = 0.02f; return "slow";'); snap("with")
print(ex('''var c = GameObject.Find("KitchenButtonCanvas"); if (c == null) return "no canvas";
var rt = c.GetComponentInChildren<UnityEngine.UI.Button>().GetComponent<RectTransform>(); var img = rt.GetComponent<UnityEngine.UI.Image>();
var cs = new Vector3[4]; rt.GetWorldCorners(cs);
var info = "button corners " + cs[0] + " " + cs[2] + " sprite " + (img.sprite != null ? img.sprite.name + " " + img.sprite.rect : "none") + " screen " + Screen.width + "x" + Screen.height;
c.GetComponent<Canvas>().enabled = false; return info;'''))
time.sleep(0.5); snap("clean")
ex('Time.timeScale = 1f; return "speed";')
U.call("manage_editor", {"action": "stop"})
