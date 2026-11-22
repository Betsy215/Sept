// r32 (21 Nov 2026): chapter 1 with days 1 to 9 done and chapter 2 not yet opened, on the map: the day 9 star waits
Chapter.Count = 2;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); sm.MarkArrangeTutorialSeen(); var s = sm.GetCurrentSession();
s.levelsCompleted = 9; s.chapterDone = new System.Collections.Generic.List<int> { 9 };
s.levelStars = new System.Collections.Generic.List<int> { 3, 3, 3, 2, 3, 3, 3, 2, 3 };
sm.AddScoreImmediately(2400f); sm.SetCurrentLevel(9);
SceneTransitionManager.Instance.TransitionToScene("Map"); return "map, ceremony due " + sm.Chapter2CeremonyDue;
