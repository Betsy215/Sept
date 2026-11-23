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
    public static bool ReplayRequested2;   // the chapter 2 tutorial's Replay button (26 October 2026)
    private bool chapter2;                 // this run is the chapter 2 tutorial: its end card restarts chapter 2's first day

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
        if (ReplayRequested2) return true;
        var s = sm != null ? sm.GetCurrentSession() : null;
        return s != null && s.isActive && !s.tutorial2Seen && SessionManager.CurrentChapter == 2 && sm.DaysDoneIn(2) == 0;   // chapter 2's first day (it can open before chapter 1 is finished)
    }

    public void Begin2(LevelManager lm, GamePhaseManager gpm, OrderSystem os, CustomerManager cm)
    {
        levelManager = lm; phaseManager = gpm; orderSystem = os; customerManager = cm; chapter2 = true;
        PatienceMeter.HoldFourth = SessionManager.Instance != null && SessionManager.Instance.Chapter2GiftDue;   // this customer is handed the fourth flower (FlowerGift)
        if (os != null) os.maxConcurrentOrders = 1;   // a demo like chapter 1's: one customer, no clock, coins given back at the end (owner, 26 October 2026)
        ReplayRequested2 = false;
        if (lm != null && lm.levelInfoText != null) lm.levelInfoText.text = "Tutorial";
        BuildUi();
        StartCoroutine(Run2());
    }

    /// The chapter 2 tutorial (owner, 26 October 2026): a real customer orders a waffle bowl with two flavours; the
    /// player taps the waffle machine; while it bakes a look at the kitchen blender (no tap needed); then a glass, the two
    /// flavours, and the glass again to serve. The day goes on afterwards.
    private IEnumerator Run2()
    {
        yield return null;
        if (phaseManager.GetCurrentPhase() == GamePhase.ARRANGEMENT) phaseManager.OnDoneButtonClicked();   // like chapter 1's demo: straight into play
        var station = FindObjectOfType<IceCreamStation>();
        if (station == null) { Finish2(); yield break; }
        var taps = IceCreamStation.Letters.Substring(0, 2);                                   // the two open flavours, tapped vanilla first
        var sorted = taps.ToCharArray(); System.Array.Sort(sorted);
        var want = new string(sorted) + IceCreamStation.WaffleLetter;   // the order as the station spells a glass (sorted scoops, then the waffle), so the serve matches (owner, 26 October: "the ice cream should get served, not it shakes")
        orderSystem.freezeTimer = true;
        orderSystem.forcedNextOrder = new List<string> { "IceCream" }; orderSystem.forcedIceCreamVariant = want;

        Say("A customer walks up");
        yield return new WaitUntil(() => customerManager.HasCustomerAtService());
        yield return new WaitUntil(() => orderSystem.IsOrderActive());
        if (PatienceMeter.HoldFourth) yield return FlowerGift();   // chapter 2's gift, once: the customer stands there with three flowers and is handed the fourth
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
            // the blender is a step to do too, not only to look at (owner, 21 November 2026: "put a hand there too and wait
            // for player to click on blender, play the blender animation after they click, ... a interactive tutorial")
            var blender = FindInScene("KitchenScene", "Blender");
            var anim = blender != null ? blender.GetComponent<BlenderAnim>() : null;
            if (blender != null && anim != null)
            {
                var brt = blender.GetComponent<RectTransform>();
                Say("Tap the blender to refill the ice cream");
                yield return PointAtRect(brt);
                yield return WaitForTapOnRect(brt);
                HideHand();
                anim.Blend();                                                       // its whirr, the colours, and the wells full again
                Say("The blender refills the ice cream when it runs low");
                yield return new WaitForSeconds(0.3f);
                while (anim != null && anim.IsBlending) yield return null;
                yield return new WaitForSeconds(0.6f);
            }
            else
            {
                Say("The blender refills the ice cream when it runs low");
                yield return new WaitForSeconds(3f);
            }
            KitchenSceneManager.Instance.CloseKitchen();
            captionBox.gameObject.SetActive(false);                                 // no banner back at the table (owner, 26 October: "remove back to the table banner")
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
            foreach (var ch in taps)
            {
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
        customerManager.enabled = false;                                           // no more customers; this one finishes its walk-out
        orderSystem.freezeTimer = false;
        HideHand();
        Say("Served! Coins and a tip for speed");
        yield return new WaitForSeconds(3f);
        ShowEndCard();
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
    private const float CaptionY = -1110f;
    private RectTransform hand;
    private Vector2 handBase;
    private Sprite arrowSprite;
    private CanvasGroup handGroup;
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

        // Arranging is taught here, as a moving demo (owner, 19 November 2026: "only shows these banner and animation in
        // tutorial. add animation to demo it in tutorial, moving things"); the arrangement phase itself shows no hint
        // (later the same day: "it's kind of confusing, player would think they can do that anytime ... take out the
        // arrangement tutorial from the initial tutorial". The lesson is given on day 2, in the arranging time itself:
        // GamePhaseManager.ShowHint and ArrangeLesson.)

        var kitchenButton = GameObject.Find("KitchenButton");
        if (kitchenButton != null && KitchenSceneManager.Instance != null)
        {
            // The kitchen is taught as a swipe (owner, 14 November 2026: "in tutorial, add a swipe arrow indication
            // 'swipe to go to kitchen', when kitchen tutorial ends, also show swipe arrow 'swipe back to serve'").
            // A tap on the truck still counts, so nobody is stuck.
            Say("Swipe to go to kitchen");
            yield return WaitForSwipe(1, kitchenButton.GetComponent<RectTransform>());
            yield return KitchenSwipe.SlideOpen();
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
                foreach (var name in new[] { "windowleft", "cableft", "cabright", "coffee", "cutboard", "cake", "spot2", "spot3" })   // the board and the cake themselves: KitchenArrange moves them out of their old slots
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
            Say("Swipe back to serve");
            var closeButton = FindObjectOfType<CloseKitchenButton>();
            yield return WaitForSwipe(-1, closeButton != null ? closeButton.GetComponent<RectTransform>() : null);
            yield return KitchenSwipe.SlideClose();
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

    // A step that waits for a swipe: +1 to the right, -1 to the left (KitchenSwipe reports the finger's swipes here)
    private int awaitingSwipe;
    private const float SwipeHintDown = 0.21f, SwipeHintReach = 0.25f, SwipeHintScale = 1.7f;   // below the caption, as shares of the screen; a bigger arrow than the pointer

    public void OnSwipe(int direction)
    {
        if (awaitingSwipe == 0 || direction != awaitingSwipe) return;
        awaitingSwipe = 0; awaitingHit = null; tapped = true;
    }

    /// The arrow slides the way the finger should go, again and again, until the player swipes that way (or taps
    /// the button that does the same).
    private IEnumerator WaitForSwipe(int direction, RectTransform alsoTap)
    {
        hand.gameObject.SetActive(true);
        if (pulse != null) StopCoroutine(pulse);
        pulse = StartCoroutine(SlideArrow(direction));
        awaitingSwipe = direction;
        yield return alsoTap != null ? WaitForTapOnRect(alsoTap) : WaitForTap(_ => false);
        awaitingSwipe = 0;
        HideHand();
    }

    private IEnumerator SlideArrow(int direction)
    {
        hand.localRotation = Quaternion.Euler(0f, 0f, direction > 0 ? 90f : -90f);   // the arrow points down as drawn
        hand.localScale = Vector3.one * SwipeHintScale;
        float y = -canvasRect.rect.height * SwipeHintDown, reach = canvasRect.rect.width * SwipeHintReach;
        var img = hand.GetComponent<Image>();
        var t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime / 1.1f;
            var u = t % 1f;
            var x = Mathf.Lerp(-reach, reach, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.75f))) * direction;
            hand.anchoredPosition = new Vector2(x, y);
            var fade = u < 0.1f ? u / 0.1f : u > 0.75f ? Mathf.Clamp01(1f - (u - 0.75f) / 0.2f) : 1f;
            if (handGroup != null) handGroup.alpha = fade; else if (img != null) { var c = img.color; c.a = fade; img.color = c; }
            yield return null;
        }
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

    #region Chapter 2 gift

    /// Chapter 2's reward (owner, 21 November 2026: "first show tutorial, when customer comes in, still has 3 flowers,
    /// then pause, and show the reward panel, and the fourth flower flies to customer, the customer shows 4 flowers"):
    /// the tutorial's customer has ordered with three flowers; the game holds (the tutorial's order clock is frozen
    /// anyway), the day paper comes up with one big flower; on Let's go the paper goes and the flower flies onto the
    /// customer's row, which has made room. From then on every chapter 2 customer has four (PatienceMeter.Flowers).
    /// Shown once per save.
    private IEnumerator FlowerGift()
    {
        var order = orderSystem != null ? orderSystem.FirstOpenOrder() : null;
        var petal = Resources.Load<Sprite>("Patience/" + PatienceMeter.Chapter2Kind + "_petal"); var centre = Resources.Load<Sprite>("Patience/" + PatienceMeter.Chapter2Kind + "_centre");
        var doily = Resources.Load<Sprite>("Map/day_paper"); var letsGo = Resources.Load<Sprite>("UI/cookie_letsgo");
        if (petal == null || centre == null || doily == null || letsGo == null || order == null) { HandOver(order); yield break; }
        var titleFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/beachday SDF"); var handFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Delius SDF");
        var ink = new Color(0.36f, 0.20f, 0.09f); var soft = new Color(0.47f, 0.31f, 0.20f);
        yield return new WaitForSecondsRealtime(1.1f);                                   // a look at the customer with three flowers
        var hadCaption = captionBox != null && captionBox.gameObject.activeSelf; if (captionBox != null) captionBox.gameObject.SetActive(false);

        var card = new GameObject("FlowerGift", typeof(RectTransform)); card.transform.SetParent(canvasRect, false);
        var cardRect = card.GetComponent<RectTransform>(); Stretch(cardRect);
        var dim = NewImage("Dim", cardRect, new Color(0f, 0f, 0f, 0f)); Stretch(dim.rectTransform);
        var panel = NewImage("Panel", cardRect, Color.white); panel.sprite = doily; panel.rectTransform.sizeDelta = new Vector2(770f, 770f);
        var group = panel.gameObject.AddComponent<CanvasGroup>();
        var title = NewText("Title", panel.rectTransform, titleFont, 80f, ink);
        title.rectTransform.anchoredPosition = new Vector2(0f, 246f); title.rectTransform.sizeDelta = new Vector2(680f, 120f);
        title.alignment = TextAlignmentOptions.Center; title.text = "A GIFT FOR YOU!";
        var line = NewText("Line", panel.rectTransform, handFont != null ? handFont : titleFont, 37f, soft);
        line.rectTransform.anchoredPosition = new Vector2(0f, -66f); line.rectTransform.sizeDelta = new Vector2(640f, 110f);
        line.alignment = TextAlignmentOptions.Center; line.text = "Extra patience flower\nfor beach customers"; line.alpha = 0f;   // the owner's words (21 November 2026); hidden until the flower opens (the alpha had slipped into this comment: the line showed, vanished and faded in)
        var go = NewImage("LetsGo", panel.rectTransform, Color.white); go.sprite = letsGo;
        go.rectTransform.anchoredPosition = new Vector2(0f, -206f); go.rectTransform.sizeDelta = new Vector2(380f, 380f * letsGo.rect.height / letsGo.rect.width);
        var done = false; go.gameObject.AddComponent<Button>().onClick.AddListener(() => done = true);
        go.transform.localScale = Vector3.zero;
        const float size = 200f; var home = new Vector2(0f, 96f);
        var flower = UiFlower(panel.rectTransform, petal, centre, size); flower.anchoredPosition = home; flower.localScale = Vector3.zero;

        // the paper comes in
        panel.transform.localScale = Vector3.one * 0.6f;
        for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.28f)
        {
            dim.color = new Color(0f, 0f, 0f, 0.55f * Mathf.Clamp01(t)); panel.transform.localScale = Vector3.one * Back(Mathf.Clamp01(t), 0.6f);
            yield return null;
        }
        dim.color = new Color(0f, 0f, 0f, 0.55f); panel.transform.localScale = Vector3.one;
        // the flower opens with a pop and throws petals
        var flowerPop = Resources.Load<AudioClip>("Audio/flower_pop");
        if (flowerPop != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(flowerPop);
        Haptics.Tap();
        StartCoroutine(PetalBurst(panel.rectTransform, petal, home));
        for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.45f)
        {
            var k = Mathf.Clamp01(t); flower.localScale = Vector3.one * Back(k, 0f); flower.localRotation = Quaternion.Euler(0f, 0f, -90f * (1f - k)); line.alpha = k;
            yield return null;
        }
        flower.localScale = Vector3.one; flower.localRotation = Quaternion.identity; line.alpha = 1f;
        for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.25f) { go.transform.localScale = Vector3.one * Back(Mathf.Clamp01(t), 0f); yield return null; }
        go.transform.localScale = Vector3.one;
        var w = 0f;
        while (!done) { w += Time.unscaledDeltaTime; flower.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(w * 4f)); yield return null; }

        // the paper goes; the flower stays and flies to the customer's row, which makes room for it
        SessionManager.Instance.MarkChapter2GiftSeen();
        flower.SetParent(cardRect, true); flower.localScale = Vector3.one;
        var from = flower.anchoredPosition; var to = from; var endScale = 0.4f;
        if (PatienceMeter.MakeRoom(order, out var world) && Camera.main != null)
        {
            var cam = Camera.main; var sp = cam.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(cardRect, sp, null, out to);
            var across = (cam.WorldToScreenPoint(world + Vector3.right * PatienceMeter.FlowerWorldSize(petal)) - sp).magnitude;   // one flower's width on the screen
            endScale = across * (cardRect.rect.width / Mathf.Max(1f, Screen.width)) / size;
        }
        for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.7f)
        {
            var k = Mathf.Clamp01(t); var fade = Mathf.Clamp01(k / 0.3f);
            group.alpha = 1f - fade; panel.transform.localScale = Vector3.one * (1f - 0.25f * fade); dim.color = new Color(0f, 0f, 0f, 0.55f * (1f - k));
            var e = Mathf.SmoothStep(0f, 1f, k);
            flower.anchoredPosition = Vector2.Lerp(from, to, e) + new Vector2(0f, 150f * Mathf.Sin(k * Mathf.PI));    // a soft arc
            flower.localScale = Vector3.one * Mathf.Lerp(1f, endScale, e); flower.localRotation = Quaternion.Euler(0f, 0f, 360f * e);
            yield return null;
        }
        HandOver(order);
        if (flowerPop != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(flowerPop);
        Haptics.Tap();
        Destroy(card);
        yield return new WaitForSecondsRealtime(0.8f);                                   // the four flowers, before the next caption
        if (captionBox != null) captionBox.gameObject.SetActive(hadCaption);
    }

    /// The fourth flower is the customer's now: their order grows by one flower, and every later customer has four
    private void HandOver(OrderSystem.ActiveOrder order)
    {
        PatienceMeter.HoldFourth = false;
        if (orderSystem != null && order != null) orderSystem.GrantExtraFlower(order);
        if (SessionManager.Instance != null) SessionManager.Instance.MarkChapter2GiftSeen();
    }

    private void OnDestroy() { PatienceMeter.HoldFourth = false; }   // a tutorial left half way must not leave later customers at three flowers

    private static float Back(float k, float from) { var c = 1.70158f; var e = 1f + (c + 1f) * Mathf.Pow(k - 1f, 3f) + c * Mathf.Pow(k - 1f, 2f); return from + (1f - from) * e; }   // a little past, then back

    /// The patience meter's flower as a picture for a card: five petals round a centre, laid out as PatienceMeter does
    private static RectTransform UiFlower(RectTransform parent, Sprite petal, Sprite centre, float size)
    {
        var root = new GameObject("Flower", typeof(RectTransform)).GetComponent<RectTransform>(); root.SetParent(parent, false); root.sizeDelta = new Vector2(size, size);
        const float radius = 0.115f;                                                     // PatienceMeter's place for a hibiscus petal, world units
        var px = size / (2f * (radius + petal.bounds.extents.y));                         // card pixels per world unit: the petals' tips touch the square
        for (var k = 0; k < 5; k++)
        {
            var angle = 90f - 72f * k; var rad = angle * Mathf.Deg2Rad;
            var p = NewImage("Petal" + k, root, Color.white); p.sprite = petal; p.raycastTarget = false;
            p.rectTransform.sizeDelta = petal.bounds.size * px; p.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius * px;
            p.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
        var c = NewImage("Centre", root, Color.white); c.sprite = centre; c.raycastTarget = false; c.rectTransform.sizeDelta = centre.bounds.size * px;
        return root;
    }

    private IEnumerator PetalBurst(RectTransform parent, Sprite petal, Vector2 at)
    {
        const int count = 10; var bits = new RectTransform[count]; var dirs = new Vector2[count];
        for (var i = 0; i < count; i++)
        {
            var img = NewImage("Bit", parent, Color.white); img.sprite = petal; img.raycastTarget = false; img.preserveAspect = true;
            bits[i] = img.rectTransform; bits[i].sizeDelta = new Vector2(34f, 34f); bits[i].anchoredPosition = at;
            var a = (360f / count * i + Random.Range(-14f, 14f)) * Mathf.Deg2Rad; dirs[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(150f, 230f);
        }
        for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.7f)
        {
            var k = Mathf.Clamp01(t); var ease = 1f - (1f - k) * (1f - k);
            for (var i = 0; i < count; i++)
            {
                if (bits[i] == null) yield break;
                bits[i].anchoredPosition = at + dirs[i] * ease + new Vector2(0f, -90f * k * k); bits[i].localRotation = Quaternion.Euler(0f, 0f, 300f * k + i * 36f);
                bits[i].GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f - k * k);
            }
            yield return null;
        }
        foreach (var b in bits) if (b != null) Destroy(b.gameObject);
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
        if (chapter2)
        {
            // the demo's coins go back; chapter 2's first day starts for real (the tutorial is no longer due)
            if (SessionManager.Instance != null) { SessionManager.Instance.RestoreScoreToSnapshot(); SessionManager.Instance.MarkTutorial2Seen(); }
            Reload(); return;
        }
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
        if (chapter2)
        {
            if (SessionManager.Instance != null) SessionManager.Instance.RestoreScoreToSnapshot();
            ReplayRequested2 = true; Reload(); return;
        }
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

    /// Arranging, shown on a real thing (owner, 19 November 2026: "a real item being dragged and repositioned instead of
    /// the item just in the animation ... in kitchen, reposition coffee bags, on table move around coffee machine"): the
    /// drag hand takes the thing, carries it to `to` (a world position), puts it down, then carries it back, so the
    /// player's layout is as it was. `cam` turns the thing's place into the screen (null for an overlay canvas).
    private IEnumerator DragDemo(string text, Transform thing, Vector3 to, Camera cam)
    {
        if (thing == null) yield break;
        HideHand();
        Say(text);
        var hand = DragHand();
        var from = thing.position;
        System.Action follow = () =>
        {
            if (hand == null || thing == null) return;
            var screen = cam != null ? (Vector2)cam.WorldToScreenPoint(thing.position) : RectTransformUtility.WorldToScreenPoint(null, thing.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
            hand.anchoredPosition = local + new Vector2(40f, -30f);   // the finger on the thing
        };
        follow(); yield return new WaitForSeconds(0.6f);                                      // the hand comes to it
        for (var leg = 0; leg < 2; leg++)
        {
            var a = leg == 0 ? from : to; var b = leg == 0 ? to : from;
            hand.localScale = Vector3.one * 0.92f;                                             // pressed
            for (var t = 0f; t < 1f; t += Time.deltaTime / 1.1f)
            {
                thing.position = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t)); follow(); yield return null;
            }
            thing.position = b; follow();
            hand.localScale = Vector3.one;                                                     // let go
            yield return new WaitForSeconds(leg == 0 ? 0.9f : 0.4f);
        }
        thing.position = from;
        Destroy(hand.gameObject);
    }

    /// The coffee bags from their shelf into the big opening under the counter (KitchenArrange's cabinets) and back.
    private IEnumerator DragBagsDemo()
    {
        var bags = FindInScene("KitchenScene", "coffee"); var bg = FindInScene("KitchenScene", "bg");
        if (bags == null || bg == null || !bags.activeInHierarchy) yield break;
        var brt = (RectTransform)bags.transform; var bgrt = (RectTransform)bg.transform;
        var c = new Vector3[4]; brt.GetWorldCorners(c);
        var foot = (c[0] + c[3]) / 2f;                                                          // the bags' bottom middle
        var r = bgrt.rect; var dest = bgrt.TransformPoint(new Vector3(r.xMin + 0.29f * r.width, r.yMin + 0.2906f * r.height, 0f));   // the big opening's floor, middle
        var canvas = bags.GetComponentInParent<Canvas>(); var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
        yield return DragDemo("Drag to arrange your kitchen", bags.transform, bags.transform.position + (dest - foot), cam);
    }

    /// The arrangement's own drag hand (the tutorial panel's picture), on the tutorial canvas.
    private RectTransform DragHand()
    {
        Image look = null;
        if (phaseManager != null && phaseManager.tutorialPanel != null)
        {
            var h = phaseManager.tutorialPanel.transform.Find("AnimationContainer/Hand");
            if (h != null) look = h.GetComponent<Image>();
        }
        var img = NewImage("DragHand", canvasRect, Color.white); var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        img.sprite = look != null ? look.sprite : arrowSprite; img.preserveAspect = true; img.raycastTarget = false;
        rt.sizeDelta = (look != null ? look.rectTransform.rect.size : new Vector2(100f, 150f)) * 1.6f;
        if (look != null) { rt.pivot = look.rectTransform.pivot; rt.localRotation = look.rectTransform.localRotation; }
        return rt;
    }

    private void Say(string text)
    {
        AnalyticsLog.TutorialStep(++saidCount, text);
        caption.text = text;
        captionBox.anchoredPosition = new Vector2(0f, CaptionY);                      // its own place; PointAtScreen moves it if the arrow would cover it
        captionBox.gameObject.SetActive(true);                                       // shown again after the chapter 2 demo hides it at the table
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
        GlowFx.Shine(img);
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
        KeepCaptionClearOf(handBase);
        if (pulse != null) StopCoroutine(pulse);
        pulse = StartCoroutine(Pulse());
        yield return null;
    }

    /// The banner has a fixed place and the arrow goes where its target is: on an iPad the chapter 2 waffle machine lies
    /// right under the banner, and the arrow was drawn over its words ("TAP THE WAFFLE MACHINE TO BA_E WAFFLE BOWLS",
    /// found 17 November 2026). Where the arrow, bobbing included, would touch the banner, the banner goes up to clear it.
    private void KeepCaptionClearOf(Vector2 arrowFoot)
    {
        if (captionBox == null || canvasRect == null || !captionBox.gameObject.activeSelf) return;
        var top = canvasRect.rect.yMax;
        var boxTop = top + captionBox.anchoredPosition.y; var boxBottom = boxTop - captionBox.sizeDelta.y;
        var arrowTop = arrowFoot.y + hand.sizeDelta.y + 52f;                        // its height and the bob
        if (arrowFoot.y >= boxTop || arrowTop <= boxBottom) return;                  // no overlap: the banner stays put
        var newBottom = arrowTop + 24f;
        var y = newBottom + captionBox.sizeDelta.y - top;
        captionBox.anchoredPosition = new Vector2(0f, Mathf.Min(y, -260f));        // never under the settings and pause buttons
    }

    private IEnumerator Pulse()
    {
        var t = 0f;
        while (true)
        {
            // a clear up-and-down tap (owner, 22 November 2026: "for the hand icon in tutorial, make it move up and down");
            // it was a 28 px drift, too small to read as moving
            t += Time.unscaledDeltaTime * 6.5f;
            hand.anchoredPosition = handBase + new Vector2(0f, 26f + 26f * Mathf.Sin(t));
            yield return null;
        }
    }

    private void HideHand()
    {
        if (pulse != null) { StopCoroutine(pulse); pulse = null; }
        hand.localRotation = Quaternion.identity; hand.localScale = Vector3.one;   // a swipe hint turns, grows and fades it
        var handImage = hand.GetComponent<Image>();
        if (handImage != null) { var c = handImage.color; c.a = 1f; handImage.color = c; }
        if (handGroup != null) handGroup.alpha = 1f;
        hand.gameObject.SetActive(false);
    }

    #endregion

    #region Runtime UI

    private void BuildUi()
    {
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/beachday SDF");
        // The pointer is a hand cookie with the gold glow and the glaze's shine passing over it (owner's pick 9 of
        // 21 November 2026, Store/ui/tutorial_arrow_2026-11-21: "use 9 pointing hand, add a glazing glow on it"); its
        // fingertip is the picture's bottom centre, as the yellow arrow's tip was. Without the picture: the arrow.
        var handSprite = Resources.Load<Sprite>("Tutorial/pointer_hand"); if (handSprite == null) handSprite = Resources.Load<Sprite>("Tutorial/arrow");
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
        captionBox.anchoredPosition = new Vector2(0f, CaptionY); // over the table, below the order bubble and customer
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
        hand.sizeDelta = new Vector2(170f, 170f);
        handImage.sprite = handSprite;
        handImage.preserveAspect = true;
        handImage.raycastTarget = false;
        handGroup = hand.gameObject.AddComponent<CanvasGroup>(); handGroup.blocksRaycasts = false; handGroup.interactable = false;   // fades the glow with the hand
        GlowFx.Shine(handImage);
        hand.gameObject.SetActive(false);

        // End card
        endCard = new GameObject("EndCard", typeof(RectTransform));
        endCard.transform.SetParent(canvasRect, false);
        var cardRect = endCard.GetComponent<RectTransform>();
        Stretch(cardRect);
        var dim = NewImage("Dim", cardRect, new Color(0f, 0f, 0f, 0.55f));
        Stretch(dim.rectTransform);
        // On the map's doily paper with cookie buttons (owner's pick 1 of 20 November 2026, Store/ui/ready_card_2026-11-20:
        // "Redesign the Ready to go panel ... use some kind of paper style"). Without the pictures: the plain card as before.
        var doily = Resources.Load<Sprite>("Map/day_paper"); var letsGo = Resources.Load<Sprite>("UI/cookie_letsgo"); var again = Resources.Load<Sprite>("UI/cookie_replay");
        var handFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Delius SDF");
        if (doily != null && letsGo != null && again != null)
        {
            var ink = new Color(0.36f, 0.20f, 0.09f); var soft = new Color(0.47f, 0.31f, 0.20f);
            var panel = NewImage("Panel", cardRect, Color.white); panel.sprite = doily; panel.rectTransform.sizeDelta = new Vector2(770f, 770f);
            var title = NewText("Title", panel.rectTransform, font, 86f, ink);
            title.rectTransform.anchoredPosition = new Vector2(0f, 246f); title.rectTransform.sizeDelta = new Vector2(680f, 120f);
            title.alignment = TextAlignmentOptions.Center; title.text = "READY TO GO?";
            var line = NewText("Line", panel.rectTransform, handFont != null ? handFont : font, 37f, soft);
            line.rectTransform.anchoredPosition = new Vector2(0f, 155f); line.rectTransform.sizeDelta = new Vector2(680f, 70f);
            line.alignment = TextAlignmentOptions.Center; line.text = "Your truck is open for business!";
            var letsGoImg = NewImage("LetsGo", panel.rectTransform, Color.white); letsGoImg.sprite = letsGo;
            letsGoImg.rectTransform.anchoredPosition = new Vector2(0f, -21f); letsGoImg.rectTransform.sizeDelta = new Vector2(444f, 444f * letsGo.rect.height / letsGo.rect.width);
            letsGoImg.gameObject.AddComponent<Button>().onClick.AddListener(OnLetsGo);
            // the replay cookie with its hand-written label beside it, the pair centred under Let's go
            var label = NewText("ReplayLabel", panel.rectTransform, handFont != null ? handFont : font, 40f, ink);
            label.text = "Replay tutorial"; label.alignment = TextAlignmentOptions.Left; label.enableWordWrapping = false;
            var lw = label.GetPreferredValues(label.text).x; const float icon = 104f, gap = 18f; var x0 = -(icon + gap + lw) / 2f;
            var rp = NewImage("Replay", panel.rectTransform, Color.white); rp.sprite = again; rp.preserveAspect = true;
            rp.rectTransform.anchoredPosition = new Vector2(x0 + icon / 2f, -209f); rp.rectTransform.sizeDelta = new Vector2(icon, icon);
            rp.gameObject.AddComponent<Button>().onClick.AddListener(OnReplay);
            label.rectTransform.pivot = new Vector2(0f, 0.5f); label.rectTransform.anchoredPosition = new Vector2(x0 + icon + gap, -212f); label.rectTransform.sizeDelta = new Vector2(lw + 10f, 70f);
            label.raycastTarget = true; label.gameObject.AddComponent<Button>().onClick.AddListener(OnReplay);   // the words are tappable too
        }
        else
        {
            var panel = NewImage("Panel", cardRect, new Color(1f, 0.96f, 0.88f, 1f));
            panel.rectTransform.sizeDelta = new Vector2(820f, 620f);
            var title = NewText("Title", panel.rectTransform, font, 72f, new Color(0.25f, 0.15f, 0.1f));
            title.rectTransform.anchoredPosition = new Vector2(0f, 180f);
            title.rectTransform.sizeDelta = new Vector2(760f, 120f);
            title.alignment = TextAlignmentOptions.Center;
            title.text = "Ready to go?";
            NewButton("LetsGo", panel.rectTransform, font, "Let's go!", new Vector2(0f, 10f), new Color(1f, 0.72f, 0.2f), OnLetsGo);
            NewButton("Replay", panel.rectTransform, font, "Replay tutorial", new Vector2(0f, -150f), new Color(0.85f, 0.85f, 0.8f), OnReplay);
        }
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
