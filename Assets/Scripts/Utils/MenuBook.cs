using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The menu book (owner, 14 October 2026): during the arrangement phase the player switches bought foods on and off.
/// A food that is off leaves the table and the kitchen and is never ordered. Each station (oven, machine, counter)
/// has a cap, and at least two foods stay on. Rules and the save list live in SessionManager; this is only the
/// placeholder look (a recipe-card panel built in code, no scene edits), the real design comes later.
/// Added by LevelManager on every normal day; never on the Day 0 tutorial.
/// </summary>
public class MenuBook : MonoBehaviour
{
    private const int SortOrder = 40;                  // under the tutorial canvas (50), over the arrangement UI (0)
    private static readonly Color Brown = new Color(0.36f, 0.22f, 0.09f);
    private static readonly Color OnGreen = new Color(0.45f, 0.68f, 0.36f);
    private static readonly Color OffGrey = new Color(0.72f, 0.68f, 0.62f);
    private static readonly Color Cream = new Color(1f, 0.98f, 0.93f);

    private LevelManager levelManager;
    private GamePhaseManager phaseManager;
    private TMP_FontAsset font;
    private Sprite cardSprite;
    private Canvas canvas;
    private RectTransform canvasRect;
    private GameObject button;
    private GameObject panel;
    private RectTransform rows;
    private TextMeshProUGUI counters;

    public bool IsOpen => panel != null && panel.activeSelf;

    public void Begin(LevelManager lm, GamePhaseManager gpm)
    {
        levelManager = lm;
        phaseManager = gpm;
        font = Resources.Load<TMP_FontAsset>("Fonts & Materials/beachday SDF");
        cardSprite = Resources.Load<Sprite>("Tutorial/card");
        BuildCanvas();
        BuildButton();
    }

    private void Update()
    {
        // The book is only for the arrangement phase: the button follows the arrangement UI
        var arranging = phaseManager != null && phaseManager.GetCurrentPhase() == GamePhase.ARRANGEMENT
                        && phaseManager.arrangmentUI != null && phaseManager.arrangmentUI.activeInHierarchy;
        if (button != null && button.activeSelf != arranging) button.SetActive(arranging);
        if (!arranging && IsOpen) Close();
    }

    #region Open, close, switch

