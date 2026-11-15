"""The truck as a pull handle on a stick (owner, 15 November 2026). Plays day 1, snaps the handle at rest and posed
fully pulled out, then taps it for real: reads its place every call during the bounce, checks that the kitchen opened,
closes it again and checks the handle is back at rest. Usage: truck_handle_test.py [night]"""
import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-700:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
def snap(tag):
    ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/th_%s.png"); return "snap";' % tag); time.sleep(1.0)
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex(open(A + "goday.cs").read().replace("DAY", "0"))); time.sleep(8)
for _ in range(12):
    r = ex(open(A + "tutstep.cs").read())
    if r == "T|none": break
    time.sleep(1.0)
print(ex(open(A + "startday.cs").read())); time.sleep(9)
WHERE = '''var b = GameObject.Find("KitchenButton"); var rt = b.GetComponent<RectTransform>(); var st = GameObject.Find("HandleStick");
var hen = GameObject.Find("MoneyFlipClock"); var ksm = KitchenSceneManager.Instance;
return "truck " + rt.anchoredPosition + " scale " + rt.localScale.x.ToString("0.000") + "; stick " + (st == null ? "none" : ((RectTransform)st.transform).anchoredPosition + " size " + ((RectTransform)st.transform).sizeDelta) + "; hen x " + (hen != null ? hen.transform.position.x.ToString("0.00") : "-") + "; kitchen open " + ksm.IsKitchenOpen + "; handle " + (b.GetComponent<PullHandle>() != null);'''
print("rest:", ex(WHERE)); snap("rest")
print(ex('var rt = GameObject.Find("KitchenButton").GetComponent<RectTransform>(); rt.anchoredPosition += new Vector2(46f, 0f); rt.localScale = new Vector3(1.07f, 0.95f, 1f); return "posed out";')); time.sleep(0.3); snap("out")
print(ex('var rt = GameObject.Find("KitchenButton").GetComponent<RectTransform>(); rt.anchoredPosition -= new Vector2(46f, 0f); rt.localScale = Vector3.one; return "posed back";')); time.sleep(0.3)
print(ex('GameObject.Find("KitchenButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); var rt = GameObject.Find("KitchenButton").GetComponent<RectTransform>(); return "tapped, kitchen open at once: " + KitchenSceneManager.Instance.IsKitchenOpen;'))
for i in range(6): print(" ", ex(WHERE)); 
time.sleep(1.0); print("after:", ex(WHERE)); snap("kitchen")
KB = 'var cb = UnityEngine.Object.FindObjectOfType<CloseKitchenButton>(); if (cb == null) return "no back button"; var rt = (RectTransform)cb.transform; '
print("kitchen handle:", ex(KB + 'return rt.parent.name + " pos " + rt.anchoredPosition + " size " + rt.sizeDelta + " handle " + (cb.GetComponent<PullHandle>() != null) + " sprite " + cb.GetComponent<UnityEngine.UI.Image>().sprite.name;'))
print(ex(KB + 'cb.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); return "tapped back, open at once: " + KitchenSceneManager.Instance.IsKitchenOpen;'))
time.sleep(1.5); print("closed:", ex(WHERE)); snap("back")
U.call("manage_editor", {"action": "stop"})
