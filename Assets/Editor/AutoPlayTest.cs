using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-only automated playthrough: plays the whole game through the real tap handlers
/// (tutorial, serving, kitchen restocks, shop purchases and upgrades, every day to Day 14) and
/// collects every error or exception logged on the way. Driven from Claude Code with
/// execute_code: AutoPlayTest.Start() in play mode, then read AutoPlayTest.Report.
/// Not part of the build (Editor folder).
/// </summary>
public static class AutoPlayTest
{
    private static bool running;
    private static double nextActionAt;
    private static double stateSince;
    private static string lastState = "";
    private static readonly StringBuilder log = new();
    private static readonly List<string> errors = new();
    private static int lastLevelSeen = -1;
    private static int purchasesThisVisit;
    private static double lastMachineTap = -100;
    private static int taps, restocks, purchases, daysDone;
    private static bool done;

    public static bool Running => running;
    public static bool Done => done;

    public static string Report =>
        $"running={running} done={done} days={daysDone} taps={taps} restocks={restocks} purchases={purchases} errors={errors.Count}\n" +
        $"state={lastState} (for {EditorApplication.timeSinceStartup - stateSince:F0}s)\n--- log\n{log}\n--- errors\n{string.Join("\n", errors)}";

    public static void Start()
    {
        Stop();
        log.Clear(); errors.Clear();
        taps = restocks = purchases = daysDone = 0; done = false; lastLevelSeen = -1;
        lastState = ""; stateSince = EditorApplication.timeSinceStartup; nextActionAt = 0;
        Application.logMessageReceived += OnLog;
        EditorApplication.update += Tick;
        running = true;
        Time.timeScale = 2f;
        Note("start");
    }

    public static void Stop()
    {
        if (!running) return;
        Application.logMessageReceived -= OnLog;
        EditorApplication.update -= Tick;
        running = false;
        Time.timeScale = 1f;
    }

    private static void OnLog(string condition, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        if (condition.Contains("Curl error") || condition.Contains("AnimationEvent has no function name")) return; // known editor noise
        errors.Add($"[{SceneManager.GetActiveScene().name}] {condition}\n{stack.Split('\n')[0]}");
    }

    private static void Note(string s)
    {
        log.AppendLine($"{EditorApplication.timeSinceStartup:F0}s {s}");
    }

    private static void State(string s)
    {
        if (s == lastState) return;
        lastState = s;
        stateSince = EditorApplication.timeSinceStartup;
    }

    private static void Wait(float seconds) => nextActionAt = EditorApplication.timeSinceStartup + seconds;

    private static void Tick()
    {
        if (!Application.isPlaying) { Note("play mode ended"); Stop(); return; }
        if (done) return;
        if (EditorApplication.timeSinceStartup < nextActionAt) return;

        if (EditorApplication.timeSinceStartup - stateSince > 90)
        {
            Note($"STUCK in '{lastState}' for 90 s; stopping");
            errors.Add($"stuck: {lastState}");
            done = true; Stop(); return;
        }

        var scene = SceneManager.GetActiveScene().name;
        switch (scene)
        {
            case "MainMenu": StepMenu(); break;
            case "GameSceneOne": StepGame(); break;
            case "Shop": StepShop(); break;
            default: State("scene " + scene); Wait(1f); break;
        }
    }

    // ---------------------------------------------------------------- menu
    private static void StepMenu()
    {
        State("menu");
        var sm = SessionManager.Instance;
        if (sm == null) { Wait(0.5f); return; }
        sm.StartNewSession();
        Note("new session");
        SceneTransitionManager.Instance.TransitionToScene("GameSceneOne");
        Wait(3f);
    }

