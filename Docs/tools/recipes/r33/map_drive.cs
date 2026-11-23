// r33 (23 Nov 2026): the map after day 4 is finished, as if the truck stood at day 4 the last time the map showed:
// the truck drives from day 4 to day 5, then day 5's card opens. Arg-free; change DAY to try another day.
Chapter.Count = 2; const int DAY = 4;   // days done
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); sm.MarkArrangeTutorialSeen(); var s = sm.GetCurrentSession();
s.levelsCompleted = DAY; s.chapterDone = new System.Collections.Generic.List<int> { DAY };
s.levelStars = new System.Collections.Generic.List<int>(); for (var i = 0; i < DAY; i++) s.levelStars.Add(i % 3 == 1 ? 2 : 3);
sm.AddScoreImmediately(900f); sm.SetCurrentLevel(DAY);
var f = typeof(MapScreen).GetField("truckStoodAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
var arr = (int[])f.GetValue(null); arr[0] = DAY - 1;
SceneTransitionManager.Instance.TransitionToScene("Map"); return "map after day " + DAY + ", truck stood at day " + DAY;
