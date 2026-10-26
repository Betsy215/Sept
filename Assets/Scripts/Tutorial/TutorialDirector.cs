using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Level 0: a self-playing demo of one order before the first real day.
///
/// A bouncing arrow shows the next thing to tap and a one-line caption says why; the game waits until
/// the player taps that thing (taps anywhere else are swallowed), then drives the real handler the way a
/// normal tap would (customer, order, serving, coffee machine, kitchen, oven), so the demo always matches
/// real behaviour. At the end two buttons offer "Let's go!" (start Day 1) and "Replay tutorial".
///
/// Everything it needs on screen is built at runtime on its own overlay canvas; no scene wiring.
/// Added by LevelManager.Start when <see cref="IsDue"/> says so.
/// </summary>
public class TutorialDirector : MonoBehaviour
{
    /// Set by the Replay button before the scene reloads.
    public static bool ReplayRequested;

    public const string GameScene = "GameSceneOne";
    public const string MapScene = "Map";

    public static bool IsDue(SessionManager sm)
    {
        if (ReplayRequested) return true;
        var s = sm != null ? sm.GetCurrentSession() : null;
        return s != null && s.isActive && !s.tutorialSeen && s.levelsCompleted == 0;
    }

    /// Chapter 2's first day (owner, 26 October 2026): once, when the session reaches chapter 2's day 1
    public static bool IsDue2(SessionManager sm)
    {
        var s = sm != null ? sm.GetCurrentSession() : null;
        return s != null && s.isActive && !s.tutorial2Seen && SessionManager.CurrentChapter == 2 && s.levelsCompleted == Chapter.DaysPerChapter[0];
    }

    public void Begin2(LevelManager lm, GamePhaseManager gpm, OrderSystem os, CustomerManager cm)
    {
        levelManager = lm; phaseManager = gpm; orderSystem = os; customerManager = cm;
        BuildUi();
        StartCoroutine(Run2());
    }

    /// The chapter 2 tutorial (owner, 26 October 2026): a real customer orders a waffle bowl with two flavours; the
    /// player taps the waffle machine; while it bakes a look at the kitchen blender (no tap needed); then a glass, the two
    /// flavours, and the glass again to serve. The day goes on afterwards.
    private IEnumerator Run2()
    {
        yield return null;
        blocker.raycastTarget = false;                                            // the player arranges the table first
        yield return new WaitUntil(() => phaseManager.GetCurrentPhase() != GamePhase.ARRANGEMENT);
        blocker.raycastTarget = true;
        var station = FindObjectOfType<IceCreamStation>();
        if (station == null) { Finish2(); yield break; }
        var want = IceCreamStation.Letters.Substring(0, 2) + IceCreamStation.WaffleLetter;   // the two open flavours and a waffle bowl
        orderSystem.freezeTimer = true;
        orderSystem.forcedNextOrder = new List<string> { "IceCream" }; orderSystem.forcedIceCreamVariant = want;

        Say("A customer walks up");
        yield return new WaitUntil(() => customerManager.HasCustomerAtService());
        yield return new WaitUntil(() => orderSystem.IsOrderActive());
        Say("An ice cream: a waffle bowl with two flavours");
        yield return new WaitForSeconds(2.5f);

        var machine = station.transform.Find("Waffle");
        if (machine != null)
        {
            Say("Tap the waffle machine to bake waffle bowls");
            yield return PointAtWorld(machine.position);
            yield return WaitForTapOnWorld(machine);
            station.OnTap(IceCreamTap.Kind.Waffle, 0);
            HideHand();
            Say("Baking. Meanwhile, the kitchen");
            yield return new WaitForSeconds(1.2f);
        }

        if (KitchenSceneManager.Instance != null)
        {
            KitchenSceneManager.Instance.OpenKitchen();
            yield return new WaitUntil(() => SceneManager.GetSceneByName("KitchenScene").isLoaded);
            yield return new WaitForSeconds(0.8f);
            var blender = FindInScene("KitchenScene", "Blender");
            Say("The blender refills the ice cream flavours when they run low");
            if (blender != null) yield return PointAtRect(blender.GetComponent<RectTransform>());
            yield return new WaitForSeconds(3.5f);
            HideHand();
            KitchenSceneManager.Instance.CloseKitchen();
            yield return new WaitForSeconds(0.6f);
        }

        while (station.IsBakingWaffles) yield return null;
        Say("Ding! A waffle bowl in each glass");
        yield return new WaitForSeconds(1.5f);

        var dish = station.transform.Find("Dish0");
        if (dish != null)
        {
            Say("Tap a glass with a waffle bowl");
            yield return PointAtWorld(dish.position);
            yield return WaitForTapOnWorld(dish);
            station.OnTap(IceCreamTap.Kind.Dish, 0);
            HideHand();
            yield return new WaitForSeconds(0.6f);
            foreach (var ch in want)
            {
                if (ch == IceCreamStation.WaffleLetter) continue;
                var f = IceCreamStation.Letters.IndexOf(ch); var sec = station.transform.Find("Sec" + f); if (sec == null) continue;
                Say("Tap the " + IceCreamStation.Names[f] + " ice cream");
                yield return PointAtWorld(sec.position);
                yield return WaitForTapOnWorld(sec);
                station.OnTap(IceCreamTap.Kind.Section, f);
                HideHand();
                yield return new WaitForSeconds(0.6f);
            }
            Say("Tap the glass again to serve");
            yield return PointAtWorld(dish.position);
            yield return WaitForTapOnWorld(dish);
            station.OnTap(IceCreamTap.Kind.Dish, 0);
            HideHand();
        }
        orderSystem.freezeTimer = false;
        Say("Served! Enjoy the beach");
        yield return new WaitForSeconds(2.5f);
        Finish2();
    }