    public void Open()
    {
        if (SessionManager.Instance == null || !SessionManager.Instance.HasActiveSession()) return;
        if (panel == null) BuildPanel();
        Refresh();
        panel.SetActive(true);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    /// Switch a food; false when the rules refused (the station is full, or too few would stay on).
    public bool Toggle(string foodType)
    {
        var sm = SessionManager.Instance; if (sm == null) return false;
        var ok = sm.SetFoodEnabled(foodType, !sm.IsFoodItemEnabled(foodType));
        if (ok && levelManager != null) levelManager.ReapplyServeableItems();
        Refresh();
        return ok;
    }

    #endregion

    #region Building the look

    private void BuildCanvas()
    {
        var go = new GameObject("MenuBookCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortOrder;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect = go.GetComponent<RectTransform>();
    }

    private void BuildButton()
    {
        // Left of the Ready button (the screen centre), between the truck and Ready
        var img = NewImage("MenuBookButton", canvasRect, Color.white);
        var sprite = Resources.Load<Sprite>("UI/menu_book");
        if (sprite != null) img.sprite = sprite; else img.color = Cream;
        img.preserveAspect = true;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-250f, 0f);
        rt.sizeDelta = new Vector2(150f, 150f);
        var b = img.gameObject.AddComponent<Button>();
        b.onClick.AddListener(Open);
        button = img.gameObject;
        button.SetActive(false);
    }

    private void BuildPanel()
    {
        panel = new GameObject("MenuBookPanel", typeof(RectTransform));
        panel.transform.SetParent(canvasRect, false);
        Stretch((RectTransform)panel.transform);

        // Dim everything; a tap outside the card closes the book
        var dim = NewImage("Dim", (RectTransform)panel.transform, new Color(0f, 0f, 0f, 0.45f));
        Stretch(dim.rectTransform);
        dim.gameObject.AddComponent<Button>().onClick.AddListener(Close);

        var card = NewImage("Card", (RectTransform)panel.transform, Color.white);
        if (cardSprite != null) { card.sprite = cardSprite; card.type = Image.Type.Sliced; }
        else card.color = Cream;
        var crt = card.rectTransform;
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
        crt.sizeDelta = new Vector2(940f, 1320f);
        crt.anchoredPosition = new Vector2(0f, 60f);
        card.gameObject.AddComponent<Button>();                       // swallows taps so they do not reach Dim

        var title = NewText("Title", crt, font, 72f, Brown);
        title.text = "MENU";
        title.alignment = TextAlignmentOptions.Center;
        Place(title.rectTransform, 0f, 570f, 800f, 90f);

        var hint = NewText("Hint", crt, font, 36f, new Color(0.5f, 0.38f, 0.26f));
        hint.text = "Tap a food to switch it on or off";
        hint.alignment = TextAlignmentOptions.Center;
        Place(hint.rectTransform, 0f, 500f, 800f, 50f);

        var rowsGo = new GameObject("Rows", typeof(RectTransform));
        rowsGo.transform.SetParent(crt, false);
        rows = (RectTransform)rowsGo.transform;
        Place(rows, 0f, 70f, 820f, 780f);

        counters = NewText("Counters", crt, font, 40f, Brown);
        counters.alignment = TextAlignmentOptions.Center;
        Place(counters.rectTransform, 0f, -460f, 860f, 60f);

        var done = NewImage("DoneButton", crt, new Color(0.98f, 0.78f, 0.36f));
        var drt = done.rectTransform;
        Place(drt, 0f, -570f, 300f, 110f);
        var dt = NewText("Label", drt, font, 52f, Brown);
        dt.text = "DONE";
        dt.alignment = TextAlignmentOptions.Center;
        Stretch(dt.rectTransform);
        done.gameObject.AddComponent<Button>().onClick.AddListener(Close);
    }

    /// Rebuilds the grid from the save: one tile per bought food, three across, in the table's order
    /// (owner, 14 October: a grid, with the oven stickers as the icons of the baked foods).
    private void Refresh()
    {
        if (rows == null) return;
        for (var i = rows.childCount - 1; i >= 0; i--) Destroy(rows.GetChild(i).gameObject);
        var sm = SessionManager.Instance; if (sm == null) return;

        var foods = new List<string>();
        if (levelManager != null && levelManager.serveableItems != null)
            foreach (var item in levelManager.serveableItems)
                if (item != null && sm.IsFoodItemPurchased(item.GetFoodType()) && !foods.Contains(item.GetFoodType()))
                    foods.Add(item.GetFoodType());

        const int cols = 3; const float tileW = 260f, tileH = 245f, gap = 15f;
        var x0 = -(cols - 1) * (tileW + gap) / 2f;
        var y0 = rows.sizeDelta.y / 2f - tileH / 2f;
        for (var i = 0; i < foods.Count; i++)
        {
            var food = foods[i];
            var on = sm.IsFoodItemEnabled(food);
            string why;
            var allowed = on ? sm.CanDisableFood(food, out why) : sm.CanEnableFood(food, out why);

            var tile = NewImage("Tile_" + food, rows, on ? new Color(1f, 1f, 1f, 0.6f) : new Color(1f, 1f, 1f, 0.25f));
            Place(tile.rectTransform, x0 + (i % cols) * (tileW + gap), y0 - (i / cols) * (tileH + gap), tileW, tileH);

            var icon = NewImage("Icon", tile.rectTransform, Color.white);
            var s = IconFor(food);
            if (s != null) icon.sprite = s; else icon.color = new Color(0, 0, 0, 0);
            icon.preserveAspect = true;
            Place(icon.rectTransform, 0f, 40f, 140f, 140f);
            if (!on) icon.color = new Color(1f, 1f, 1f, 0.5f);

            var name = NewText("Name", tile.rectTransform, font, 36f, allowed || on ? Brown : OffGrey);
            name.text = food + (allowed || on ? "" : "\n<size=22>" + why + "</size>");
            name.alignment = TextAlignmentOptions.Center;
            Place(name.rectTransform, 0f, -58f, tileW - 20f, 60f);

            var pill = NewImage("Pill", tile.rectTransform, on ? OnGreen : OffGrey);
            Place(pill.rectTransform, 0f, -100f, 120f, 40f);
            var pt = NewText("Label", pill.rectTransform, font, 30f, Color.white);
            pt.text = on ? "ON" : "OFF";
            pt.alignment = TextAlignmentOptions.Center;
            Stretch(pt.rectTransform);

            var captured = food;
            tile.gameObject.AddComponent<Button>().onClick.AddListener(() => Toggle(captured));
        }

        counters.text = $"Oven {sm.EnabledCountAtStation("Oven")}/{SessionManager.CapOf("Oven")}    " +
                        $"Machine {sm.EnabledCountAtStation("Machine")}/{SessionManager.CapOf("Machine")}    " +
                        $"Counter {sm.EnabledCountAtStation("Counter")}/{SessionManager.CapOf("Counter")}";
    }

    /// The oven sticker of a baked food (Resources/UI/stickers/<food>), else the food's own table picture.
    private Sprite IconFor(string food)
    {
        var sticker = Resources.Load<Sprite>("UI/stickers/" + food);
        if (sticker != null) return sticker;
        if (levelManager == null || levelManager.serveableItems == null) return null;
        foreach (var item in levelManager.serveableItems)
        {
            if (item == null || item.GetFoodType() != food) continue;
            var sr = item.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null && sr.sprite != null) return sr.sprite;
        }
        return null;
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

    private static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
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
