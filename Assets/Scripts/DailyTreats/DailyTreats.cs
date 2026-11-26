using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Daily Treats: the first time the game is opened each day, a popup on the main menu gives +50 coins.
/// Four days in a row also gives a +100 surprise on the last day, then the row starts again (four since 19 November
/// 2026, owner: "Change the day streak to 4 days instead of 5 so we have one less sticker"). Missing a day
/// resets the row to day 1. Uses the phone's local date; a clock moved back gives nothing.
/// The coins wait (PlayerPrefs) and are paid when the player taps Continue or starts a New game.
/// Layout follows the approved mock (Store/artkit/checkin2.py) on a 1080x1920 canvas; the pieces are
/// drawn by Store/artkit/treats_pieces.py into Resources/DailyTreats.
/// </summary>
public class DailyTreats : MonoBehaviour
{
    public const string LastKey = "ftc.daily.last";       // yyyy-MM-dd of the last collect
    public const string StreakKey = "ftc.daily.streak";   // days collected in the current row, 1..RowDays
    public const string PendingKey = "ftc.daily.pending"; // coins collected with no game running

    public const int DailyCoins = 50;
    public const int RowDays = 4;       // the row's length; its last day gives the gift
    public const int Day5Bonus = 100;   // the last day's surprise (the name is from the five-day row)
    /// Each day of the row gives a different amount (owner, 26 November 2026: "make them different numbers of coins"):
    /// 30, 50, 70, then the gift's 150: the same 300 over the row as the old 50, 50, 50, 150.
    public static readonly int[] DayCoins = { 30, 50, 70, DailyCoins + Day5Bonus };
    public static int CoinsFor(int day) => DayCoins[Mathf.Clamp(day, 1, DayCoins.Length) - 1];

    /// The card's look (26 November 2026, DailyTreatsPreview): the owner picked 3, Spotlight and trail, with every treat
    /// face down until it is collected. 0 = the old card, 1 Stamp card, 2 Peel and take (kept for comparison).
    public static int PreviewStyle = 3;
    private DailyTreatsPreview preview;

    public Sprite boardSprite;   // Assets/Images/board.png
    public Sprite collectSprite; // Assets/Images/collect.png
    public Sprite starSprite;    // Assets/Images/star 1.png

    private GameObject root;
    private int dayToday;        // 1..RowDays
    private bool collecting;

    private DateTime dueDate;    // the local date the popup opened on

    private void Start()
    {
        dueDate = DateTime.Now.Date;
        if (!TutorialDone()) return;   // the day's treat waits; it shows on the first menu after the tutorial
        if (DueDay(out dayToday)) Show(dayToday);
    }

    /// No Daily Treats before the tutorial is done (owner, 19 November 2026, from the player data: new players watched the
    /// treats ad before playing at all, and some never started the tutorial).
    private static bool TutorialDone()
    {
        var sm = SessionManager.Instance;
        var s = sm != null ? sm.GetCurrentSession() : null;
        return s != null && s.isActive && (s.tutorialSeen || s.levelsCompleted >= 1);
    }

    /// <summary>True when today's treat has not been collected; day is the row day (1..RowDays) it would be. After the
    /// gift day the row starts again at day 1 (owner, 26 November 2026: "reset every 4 days"); a missed day also starts it again.</summary>
    public static bool DueDay(out int day)
    {
        day = 1;
        var today = DateTime.Now.Date;
        var last = PlayerPrefs.GetString(LastKey, "");
        if (DateTime.TryParseExact(last, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastDate))
        {
            if (lastDate >= today) return false; // already collected today, or the clock was moved back
            var streak = PlayerPrefs.GetInt(StreakKey, 0);
            if (lastDate == today.AddDays(-1) && streak >= 1 && streak < RowDays) day = streak + 1;
        }
        return true;
    }

    /// <summary>Coins collected while no game was running; a new game starts with them.</summary>
    public static int TakePending()
    {
        var p = PlayerPrefs.GetInt(PendingKey, 0);
        if (p > 0) { PlayerPrefs.DeleteKey(PendingKey); PlayerPrefs.Save(); }
        return p;
    }

    /// <summary>Editor and test helper: show the popup for a given row day without touching the saved state.</summary>
    public void DebugShow(int day)
    {
        if (root != null) Destroy(root);
        collecting = false;
        dayToday = Mathf.Clamp(day, 1, RowDays);
        Show(dayToday);
    }

    // ---- UI ----

    private Image collectButton, rewardImage, giftImage, sparkles;