    // ---------------------------------------------------------------- game
    private static void StepGame()
    {
        var lm = Object.FindObjectOfType<LevelManager>();
        var gpm = Object.FindObjectOfType<GamePhaseManager>();
        var os = Object.FindObjectOfType<OrderSystem>();
        if (lm == null || gpm == null || os == null) { State("game loading"); Wait(0.5f); return; }
        if (Time.timeScale > 0f && Time.timeScale < 3f) Time.timeScale = 3f; // the scene load resets it to 1

        // Tutorial (Day 1 of a new game)
        var tutorial = GameObject.Find("TutorialCanvas");
        if (tutorial != null) { StepTutorial(tutorial); return; }

        var session = SessionManager.Instance.GetCurrentSession();
        if (session.currentLevel != lastLevelSeen)
        {
            lastLevelSeen = session.currentLevel;
            Note($"Day {session.currentLevel + 1} starts: money={session.totalScore:F2}, orders={os.ordersPerLevel}");
        }

        // Level complete popup
        var panel = GameObject.Find("LevelCompletePanel");
        if (panel != null && panel.activeInHierarchy)
        {
            State("level complete");
            if (EditorApplication.timeSinceStartup - stateSince < 1.5) { Wait(0.5f); return; }
            daysDone++;
            Note($"Day {session.currentLevel + 1} done: money={session.totalScore:F2}, completed={session.levelsCompleted}");
            if (session.currentLevel + 1 >= lm.allLevels.Length)
            {
                Note("ALL DAYS COMPLETE");
                done = true; Stop(); return;
            }
            lm.LoadNextLevel();
            Wait(3f);
            return;
        }

        if (gpm.GetCurrentPhase() == GamePhase.ARRANGEMENT)
        {
            State("arrangement");
            gpm.OnDoneButtonClicked();
            Note("arrangement done");
            Wait(0.5f);
            return;
        }

        var kitchen = SceneManager.GetSceneByName("KitchenScene");
        if (kitchen.isLoaded)
        {
            // We are inside the kitchen for a restock: close it again after the tap
            State("kitchen");
            KitchenSceneManager.Instance.CloseKitchen();
            Wait(1f);
            return;
        }

        if (!os.IsOrderActive()) { State("waiting for order"); Wait(0.4f); return; }

        State("serving");
        var types = os.GetCurrentOrderTypes();
        if (types.Count == 0) { Wait(0.3f); return; }

        // Serve whichever ordered item is in stock; restock the first one only when nothing is servable
        ServeableItem item = null;
        foreach (var t in types)
        {
            var candidate = FindItem(t);
            if (candidate == null) continue;
            var r = candidate.GetComponent<RefillableItem>();
            if (r == null || !r.IsOutOfStock()) { item = candidate; break; }
        }
        if (item == null)
        {
            if (FindItem(types[0]) == null) { Note($"no counter item for {types[0]}"); Wait(0.5f); return; }
            Restock(types[0]);
            return;
        }

        var ed = EventFor(item.gameObject);
        item.OnPointerDown(ed);
        item.OnPointerUp(ed);
        item.OnPointerClick(ed);
        taps++;
        Wait(0.35f);
    }

    private static void Restock(string type)
    {
        restocks++;
        if (type == "Coffee" && EditorApplication.timeSinceStartup - lastMachineTap > 8)
        {
            // Brew first; if coffee is still out 8 s later the beans are gone, so fall through to the kitchen bag
            var machine = Object.FindObjectOfType<CoffeeMachine>();
            if (machine != null) { machine.OnPointerClick(EventFor(machine.gameObject)); lastMachineTap = EditorApplication.timeSinceStartup; Note("coffee machine tapped"); Wait(2f); return; }
        }

        if (KitchenSceneManager.Instance == null) { Wait(1f); return; }
        KitchenSceneManager.Instance.OpenKitchen();
        Note($"kitchen opened to restock {type}");
        // Tap the matching kitchen item on the next tick once the scene is loaded
        EditorApplication.delayCall += () => EditorApplication.delayCall += () => TapKitchenFor(type);
        Wait(2.5f);
    }

    private static void TapKitchenFor(string type)
    {
        var name = type switch
        {
            "Bread" => "oven2", "Choux" => "oven1", "Cake" => "oven3",
            "Apple" => "apples", "Juice" => "juice", "Coffee" => "coffee", _ => null
        };
        if (name == null) return;
        var go = FindInScene("KitchenScene", name);
        if (go == null) { Note($"kitchen item {name} not found (locked?)"); return; }
        // Ovens, timed items (apples) and plain click items (juice, coffee bag) all take a pointer click
        var handler = go.GetComponent<IPointerClickHandler>();
        if (handler == null) { Note($"kitchen item {name} has no click handler"); return; }
        if (!go.activeInHierarchy) { Note($"kitchen item {name} is locked (not purchased yet)"); return; }
        handler.OnPointerClick(EventFor(go));
        Note($"kitchen {name} tapped");
    }

