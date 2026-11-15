"""Yoga lady placement check: she is shown (first pose, held) in a chapter 1 day and a chapter 2 day, with customers. Usage: yoga_place.py tag"""
import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-600:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
tag = sys.argv[1]
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex('var sm = SessionManager.Instance; var s = sm.GetCurrentSession(); s.levelStars = new System.Collections.Generic.List<int> { 3, 3, 3, 3, 3 }; return "hired " + sm.BuyWithStars(YogaHire.ItemName, YogaHire.StarPrice);'))
for day, ch in ((0, "ch1"), (12, "ch2")):
    if day: print(ex('var s = SessionManager.Instance.GetCurrentSession(); s.levelsCompleted = 12; s.tutorial2Seen = true; return "set";'))
    print(ex(open(A + "goday.cs").read().replace("DAY", str(day)))); time.sleep(8)
    for _ in range(12):
        r = ex(open(A + "tutstep.cs").read())
        if r == "T|none": break
        time.sleep(1.0)
    print(ex(open(A + "startday.cs").read())); time.sleep(6.0)
    for pose in (0, 2):
        print(ex('var y = UnityEngine.Object.FindObjectOfType<YogaHire>(); if (y == null) return "no yoga lady"; y.StopAllCoroutines(); var fr = y.transform.Find("Front").GetComponent<SpriteRenderer>(); fr.sprite = Resources.Load<Sprite>("Yoga/sit_%d"); fr.color = Color.white; return "shown at " + y.transform.position.ToString("F2") + " chapter " + SessionManager.CurrentChapter;' % pose)); time.sleep(0.6)
        ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/place_%s_%s_%d.png"); return "snap";' % (tag, ch, pose)); time.sleep(1.2)
U.call("manage_editor", {"action": "stop"})
