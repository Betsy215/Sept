// r34 (24 Nov 2026): the map the moment the bonus days appear: chapter __CH__ with the days before the bonus day done
// (chapter 1: 8 done, day 9 next; chapter 2: 12 done, day 13 next), the truck one day back so it drives in first
Chapter.Count = 2; const int CH = __CH__;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); sm.MarkArrangeTutorialSeen(); sm.MarkTutorial2Seen(); var s = sm.GetCurrentSession();
var done = CH == 1 ? 8 : 12;
if (CH == 1) { s.levelsCompleted = done; s.chapterDone = new System.Collections.Generic.List<int> { done, 0 }; }
else { s.levelsCompleted = 12; s.chapterDone = new System.Collections.Generic.List<int> { 12, done }; s.chapter2Claimed = true; if (s.bonusShown == null) s.bonusShown = new System.Collections.Generic.List<int>(); s.bonusShown.Add(1); }
s.levelStars = new System.Collections.Generic.List<int>(); var total = CH == 1 ? done : 12 + done; for (var i = 0; i < total; i++) s.levelStars.Add(i % 3 == 1 ? 2 : 3);
sm.AddScoreImmediately(2400f); sm.SetCurrentLevel(CH == 1 ? done : 12 + done);
var f = typeof(MapScreen).GetField("truckStoodAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
var arr = (int[])f.GetValue(null); for (var i = 0; i < arr.Length; i++) arr[i] = -1; arr[CH - 1] = done;   // already there: the reveal starts at once (the drive is real-time, the recording is not)
SceneTransitionManager.Instance.TransitionToScene("Map"); return "map chapter " + CH + ", " + done + " days done";
