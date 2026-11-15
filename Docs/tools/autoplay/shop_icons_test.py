"""The shop's tab pictures and the star picture in the star cards' texts (15 November 2026). Opens the shop with stars
and money, snaps each tab, then the yoga lady's description card (star tab) and the star pack's (the $ tab).
Snaps /tmp/ftc/popframes/si_<tag>.png."""
import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-900:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
def snap(tag): ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/si_%s.png"); return "snap";' % tag); time.sleep(1.3)
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex('var sm = SessionManager.Instance; var s = sm.GetCurrentSession(); s.levelsCompleted = 3; s.levelStars = new System.Collections.Generic.List<int> { 3, 3, 3, 3, 3, 3 }; sm.AddScoreImmediately(800f); return "stars " + sm.StarsAvailable;'))
print(ex(open("/tmp/ftc/go_shop.cs").read())); time.sleep(7)
F = 'var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public; var shop = UnityEngine.Object.FindObjectOfType<ShopManager>(); var tabs = (System.Collections.Generic.List<UnityEngine.UI.Button>)typeof(ShopManager).GetField("tabButtons", F).GetValue(shop); '
snap("coins")
print(ex(F + 'tabs[0].onClick.Invoke(); return "stars tab";')); time.sleep(1.5); snap("stars")
CARD = 'ShopItemController card = null; foreach (var c in UnityEngine.Object.FindObjectsOfType<ShopItemController>(false)) if (c.itemName == NAME) card = c; if (card == null) return "no card"; card.transform.Find("ItemIcon").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); return "opened";'
print(ex(CARD.replace("NAME", "YogaHire.ItemName"))); time.sleep(1.5); snap("yoga")
print(ex(F + 'shop.CancelPurchase(); return "closed";')); time.sleep(1)
print(ex(F + 'tabs[2].onClick.Invoke(); return "dollar tab " + tabs[2].gameObject.activeSelf;')); time.sleep(1.5); snap("dollar")
print(ex(CARD.replace("NAME", "ShopManager.StarPackCardName"))); time.sleep(1.5); snap("pack")
U.call("manage_editor", {"action": "stop"})
