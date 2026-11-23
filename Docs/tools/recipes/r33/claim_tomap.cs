// r33: day 9's star taken (so the map opens on the player's own chapter), then to the map
var sm = SessionManager.Instance; if (sm == null) return "no session"; sm.ClaimChapter2();
SceneTransitionManager.Instance.TransitionToScene("Map"); return "claimed, to map";
