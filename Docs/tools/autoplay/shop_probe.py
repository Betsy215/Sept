import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-900:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
tag = sys.argv[1]
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex('var sm = SessionManager.Instance; var s = sm.GetCurrentSession(); s.levelStars = new System.Collections.Generic.List<int> { 3, 3, 3, 3, 2, 2 }; s.levelsCompleted = 6; sm.AddScoreImmediately(900f); return "stars " + sm.StarsAvailable;'))
print(ex(open("/tmp/ftc/go_shop.cs").read())); time.sleep(6)
ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/shop_%s_coins.png"); return "snap";' % tag); time.sleep(1.2)
DUMP = '''var sb = new System.Text.StringBuilder();
foreach (var c in UnityEngine.Object.FindObjectsOfType<ShopItemController>(true)) { if (c.itemName != "NAME") continue;
  sb.Append(c.name + " type " + c.itemType + " price " + c.price + " scale " + c.transform.localScale.x.ToString("F2") + " :: ");
  foreach (var rt in c.GetComponentsInChildren<RectTransform>(true)) { if (rt == c.transform) continue; var tm = rt.GetComponent<TMPro.TMP_Text>(); var im = rt.GetComponent<UnityEngine.UI.Image>();
    sb.Append(rt.name + (rt.gameObject.activeSelf ? "" : "(off)") + " pos " + rt.anchoredPosition.ToString("F0") + " size " + rt.sizeDelta.ToString("F0") + (tm != null ? " TEXT '" + tm.text + "' font " + tm.fontSize + " color #" + ColorUtility.ToHtmlStringRGBA(tm.color) + " align " + tm.alignment + " mat " + tm.fontSharedMaterial.name : "") + (im != null ? " IMG " + (im.sprite != null ? im.sprite.name + " " + im.sprite.rect.size.ToString("F0") : "none") + " preserve " + im.preserveAspect : "") + " | "); } }
return sb.ToString();'''

F = 'var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; '
print(ex(F + 'var shop = UnityEngine.Object.FindObjectOfType<ShopManager>(); typeof(ShopManager).GetMethod("SetTab", F).Invoke(shop, new object[] { ShopManager.ShopTab.Stars }); return "star tab";')); time.sleep(1.0)
ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/shop_%s_stars.png"); return "snap";' % tag); time.sleep(1.2)
print(ex('var shop = UnityEngine.Object.FindObjectOfType<ShopManager>(); foreach (var c in UnityEngine.Object.FindObjectsOfType<ShopItemController>(true)) if (c.itemName == YogaHire.ItemName) { shop.ShowPurchasePopup(c); return "popup"; } return "no card";')); time.sleep(1.0)
ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/shop_%s_popup.png"); return "snap";' % tag); time.sleep(1.2)
U.call("manage_editor", {"action": "stop"})
