Chapter.Count = 2; var sm = SessionManager.Instance; if (sm == null) return "no SessionManager";
sm.StartNewSession(); sm.MarkTutorialSeen();
SceneTransitionManager.Instance.TransitionToScene("Map"); return "fresh, to map";
