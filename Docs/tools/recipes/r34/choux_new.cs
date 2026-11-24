// r34 (24 Nov 2026): the day after choux is bought (NEW on what came with it): day 6, choux on the menu and new to arrange
Chapter.Count = 2;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); sm.MarkArrangeTutorialSeen(); var s = sm.GetCurrentSession();
if (!s.purchasedFoodItems.Contains("Choux")) s.purchasedFoodItems.Add("Choux");
s.levelsCompleted = 5; s.chapterDone = new System.Collections.Generic.List<int> { 5, 0 }; s.levelStars = new System.Collections.Generic.List<int> { 3, 3, 2, 3, 3 };
sm.AddScoreImmediately(300f); sm.SetCurrentLevel(5); sm.SetFoodEnabled("Choux", true);
if (s.newThings == null) s.newThings = new System.Collections.Generic.List<string>(); if (!s.newThings.Contains("Food:Choux")) s.newThings.Add("Food:Choux");
SceneTransitionManager.Instance.TransitionToScene("GameSceneOne"); return "day 6, choux new";
