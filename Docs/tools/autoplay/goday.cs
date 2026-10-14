var sm = SessionManager.Instance; sm.SetCurrentLevel(DAY); SceneTransitionManager.Instance.TransitionToScene("GameSceneOne"); return "to day " + (DAY + 1);
