var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen();
var s = sm.GetCurrentSession();
foreach (var f in new[]{"Juice","Apple","Choux","Cake","Cookies"}) sm.PurchaseFoodItem(f);   // bought and switched on (menu book, 14 October)
sm.AddScoreImmediately(420f); sm.SetCurrentLevel(2);
SceneTransitionManager.Instance.TransitionToScene("GameSceneOne");
return "to the table";
