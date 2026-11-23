// r33 (23 Nov 2026): a new player's day 2 (only the starting foods), arranging lesson seen: for the thinking-dots probe
Chapter.Count = 2;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); sm.MarkArrangeTutorialSeen(); var s = sm.GetCurrentSession();
s.levelsCompleted = 1; s.levelStars = new System.Collections.Generic.List<int> { 3 }; sm.SetCurrentLevel(1);
SceneTransitionManager.Instance.TransitionToScene("GameSceneOne"); return "day 2, foods " + string.Join(",", s.purchasedFoodItems);
