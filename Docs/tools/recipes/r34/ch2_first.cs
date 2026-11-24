// r34: chapter 2 just opened, its map never seen: the welcome
Chapter.Count = 2; MapScreen.GiftAutoSeconds = 1.6f;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); sm.MarkArrangeTutorialSeen(); var s = sm.GetCurrentSession();
s.levelsCompleted = 9; s.chapterDone = new System.Collections.Generic.List<int> { 9, 0 }; s.day9StarGiven = true; s.chapter2Claimed = true; s.chapter2MapSeen = false;
if (s.bonusShown == null) s.bonusShown = new System.Collections.Generic.List<int>(); s.bonusShown.Add(1);
s.levelStars = new System.Collections.Generic.List<int>(); for (var i = 0; i < 9; i++) s.levelStars.Add(3);
sm.AddScoreImmediately(2600f); sm.SetCurrentLevel(12);
var f = typeof(MapScreen).GetField("truckStoodAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
var arr = (int[])f.GetValue(null); for (var i = 0; i < arr.Length; i++) arr[i] = -1;
SceneTransitionManager.Instance.TransitionToScene("Map"); return "chapter 2 map, first time";
