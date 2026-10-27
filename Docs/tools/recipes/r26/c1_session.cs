Chapter.Count = 2;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); var s = sm.GetCurrentSession();
foreach (var f in new[]{"Juice","Apple","Choux","Cake"}) if (!s.purchasedFoodItems.Contains(f)) s.purchasedFoodItems.Add(f);
foreach (var c in new[]{"Grandma","Chef"}) if (!s.purchasedCharacters.Contains(c)) s.purchasedCharacters.Add(c);
sm.SetFoodEnabled("Juice", true); sm.SetFoodEnabled("Apple", true); sm.SetFoodEnabled("Cake", true);
s.levelsCompleted = 6; sm.AddScoreImmediately(1200f); sm.SetCurrentLevel(6); sm.PurchaseDecor("PotFlowers"); sm.BuyCandies();
SceneTransitionManager.Instance.TransitionToScene("GameSceneOne"); return "chapter 1 day 7";
