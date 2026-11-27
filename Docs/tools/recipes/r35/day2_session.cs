// A new game at chapter 1 day 2 (day 1 done): the first arrangement and the first menu a player meets (menu lesson, 26 November 2026).
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); var s = sm.GetCurrentSession();
s.levelsCompleted = 1; sm.AddScoreImmediately(150f); sm.SetCurrentLevel(1);
SceneTransitionManager.Instance.TransitionToScene("GameSceneOne"); return "chapter 1 day 2, menu lesson due " + sm.MenuTutorialDue;
