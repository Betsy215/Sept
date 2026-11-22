using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public enum GamePhase
{
    ARRANGEMENT, // Player can drag items around
    PLAYING // Normal gameplay - serving customers
}

public class GamePhaseManager : MonoBehaviour
{
    [Header("UI References")] public GameObject arrangmentUI;
    public Button doneButton;

    [Header("Game References")] public LevelManager levelManager;
    public OrderSystem orderSystem;
    public CustomerManager customerManager;
    public TableLayer tableLayer;
    public RefillSystem refillSystem;

    [Header("Tutorial")] public GameObject tutorialPanel;

    [Header("Level-Specific Tutorial Text")] [Tooltip("Text component inside tutorial panel to display messages")]
    public TextMeshProUGUI tutorialText;

    [Tooltip("Tutorial messages that rotate based on level (flexible array length)")]
    public string[] levelTutorialMessages = new string[]
    {
        "Drag to arrange table",
        "Hold on item to refill",
        "The fast you serve, the more tips you earn!"
    };

    [Header("Debug")] public bool enableDebugLogs = true;

    // Private variables
    private GamePhase currentPhase = GamePhase.ARRANGEMENT;
    private bool firstDaySkipped;   // day 1 goes straight into play once per scene load (owner, 31 October 2026)

    // Two Ready buttons (owner, 18 November 2026: "have a ready button in both kitchen and game scene, after one
    // button is clicked, go to the other scene, only start game when both ready is clicked"). One button, shown on
    // both: on the table it answers for the table, in the kitchen (moved down onto the rug) for the kitchen.
    private bool tableReady, kitchenReady, inKitchen;
    private Vector2 doneButtonHome; private bool doneButtonHomeKept;
    private ArrangeHintAnim kitchenHint;
    private const string TableHint = "Drag to arrange your table", KitchenHint = "Drag to arrange your kitchen";   // the short, one-line words (owner, 19 November late: the two-line banner was too much)
    private bool tableDemoPlayed, kitchenDemoPlayed, lessonInKitchen;   // the lesson's moving hand plays once in each place

    /// The arrangement's hint (the card and the hand moving an item): on the first arrangement a player meets, which is
    /// day 2, on the table and in the kitchen, and on no later day (owner, 19 November 2026: "let's show drag to
    /// arrangement banner the first time player needs to do this, that is level 2. for the other levels we won't show").
    /// In the kitchen the card sits low, over the opening under the counter, clear of the coffee bags and the table.
    public bool HintDue => SessionManager.Instance != null && SessionManager.Instance.ArrangeTutorialDue && !SkipsArrangement
                           && currentPhase == GamePhase.ARRANGEMENT && FindObjectOfType<TutorialDirector>() == null;

    /// Day 1 comes pre-arranged: no drag, no Ready; the arrangement is taught on day 2 (owner, 31 October 2026).
    /// The tutorial day (level 0 while the tutorial is due) keeps its own flow.
    public bool SkipsArrangement => SessionManager.Instance != null && SessionManager.Instance.GetCurrentLevelIndex() == 0
                                    && !TutorialDirector.IsDue(SessionManager.Instance);
    private ServeableItem[] allFoodItems;

    // Draggable objects in the scene (ServeableItem + CoffeeMachine). Rebuilt on each phase change
    // instead of scanning every MonoBehaviour in the scene on every drag/overlap callback.
    private readonly List<IDraggable> draggableItems = new();

    private void Awake()
    {
        // Fill the item list before any Start runs. LevelManager.Start also calls StartArrangementPhase,
        // and Unity does not guarantee which Start runs first; with an empty list, loading saved
        // positions would throw and abort LevelManager.Start (popup canvas left up, every tap refused).
        EnsureFoodItems();
    }

    private void Start()
    {
        InitializeGamePhase();
    }

    private void EnsureFoodItems()
    {
        if (allFoodItems != null && allFoodItems.Length > 0) return;
        allFoodItems = levelManager != null && levelManager.serveableItems != null && levelManager.serveableItems.Length > 0
            ? levelManager.serveableItems
            : FindObjectsOfType<ServeableItem>();
    }

