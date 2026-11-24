// r34 (24 Nov 2026): an older save: every kitchen piece's place kept, the tray and mixer at their cabinet fallbacks; choux just bought
Chapter.Count = 2;
var sm = SessionManager.Instance; if (sm == null) return "no SessionManager yet";
sm.StartNewSession(); sm.MarkTutorialSeen(); sm.MarkArrangeTutorialSeen(); var s = sm.GetCurrentSession();
if (!s.purchasedFoodItems.Contains("Choux")) s.purchasedFoodItems.Add("Choux");
s.levelsCompleted = 5; s.chapterDone = new System.Collections.Generic.List<int> { 5, 0 }; s.levelStars = new System.Collections.Generic.List<int> { 3, 3, 2, 3, 3 };
sm.AddScoreImmediately(300f); sm.SetCurrentLevel(5); sm.SetFoodEnabled("Choux", true);
if (s.newThings == null) s.newThings = new System.Collections.Generic.List<string>(); if (!s.newThings.Contains("Food:Choux")) s.newThings.Add("Food:Choux");
var places = new System.Collections.Generic.Dictionary<string, Vector2> {
 {"Blender", new Vector2(0.145f, 0.5035f)}, {"cutboard", new Vector2(0.385f, 0.428f)}, {"whisk", new Vector2(0.0505f, 0.437f)}, {"basket", new Vector2(0.56f, 0.5177f)},
 {"cake", new Vector2(0.8078f, 0.451f)}, {"coffee", new Vector2(0.2694f, 0.6859f)}, {"chouxplate", new Vector2(0.2861f, 0.2906f)}, {"mixer", new Vector2(0.40f, 0.2906f)}, {"juice", new Vector2(0.1662f, 0.1469f)} };
sm.SetKitchenPlaces(places);
var w = typeof(KitchenArrange).GetField("working", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static); w.SetValue(null, null);
SceneTransitionManager.Instance.TransitionToScene("GameSceneOne"); return "old save, choux new";
