// r34 (23 Nov 2026, owner: "show me the bound for table"): a chapter 1 day 8 with most chapter 1 foods on the table
Chapter.Count = 2;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); sm.MarkArrangeTutorialSeen(); var s = sm.GetCurrentSession();
foreach (var f in new[]{"Juice","Apple","Choux","Cake","Cookies"}) if (!s.purchasedFoodItems.Contains(f)) s.purchasedFoodItems.Add(f);
sm.UpgradeFood("Coffee"); sm.UpgradeFood("CoffeeMachine");
s.levelsCompleted = 7; s.levelStars = new System.Collections.Generic.List<int> { 3, 3, 3, 3, 3, 3, 3 }; sm.SetCurrentLevel(7);
SceneTransitionManager.Instance.TransitionToScene("GameSceneOne");
return "chapter 1 day 8, foods " + string.Join(",", s.purchasedFoodItems) + " menu " + (s.enabledFoodItems == null ? "-" : string.Join(",", s.enabledFoodItems));
