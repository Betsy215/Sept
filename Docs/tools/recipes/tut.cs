var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession();
SceneTransitionManager.Instance.TransitionToScene("GameSceneOne");
return "tutorial day";
