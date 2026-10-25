// a chapter 2 session for the autoplay: everything bought, day 17 next, $2000 (17 October)
Chapter.Count = 2;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); var s = sm.GetCurrentSession();
foreach (var f in new[]{"Juice","Apple","Choux","Cake","Cookies","Coconut"}) if (!s.purchasedFoodItems.Contains(f)) s.purchasedFoodItems.Add(f);
foreach (var c in new[]{"Grandma","Chef","Yoga","Businessman"}) if (!s.purchasedCharacters.Contains(c)) s.purchasedCharacters.Add(c);
sm.UpgradeFood("Coffee"); sm.UpgradeFood("CoffeeMachine");
s.levelsCompleted = 12; sm.AddScoreImmediately(2000f); sm.SetCurrentLevel(12); sm.SetFoodEnabled("Coconut", true); sm.SetFoodEnabled("Juice", true);   // after the level is set: Coconut is a chapter 2 food (21 October)
return "chapter 2 session: day 17, everything bought, $2000";
