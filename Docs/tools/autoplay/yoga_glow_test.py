"""The yoga lady's pose-finish effect, every style (15 November 2026). Chapter 1 day 1 with her hired: waits until she
shows, then for each style plays the effect and steps through it in game time (Time.timeScale 0 between frames), one
screenshot per step. Frames /tmp/ftc/popframes/yg<style>_NN.png. Usage: yoga_glow_test.py [chapter] [styles, e.g. 2,5,6]"""
import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-900:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
ch = sys.argv[1] if len(sys.argv) > 1 else "1"
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex('var sm = SessionManager.Instance; var s = sm.GetCurrentSession(); s.levelStars = new System.Collections.Generic.List<int> { 3, 3, 3, 3, 3 }; ' + ('s.levelsCompleted = 12; s.tutorial2Seen = true; ' if ch == "2" else '') + 'return "hired " + sm.BuyWithStars(YogaHire.ItemName, YogaHire.StarPrice);'))
print(ex(open(A + "goday.cs").read().replace("DAY", "12" if ch == "2" else "0"))); time.sleep(8)
for _ in range(12):
    r = ex(open(A + "tutstep.cs").read())
    if r == "T|none": break
    time.sleep(1.0)
print(ex(open(A + "startday.cs").read())); time.sleep(4)
for i in range(16):
    r = ex('var y = UnityEngine.Object.FindObjectOfType<YogaHire>(); if (y == null) return "none"; return y.transform.Find("Front").GetComponent<SpriteRenderer>().color.a.ToString("0.00");')
    if r == "1.00": break
    time.sleep(1.2)
print("she shows:", r)
ex('var y = UnityEngine.Object.FindObjectOfType<YogaHire>(); y.StopAllCoroutines(); return "held";')
STEP = 0.1
STYLES = [int(x) for x in sys.argv[2].split(",")] if len(sys.argv) > 2 else [1, 2, 3, 4]
for style in STYLES:
    ex('Time.timeScale = 1f; YogaGlow.Style = %d; var y = UnityEngine.Object.FindObjectOfType<YogaHire>(); YogaGlow.Play(y, y.transform.Find("Front").GetComponent<SpriteRenderer>()); Time.timeScale = 0f; return "play";' % style)
    for k in range(15):
        ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/yg%d_%02d.png"); return "snap";' % (style, k)); time.sleep(0.9)
        ex('Time.timeScale = 1f; return "go";'); time.sleep(STEP); ex('Time.timeScale = 0f; return "stop";')
    ex('Time.timeScale = 1f; return "go";'); time.sleep(1.5)
U.call("manage_editor", {"action": "stop"})
