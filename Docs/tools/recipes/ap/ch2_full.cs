Chapter.Count = 2;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); var s = sm.GetCurrentSession();
foreach (var f in new[]{"Juice","Apple","Choux","Cake","Cookies","Coconut"}) if (!s.purchasedFoodItems.Contains(f)) s.purchasedFoodItems.Add(f);
foreach (var c in new[]{"Grandma","Chef","Yoga","Businessman"}) if (!s.purchasedCharacters.Contains(c)) s.purchasedCharacters.Add(c);
sm.UpgradeFood("Coffee"); sm.UpgradeFood("CoffeeMachine");
sm.SetFoodEnabled("Coconut", true); sm.SetFoodEnabled("Juice", true);
s.levelsCompleted = 16; sm.AddScoreImmediately(9000f); sm.SetCurrentLevel(16);
SceneTransitionManager.Instance.TransitionToScene("GameSceneOne"); return "chapter 2 day 17, everything bought";
