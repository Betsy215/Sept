"""Yoga lady check (14 November 2026): a session with 16 stars; the star shop card, its popup, the purchase (stars taken,
owned after), then a day with her: when she is there, which pose, and what her flowers do to the open orders. Filmed."""
import sys, time, re, glob, os
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-600:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
def shot(name): ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/%s.png"); return "snap";' % name); time.sleep(1.2)
DAY = sys.argv[1] if len(sys.argv) > 1 else "0"
for f in glob.glob("/tmp/ftc/popframes/yoga_*"): os.remove(f)
F = 'var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; '
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex('var sm = SessionManager.Instance; var s = sm.GetCurrentSession(); s.levelStars = new System.Collections.Generic.List<int> { 3, 3, 3, 3, 2, 2 }; s.levelsCompleted = 6; return "stars " + sm.GetTotalStars() + " available " + sm.StarsAvailable + " owned " + YogaHire.Owned;'))
print(ex(open("/tmp/ftc/go_shop.cs").read())); time.sleep(6)
print(ex(F + 'var shop = UnityEngine.Object.FindObjectOfType<ShopManager>(); typeof(ShopManager).GetMethod("SetTab", F).Invoke(shop, new object[] { ShopManager.ShopTab.Stars }); return "star tab";')); time.sleep(1.0)
shot("yoga_shop_tab")
print(ex('var shop = UnityEngine.Object.FindObjectOfType<ShopManager>(); foreach (var c in UnityEngine.Object.FindObjectsOfType<ShopItemController>(true)) if (c.itemName == YogaHire.ItemName) { shop.ShowPurchasePopup(c); return "popup for " + c.itemName + " price " + c.price; } return "no card";')); time.sleep(1.0)
shot("yoga_shop_popup")
print(ex('var shop = UnityEngine.Object.FindObjectOfType<ShopManager>(); shop.ConfirmPurchase(); var sm = SessionManager.Instance; return "bought: owned " + YogaHire.Owned + " stars left " + sm.StarsAvailable + " of " + sm.GetTotalStars() + " decor " + sm.GetCurrentSession().purchasedDecor.Count + " patience x" + sm.PatienceMultiplier;')); time.sleep(1.5)
shot("yoga_shop_owned")
print("second buy:", ex('var shop = UnityEngine.Object.FindObjectOfType<ShopManager>(); var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; foreach (var c in UnityEngine.Object.FindObjectsOfType<ShopItemController>(true)) if (c.itemName == YogaHire.ItemName) return "TryPurchase " + typeof(ShopManager).GetMethod("TryPurchaseItem", F).Invoke(shop, new object[] { c }) + " stars left " + SessionManager.Instance.StarsAvailable; return "no card";'))
print(ex(open(A + "goday.cs").read().replace("DAY", DAY))); time.sleep(7)
for _ in range(20):
    r = ex(open(A + "tutstep.cs").read())
    if r == "T|none": break
    time.sleep(1.0)
print(ex(open(A + "startday.cs").read())); time.sleep(0.5)
print("plan:", ex('var lm = UnityEngine.Object.FindObjectOfType<LevelManager>(); return "day " + lm.DayDuration + " s, visits " + string.Join("+", YogaHire.Plan(lm.DayDuration)) + " poses";'))
FILM = '''var ksm = KitchenSceneManager.Instance;
System.Collections.IEnumerator Film() { float last = -1f; int n = 0; float t0 = Time.time;
  while (Time.time - t0 < 130f) { yield return new WaitForEndOfFrame(); var y = UnityEngine.Object.FindObjectOfType<YogaHire>(); if (y == null) continue;
    var fr = y.transform.Find("Front").GetComponent<SpriteRenderer>(); if (fr.color.a <= 0.01f && fr.sprite == null) continue; if (fr.color.a <= 0.01f && n > 0 && Time.time - last < 1f) continue;
    if (Time.time - last >= 0.1f && n < 400) { last = Time.time; var tex = ScreenCapture.CaptureScreenshotAsTexture();
      System.IO.File.WriteAllBytes("/tmp/ftc/popframes/yoga_" + n.ToString("D3") + "_" + ((Time.time - t0) * 1000f).ToString("F0") + ".jpg", tex.EncodeToJPG(70)); UnityEngine.Object.Destroy(tex); n++; } } }
ksm.StartCoroutine(Film()); return "filming";'''
print(ex(FILM))
ST = '''var y = UnityEngine.Object.FindObjectOfType<YogaHire>(); var os = UnityEngine.Object.FindObjectOfType<OrderSystem>(); var lm = UnityEngine.Object.FindObjectOfType<LevelManager>();
var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var list = (System.Collections.IList)typeof(OrderSystem).GetField("orders", F).GetValue(os); var sb = new System.Text.StringBuilder();
foreach (OrderSystem.ActiveOrder o in list) sb.Append(" order" + o.slot + " " + o.timer.ToString("F1") + "/" + o.displayTime.ToString("F1"));
var fr = y != null ? y.transform.Find("Front").GetComponent<SpriteRenderer>() : null;
return "left " + lm.DayTimeLeft.ToString("F0") + " yoga " + (y == null ? "none" : (fr.color.a > 0.05f ? "there " + (fr.sprite != null ? fr.sprite.name : "?") : "away")) + sb + " served " + os.CustomersServed + " lost " + os.CustomersLost;'''
t0 = time.time(); last = ""; took = False
while time.time() - t0 < 220:
    r = ex(open(A + "step.cs").read()); kind = r.split("|")[1] if "|" in r else r
    c = ex(ST); key = c.split(" order")[0].split("left ")[1].split(" ", 1)[1] if "left " in c else c
    if key != last: print("  %5.1fs %s" % (time.time() - t0, c), flush=True); last = key
    if (not took) and "there sit_2" in c: took = True; shot("yoga_in_game")
    if kind == "done": break
    time.sleep(0.5)
time.sleep(2)
print("END", ex(ST))
U.call("manage_editor", {"action": "stop"})
print(len(glob.glob("/tmp/ftc/popframes/yoga_*.jpg")), "frames")
