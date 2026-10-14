var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen();
var s = sm.GetCurrentSession();
foreach (var f in new[]{"Juice","Apple","Choux","Cake","Cookies"}) if (!s.purchasedFoodItems.Contains(f)) s.purchasedFoodItems.Add(f);
foreach (var c in new[]{"Grandma","Chef","Yoga","Businessman"}) if (!s.purchasedCharacters.Contains(c)) s.purchasedCharacters.Add(c);
sm.AddScoreImmediately(420f);
sm.SetCurrentLevel(2);
SceneTransitionManager.Instance.TransitionToScene("GameSceneOne");
return "new game with everything, day 3, $420";
