"""The 15-star pack on the shop's $ tab (15 November 2026): opens the shop, shows the $ tab, buys the pack through the
editor's stand-in store and reads the stars before and after. Snaps: starpack_tab.png, starpack_popup.png, starpack_after.png."""
import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-900:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
def snap(tag): ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/starpack_%s.png"); return "snap";' % tag); time.sleep(1.2)
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex('var sm = SessionManager.Instance; var s = sm.GetCurrentSession(); s.levelsCompleted = 3; s.levelStars = new System.Collections.Generic.List<int> { 3, 2, 3 }; sm.AddScoreImmediately(300f); return "stars " + sm.StarsAvailable;'))
print(ex(open("/tmp/ftc/go_shop.cs").read())); time.sleep(7)
F = 'var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public; var shop = UnityEngine.Object.FindObjectOfType<ShopManager>(); '
print(ex(F + 'return "offered: remove ads " + RemoveAds.Available + ", star pack " + StarPack.Available + " " + StarPack.PriceText;'))
print(ex(F + 'var tabs = (System.Collections.Generic.List<UnityEngine.UI.Button>)typeof(ShopManager).GetField("tabButtons", F).GetValue(shop); tabs[2].onClick.Invoke(); return "dollar tab, tabs " + tabs.Count;')); time.sleep(1.5)
print(ex('var sb = new System.Text.StringBuilder(); foreach (var c in UnityEngine.Object.FindObjectsOfType<ShopItemController>(false)) sb.Append(c.itemName + " "); return "cards showing: " + sb;'))
snap("tab")
CARD = 'ShopItemController card = null; foreach (var c in UnityEngine.Object.FindObjectsOfType<ShopItemController>(false)) if (c.itemName == ShopManager.StarPackCardName) card = c; if (card == null) return "no star pack card"; '
print(ex(CARD + 'card.transform.Find("ItemIcon").GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); return "card tapped";')); time.sleep(1.5); snap("popup")
print(ex(F + 'shop.ConfirmPurchase(); return "confirmed; stars now " + SessionManager.Instance.StarsAvailable;')); time.sleep(2.5)
print(ex('var s = SessionManager.Instance.GetCurrentSession(); return "stars " + SessionManager.Instance.StarsAvailable + " bought " + s.starsBought + " earned " + SessionManager.Instance.GetTotalStars();')); snap("after")
print(ex(F + 'var tabs = (System.Collections.Generic.List<UnityEngine.UI.Button>)typeof(ShopManager).GetField("tabButtons", F).GetValue(shop); tabs[0].onClick.Invoke(); return "star tab";')); time.sleep(1.5); snap("stars")
U.call("manage_editor", {"action": "stop"})
