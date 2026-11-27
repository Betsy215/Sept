// A new game straight into the day-1 tutorial (Day 0, TutorialDirector).
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); SceneTransitionManager.Instance.TransitionToScene("GameSceneOne"); return "tutorial due " + TutorialDirector.IsDue(sm);