    private void Show(int day)
    {
        giftImage = null;
        root = new GameObject("DailyTreatsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        var canvasRect = root.GetComponent<RectTransform>();

        // Dim the menu; the dim also swallows taps on the menu underneath
        var dim = NewImage("Dim", canvasRect, null, new Color(30f / 255f, 20f / 255f, 10f / 255f, 140f / 255f));
        dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.offsetMin = Vector2.zero; dim.rectTransform.offsetMax = Vector2.zero;

        var paper = Load("board_paper");
        if (paper != null && Load("note1") != null) { ShowOnPaper(canvasRect, paper, day); return; }
        ShowOnWood(canvasRect, day);
    }

    // Where the reward, the button and the close cross sit: DailyTreatsAd reads them (they differ on the paper card).
    public static float RewardY = -190f, RewardScale = 1f, ButtonY = -431f, ButtonScale = 1f;
    public static Vector2 ClosePos = new Vector2(440f, 540f);
    public static Color AmountTint = Color.white;

    /// The card since 19 November 2026 (owner: "for panels, i want to use papers"; "use H for daily treat card"; then design
    /// 2 with their changes: "Remove that red sticker, only keep the texts, style the text like the stamp card ... Change
    /// the shape of this card to be square, reposition things inside it, the dot line is the edge, add some padding
    /// between the edge and content. Also add some space around the close button. Don't show the coin amount on the next
    /// few days, only show icon"; then "Leave margin between left and right of screen edge and the daily treat panel.
    /// Change the day streak to 4 days instead of 5 so we have one less sticker"). Pieces:
    /// Store/ui/daily_treats_2026-11-19/build.py. A tall phone's canvas is only about 977 wide, so the square card is 890
    /// (846 painted): about 65 free on each side. Its dotted line runs at +-385 (top: 291); everything keeps 30 inside it.
    private void ShowOnPaper(RectTransform canvasRect, Sprite paper, int day)
    {
        // the buttons a little smaller and higher (owner, 19 November: "make the buttons slightly smaller, leave some space
        // between button and the bottom dot line")
        RewardY = -146f; RewardScale = 0.66f; ButtonY = -272f; ButtonScale = 0.68f; ClosePos = new Vector2(322f, 229f);
        AmountTint = new Color(0.90f, 0.56f, 0.24f);   // the cream "+50" letters, orange on the pale card
        var board = NewImage("Board", canvasRect, paper, Color.white);
        Place(board.rectTransform, 0f, 0f, 890f, 890f);

        // the title: plain letters with a row of dots under them
        var titleGo = new GameObject("Title", typeof(RectTransform)); titleGo.transform.SetParent(canvasRect, false);
        var title = titleGo.AddComponent<TMPro.TextMeshProUGUI>();
        var font = Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/beachday SDF"); if (font != null) title.font = font;
        var plain = Resources.Load<Material>("Fonts & Materials/beachday SDF - Plain"); if (plain != null) title.fontSharedMaterial = plain;
        title.text = "DAILY TREATS"; title.fontSize = 68f; title.color = new Color(0.36f, 0.20f, 0.10f); title.alignment = TMPro.TextAlignmentOptions.Center;
        title.enableWordWrapping = false; title.raycastTarget = false;
        Place(title.rectTransform, -25f, 226f, 540f, 92f);
        var dots = NewImage("TitleDots", canvasRect, Load("title_dots"), Color.white);
        Place(dots.rectTransform, -25f, 182f, 410f, 10f);

        // the row's notes, each a little tilted; today's is bigger and shows what it gives, earlier days are pale and
        // ticked, later days show only their picture
        float[] tilt = { 3f, -5f, 2f, -3f, 4f };
        const float step = 178f;
        for (var i = 0; i < RowDays; i++)
        {
            var d = i + 1; var x = -step * (RowDays - 1) / 2f + i * step; var today = d == day;
            var note = NewImage("Note" + d, canvasRect, Load(today ? "note" + d + "_today" : "note" + d), Color.white);
            var k = today ? 1.16f : 1f;
            Place(note.rectTransform, x, today ? 54f : 46f, 140f * k, 190f * k);
            note.rectTransform.localRotation = Quaternion.Euler(0f, 0f, tilt[i % tilt.Length]);
            if (d < day)
            {
                note.color = new Color(1f, 1f, 1f, 0.55f);
                var tick = NewImage("Tick" + d, canvasRect, Load("tick"), Color.white);
                Place(tick.rectTransform, x, 34f, 82f, 82f);
            }
            else if (today) note.transform.SetAsLastSibling();
        }

        // the dotted trail to the star, filled as far as the row has come
        var trail = NewImage("Trail", canvasRect, Load("trail"), Color.white); trail.preserveAspect = false;
        Place(trail.rectTransform, -30f, -88f, 600f, 19f);
        var fill = NewImage("TrailFill", canvasRect, Load("trail_fill"), Color.white); fill.preserveAspect = false;
        Place(fill.rectTransform, -30f, -88f, 600f, 19f);
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = (day - 1) / (float)RowDays;
        var star = NewImage("Star", canvasRect, starSprite, Color.white);
        Place(star.rectTransform, 314f, -86f, 80f, 80f);

        // today's reward: hidden until Collect is tapped (the gift box shows at once on the last day)
        sparkles = NewImage("Sparkles", canvasRect, Load("sparkles"), Color.white);
        Place(sparkles.rectTransform, 0f, RewardY, 700f * RewardScale, 240f * RewardScale);
        sparkles.gameObject.SetActive(false);
        if (day == RowDays && PreviewStyle != 3)   // Spotlight keeps every treat face down until it is flipped
        {
            giftImage = NewImage("Gift", canvasRect, Load("gift_big"), Color.white);
            Place(giftImage.rectTransform, 0f, RewardY + 4f, 124f, 146f);
        }
        // the coin cookie and the amount in orange letters, put together here (the old baked picture was made for the
        // wood); laid out for a scale of 0.8 and shrunk as a whole
        rewardImage = NewImage("Reward", canvasRect, null, new Color(1f, 1f, 1f, 0f));
        Place(rewardImage.rectTransform, 0f, RewardY, 416f, 144f);
        rewardImage.rectTransform.localScale = Vector3.one * (RewardScale / 0.8f);
        var rc = NewImage("Coin", rewardImage.rectTransform, Load("glyph_coin"), Color.white);
        Place(rc.rectTransform, -160f, 0f, 120f, 120f);
        var amountText = "+" + CoinsFor(day); var gx = -70f;
        foreach (var ch in amountText)
        {
            var gs = Load(ch == '+' ? "glyph_plus" : "glyph_" + ch); if (gs == null) continue;
            var gw = 144f * gs.rect.width / gs.rect.height;
            var g = NewImage("G" + ch, rewardImage.rectTransform, gs, AmountTint);
            Place(g.rectTransform, gx + gw / 2f, 0f, gw, 144f); gx += gw - 5f;
        }
        rewardImage.gameObject.SetActive(false);

        collectButton = NewImage("Collect", canvasRect, collectSprite, Color.white);
        Place(collectButton.rectTransform, 12f, ButtonY, 548f * ButtonScale, 305f * ButtonScale);
        var button = collectButton.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(Collect);
        preview = PreviewStyle != 0 ? DailyTreatsPreview.Dress(canvasRect, day, PreviewStyle) : null;
    }

    /// The wood board of before 19 November 2026, kept for a build without the paper pieces.
    private void ShowOnWood(RectTransform canvasRect, int day)
    {
        RewardY = -190f; RewardScale = 1f; ButtonY = -431f; ButtonScale = 1f; ClosePos = new Vector2(440f, 540f); AmountTint = Color.white;
        var shadow = NewImage("BoardShadow", canvasRect, boardSprite, new Color(0.08f, 0.04f, 0f, 0.45f));
        Place(shadow.rectTransform, 10f, -30f, 960f, 1180f);
        var board = NewImage("Board", canvasRect, boardSprite, Color.white);
        Place(board.rectTransform, 0f, -10f, 960f, 1180f);

        var ribbon = NewImage("Ribbon", canvasRect, Load("ribbon"), Color.white);
        Place(ribbon.rectTransform, 0f, 560f, 800f, 220f);

        // Five day cards; today glows, earlier days are ticked
        for (var i = 0; i < RowDays; i++)
        {
            var d = i + 1;
            var x = -87f * (RowDays - 1) + i * 174f;
            if (d == day)
            {
                var glow = NewImage("Glow" + d, canvasRect, Load("glow"), Color.white);
                Place(glow.rectTransform, x, 245f, 226f, 298f);
            }
            var card = NewImage("Card" + d, canvasRect, Load("card" + (d == RowDays ? 5 : d)), Color.white);
            Place(card.rectTransform, x, 245f, 158f, 230f);
            if (d < day)
            {
                var veil = NewImage("Veil" + d, canvasRect, null, new Color(1f, 1f, 1f, 110f / 255f));
                Place(veil.rectTransform, x, 245f, 150f, 222f);
                var tick = NewImage("Tick" + d, canvasRect, Load("tick"), Color.white);
                Place(tick.rectTransform, x, 238f, 96f, 96f);
            }
        }

        // Progress bar with the star at its end
        var barBack = NewImage("BarBack", canvasRect, Load("bar_back"), Color.white);
        Place(barBack.rectTransform, 0f, 10f, 720f, 40f);
        var barFill = NewImage("BarFill", canvasRect, Load("bar_fill"), Color.white);
        Place(barFill.rectTransform, 0f, 10f, 708f, 28f);
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillAmount = (day - 1) / (float)RowDays;
        var star = NewImage("Star", canvasRect, starSprite, Color.white);
        Place(star.rectTransform, 368f, 12f, 96f, 96f);

        // Today's reward: the coin and +50, or the gift box on day 5. The coin, the +50 and the sparkles are the
        // effect after Collect is tapped, so they start hidden (owner, 12 October); the gift box shows at once
        sparkles = NewImage("Sparkles", canvasRect, Load("sparkles"), Color.white);
        Place(sparkles.rectTransform, 0f, -190f, 700f, 240f);
        sparkles.gameObject.SetActive(false);
        if (day == RowDays)
        {
            giftImage = NewImage("Gift", canvasRect, Load("gift_big"), Color.white);
            Place(giftImage.rectTransform, 0f, -170f, 220f, 260f);
            rewardImage = NewImage("Reward", canvasRect, Load("reward150"), Color.white);
            Place(rewardImage.rectTransform, 0f, -190f, 520f, 180f);
            rewardImage.gameObject.SetActive(false);
        }
        else
        {
            rewardImage = NewImage("Reward", canvasRect, Load("reward50"), Color.white);
            Place(rewardImage.rectTransform, 0f, -190f, 520f, 180f);
            rewardImage.gameObject.SetActive(false);
        }

        // Collect: the cookie button (collect.png keeps the same canvas as continue.png)
        collectButton = NewImage("Collect", canvasRect, collectSprite, Color.white);
        Place(collectButton.rectTransform, 16f, -431f, 548f, 305f);
        var button = collectButton.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(Collect);
    }

    private void Collect()
    {
        if (collecting) return;
        collecting = true;
        var day = dayToday;
        var amount = CoinsFor(day);

        // Always held until the player taps Continue or New, which both pay it (paying into the running game here
        // lost it when the player then started a new game)
        PlayerPrefs.SetInt(PendingKey, PlayerPrefs.GetInt(PendingKey, 0) + amount);
        // The day the popup was opened for, so a tap just after midnight doesn't use up the next day
        PlayerPrefs.SetString(LastKey, dueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        PlayerPrefs.SetInt(StreakKey, day);
        PlayerPrefs.Save();

        StartCoroutine(CloseAfterReveal(day == RowDays));
    }

    private IEnumerator CloseAfterReveal(bool gift)
    {
        collectButton.gameObject.SetActive(false);
        if (preview != null) yield return preview.Collect(rewardImage, sparkles, gift ? giftImage : null);   // PreviewStyle 1-3 only
        else
        {
            // The reward appears on the tap: the coin and +50 with sparkles, or on day 5 the gift opening into the +150
            if (gift) giftImage.gameObject.SetActive(false);
            sparkles.gameObject.SetActive(true);
            rewardImage.gameObject.SetActive(true);
            yield return new WaitForSeconds(gift ? 2f : 1.4f);
        }
        // One more treat (owner, 16 October): the board stays and the button comes back as a repeatable ad button
        var ad = root.AddComponent<DailyTreatsAd>();
        ad.Begin(root.GetComponent<RectTransform>(), rewardImage, sparkles, collectSprite, PayExtra, CloseBoard);
    }

    /// Coins from a watched ad: held like the daily coins, paid on Continue or New
    private void PayExtra(int coins)
    {
        PlayerPrefs.SetInt(PendingKey, PlayerPrefs.GetInt(PendingKey, 0) + coins);
        PlayerPrefs.Save();
    }

    private void CloseBoard()
    {
        if (root != null) Destroy(root);
        root = null;
    }

    // ---- helpers ----

    private static Sprite Load(string name) => Resources.Load<Sprite>("DailyTreats/" + name);

    private static Image NewImage(string name, RectTransform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = name == "Dim" || name == "Collect";
        return img;
    }

    private static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }
}
