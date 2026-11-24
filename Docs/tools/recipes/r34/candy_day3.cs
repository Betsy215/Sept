// r34 (24 Nov 2026): a new player's day 3 (two days done), the candy gift not seen, the jar empty: style __STYLE__
Chapter.Count = 2;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); sm.MarkArrangeTutorialSeen(); var s = sm.GetCurrentSession();
s.levelsCompleted = 2; s.chapterDone = new System.Collections.Generic.List<int> { 2, 0 }; s.levelStars = new System.Collections.Generic.List<int> { 3, 2 };
s.candies = 0; s.candyGiftSeen = false; sm.AddScoreImmediately(420f); sm.SetCurrentLevel(2);
CandyGift.Current = (CandyGift.Style)__STYLE__; CandyGift.PreviewMode = true;
SceneTransitionManager.Instance.TransitionToScene("GameSceneOne"); return "day 3, gift style " + CandyGift.Current;
