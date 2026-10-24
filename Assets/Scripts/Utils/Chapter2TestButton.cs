using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// A "Chapter 2 test" button on the main menu, only in the editor and in TestFlight builds (owner, 23 October 2026:
/// "a test build that I can play chapter 2 directly with everything in place"). One tap does what the test recipes do:
/// a fresh session with every food and person bought, the coffee upgrades, day 17 (chapter 2's first day) with Coconut and
/// Juice on the menu, then the game scene. Store builds never show it (AdsInitializer.IsTestFlightBuild is false there).
public static class Chapter2TestButton
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        SceneManager.sceneLoaded += (scene, mode) => { if (scene.name == "MainMenu") Build(); };
        if (SceneManager.GetActiveScene().name == "MainMenu") Build();
    }

    private static bool Allowed => Application.isEditor || Debug.isDebugBuild || AdsInitializer.IsTestFlightBuild();

    private static void Build()
    {
        if (!Allowed || GameObject.Find("Chapter2TestCanvas") != null) return;
        var root = new GameObject("Chapter2TestCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 900;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080f, 1920f); scaler.matchWidthOrHeight = 0.5f;
        var go = new GameObject("Chapter2Test", typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(root.transform, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f); rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-24f, 120f); rt.sizeDelta = new Vector2(300f, 84f);
        var img = go.GetComponent<Image>(); img.color = new Color(0.17f, 0.17f, 0.26f, 0.82f);
        var label = new GameObject("Label", typeof(RectTransform), typeof(Text)); label.transform.SetParent(go.transform, false);
        var lrt = (RectTransform)label.transform; lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var text = label.GetComponent<Text>(); text.text = "Chapter 2 test"; text.alignment = TextAnchor.MiddleCenter; text.fontSize = 34; text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        go.GetComponent<Button>().onClick.AddListener(StartChapter2);
    }

    /// The same set-up as Docs/tools/recipes/ap/ch2_full.cs
    private static void StartChapter2()
    {
        var sm = SessionManager.Instance; if (sm == null) { Debug.LogWarning("[Chapter2Test] no SessionManager yet"); return; }
        Chapter.Count = 2;
        sm.StartNewSession(); sm.MarkTutorialSeen(); var s = sm.GetCurrentSession();
        foreach (var f in new[] { "Juice", "Apple", "Choux", "Cake", "Cookies", "Coconut" }) if (!s.purchasedFoodItems.Contains(f)) s.purchasedFoodItems.Add(f);
        foreach (var c in new[] { "Grandma", "Chef", "Yoga", "Businessman" }) if (!s.purchasedCharacters.Contains(c)) s.purchasedCharacters.Add(c);
        sm.UpgradeFood("Coffee"); sm.UpgradeFood("CoffeeMachine");
        s.levelsCompleted = 16; sm.AddScoreImmediately(9000f); sm.SetCurrentLevel(16); sm.SetFoodEnabled("Coconut", true); sm.SetFoodEnabled("Juice", true);
        var t = SceneTransitionManager.Instance; if (t != null) t.TransitionToScene("GameSceneOne"); else SceneManager.LoadScene("GameSceneOne");
    }
}