    private void Finish2()
    {
        if (SessionManager.Instance != null) SessionManager.Instance.MarkTutorial2Seen();
        if (orderSystem != null) orderSystem.freezeTimer = false;
        if (canvas != null) Destroy(canvas.gameObject);
        Destroy(this);
    }

    private LevelManager levelManager;
    private GamePhaseManager phaseManager;
    private OrderSystem orderSystem;
    private CustomerManager customerManager;

    private Canvas canvas;
    private RectTransform canvasRect;
    private Image blocker;
    private TextMeshProUGUI caption;
    private RectTransform captionBox;
    private RectTransform hand;
    private Vector2 handBase;
    private Sprite arrowSprite;
    private readonly List<RectTransform> extraArrows = new();
    private readonly List<Vector2> extraArrowBases = new();
    private Coroutine extraBob;
    private GameObject endCard;
    private Coroutine pulse;

    // Set while a step waits for the player: returns true when a tap at that screen point hits the target
    private System.Func<Vector2, bool> awaitingHit;
    private bool tapped;

    public void Begin(LevelManager lm, GamePhaseManager gpm, OrderSystem os, CustomerManager cm)
    {
        levelManager = lm;
        phaseManager = gpm;
        orderSystem = os;
        customerManager = cm;
        if (os != null) os.maxConcurrentOrders = 1;   // the demo is one customer at the scene's place
        ReplayRequested = false;
        if (lm != null && lm.levelInfoText != null) lm.levelInfoText.text = "Day 0";   // the tutorial is day 0, not counted
        BuildUi();
        StartCoroutine(Run());
    }

    #region Sequence