    // ---------------------------------------------------------------- tutorial
    private static void StepTutorial(GameObject tc)
    {
        State("tutorial");
        var end = tc.transform.Find("EndCard");
        if (end != null && end.gameObject.activeSelf)
        {
            foreach (var b in tc.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                if (b.name.Contains("LetsGo")) { b.onClick.Invoke(); Note("tutorial: Let's go"); Wait(3f); return; }
        }

        var catcher = tc.transform.Find("Blocker")?.GetComponent<TutorialDirector.TapCatcher>();
        var cap = tc.GetComponentInChildren<TMPro.TextMeshProUGUI>(true)?.text ?? "";
        if (catcher == null) { Wait(0.5f); return; }

        Vector2 pos = Vector2.zero; var what = "";
        if (cap.Contains("bread to serve") || cap.Contains("tap the coffee"))
        {
            var food = cap.Contains("bread") ? "Bread" : "Coffee";
            var it = FindItem(food);
            if (it != null) { pos = Camera.main.WorldToScreenPoint(it.transform.position); what = food; }
        }
        else if (cap.Contains("machine"))
        {
            var m = Object.FindObjectOfType<CoffeeMachine>();
            if (m != null) { pos = Camera.main.WorldToScreenPoint(m.transform.position); what = "machine"; }
        }
        else if (cap.Contains("Kitchen"))
        {
            var kb = GameObject.Find("KitchenButton")?.GetComponent<RectTransform>();
            if (kb != null) { pos = RectTransformUtility.WorldToScreenPoint(kb.GetComponentInParent<Canvas>().worldCamera, kb.position); what = "kitchen"; }
        }
        else if (cap.Contains("oven"))
        {
            var ov = FindInScene("KitchenScene", "oven2")?.GetComponent<RectTransform>();
            if (ov != null)
            {
                var c = ov.GetComponentInParent<Canvas>();
                pos = RectTransformUtility.WorldToScreenPoint(c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera, ov.position);
                what = "oven";
            }
        }
        if (what == "") { Wait(0.5f); return; }
        catcher.OnPointerClick(new PointerEventData(EventSystem.current) { position = pos });
        Note($"tutorial: tapped {what} ('{cap}')");
        Wait(1.5f);
    }

    // ---------------------------------------------------------------- shop
    private static void StepShop()
    {
        var shop = Object.FindObjectOfType<ShopManager>();
        if (shop == null) { State("shop loading"); Wait(0.5f); return; }
        if (lastState != "shop") purchasesThisVisit = 0;
        State("shop");

        var session = SessionManager.Instance;
        foreach (var item in Object.FindObjectsOfType<ShopItemController>(true))
        {
            if (item.isPurchased) continue;
            if (!session.CanAfford(item.price)) continue;
            var before = session.GetTotalScore();
            shop.PurchaseItem(item);
            var after = session.GetTotalScore();
            if (after < before)
            {
                purchases++; purchasesThisVisit++;
                Note($"bought {item.itemName} ({item.itemType}) for {item.price}: {before:F2} -> {after:F2}");
                Wait(0.3f);
                return;
            }
        }

        Note($"shop: {purchasesThisVisit} purchases, money={session.GetTotalScore():F2}; next day");
        shop.LoadNextGameLevel();
        Wait(3f);
    }

    // ---------------------------------------------------------------- helpers
    private static ServeableItem FindItem(string foodType)
    {
        foreach (var item in Object.FindObjectsOfType<ServeableItem>(false))
            if (item.GetFoodType() == foodType) return item;
        return null;
    }

    private static GameObject FindInScene(string sceneName, string name)
    {
        var scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded) return null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
        return null;
    }

    private static PointerEventData EventFor(GameObject target)
    {
        var ed = new PointerEventData(EventSystem.current) { pointerId = 0 };
        ed.pointerCurrentRaycast = new RaycastResult { gameObject = target };
        return ed;
    }
}
