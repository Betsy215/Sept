import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-900:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex('var sm = SessionManager.Instance; var s = sm.GetCurrentSession(); s.levelsCompleted = 3; sm.AddScoreImmediately(300f); return "ok";'))
print(ex(open("/tmp/ftc/go_shop.cs").read())); time.sleep(6)
R = 'var r = GameObject.Find("RestorePurchases"); if (r == null) { foreach (var t in Resources.FindObjectsOfTypeAll<TMPro.TextMeshProUGUI>()) if (t.name == "RestorePurchases") return "inactive: " + t.text + " available=" + RemoveAds.Available; return "no button; available=" + RemoveAds.Available; } '
print(ex(R + 'return "button: " + r.GetComponent<TMPro.TextMeshProUGUI>().text + " available=" + RemoveAds.Available + " owned=" + RemoveAds.Owned;'))
print(ex(R + 'r.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); return "tapped: " + r.GetComponent<TMPro.TextMeshProUGUI>().text;'))
ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/restore_0.png"); return "snap";')
for i in range(9):
    time.sleep(1.0); print(i + 1, ex(R + 'return r.GetComponent<TMPro.TextMeshProUGUI>().text;'))
    if i == 1: ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/restore_1.png"); return "snap";')
U.call("manage_editor", {"action": "stop"})
