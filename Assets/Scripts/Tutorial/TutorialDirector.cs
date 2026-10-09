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
/// The player cannot tap during it. A pointing hand shows each thing the game is about to do and a
/// one-line caption says what is happening. The director drives the real systems (customer, order,
/// serving, coffee machine, kitchen, oven) through the same code paths a real tap uses, so the demo
/// always matches real behaviour. At the end two buttons offer "Let's go!" (start Day 1) and
/// "Replay tutorial".
///
/// Everything it needs on screen is built at runtime on its own overlay canvas; no scene wiring.
/// Added by LevelManager.Start when <see cref="IsDue"/> says so.
/// </summary>
public class TutorialDirector : MonoBehaviour
{
    /// Set by the Replay button before the scene reloads.
    public static bool ReplayRequested;

    public static bool IsDue(SessionManager sm)
    {
        if (ReplayRequested) return true;
        var s = sm != null ? sm.GetCurrentSession() : null;
        return s != null && s.isActive && !s.tutorialSeen && s.levelsCompleted == 0;
    }

    private const float HoldOnPointer = 1.4f;   // seconds the hand rests on a target before the "tap"

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
    private GameObject endCard;
    private Coroutine pulse;

    public void Begin(LevelManager lm, GamePhaseManager gpm, OrderSystem os, CustomerManager cm)
    {
        levelManager = lm;
        phaseManager = gpm;
        orderSystem = os;
        customerManager = cm;
        ReplayRequested = false;
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
        Tap(bread);
        yield return new WaitForSeconds(1.0f);

        Say("Now tap the coffee");
        yield return PointAtWorld(coffee.transform.position);
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
            TapMachine(machine);
            yield return new WaitForSeconds(2.5f);
        }

        var kitchenButton = GameObject.Find("KitchenButton");
        if (kitchenButton != null && KitchenSceneManager.Instance != null)
        {
            Say("Out of bread? Tap Kitchen");
            yield return PointAtRect(kitchenButton.GetComponent<RectTransform>());
            HideHand();
            KitchenSceneManager.Instance.OpenKitchen();
            yield return new WaitUntil(() => SceneManager.GetSceneByName("KitchenScene").isLoaded);
            yield return new WaitForSeconds(0.8f);

            var oven = GameObject.Find("oven2");
            if (oven != null)
            {
                Say("Tap the oven to bake more bread");
                yield return PointAtRect(oven.GetComponent<RectTransform>());
                var ovenScript = oven.GetComponent<OvenKitchenBase>();
                if (ovenScript != null) ovenScript.OnPointerClick(EventFor(oven));
                yield return new WaitForSeconds(2.5f);
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

        Reload();
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

    private void Say(string text)
    {
        caption.text = text;
        caption.gameObject.SetActive(true);
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
        var targetCanvas = target.GetComponentInParent<Canvas>();
        var cam = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? targetCanvas.worldCamera : null;
        var screen = RectTransformUtility.WorldToScreenPoint(cam, target.position);
        yield return PointAtScreen(screen);
    }

    private IEnumerator PointAtScreen(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
        hand.gameObject.SetActive(true);
        // The fingertip is the sprite's top-centre (its pivot), so it lands on the target; a small
        // offset keeps the finger from hiding the item it points at
        hand.anchoredPosition = local + new Vector2(18f, -10f);
        if (pulse != null) StopCoroutine(pulse);
        pulse = StartCoroutine(Pulse());
        yield return new WaitForSeconds(HoldOnPointer);
    }

    private IEnumerator Pulse()
    {
        var t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime * 4f;
            hand.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(t));
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
        var handSprite = Resources.Load<Sprite>("Tutorial/hand");

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

        // Caption at the top
        var box = NewImage("CaptionBox", canvasRect, new Color(0.12f, 0.08f, 0.06f, 0.82f));
        captionBox = box.rectTransform;
        captionBox.anchorMin = new Vector2(0.5f, 1f);
        captionBox.anchorMax = new Vector2(0.5f, 1f);
        captionBox.pivot = new Vector2(0.5f, 1f);
        captionBox.anchoredPosition = new Vector2(0f, -1130f); // over the table, below the order bubble and customer
        captionBox.sizeDelta = new Vector2(900f, 150f);
        caption = NewText("Caption", captionBox, font, 52f, Color.white);
        Stretch(caption.rectTransform);
        caption.alignment = TextAlignmentOptions.Center;
        caption.enableWordWrapping = true;

        // Pointing hand
        var handImage = NewImage("Hand", canvasRect, Color.white);
        hand = handImage.rectTransform;
        hand.anchorMin = hand.anchorMax = new Vector2(0.5f, 0.5f);
        hand.pivot = new Vector2(0.5f, 1f);
        hand.sizeDelta = new Vector2(220f, 220f);
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
