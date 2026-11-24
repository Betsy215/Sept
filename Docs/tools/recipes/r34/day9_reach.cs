// r34 (24 Nov 2026): chapter 1, day 8 done, day 9 next: bonus days appear, then day 9's star on its gift card (Let's go by itself after 1.6 s)
Chapter.Count = 2; MapScreen.GiftAutoSeconds = 1.6f;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); sm.MarkArrangeTutorialSeen(); var s = sm.GetCurrentSession();
s.levelsCompleted = 8; s.chapterDone = new System.Collections.Generic.List<int> { 8, 0 };
s.levelStars = new System.Collections.Generic.List<int>(); for (var i = 0; i < 8; i++) s.levelStars.Add(i % 3 == 1 ? 2 : 3);
sm.AddScoreImmediately(2400f); sm.SetCurrentLevel(8);
var f = typeof(MapScreen).GetField("truckStoodAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
var arr = (int[])f.GetValue(null); for (var i = 0; i < arr.Length; i++) arr[i] = -1; arr[0] = 8;
SceneTransitionManager.Instance.TransitionToScene("Map"); return "day 9 reached";