    private IEnumerator Run()
    {
        yield return null; // let every Start() finish

        // Skip the arrangement phase; the demo starts straight into play
        if (phaseManager.GetCurrentPhase() == GamePhase.ARRANGEMENT)
            phaseManager.OnDoneButtonClicked();

        orderSystem.freezeTimer = true; // the demo order must not expire while the captions play
        orderSystem.forcedNextOrder = new List<string> { "Coffee", "Bread" };

        Say("A customer walks up");
        yield return new WaitUntil(() => customerManager.HasCustomerAtService());
        yield return new WaitUntil(() => orderSystem.IsOrderActive());

        Say("She wants a coffee and a bread");
        yield return new WaitForSeconds(2.5f);

        var bread = FindItem("Bread");
        var coffee = FindItem("Coffee");

        Say("Tap the bread to serve it");
        yield return PointAtWorld(bread.transform.position);
        yield return WaitForTapOnWorld(bread.transform);
        Tap(bread);
        HideHand();
        yield return new WaitForSeconds(0.8f);

        Say("Now tap the coffee");
        yield return PointAtWorld(coffee.transform.position);
        yield return WaitForTapOnWorld(coffee.transform);
        Tap(coffee);

        // No more customers after this one; the current one still finishes its walk-out
        customerManager.enabled = false;
        orderSystem.freezeTimer = false;

        HideHand();
        Say("Order done: coins plus a tip for speed");
        yield return new WaitForSeconds(3.5f);

        var machine = FindObjectOfType<CoffeeMachine>();
        if (machine != null)
        {
            Say("Coffee ran low? Tap the machine");
            yield return PointAtWorld(machine.transform.position);
            yield return WaitForTapOnWorld(machine.transform);
            TapMachine(machine);
            HideHand();
            yield return new WaitForSeconds(2.5f);
        }

        var kitchenButton = GameObject.Find("KitchenButton");
        if (kitchenButton != null && KitchenSceneManager.Instance != null)
        {
            Say("Out of bread? Tap Kitchen");
            yield return PointAtRect(kitchenButton.GetComponent<RectTransform>());
            yield return WaitForTapOnRect(kitchenButton.GetComponent<RectTransform>());
            HideHand();
            KitchenSceneManager.Instance.OpenKitchen();
            yield return new WaitUntil(() => SceneManager.GetSceneByName("KitchenScene").isLoaded);
            yield return new WaitForSeconds(0.8f);

            var oven = GameObject.Find("oven2");
            if (oven != null)
            {
                Say("Tap the oven to bake more bread");
                yield return PointAtRect(oven.GetComponent<RectTransform>());
                yield return WaitForTapOnRect(oven.GetComponent<RectTransform>());
                var ovenScript = oven.GetComponent<OvenKitchenBase>();
                if (ovenScript != null) ovenScript.OnPointerClick(EventFor(oven));
                HideHand();
                Say("Bread is baking");
                yield return new WaitForSeconds(1.5f);

                // While it bakes: the coffee bags refill the machine's beans (owner, 14 October)
                var beans = FindInScene("KitchenScene", "coffee");
                var beansScript = beans != null ? beans.GetComponent<CoffeeInKitchen>() : null;
                var beansShown = false;
                if (beansScript != null && beans.activeInHierarchy)
                {
                    Say("Machine out of beans? Tap the coffee bags");
                    yield return PointAtRect(beans.GetComponent<RectTransform>());
                    yield return WaitForTapOnRect(beans.GetComponent<RectTransform>());
                    beansScript.OnPointerClick(EventFor(beans));
                    HideHand();
                    Say("Beans are full again");
                    yield return new WaitForSeconds(1.5f);
                    beansShown = true;
                }

                // The apple tree in the kitchen window: just show it (owner, 14 October)
                var tree = FindInScene("KitchenScene", "apples");
                if (tree != null)
                {
                    Say("Fruits grow here, you can pick them!");
                    yield return PointAtRect(tree.GetComponent<RectTransform>());
                    yield return new WaitForSeconds(2.5f);
                    HideHand();
                }

                // While it bakes, point at every other kitchen section the player can unlock later
                Say("More kitchen upgrades available as you progress!");
                var sections = new List<RectTransform>();
                // Slots rather than the items: unpurchased items are inactive, their slots are always there
                foreach (var name in new[] { "windowleft", "cableft", "cabright", "coffee", "tableleft", "tableright", "spot2", "spot3" })
                {
                    if (name == "cabright" && SessionManager.IsHeldBack("Cookies")) continue;   // the Cookies plate's slot, not in this release
                    if (name == "coffee" && beansShown) continue;   // just taught; the bags are not an upgrade
                    var go = FindInScene("KitchenScene", name);
                    if (go != null) sections.Add(go.GetComponent<RectTransform>());
                }
                ShowArrowsAt(sections);

                // The real bake, however long this oven takes
                while (ovenScript != null && ovenScript.IsBaking) yield return null;

                HideExtraArrows();
                Say("Ding! Fresh bread");
                yield return new WaitForSeconds(2f);
            }

            HideHand();
            KitchenSceneManager.Instance.CloseKitchen();
            yield return new WaitForSeconds(0.6f);
        }

        HideHand();
        ShowEndCard();
    }

    #endregion

    #region Driving the game

