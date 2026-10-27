Chapter.Count = 2;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); sm.MarkTutorial2Seen(); var s = sm.GetCurrentSession();
foreach (var f in new[]{"Juice","Apple","Choux","Cake","Cookies","Coconut"}) if (!s.purchasedFoodItems.Contains(f)) s.purchasedFoodItems.Add(f);
foreach (var c in new[]{"Grandma","Chef","Yoga","Businessman"}) if (!s.purchasedCharacters.Contains(c)) s.purchasedCharacters.Add(c);
sm.UpgradeFood("Coffee"); sm.UpgradeFood("CoffeeMachine"); sm.UpgradeFood("IceCream");
s.levelsCompleted = 14; sm.AddScoreImmediately(6000f); sm.SetCurrentLevel(14); sm.SetFoodEnabled("Coconut", true); sm.SetFoodEnabled("Juice", true);
sm.PurchaseDecor("PotFlowers"); sm.PurchaseDecor("PotGreen"); sm.BuyCandies(); sm.BuyCandies();
SceneTransitionManager.Instance.TransitionToScene("GameSceneOne"); return "chapter 2 day 15, four flavours, six candies";
