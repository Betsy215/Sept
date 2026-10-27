Chapter.Count = 2;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); var s = sm.GetCurrentSession();
foreach (var f in new[]{"Juice","Apple","Choux","Cake","Cookies","Coconut"}) if (!s.purchasedFoodItems.Contains(f)) s.purchasedFoodItems.Add(f);
s.levelsCompleted = Chapter.DaysPerChapter[0]; sm.AddScoreImmediately(3000f); sm.SetCurrentLevel(Chapter.DaysPerChapter[0]); sm.SetFoodEnabled("Juice", true);
SceneTransitionManager.Instance.TransitionToScene("GameSceneOne"); return "chapter 2 first day, tutorial due " + TutorialDirector.IsDue2(sm);
