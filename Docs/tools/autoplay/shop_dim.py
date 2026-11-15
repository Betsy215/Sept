"""Why a coins-tab card can look dimmed (15 November 2026): for each card the picture's own colour, the button's tint and
whether it can be tapped, in a given state. Usage: shop_dim.py tag money levelsCompleted"""
import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-900:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
tag, money, done = sys.argv[1], sys.argv[2], sys.argv[3]
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex('var sm = SessionManager.Instance; var s = sm.GetCurrentSession(); s.levelsCompleted = %s; sm.AddScoreImmediately(%sf); return "money " + sm.GetTotalScore() + " days done " + s.levelsCompleted + " chars " + string.Join(",", s.purchasedCharacters);' % (done, money)))
print(ex(open("/tmp/ftc/go_shop.cs").read())); time.sleep(6)
DUMP = '''var sb = new System.Text.StringBuilder();
foreach (var c in UnityEngine.Object.FindObjectsOfType<ShopItemController>(false)) { var ic = c.transform.Find("ItemIcon"); if (ic == null) continue; var img = ic.GetComponent<UnityEngine.UI.Image>(); var b = ic.GetComponent<UnityEngine.UI.Button>(); var cr = ic.GetComponent<CanvasRenderer>();
  sb.Append(c.itemName + " $" + c.price + " type " + c.itemType + " img " + ColorUtility.ToHtmlStringRGBA(img.color) + " tint " + ColorUtility.ToHtmlStringRGBA(cr.GetColor()) + " tap " + (b != null && b.interactable) + " transition " + (b != null ? b.transition.ToString() : "-") + " | "); }
return sb.ToString();'''
print(ex(DUMP))
ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/shopdim_%s.png"); return "snap";' % tag); time.sleep(1.2)
U.call("manage_editor", {"action": "stop"})
