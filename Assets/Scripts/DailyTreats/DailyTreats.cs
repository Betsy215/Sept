using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Daily Treats: the first time the game is opened each day, a popup on the main menu gives +50 coins.
/// Five days in a row also gives a +100 surprise on day 5, then the row starts again. Missing a day
/// resets the row to day 1. Uses the phone's local date; a clock moved back gives nothing.
/// The coins wait (PlayerPrefs) and are paid when the player taps Continue or starts a New game.
/// Layout follows the approved mock (Store/artkit/checkin2.py) on a 1080x1920 canvas; the pieces are
/// drawn by Store/artkit/treats_pieces.py into Resources/DailyTreats.
/// </summary>
public class DailyTreats : MonoBehaviour
{
    public const string LastKey = "ftc.daily.last";       // yyyy-MM-dd of the last collect
    public const string StreakKey = "ftc.daily.streak";   // days collected in the current row, 1..5
    public const string PendingKey = "ftc.daily.pending"; // coins collected with no game running

    public const int DailyCoins = 50;
    public const int Day5Bonus = 100;

    public Sprite boardSprite;   // Assets/Images/board.png
    public Sprite collectSprite; // Assets/Images/collect.png
    public Sprite starSprite;    // Assets/Images/star 1.png

    private GameObject root;
    private int dayToday;        // 1..5
    private bool collecting;

    private DateTime dueDate;    // the local date the popup opened on

    private void Start()
    {
        dueDate = DateTime.Now.Date;
        if (DueDay(out dayToday)) Show(dayToday);
    }

    /// <summary>True when today's treat has not been collected; day is the row day (1..5) it would be.</summary>
    public static bool DueDay(out int day)
    {
        day = 1;
        var today = DateTime.Now.Date;
        var last = PlayerPrefs.GetString(LastKey, "");
        if (DateTime.TryParseExact(last, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastDate))
        {
            if (lastDate >= today) return false; // already collected today, or the clock was moved back
            var streak = PlayerPrefs.GetInt(StreakKey, 0);
            if (lastDate == today.AddDays(-1) && streak >= 1 && streak < 5) day = streak + 1;
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
        dayToday = Mathf.Clamp(day, 1, 5);
        Show(dayToday);
    }

    // ---- UI ----

    private Image collectButton, rewardImage, giftImage, sparkles;

    private void Show(int day)
    {
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

        // Wood board with a soft shadow under it
        var shadow = NewImage("BoardShadow", canvasRect, boardSprite, new Color(0.08f, 0.04f, 0f, 0.45f));
        Place(shadow.rectTransform, 10f, -30f, 960f, 1180f);
        var board = NewImage("Board", canvasRect, boardSprite, Color.white);
        Place(board.rectTransform, 0f, -10f, 960f, 1180f);

        var ribbon = NewImage("Ribbon", canvasRect, Load("ribbon"), Color.white);
        Place(ribbon.rectTransform, 0f, 560f, 800f, 220f);

        // Five day cards; today glows, earlier days are ticked
        for (var i = 0; i < 5; i++)
        {
            var d = i + 1;
            var x = -348f + i * 174f;
            if (d == day)
            {
                var glow = NewImage("Glow" + d, canvasRect, Load("glow"), Color.white);
                Place(glow.rectTransform, x, 245f, 226f, 298f);
            }
            var card = NewImage("Card" + d, canvasRect, Load("card" + d), Color.white);
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
        barFill.fillAmount = (day - 1) / 5f;
        var star = NewImage("Star", canvasRect, starSprite, Color.white);
        Place(star.rectTransform, 368f, 12f, 96f, 96f);

        // Today's reward: the coin and +50, or the gift box on day 5. The coin, the +50 and the sparkles are the
        // effect after Collect is tapped, so they start hidden (owner, 12 October); the gift box shows at once
        sparkles = NewImage("Sparkles", canvasRect, Load("sparkles"), Color.white);
        Place(sparkles.rectTransform, 0f, -190f, 700f, 240f);
        sparkles.gameObject.SetActive(false);
        if (day == 5)
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
        var amount = DailyCoins + (day == 5 ? Day5Bonus : 0);

        // Always held until the player taps Continue or New, which both pay it (paying into the running game here
        // lost it when the player then started a new game)
        PlayerPrefs.SetInt(PendingKey, PlayerPrefs.GetInt(PendingKey, 0) + amount);
        // The day the popup was opened for, so a tap just after midnight doesn't use up the next day
        PlayerPrefs.SetString(LastKey, dueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        PlayerPrefs.SetInt(StreakKey, day);
        PlayerPrefs.Save();

        StartCoroutine(CloseAfterReveal(day == 5));
    }

    private IEnumerator CloseAfterReveal(bool gift)
    {
        collectButton.gameObject.SetActive(false);
        // The reward appears on the tap: the coin and +50 with sparkles, or on day 5 the gift opening into the +150
        if (gift) giftImage.gameObject.SetActive(false);
        sparkles.gameObject.SetActive(true);
        rewardImage.gameObject.SetActive(true);
        yield return new WaitForSeconds(gift ? 2f : 1.4f);
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
