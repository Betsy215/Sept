"""The kitchen with everything bought (15 November 2026, owner: "Show me a screenshot of fully equipped kitchen, with
every upgrades etc."). Every food, person and decoration, every upgrade at its top level (three ovens, the coffee
machine's two cups, four ice cream flavours), all foods on the menu; one chapter 1 day and one chapter 2 day, the
kitchen opened in each. Snaps /tmp/ftc/popframes/kfull_ch1.png, kfull_ch2.png (+ the table beside it)."""
import sys, time, re
sys.path.insert(0, "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools")
import unity_mcp as U
A = "/Users/betzthebest/Desktop/Sept/FoodTruckCafe/Docs/tools/autoplay/"
def res(t):
    m = re.search(r'"result":"(.*?)","compiler"', t, re.S); return m.group(1) if m else t[-900:]
def ex(code): return res(U.call("execute_code", {"action": "execute", "code": code}))
SETUP = '''Chapter.Count = 2; var sm = SessionManager.Instance; sm.StartNewSession(); sm.MarkTutorialSeen(); sm.MarkTutorial2Seen(); var s = sm.GetCurrentSession();
foreach (var f in new[] { "Juice", "Apple", "Choux", "Cake", "Cookies", "Coconut", "IceCream" }) if (!s.purchasedFoodItems.Contains(f)) s.purchasedFoodItems.Add(f);
foreach (var c in new[] { "Grandma", "Chef", "Yoga", "Businessman" }) if (!s.purchasedCharacters.Contains(c)) s.purchasedCharacters.Add(c);
foreach (var u in new[] { "Coffee", "CoffeeMachine", "Oven", "IceCream" }) while (sm.CanUpgradeFood(u)) sm.UpgradeFood(u);   // each to its own top level
sm.PurchaseDecor("PotFlowers"); sm.PurchaseDecor("PotGreen"); sm.BuyCandies(); sm.BuyCandies();
s.levelStars = new System.Collections.Generic.List<int> { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 }; sm.BuyWithStars(YogaHire.ItemName, YogaHire.StarPrice);
s.levelsCompleted = DAY; sm.SetCurrentLevel(DAY); sm.AddScoreImmediately(9000f);
foreach (var f in s.purchasedFoodItems) sm.SetFoodEnabled(f, true);
var lv = new System.Text.StringBuilder(); foreach (var u in new[] { "Coffee", "CoffeeMachine", "Oven", "IceCream" }) lv.Append(u + " " + sm.GetFoodUpgradeLevel(u) + ", ");
SceneTransitionManager.Instance.TransitionToScene("GameSceneOne"); return "levels " + lv + "menu " + string.Join(",", s.enabledFoodItems);'''
U.call("manage_editor", {"action": "stop"}); time.sleep(2)
U.call("manage_editor", {"action": "play"}); time.sleep(10)
for tag, day in (("ch1", 6), ("ch2", 14)):
    print(tag, ex(SETUP.replace("DAY", str(day)))); time.sleep(10)
    for _ in range(12):
        r = ex(open(A + "tutstep.cs").read())
        if r == "T|none": break
        time.sleep(1.0)
    print(ex(open(A + "startday.cs").read())); time.sleep(5)
    ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/kfull_%s_table.png"); return "snap";' % tag); time.sleep(1.2)
    print(ex('KitchenSceneManager.Instance.OpenKitchen(); return "kitchen";')); time.sleep(3)
    ex('ScreenCapture.CaptureScreenshot("/tmp/ftc/popframes/kfull_%s.png"); return "snap";' % tag); time.sleep(1.2)
    print(ex('var sb = new System.Text.StringBuilder(); var k = UnityEngine.SceneManagement.SceneManager.GetSceneByName("KitchenScene"); foreach (var g in k.GetRootGameObjects()) foreach (var t in g.GetComponentsInChildren<Transform>(false)) if (t.parent != null && t.parent.name == "bg") sb.Append(t.name + " "); return "kitchen parts: " + sb;'))
    ex('KitchenSceneManager.Instance.CloseKitchen(); return "closed";'); time.sleep(1.5)
U.call("manage_editor", {"action": "stop"})
