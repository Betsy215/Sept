"""The chapter 1 yoga lady in the middle of the screen with the painting's near grass in front of her (owner, 15 November
2026). Plays day 1 with her hired through the game's own code and snaps: by day, in the evening light, at night, and
after she is gone (the grass piece must be invisible then). Usage: yoga_mid_test.py"""
import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-700:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
def snap(tag):
    ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/ymid_%s.png"); return "snap";' % tag); time.sleep(1.2)
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex('var sm = SessionManager.Instance; var s = sm.GetCurrentSession(); s.levelStars = new System.Collections.Generic.List<int> { 3, 3, 3, 3, 3 }; return "hired " + sm.BuyWithStars(YogaHire.ItemName, YogaHire.StarPrice);'))
print(ex(open(A + "goday.cs").read().replace("DAY", "0"))); time.sleep(8)
for _ in range(12):
    r = ex(open(A + "tutstep.cs").read())
    if r == "T|none": break
    time.sleep(1.0)
print(ex(open(A + "startday.cs").read())); time.sleep(6.5)
STATE = '''var y = UnityEngine.Object.FindObjectOfType<YogaHire>(); var g = GameObject.Find("YogaFrontGrass");
if (y == null) return "no yoga lady; grass " + (g == null ? "gone" : "alpha " + g.GetComponent<SpriteRenderer>().color.a);
var f = y.transform.Find("Front").GetComponent<SpriteRenderer>(); var b = y.transform.Find("Back").GetComponent<SpriteRenderer>();
return "lady at " + y.transform.position + " alpha " + Mathf.Max(f.color.a, b.color.a).ToString("0.00") + "; grass " + (g == null ? "none" : g.transform.position + " scale " + g.transform.localScale + " colour " + g.GetComponent<SpriteRenderer>().color);'''
for i in range(14):
    r = ex(STATE); print(i, r)
    if "alpha 1.00" in r: break
    time.sleep(1.5)
ex('Time.timeScale = 0.02f; return "slow";'); snap("day")
TINT = '''var bg = GameObject.Find("Canvas").transform.Find("bg").GetComponent<UnityEngine.UI.Image>(); var t = UnityEngine.Object.FindObjectOfType<DayNightTint>();
if (t != null) t.enabled = false; bg.color = new Color(R); return "bg " + bg.color;'''
print(ex(TINT.replace("R", "1f, 0.82f, 0.72f, 1f"))); time.sleep(0.5); snap("dusk")
print(ex(TINT.replace("R", "0.45f, 0.5f, 0.75f, 1f"))); time.sleep(0.5); snap("night")
print(ex('var bg = GameObject.Find("Canvas").transform.Find("bg").GetComponent<UnityEngine.UI.Image>(); bg.color = Color.white; var t = UnityEngine.Object.FindObjectOfType<DayNightTint>(true); if (t != null) t.enabled = true; var y = UnityEngine.Object.FindObjectOfType<YogaHire>(); foreach (var sr in y.GetComponentsInChildren<SpriteRenderer>()) sr.color = new Color(1f, 1f, 1f, 0f); y.StopAllCoroutines(); return "hidden";'))
time.sleep(0.6); print(ex(STATE)); snap("gone")
ex('Time.timeScale = 1f; return "speed";')
U.call("manage_editor", {"action": "stop"})