    private void InitializeGamePhase()
    {
        // Get all food items from LevelManager
        if (levelManager != null)
            allFoodItems = levelManager.serveableItems;
        else
            allFoodItems = FindObjectsOfType<ServeableItem>();

        if (refillSystem == null)
            refillSystem = FindObjectOfType<RefillSystem>();

        doneButton.onClick.AddListener(OnDoneButtonClicked);
        // Ready's place on the table, kept before any tap (a press moves it down for a moment)
        doneButtonHome = ((RectTransform)doneButton.transform).anchoredPosition; doneButtonHomeKept = true;

        // Start in arrangement phase
        StartArrangementPhase();

        DebugLog("GamePhaseManager initialized");
    }

    public void StartArrangementPhase()
    {
        // LevelManager.Start and our own Start both call this on a load; the second call must not undo the day 1 skip
        if (firstDaySkipped) return;
        currentPhase = GamePhase.ARRANGEMENT;
        DebugLog("=== ARRANGEMENT PHASE STARTED ===");

        RefreshDraggableCache();
        KitchenArrange.BeginDay();   // the kitchen is arranged in the same phase (16 November 2026)
        tableReady = false; kitchenReady = false;

        ShowHint(true);

        LoadSavedFoodPositions();
        FixedStation.Place(tableLayer, allFoodItems);   // the chapter's fixed food on its station, bottom left (owner, 16 October)
        FixedStation.PushOthersOff(tableLayer, allFoodItems);   // nothing may sit on the station (owner, 18 October)
        EnableArrangementMode();
        DisableGameplaySystems();
        arrangmentUI.gameObject.SetActive(true);

        // Initial button state check
        StartCoroutine(DelayedButtonStateUpdate());
        if (doneButton != null) GlowFx.Shine(doneButton.GetComponent<Image>());   // C on Ready, table and kitchen (owner, 18 November 2026); off while Ready is off
        StartCoroutine(TagNewThings());

        if (refillSystem != null)
            refillSystem.NotifyPhaseChange(currentPhase);

        if (SkipsArrangement) { firstDaySkipped = true; StartCoroutine(SkipArrangement()); }
    }

    /// Day 1: the table as laid out (saved positions or the scene's), then play, as if Ready were tapped.
    /// One frame later, so every Start of the load has run (LevelManager adds the menu book and the tint in its Start).
    private IEnumerator SkipArrangement()
    {
        yield return null;
        if (currentPhase != GamePhase.ARRANGEMENT) yield break;
        OnDoneButtonClicked();
    }

