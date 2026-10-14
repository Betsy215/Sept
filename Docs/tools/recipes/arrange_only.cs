var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen();
var s = sm.GetCurrentSession();
foreach (var f in new[]{"Juice","Apple","Choux","Cake","Cookies"}) if (!s.purchasedFoodItems.Contains(f)) s.purchasedFoodItems.Add(f);
sm.AddScoreImmediately(420f); sm.SetCurrentLevel(2);
SceneTransitionManager.Instance.TransitionToScene("GameSceneOne");
return "to the table";
