var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); sm.AddScoreImmediately(1234f); sm.SetCurrentLevel(2);
SceneTransitionManager.Instance.TransitionToScene("Shop"); return "fresh session, $1234, to shop";
