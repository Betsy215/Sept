// r34: chapter 1, day 9 done (its star given on reaching it), chapter 2 not open yet: the map opens it by itself
Chapter.Count = 2; MapScreen.GiftAutoSeconds = 1.6f;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); sm.MarkArrangeTutorialSeen(); var s = sm.GetCurrentSession();
s.levelsCompleted = 9; s.chapterDone = new System.Collections.Generic.List<int> { 9, 0 }; s.day9StarGiven = true; s.chapter2Claimed = false;
if (s.bonusShown == null) s.bonusShown = new System.Collections.Generic.List<int>(); s.bonusShown.Add(1);
s.levelStars = new System.Collections.Generic.List<int>(); for (var i = 0; i < 9; i++) s.levelStars.Add(i % 3 == 1 ? 2 : 3);
sm.AddScoreImmediately(2600f); sm.SetCurrentLevel(9);
var f = typeof(MapScreen).GetField("truckStoodAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
var arr = (int[])f.GetValue(null); for (var i = 0; i < arr.Length; i++) arr[i] = -1; arr[0] = 9;
var p = typeof(MapScreen).GetField("starPageShown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static); p.SetValue(null, false);
SceneTransitionManager.Instance.TransitionToScene("Map"); return "day 9 done";
