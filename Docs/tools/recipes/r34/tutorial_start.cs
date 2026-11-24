// r34: a new game straight into the chapter 1 tutorial (day 0)
Chapter.Count = 2;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); SceneTransitionManager.Instance.TransitionToScene("GameSceneOne"); return "tutorial game";
