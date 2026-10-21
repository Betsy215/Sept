Chapter.Count = 2;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); var s = sm.GetCurrentSession();
foreach (var f in new[]{"Juice","Apple","Choux","Cake","Cookies","Coconut"}) if (!s.purchasedFoodItems.Contains(f)) s.purchasedFoodItems.Add(f);
if (!s.enabledFoodItems.Contains("Coconut")) s.enabledFoodItems.Add("Coconut");   // as a chapter 2 player's save would hold it
sm.SetFoodEnabled("Juice", true); sm.SetFoodEnabled("Apple", true);
s.levelsCompleted = 6; sm.AddScoreImmediately(1200f); sm.SetCurrentLevel(6);
SceneTransitionManager.Instance.TransitionToScene("GameSceneOne"); return "chapter 1 day 7 with Coconut in the save";