    private static ServeableItem FindItem(string foodType)
    {
        foreach (var item in FindObjectsOfType<ServeableItem>(false))
            if (item.GetFoodType() == foodType) return item;
        return null;
    }

    private static PointerEventData EventFor(GameObject target)
    {
        var ed = new PointerEventData(EventSystem.current) { pointerId = 0 };
        var rr = new RaycastResult { gameObject = target };
        ed.pointerCurrentRaycast = rr;
        return ed;
    }

    private static void Tap(ServeableItem item)
    {
        if (item == null) return;
        var ed = EventFor(item.gameObject);
        item.OnPointerDown(ed);
        item.OnPointerUp(ed);
        item.OnPointerClick(ed);
    }

    private static void TapMachine(CoffeeMachine machine)
    {
        machine.OnPointerClick(EventFor(machine.gameObject));
    }

    #endregion

    #region Waiting for the player's tap

    /// Sits on the blocker image; every tap on screen lands here while the tutorial runs.
    public class TapCatcher : MonoBehaviour, IPointerClickHandler
    {
        public System.Action<Vector2> OnTap;
        public void OnPointerClick(PointerEventData eventData) => OnTap?.Invoke(eventData.position);
    }

    private void HandleTap(Vector2 screen)
    {
        if (awaitingHit == null || !awaitingHit(screen)) return; // not waiting, or missed the target
        awaitingHit = null;
        tapped = true;
    }

    private IEnumerator WaitForTap(System.Func<Vector2, bool> hit)
    {
        tapped = false;
        awaitingHit = hit;
        yield return new WaitUntil(() => tapped);
    }

    private IEnumerator WaitForTapOnWorld(Transform target)
    {
        return WaitForTap(screen => HitsWorld(screen, target));
    }

    private IEnumerator WaitForTapOnRect(RectTransform target)
    {
        return WaitForTap(screen =>
        {
            if (target == null) return false;
            var targetCanvas = target.GetComponentInParent<Canvas>();
            var cam = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? targetCanvas.worldCamera : null;
            return RectTransformUtility.RectangleContainsScreenPoint(target, screen, cam);
        });
    }

    /// True when a tap at this screen point lands on the target's collider (3D or 2D) or one of its children.
    private static bool HitsWorld(Vector2 screen, Transform target)
    {
        var cam = Camera.main;
        if (cam == null || target == null) return false;
        var ray = cam.ScreenPointToRay(screen);
        foreach (var h in Physics.RaycastAll(ray, 1000f))
            if (h.transform == target || h.transform.IsChildOf(target)) return true;
        foreach (var h in Physics2D.GetRayIntersectionAll(ray, 1000f))
            if (h.transform == target || h.transform.IsChildOf(target)) return true;
        return false;
    }

    #endregion

    #region Ending

    private void ShowEndCard()
    {
        captionBox.gameObject.SetActive(false);
        endCard.SetActive(true);
    }

    private void OnLetsGo()
    {
        // The demo earned coins through the real systems; give them back so Day 1 starts clean
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.RestoreScoreToSnapshot();
            SessionManager.Instance.MarkTutorialSeen();
        }
        AnalyticsLog.TutorialFinished(saidCount);

