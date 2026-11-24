// r34: the map one day earlier (chapter 1: 7 days done): the bonus days are still hidden
Chapter.Count = 2;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); sm.MarkArrangeTutorialSeen(); var s = sm.GetCurrentSession();
s.levelsCompleted = 7; s.chapterDone = new System.Collections.Generic.List<int> { 7, 0 };
s.levelStars = new System.Collections.Generic.List<int>(); for (var i = 0; i < 7; i++) s.levelStars.Add(i % 3 == 1 ? 2 : 3);
sm.AddScoreImmediately(1800f); sm.SetCurrentLevel(7);
var f = typeof(MapScreen).GetField("truckStoodAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
var arr = (int[])f.GetValue(null); for (var i = 0; i < arr.Length; i++) arr[i] = -1;
SceneTransitionManager.Instance.TransitionToScene("Map"); return "map, 7 days done";
