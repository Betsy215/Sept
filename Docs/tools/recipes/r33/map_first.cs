// r33 (23 Nov 2026): four days done, to the MAP (not into the day): the real flow's start for the truck drive test
Chapter.Count = 2;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); sm.MarkArrangeTutorialSeen(); var s = sm.GetCurrentSession();
s.levelsCompleted = 4; s.chapterDone = new System.Collections.Generic.List<int> { 4 }; s.levelStars = new System.Collections.Generic.List<int> { 3, 3, 2, 3 };
sm.AddScoreImmediately(900f); sm.SetCurrentLevel(4);
SceneTransitionManager.Instance.TransitionToScene("Map"); return "map, day 5 next";
