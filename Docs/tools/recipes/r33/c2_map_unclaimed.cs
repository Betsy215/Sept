// r33 (23 Nov 2026): a player already in chapter 2 (14 days done) whose day 9 star was never taken, on the map:
// the map opens on chapter 1 with day 9 waiting
Chapter.Count = 2;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); sm.MarkArrangeTutorialSeen(); var s = sm.GetCurrentSession();
s.levelsCompleted = 14; s.chapterDone = new System.Collections.Generic.List<int> { 12, 2 }; s.chapter2Claimed = false; s.tutorial2Seen = true;
s.levelStars = new System.Collections.Generic.List<int>(); for (var i = 0; i < 14; i++) s.levelStars.Add(i % 4 == 1 ? 2 : 3);
sm.AddScoreImmediately(2400f); sm.SetCurrentLevel(14);
SceneTransitionManager.Instance.TransitionToScene("Map"); return "map, ceremony due " + sm.Chapter2CeremonyDue + ", stars " + sm.StarsAvailable;
