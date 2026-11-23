// r33 (23 Nov 2026, owner: "For the kitchen screenshot, use a fully upgraded kitchen"): the r29 chapter 2 session with
// three ovens, the kitchen upgrade, and every kitchen food on the menu (bread, choux, cake, cookies, coconut, ice cream,
// juice), so the baking tray, the cake plate, the mixer, the fruit board and the cookie plate all stand in the kitchen.
Chapter.Count = 2;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); sm.MarkArrangeTutorialSeen(); sm.MarkTutorial2Seen(); var s = sm.GetCurrentSession();
foreach (var f in new[]{"Juice","Apple","Choux","Cake","Cookies","Coconut"}) if (!s.purchasedFoodItems.Contains(f)) s.purchasedFoodItems.Add(f);
foreach (var c in new[]{"Grandma","Chef","Yoga","Businessman"}) if (!s.purchasedCharacters.Contains(c)) s.purchasedCharacters.Add(c);
sm.UpgradeFood("Coffee"); sm.UpgradeFood("CoffeeMachine"); sm.UpgradeFood("IceCream");
s.levelsCompleted = 14; s.chapterDone = new System.Collections.Generic.List<int> { 12, 2 }; s.chapter2Claimed = true;
sm.AddScoreImmediately(6000f); sm.SetCurrentLevel(14);
if (s.foodUpgradeLevels == null) s.foodUpgradeLevels = new System.Collections.Generic.Dictionary<string, int>(); s.foodUpgradeLevels["Oven"] = 3;
s.starsGranted += 60; sm.BuyWithStars(KitchenSpeed.ItemName, KitchenSpeed.StarPrice); sm.BuyWithStars(YogaHire.ItemName, YogaHire.StarPrice);
s.enabledFoodItems = new System.Collections.Generic.List<string> { "Bread", "Juice", "Choux", "Cake", "Cookies", "Coconut", "IceCream" };   // past the menu's caps: this session is for the kitchen picture only
sm.PurchaseDecor("PotFlowers"); sm.PurchaseDecor("PotGreen"); sm.BuyCandies(); sm.BuyCandies();
s.levelStars = new System.Collections.Generic.List<int> { 3, 3, 3, 3, 3, 3 };
SceneTransitionManager.Instance.TransitionToScene("GameSceneOne");
return "chapter 2, full kitchen: ovens " + sm.OvensOwned + ", kitchen upgrade " + KitchenSpeed.Owned + ", on the menu " + string.Join(",", s.enabledFoodItems);