    /// The hint's card, words and moving hand: on while the tutorial's arrangement wants them (and `show`: the menu
    /// book hides them while it is open). In the kitchen the words name the kitchen and the hand moves a kitchen thing.
    public void ShowHint(bool show)
    {
        var on = show && HintDue;
        // a running lesson belongs to the place and moment it began in; cut short by the menu book or by the other place
        // opening (not by the player's own touch), it plays again when its place is shown again
        if (ArrangeLesson.Cancel()) { if (lessonInKitchen) kitchenDemoPlayed = false; else tableDemoPlayed = false; }
        if (kitchenHint != null && !(on && inKitchen)) { Destroy(kitchenHint.gameObject); kitchenHint = null; }
        if (tutorialPanel != null) tutorialPanel.SetActive(on);
        // the old lesson picture (a hand moving a bread basket) is gone; the banner stays, and the hand moves a real
        // thing once (owner, 19 November 2026)
        var oldAnim = tutorialPanel != null ? tutorialPanel.transform.Find("AnimationContainer") : null;
        if (oldAnim != null) oldAnim.gameObject.SetActive(false);
        if (tutorialText != null) tutorialText.gameObject.SetActive(on);
        var card = arrangmentUI != null ? arrangmentUI.transform.Find("HintCard") : null;
        if (card != null) card.gameObject.SetActive(on);
        if (!on) return;
        PlaceHint(card as RectTransform);
        if (tutorialText != null)
        {
            tutorialText.text = inKitchen ? KitchenHint : TableHint;
            // Hints: dark brown letters with no rim (owner, 16 October: one text style per purpose)
            tutorialText.color = new Color(0.28f, 0.15f, 0.055f);
            var plain = Resources.Load<Material>("Fonts & Materials/beachday SDF - Plain"); if (plain != null) tutorialText.fontSharedMaterial = plain;
        }
        // The lesson on a real thing, once in each place: the coffee machine on the table, the coffee bags in the kitchen
        var handPic = tutorialPanel != null ? tutorialPanel.transform.Find("AnimationContainer/Hand") : null;
        var handLook = handPic != null ? handPic.GetComponent<Image>() : null;
        var demoCanvas = arrangmentUI != null ? arrangmentUI.transform as RectTransform : null;
        if (!inKitchen && !tableDemoPlayed)
        {
            var machine = FindObjectOfType<CoffeeMachine>(); var cam = Camera.main;
            if (machine != null && cam != null)
            {
                tableDemoPlayed = true; lessonInKitchen = false;
                var p = cam.WorldToScreenPoint(machine.transform.position);   // over to the open middle of the table and back
                var to = cam.ScreenToWorldPoint(new Vector3(Mathf.Clamp(p.x + Screen.width * 0.25f, Screen.width * 0.2f, Screen.width * 0.75f), Mathf.Clamp(p.y + Screen.height * 0.14f, 0f, Screen.height * 0.45f), p.z));
                ArrangeLesson.Play(machine.transform, to, cam, demoCanvas, handLook);
            }
        }
        else if (inKitchen && !kitchenDemoPlayed)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("KitchenScene"); Transform bags = null, bg = null;
            if (scene.isLoaded) foreach (var rootGo in scene.GetRootGameObjects()) foreach (var t in rootGo.GetComponentsInChildren<Transform>(true)) { if (t.name == "coffee" && bags == null) bags = t; if (t.name == "bg" && bg == null) bg = t; }
            if (bags != null && bg != null && bags.gameObject.activeInHierarchy)
            {
                kitchenDemoPlayed = true; lessonInKitchen = true;
                var brt = (RectTransform)bags; var bgrt = (RectTransform)bg; var c = new Vector3[4]; brt.GetWorldCorners(c);
                var foot = (c[0] + c[3]) / 2f; var r = bgrt.rect;                // down onto the counter and back (the banner lies over the opening below)
                var dest = bgrt.TransformPoint(new Vector3(r.xMin + 0.30f * r.width, r.yMin + 0.47f * r.height, 0f));
                var kc = bags.GetComponentInParent<Canvas>(); var kcam = kc != null && kc.renderMode != RenderMode.ScreenSpaceOverlay ? kc.rootCanvas.worldCamera : null;
                ArrangeLesson.Play(bags, bags.position + (dest - foot), kcam, demoCanvas, handLook);
            }
        }
    }

    private Vector3 hintCardHome, hintTextHome, hintCardScale = Vector3.one, hintTextScale = Vector3.one; private bool hintHomeKept;
    // On the kitchen floor, between the lower cabinets and the Ready button on the rug (owner, 21 November 2026: "drag to
    // arrangement your kitchen banner is in the way ... position it on the kitchen floor"); it was over the big
    // opening under the counter (0.653), where things are dragged into the cabinets. A little smaller there.
    private const float KitchenHintFromTop = 0.86f, KitchenHintScale = 0.75f;

    /// The card and its words at their own place on the table; in the kitchen lower, over the opening under the
    /// counter, so the coffee bags' shelf and the kitchen table stay clear (owner, 19 November 2026).
    private void PlaceHint(RectTransform card)
    {
        var words = tutorialText != null ? tutorialText.rectTransform : null;
        if (card == null) return;
        if (!hintHomeKept) { hintCardHome = card.localPosition; hintCardScale = card.localScale; if (words != null) { hintTextHome = words.localPosition; hintTextScale = words.localScale; } hintHomeKept = true; }
        card.localPosition = hintCardHome; if (words != null) words.localPosition = hintTextHome;
        card.localScale = hintCardScale; if (words != null) words.localScale = hintTextScale;
        var canvas = arrangmentUI != null ? arrangmentUI.transform as RectTransform : null;
        if (!inKitchen || canvas == null) return;
        var want = canvas.TransformPoint(new Vector3(0f, canvas.rect.yMax - KitchenHintFromTop * canvas.rect.height, 0f)).y;
        var move = new Vector3(0f, want - card.position.y, 0f);
        card.position += move; if (words != null) words.position += move;
        card.localScale = hintCardScale * KitchenHintScale; if (words != null) words.localScale = hintTextScale * KitchenHintScale;
    }

    /// The kitchen opened or closed while arranging (KitchenArrangeRunner): the Ready button goes down onto the
    /// kitchen's rug, clear of the counter, and answers for the kitchen; the hint names the kitchen.
    public void KitchenShown(bool open)
    {
        if (this == null) return;
        inKitchen = open;
        if (doneButton != null)
        {
            var rt = (RectTransform)doneButton.transform;
            if (!doneButtonHomeKept) { doneButtonHome = rt.anchoredPosition; doneButtonHomeKept = true; }
            var canvas = arrangmentUI != null ? arrangmentUI.transform as RectTransform : null;
            if (open && canvas != null)
            {
                // on the rug: its own height (the button is drawn at scale 2) and the phone's home-bar strip above the bottom edge
                var safeBottom = Screen.height > 0 ? Screen.safeArea.yMin / Screen.height * canvas.rect.height : 0f;
                rt.anchoredPosition = new Vector2(0f, -canvas.rect.height / 2f + safeBottom + rt.rect.height * Mathf.Abs(rt.localScale.y) / 2f + 24f);   // 24 (was 60): room for the hint above it on the floor
            }
            else rt.anchoredPosition = doneButtonHome;
        }
        if (currentPhase == GamePhase.ARRANGEMENT) { ShowHint(true); UpdateDoneButtonState(); }
    }

    /// "NEW" on what was bought since the last day (owner, 18 November 2026), a moment after the load, when every
    /// thing on the table has been switched on or off.
    private IEnumerator TagNewThings()
    {
        yield return new WaitForSeconds(0.25f);
        NewTag.ClearWorld(); GlowFx.ClearWorld();
        var sm = SessionManager.Instance;
        if (sm == null || currentPhase != GamePhase.ARRANGEMENT || SkipsArrangement) yield break;
        if (allFoodItems != null)
            foreach (var item in allFoodItems)
                if (item != null && item.gameObject.activeInHierarchy && (sm.IsNewToArrange("Food:" + item.GetFoodType()) || sm.IsNewToArrange("Upgrade:" + item.GetFoodType())))
                { NewTag.OnWorld(item.transform); GlowFx.NewWorld(item.transform); }   // B: the white glow and stars (owner, 18 November 2026)
        foreach (var decor in FindObjectsOfType<DecorGate>())
            if (decor != null && sm.IsNewToArrange("Decor:" + decor.decorName)) { NewTag.OnWorld(decor.transform); GlowFx.NewWorld(decor.transform); }
        // only its own upgrade: the cups' upgrade (Upgrade:Coffee) marks the cups, not the machine (owner, 19 November 2026)
        if (sm.IsNewToArrange("Upgrade:CoffeeMachine"))
            foreach (var machine in FindObjectsOfType<CoffeeMachine>())
                if (machine != null && machine.gameObject.activeInHierarchy) { NewTag.OnWorld(machine.transform); GlowFx.NewWorld(machine.transform); }
    }

    /// <summary>
    /// Gets the current level number from SessionManager or defaults to 1
    /// </summary>
    private int GetCurrentLevel()
    {
        if (SessionManager.Instance != null)
            return SessionManager.Instance.GetCurrentLevelIndex() + 1; // +1 because SessionManager is 0-based

        DebugLog("Could not determine current level, defaulting to Level 1");
        return 1;
    }

    private IEnumerator DelayedButtonStateUpdate()
    {
        // Wait for draggable components to fully initialize
        yield return new WaitForSeconds(0.2f);
        UpdateDoneButtonState();
        DebugLog("Initial done button state updated");
    }

    public void StartPlayPhase()
    {
        currentPhase = GamePhase.PLAYING;
        DebugLog("=== PLAY PHASE STARTED ===");
        if (SessionManager.Instance != null && FindObjectOfType<TutorialDirector>() == null)
            AnalyticsLog.DayStarted(SessionManager.Instance.GetCurrentLevelIndex());

        if (refillSystem != null)
            refillSystem.NotifyPhaseChange(currentPhase);

        // Disable dragging
        DisableArrangementMode();

        // Enable gameplay systems
        EnableGameplaySystems();

        arrangmentUI.gameObject.SetActive(false);
        ShowHint(false);
        NewTag.ClearWorld(); GlowFx.ClearWorld();
        // seen: not new tomorrow. The demos (both tutorials) skip the arrangement, so what is new waits for the next one.
        if (SessionManager.Instance != null && FindObjectOfType<TutorialDirector>() == null) SessionManager.Instance.ClearNewToArrange();
    }

    /// <summary>
    /// Collect the draggable components (the concrete IDraggable implementers) once per phase change.
    /// </summary>
    private void RefreshDraggableCache()
    {
        draggableItems.Clear();

        var serveables = allFoodItems != null && allFoodItems.Length > 0
            ? allFoodItems
            : FindObjectsOfType<ServeableItem>();
        foreach (var item in serveables)
            if (item != null) draggableItems.Add(item);

        foreach (var machine in FindObjectsOfType<CoffeeMachine>())
            if (machine != null) draggableItems.Add(machine);

        DebugLog($"Cached {draggableItems.Count} draggable items");
    }

    private static bool IsLive(IDraggable item)
    {
        var behaviour = item as MonoBehaviour;
        return behaviour != null && behaviour.gameObject.activeInHierarchy;
    }

    private void EnableArrangementMode()
    {
        foreach (var item in draggableItems)
        {
            if (!IsLive(item)) continue;

            item.InitializeDragging(tableLayer, this, allFoodItems);
            item.SetDraggingEnabled(true);
            if (FixedStation.IsFixed(item as Component) && !FixedStation.IsHandle(item as Component))
            {
                // the cups cannot be dragged on their own: the machine is the station's handle and the cups follow it
                var d = ((Component)item).GetComponent<DraggableFood>(); if (d != null) d.SetDraggingEnabled(false);
            }

            DebugLog($"Enabled arrangement mode for {((MonoBehaviour)item).gameObject.name}");
        }
    }

    private void DisableArrangementMode()
    {
        foreach (var item in draggableItems)
            if (item is MonoBehaviour behaviour && behaviour != null) // skip destroyed objects
                item.SetDraggingEnabled(false);
    }

    private bool AnyItemsOverlapping()
    {
        foreach (var item in draggableItems)
            if (IsLive(item) && item.HasOverlap())
                return true;

        return false;
    }

    public void OnItemOverlapChanged()
    {
        UpdateDoneButtonState();
    }

    /// <summary>
    /// Update done button state - disable if overlaps exist
    /// </summary>
    private void UpdateDoneButtonState()
    {
        if (doneButton == null) return;

        var hasOverlaps = AnyItemsOverlapping();
        // each Ready answers for what is on screen: the table's for overlaps on the table, the kitchen's for a red
        // piece there (a red kitchen still keeps the day from starting: the table's Ready then leads into the kitchen)
        doneButton.interactable = inKitchen ? !KitchenArrange.AnyRed : !hasOverlaps;

        if (hasOverlaps) DebugLog("Done button disabled - overlaps detected");
    }

    private void DisableGameplaySystems()
    {
        if (orderSystem != null) orderSystem.enabled = false;
        if (customerManager != null) customerManager.enabled = false;
    }

    private void EnableGameplaySystems()
    {
        orderSystem.enabled = true;
        customerManager.enabled = true;
        levelManager.StartGamePlay();
    }

    public void OnDoneButtonClicked()
    {
        // A second tap in the same frame (or a double-wired button) must not start play twice
        if (currentPhase == GamePhase.PLAYING) return;
        ArrangeLesson.Cancel();   // the lesson's thing goes back to its place before anything is saved

        // Day 1 and the demos go straight into play. Otherwise both Ready buttons are needed: the first one tapped
        // leads to the other place, the second starts the day.
        var direct = SkipsArrangement || FindObjectOfType<TutorialDirector>() != null;
        var kitchen = KitchenSceneManager.Instance;
        if (!direct && kitchen != null)
        {
            if (kitchen.IsKitchenOpen)
            {
                if (KitchenArrange.AnyRed) return;
                kitchenReady = true;
                kitchen.CloseKitchen();
                if (!tableReady || AnyItemsOverlapping()) { tableReady = false; return; }
            }
            else
            {
                if (AnyItemsOverlapping()) return;
                tableReady = true;
                if (!kitchenReady || KitchenArrange.AnyRed) { kitchen.OpenKitchen(); return; }
            }
        }

        if (!direct && SessionManager.Instance != null && SessionManager.Instance.ArrangeTutorialDue) SessionManager.Instance.MarkArrangeTutorialSeen();
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (KitchenArrange.AnyRed && !SkipsArrangement) return;   // covers a tap that slipped through
        if (SessionManager.Instance != null) SessionManager.Instance.UpdateFoodPositions(allFoodItems);
        KitchenArrange.Commit();
        StartPlayPhase();
    }

    public GamePhase GetCurrentPhase()
    {
        return currentPhase;
    }

    /// <summary>
    /// Load saved food positions from SessionManager when entering arrangement phase
    /// </summary>
    private void LoadSavedFoodPositions()
    {
        if (SessionManager.Instance == null || !SessionManager.Instance.HasSavedPositions())
        {
            DebugLog("No saved positions found - using scene default positions");
            return;
        }

        var sessionData = SessionManager.Instance.GetCurrentSession();
        var savedPositions = sessionData.savedFoodPositions;

        EnsureFoodItems();

        foreach (var item in allFoodItems)
            if (item != null && item.gameObject.activeInHierarchy)
            {
                // Find saved position for this food type
                var savedPos = savedPositions.Find(p => p.foodType == item.GetFoodType());

                if (savedPos != null && !FixedStation.IsFixed(item))
                {
                    // Preserve the original Z position for layering
                    var originalZ = item.transform.position.z;
                    var position = savedPos.ToVector3(originalZ);

                    // Positions were saved on whatever device/safe area the player had then;
                    // keep them inside the current table's drag bounds.
                    if (tableLayer != null)
                        position = tableLayer.ClampToTableBounds(position);

                    var savedDrag = item.GetComponent<DraggableFood>();
                    if (savedDrag != null) savedDrag.SetRestPosition(position); else item.transform.position = position;   // the rest position too, or the wiggle pulls it back (18 October)

                    DebugLog($"Loaded saved position for {item.GetFoodType()}: ({savedPos.x:F2}, {savedPos.y:F2})");
                }
            }
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs) Debug.Log($"GamePhaseManager: {message}");
    }

    #region Debug Methods

    [ContextMenu("Test Tutorial Message Rotation")]
    public void TestTutorialMessageRotation()
    {
        DebugLog("=== TESTING TUTORIAL MESSAGE ROTATION ===");

        for (var testLevel = 1; testLevel <= levelTutorialMessages.Length + 3; testLevel++)
        {
            var messageIndex = (testLevel - 1) % levelTutorialMessages.Length;
            var message = string.Format(levelTutorialMessages[messageIndex], testLevel);
            DebugLog($"Level {testLevel} → Message Index {messageIndex}: {message}");
        }
    }

    #endregion
}