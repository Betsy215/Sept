"""Kitchen with and without the blender (15 November 2026): chapter 1 (no ice cream: no blender) and chapter 2 with ice cream bought."""
import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-700:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(9)
print(ex(open("/tmp/ftc/setup_day1.cs").read()))
for day, tag in ((0, "ch1"), (12, "ch2_noice"), (12, "ch2_ice")):
    if tag == "ch2_noice": print(ex('var s = SessionManager.Instance.GetCurrentSession(); s.levelsCompleted = 12; s.tutorial2Seen = true; s.purchasedFoodItems.Remove("IceCream"); return "ch2, foods " + string.Join(",", s.purchasedFoodItems);'))
    if tag == "ch2_ice": print(ex('var sm = SessionManager.Instance; var s = sm.GetCurrentSession(); if (!s.purchasedFoodItems.Contains("IceCream")) sm.PurchaseFoodItem("IceCream"); return "foods " + string.Join(",", s.purchasedFoodItems) + " owned " + sm.IsFoodItemPurchased("IceCream");'))
    print(ex(open(A + "goday.cs").read().replace("DAY", str(day)))); time.sleep(8)
    for _ in range(12):
        r = ex(open(A + "tutstep.cs").read())
        if r == "T|none": break
        time.sleep(1.0)
    print(ex(open(A + "startday.cs").read())); time.sleep(1.5)
    print(ex('KitchenSceneManager.Instance.OpenKitchen(); return "open";')); time.sleep(2.0)
    print(tag, ex('var b = GameObject.Find("Blender"); return "blender " + (b != null) + " ice cream owned " + SessionManager.Instance.IsFoodItemPurchased("IceCream") + " chapter " + SessionManager.CurrentChapter;'))
    ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/kitchen_%s.png"); return "snap";' % tag); time.sleep(1.5)
    print(ex('KitchenSceneManager.Instance.CloseKitchen(); return "closed";')); time.sleep(1.0)
U.call("manage_editor", {"action": "stop"})
