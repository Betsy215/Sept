"""Shop purchase card check (15 November 2026): the text size and line count of the card for a coins item, the star item
and Remove Ads, with captures."""
import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-700:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
tag = sys.argv[1]; money = sys.argv[2] if len(sys.argv) > 2 else "900"
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
print(ex('var sm = SessionManager.Instance; var s = sm.GetCurrentSession(); s.levelStars = new System.Collections.Generic.List<int> { 3, 3, 3, 3, 2, 2 }; s.levelsCompleted = 6; sm.AddScoreImmediately(%sf); return "money " + sm.GetTotalScore();' % money))
print(ex(open("/tmp/ftc/go_shop.cs").read())); time.sleep(6)
ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/shopc_%s_coins.png"); return "snap";' % tag); time.sleep(1.2)
POP = '''var shop = UnityEngine.Object.FindObjectOfType<ShopManager>(); var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
foreach (var c in UnityEngine.Object.FindObjectsOfType<ShopItemController>(true)) if (c.itemName == "NAME") { shop.ShowPurchasePopup(c);
  var t = (TMPro.TextMeshProUGUI)typeof(ShopManager).GetField("popupItemInfo", F).GetValue(shop); t.ForceMeshUpdate();
  return "NAME: font " + t.fontSize.ToString("F1") + " auto " + t.enableAutoSizing + " lines " + t.textInfo.lineCount + " height " + t.preferredHeight.ToString("F0") + " box " + t.rectTransform.sizeDelta + " at " + t.rectTransform.anchoredPosition + " text [" + t.text.Replace("\\n", " / ") + "]"; }
return "no card NAME";'''
print(ex('''var sb = new System.Text.StringBuilder(); foreach (var c in UnityEngine.Object.FindObjectsOfType<ShopItemController>(true)) if (c.itemName == "Choux" || c.itemName == "Cake") { var pt = c.transform.Find("PriceText"); sb.Append(c.itemName + ": price " + c.price + " card text [" + (pt != null ? pt.GetComponent<TMPro.TMP_Text>().text : "?") + "] button [" + c.purchaseButtonText + "] ; "); } return sb.ToString();'''))
for n in ("Candies", "Choux", "Cake", "StarYoga", "RemoveAds"):
    print(ex(POP.replace("NAME", n))); time.sleep(0.8)
    ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/shopc_%s_pop_%s.png"); return "snap";' % (tag, n)); time.sleep(1.0)
    ex('UnityEngine.Object.FindObjectOfType<ShopManager>().CancelPurchase(); return "closed";'); time.sleep(0.3)
U.call("manage_editor", {"action": "stop"})