        // Day 0 is over: on to the map, where day 1 is the next star (12 October; it used to reload into day 1)
        Time.timeScale = 1f;
        if (AudioManager.Instance != null) AudioManager.Instance.StopMusic();
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionToScene(MapScene);
        else
            SceneManager.LoadScene(MapScene);
    }

    private void OnReplay()
    {
        if (SessionManager.Instance != null) SessionManager.Instance.RestoreScoreToSnapshot();
        ReplayRequested = true;
        Reload();
    }

    private void Reload()
    {
        Time.timeScale = 1f;
        if (AudioManager.Instance != null) AudioManager.Instance.StopMusic();
        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionToScene(SceneManager.GetActiveScene().name);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    #endregion

    #region Pointer and captions

    private int saidCount;

    private void Say(string text)
    {
        AnalyticsLog.TutorialStep(++saidCount, text);
        caption.text = text;
        caption.gameObject.SetActive(true);
        // The card grows for a second line (owner, 12 October: two lines did not sit well on the one-line card)
        caption.ForceMeshUpdate();
        var lines = Mathf.Max(1, caption.textInfo.lineCount);
        captionBox.sizeDelta = new Vector2(1000f, 200f + (lines - 1) * 72f);
        // one line sits on its cap height (centred on the card); two lines are centred as a block (owner, 13 October)
        caption.alignment = lines > 1 ? TextAlignmentOptions.Center : TextAlignmentOptions.Capline;
    }

    private IEnumerator PointAtWorld(Vector3 world)
    {
        var cam = Camera.main;
        var screen = cam != null ? cam.WorldToScreenPoint(world) : (Vector3)new Vector2(Screen.width / 2f, Screen.height / 2f);
        yield return PointAtScreen(screen);
    }

    private IEnumerator PointAtRect(RectTransform target)
    {
        if (target == null) yield break;
        yield return PointAtScreen(ScreenPointOf(target));
    }

    private static Vector2 ScreenPointOf(RectTransform target)
    {
        var targetCanvas = target.GetComponentInParent<Canvas>();
        var cam = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? targetCanvas.worldCamera : null;
        return RectTransformUtility.WorldToScreenPoint(cam, target.position);
    }

    /// Finds an active object by name inside one loaded scene (GameObject.Find could pick a same-named object elsewhere).
    private static GameObject FindInScene(string sceneName, string name)
    {
        var scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.isLoaded) return null;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
        return null;
    }

    /// One bobbing arrow over each target at once (used for the kitchen sections while the bread bakes).
    private void ShowArrowsAt(List<RectTransform> targets)
    {
        HideExtraArrows();
        foreach (var target in targets)
        {
            if (target == null) continue;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, ScreenPointOf(target), null, out var local);
            var arrow = NewArrow("SectionArrow", 110f);
            extraArrows.Add(arrow);
            extraArrowBases.Add(local + new Vector2(0f, 40f));
        }
        if (extraArrows.Count > 0) extraBob = StartCoroutine(BobExtraArrows());
    }

    private IEnumerator BobExtraArrows()
    {
        var t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime * 5f;
            var lift = 12f + 12f * Mathf.Sin(t);
            for (var i = 0; i < extraArrows.Count; i++)
                extraArrows[i].anchoredPosition = extraArrowBases[i] + new Vector2(0f, lift);
            yield return null;
        }
    }

    private void HideExtraArrows()
    {
        if (extraBob != null) { StopCoroutine(extraBob); extraBob = null; }
        foreach (var a in extraArrows) if (a != null) Destroy(a.gameObject);
        extraArrows.Clear();
        extraArrowBases.Clear();
    }

    private RectTransform NewArrow(string name, float size)
    {
        var img = NewImage(name, canvasRect, Color.white);
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(size, size);
        img.sprite = arrowSprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return rt;
    }

    private IEnumerator PointAtScreen(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
        hand.gameObject.SetActive(true);
        // The arrow tip is the sprite's bottom-centre (its pivot), so it points down at the target
        // from just above it and never covers the item
        handBase = local + new Vector2(0f, 60f);
        hand.anchoredPosition = handBase;
        if (pulse != null) StopCoroutine(pulse);
        pulse = StartCoroutine(Pulse());
        yield return null;
    }

    private IEnumerator Pulse()
    {
        var t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime * 5f;
            hand.anchoredPosition = handBase + new Vector2(0f, 14f + 14f * Mathf.Sin(t));
            yield return null;
        }
    }

    private void HideHand()
    {
        if (pulse != null) { StopCoroutine(pulse); pulse = null; }
        hand.gameObject.SetActive(false);
    }

    #endregion

    #region Runtime UI

    private void BuildUi()
    {
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/beachday SDF");
        var handSprite = Resources.Load<Sprite>("Tutorial/arrow");
        arrowSprite = handSprite;

        var go = new GameObject("TutorialCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; // above PopupCanvas (1) and the kitchen
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect = go.GetComponent<RectTransform>();

        // Invisible full-screen blocker: swallows every touch while the demo runs
        blocker = NewImage("Blocker", canvasRect, new Color(0, 0, 0, 0f));
        Stretch(blocker.rectTransform);
        blocker.raycastTarget = true;
        blocker.gameObject.AddComponent<TapCatcher>().OnTap = HandleTap;

        // Caption at the top: a gingham recipe card (Resources/Tutorial/card, 9-sliced) with brown letters
        var box = NewImage("CaptionBox", canvasRect, Color.white);
        var cardSprite = Resources.Load<Sprite>("Tutorial/card");
        if (cardSprite != null) { box.sprite = cardSprite; box.type = Image.Type.Sliced; }
        else box.color = new Color(1f, 0.98f, 0.93f, 0.95f);
        captionBox = box.rectTransform;
        captionBox.anchorMin = new Vector2(0.5f, 1f);
        captionBox.anchorMax = new Vector2(0.5f, 1f);
        captionBox.pivot = new Vector2(0.5f, 1f);
        captionBox.anchoredPosition = new Vector2(0f, -1110f); // over the table, below the order bubble and customer
        captionBox.sizeDelta = new Vector2(1000f, 200f);
        caption = NewText("Caption", captionBox, font, 64f, new Color(0.28f, 0.15f, 0.055f));   // hints: dark brown, no rim (owner, 16 October)
        var plain = Resources.Load<Material>("Fonts & Materials/beachday SDF - Plain"); if (plain != null) caption.fontSharedMaterial = plain;
        Stretch(caption.rectTransform);
        caption.rectTransform.offsetMin = new Vector2(40f, 36f);
        caption.rectTransform.offsetMax = new Vector2(-40f, -36f);
        caption.alignment = TextAlignmentOptions.Capline; // centred on the cap height, so the line sits in the middle of the card
        caption.enableWordWrapping = true;
        caption.characterSpacing = 3f;
        caption.enableAutoSizing = true; // long lines shrink a little instead of spilling off the card
        caption.fontSizeMax = 64f;
        caption.fontSizeMin = 40f;

        // Pointing arrow
        var handImage = NewImage("Hand", canvasRect, Color.white);
        hand = handImage.rectTransform;
        hand.anchorMin = hand.anchorMax = new Vector2(0.5f, 0.5f);
        hand.pivot = new Vector2(0.5f, 0f);
        hand.sizeDelta = new Vector2(150f, 150f);
        handImage.sprite = handSprite;
        handImage.preserveAspect = true;
        handImage.raycastTarget = false;
        hand.gameObject.SetActive(false);

        // End card
        endCard = new GameObject("EndCard", typeof(RectTransform));
        endCard.transform.SetParent(canvasRect, false);
        var cardRect = endCard.GetComponent<RectTransform>();
        Stretch(cardRect);
        var dim = NewImage("Dim", cardRect, new Color(0f, 0f, 0f, 0.55f));
        Stretch(dim.rectTransform);
        var panel = NewImage("Panel", cardRect, new Color(1f, 0.96f, 0.88f, 1f));
        panel.rectTransform.sizeDelta = new Vector2(820f, 620f);
        var title = NewText("Title", panel.rectTransform, font, 72f, new Color(0.25f, 0.15f, 0.1f));
        title.rectTransform.anchoredPosition = new Vector2(0f, 180f);
        title.rectTransform.sizeDelta = new Vector2(760f, 120f);
        title.alignment = TextAlignmentOptions.Center;
        title.text = "Ready to go?";
        NewButton("LetsGo", panel.rectTransform, font, "Let's go!", new Vector2(0f, 10f), new Color(1f, 0.72f, 0.2f), OnLetsGo);
        NewButton("Replay", panel.rectTransform, font, "Replay tutorial", new Vector2(0f, -150f), new Color(0.85f, 0.85f, 0.8f), OnReplay);
        endCard.SetActive(false);
    }

    private static Image NewImage(string name, RectTransform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    private static TextMeshProUGUI NewText(string name, RectTransform parent, TMP_FontAsset font, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void NewButton(string name, RectTransform parent, TMP_FontAsset font, string label, Vector2 pos, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var img = NewImage(name, parent, color);
        img.rectTransform.anchoredPosition = pos;
        img.rectTransform.sizeDelta = new Vector2(560f, 130f);
        var button = img.gameObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);
        var text = NewText("Label", img.rectTransform, font, 56f, new Color(0.25f, 0.15f, 0.1f));
        Stretch(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;
        text.text = label;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    #endregion
}
