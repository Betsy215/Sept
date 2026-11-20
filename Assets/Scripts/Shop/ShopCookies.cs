using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// The shop in cookie style (owner, 17 November 2026: "i wanna use the cookie style in shop items ... when purchased,
/// i want to have animation of this cookie being eaten piece by piece with a crunch sound effect. after item is
/// purchased, we don't show it in shop anymore, we move them to a book, this is where we show all owned items, in
/// grids of cookie icons, this book is always there in shop ... when clicks on an icon, it shows the information of
/// that item").
/// Three parts, all drawn in code: the cards' cookie pictures (Resources/Cookies/<card name>), the eating of a bought
/// cookie, and the book of owned things. ShopManager decides what is owned and what leaves the shop.
public static class ShopCookies
{
    // ---------------------------------------------------------------- the cards' pictures ----------------------

    /// The cookie for a card: by the card object's name first (the two "Upgrade" cards share an item name), then by item name.
    public static Sprite For(ShopItemController card)
    {
        if (card == null) return null;
        var s = Resources.Load<Sprite>("Cookies/" + card.gameObject.name);
        if (s == null && !string.IsNullOrEmpty(card.itemName)) s = Resources.Load<Sprite>("Cookies/" + card.itemName);
        return s;
    }

    /// Every cookie stands in the same square on its card, its foot where the old picture's foot was, so the price
    /// under it keeps its distance. A card without a cookie picture keeps its drawing.
    public const float IconSize = 340f;
    public static void Dress(ShopItemController card)
    {
        var cookie = For(card); if (cookie == null) return;
        var icon = card.transform.Find("ItemIcon") as RectTransform; if (icon == null) return;
        var img = icon.GetComponent<Image>(); if (img == null) return;
        var r = icon.rect;
        var foot = icon.anchoredPosition + new Vector2((0.5f - icon.pivot.x) * r.width, -icon.pivot.y * r.height);
        img.sprite = cookie; img.preserveAspect = true;
        icon.sizeDelta = new Vector2(IconSize, IconSize);
        icon.anchoredPosition = foot + new Vector2((icon.pivot.x - 0.5f) * IconSize, icon.pivot.y * IconSize);
    }

    // ---------------------------------------------------------------- eating a bought cookie -------------------

    public static bool Eating { get; private set; }

    /// Called when the shop opens: an eating cut short by leaving the shop must not leave its flag behind.
    public static void Reset() { Eating = false; bookPanel = null; bookButton = null; bookGlowing = false; }

    /// The bought cookie, big in the middle of the screen, eaten in five bites: each bite takes a scalloped piece out
    /// of the picture itself, with a crunch, a little shake and crumbs. Taps are held off while it runs (about two seconds).
    public static void Eat(MonoBehaviour host, RectTransform root, Sprite cookie, System.Action done)
    {
        if (host == null || root == null || cookie == null || !host.isActiveAndEnabled) { done?.Invoke(); return; }
        host.StartCoroutine(EatRoutine(host, root, cookie, done));
    }

    private static readonly float[] BiteAngles = { 40f, 330f, 250f, 150f };   // round the cookie, from the upper right
    private static readonly string[] BiteCrunches = { "UI/button_crunch_go", "UI/button_crunch_tap", "UI/button_crunch_back" };

    private static IEnumerator EatRoutine(MonoBehaviour host, RectTransform root, Sprite cookie, System.Action done)
    {
        Eating = true;
        var go = new GameObject("CookieEaten", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        go.transform.SetParent(root, false); go.transform.SetAsLastSibling();
        var rt = (RectTransform)go.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var dim = go.GetComponent<Image>(); dim.color = new Color(0f, 0f, 0f, 0.55f); dim.raycastTarget = true;
        var group = go.GetComponent<CanvasGroup>(); group.alpha = 0f;

        var tex = Copy(cookie, 320);
        var cgo = new GameObject("Cookie", typeof(RectTransform), typeof(RawImage));
        cgo.transform.SetParent(rt, false);
        var crt = (RectTransform)cgo.transform; crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.55f); crt.pivot = new Vector2(0.5f, 0.5f);
        const float size = 560f; var aspect = (float)tex.width / tex.height;
        crt.sizeDelta = aspect >= 1f ? new Vector2(size, size / aspect) : new Vector2(size * aspect, size);
        var raw = cgo.GetComponent<RawImage>(); raw.texture = tex; raw.raycastTarget = false;
        var px = tex.GetPixels32(); int w = tex.width, h = tex.height;
        var rng = new System.Random(cookie.name.GetHashCode());

        for (var t = 0f; t < 0.18f; t += Time.unscaledDeltaTime)
        {
            var k = t / 0.18f; group.alpha = k; crt.localScale = Vector3.one * Mathf.Lerp(0.5f, 1f, 1f - (1f - k) * (1f - k));
            yield return null;
        }
        group.alpha = 1f; crt.localScale = Vector3.one;
        yield return new WaitForSecondsRealtime(0.25f);

        var bites = BiteAngles.Length + 1;
        for (var b = 0; b < bites; b++)
        {
            var last = b == bites - 1;
            Vector2 at;   // where the bite landed, in the picture's own 0..1 space
            Color32 crumb;
            if (last) { at = Centroid(px, w, h); crumb = Sample(px, w, h, at); for (var i = 0; i < px.Length; i++) px[i].a = 0; }
            else at = Bite(px, w, h, BiteAngles[b], out crumb);
            tex.SetPixels32(px); tex.Apply(false);
            var clip = Resources.Load<AudioClip>(BiteCrunches[b % BiteCrunches.Length]);   // the owner's three button crunches (18 Nov 2026)
            if (clip != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clip);
            host.StartCoroutine(Crumbs(rt, crt, at, crumb, last ? 12 : 7, rng));
            for (var t = 0f; t < 0.14f; t += Time.unscaledDeltaTime)
            {
                var k = t / 0.14f;
                crt.localScale = Vector3.one * (1f + 0.07f * Mathf.Sin(k * Mathf.PI));
                crt.localRotation = Quaternion.Euler(0f, 0f, 5f * Mathf.Sin(k * Mathf.PI * 2f));
                yield return null;
            }
            crt.localScale = Vector3.one; crt.localRotation = Quaternion.identity;
            yield return new WaitForSecondsRealtime(last ? 0.3f : 0.2f);
        }
        for (var t = 0f; t < 0.18f; t += Time.unscaledDeltaTime) { group.alpha = 1f - t / 0.18f; yield return null; }
        Object.Destroy(go); Object.Destroy(tex);
        Eating = false;
        done?.Invoke();
    }

    /// A copy of the sprite's picture that can be written to, whatever the sprite's import settings.
    private static Texture2D Copy(Sprite s, int maxSide)
    {
        var src = s.texture; var r = s.textureRect;
        var k = Mathf.Min(1f, maxSide / Mathf.Max(r.width, r.height));
        int w = Mathf.Max(8, Mathf.RoundToInt(r.width * k)), h = Mathf.Max(8, Mathf.RoundToInt(r.height * k));
        var rtx = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(src, rtx, new Vector2(r.width / src.width, r.height / src.height), new Vector2(r.x / src.width, r.y / src.height));
        var before = RenderTexture.active; RenderTexture.active = rtx;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(false);
        RenderTexture.active = before; RenderTexture.ReleaseTemporary(rtx);
        tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }

    private static Vector2 Centroid(Color32[] px, int w, int h)
    {
        double sx = 0, sy = 0; long n = 0;
        for (var y = 0; y < h; y++) for (var x = 0; x < w; x++) if (px[y * w + x].a > 40) { sx += x; sy += y; n++; }
        return n == 0 ? new Vector2(0.5f, 0.5f) : new Vector2((float)(sx / n) / w, (float)(sy / n) / h);
    }

    private static Color32 Sample(Color32[] px, int w, int h, Vector2 at)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(at.x * w), 0, w - 1), y = Mathf.Clamp(Mathf.RoundToInt(at.y * h), 0, h - 1);
        var c = px[y * w + x]; return c.a > 40 ? c : new Color32(229, 186, 128, 255);
    }

    /// One bite: from the side given by the angle, a round piece with a scalloped edge (a row of tooth marks) comes out
    /// of what is left of the cookie. Returns where it landed (0..1 in the picture) and a colour for the crumbs.
    private static Vector2 Bite(Color32[] px, int w, int h, float angleDeg, out Color32 crumb)
    {
        var c = Centroid(px, w, h); var cx = c.x * w; var cy = c.y * h;
        var dir = new Vector2(Mathf.Cos(angleDeg * Mathf.Deg2Rad), Mathf.Sin(angleDeg * Mathf.Deg2Rad));
        var best = float.MinValue; int bx = (int)cx, by = (int)cy;
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
                if (px[y * w + x].a > 40) { var d = (x - cx) * dir.x + (y - cy) * dir.y; if (d > best) { best = d; bx = x; by = y; } }
        var R = 0.30f * Mathf.Max(w, h);
        var centre = new Vector2(bx, by) - dir * (R * 0.30f);
        crumb = Sample(px, w, h, new Vector2((centre.x - dir.x * R * 0.5f) / w, (centre.y - dir.y * R * 0.5f) / h));
        Carve(px, w, h, centre, R);
        // tooth marks along the bite's inner edge, facing what is left
        var inward = Mathf.Atan2(-dir.y, -dir.x);
        for (var j = -2; j <= 2; j++)
        {
            var a = inward + j * 0.42f;
            Carve(px, w, h, centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (R * 0.92f), R * 0.26f);
        }
        return new Vector2(centre.x / w, centre.y / h);
    }

    private static void Carve(Color32[] px, int w, int h, Vector2 centre, float radius)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(centre.x - radius - 2f)), x1 = Mathf.Min(w - 1, Mathf.CeilToInt(centre.x + radius + 2f));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(centre.y - radius - 2f)), y1 = Mathf.Min(h - 1, Mathf.CeilToInt(centre.y + radius + 2f));
        for (var y = y0; y <= y1; y++)
            for (var x = x0; x <= x1; x++)
            {
                var d = Mathf.Sqrt((x - centre.x) * (x - centre.x) + (y - centre.y) * (y - centre.y));
                var keep = Mathf.Clamp01((d - radius) / 1.5f + 0.5f);   // a soft edge about a pixel wide
                var i = y * w + x; var a = (byte)(px[i].a * keep);
                if (a < px[i].a) px[i].a = a;
            }
    }

    private static IEnumerator Crumbs(RectTransform overlay, RectTransform cookie, Vector2 at, Color32 colour, int count, System.Random rng)
    {
        var parts = new List<RectTransform>(); var vel = new List<Vector2>(); var spin = new List<float>();
        var origin = cookie.anchoredPosition + new Vector2((at.x - 0.5f) * cookie.sizeDelta.x, (at.y - 0.5f) * cookie.sizeDelta.y);
        var biscuit = new Color32(229, 186, 128, 255);
        for (var i = 0; i < count; i++)
        {
            var go = new GameObject("Crumb", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(overlay, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = cookie.anchorMin; rt.pivot = new Vector2(0.5f, 0.5f);
            var s = 10f + (float)rng.NextDouble() * 14f; rt.sizeDelta = new Vector2(s, s * (0.7f + (float)rng.NextDouble() * 0.5f));
            rt.anchoredPosition = origin; rt.localRotation = Quaternion.Euler(0f, 0f, (float)rng.NextDouble() * 360f);
            var img = go.GetComponent<Image>(); img.raycastTarget = false; img.color = i % 3 == 0 ? colour : biscuit;
            var a = (float)rng.NextDouble() * Mathf.PI * 2f; var sp = 260f + (float)rng.NextDouble() * 420f;
            parts.Add(rt); vel.Add(new Vector2(Mathf.Cos(a) * sp, Mathf.Abs(Mathf.Sin(a)) * sp + 180f)); spin.Add(((float)rng.NextDouble() - 0.5f) * 900f);
        }
        const float life = 0.6f;
        for (var t = 0f; t < life; t += Time.unscaledDeltaTime)
        {
            var dt = Time.unscaledDeltaTime;
            for (var i = 0; i < parts.Count; i++)
            {
                if (parts[i] == null) continue;
                vel[i] += new Vector2(0f, -2400f) * dt;
                parts[i].anchoredPosition += vel[i] * dt; parts[i].Rotate(0f, 0f, spin[i] * dt);
                var img = parts[i].GetComponent<Image>(); var c = img.color; c.a = Mathf.Clamp01(1.4f * (1f - t / life)); img.color = c;
            }
            yield return null;
        }
        foreach (var p in parts) if (p != null) Object.Destroy(p.gameObject);
    }

    // ---------------------------------------------------------------- the book of owned things -----------------

    public struct Entry { public Sprite cookie; public string name; public string info; public string key; public bool staff; }

    // The book's three parts since 19 November 2026 (owner: "a pencil, this one opens the journals page, a chef hat,
    // this one opens staff I own, a heart, this one opens my cookies"; "staff is just staff, not customers. only show
    // hired staff"): square index tabs on the book's right edge, cut from their own paper (Resources/Journal/tab_*).
    private const int Journal = 0, Staff = 1, Cookies = 2;
    private static readonly string[] TabArt = { "Journal/tab_journal", "Journal/tab_staff", "Journal/tab_cookies" };
    private static readonly Color[] SectionPaper = { new Color(1f, 0.976f, 0.925f), new Color(0.886f, 0.933f, 0.965f), new Color(0.988f, 0.902f, 0.886f) };   // cream, pale blue, pale pink: the tabs' own papers
    // where a journal page's day stamp goes: x and y from the sheet's top left on a sheet of 800 x 1212, the turn in degrees
    private static readonly Dictionary<string, Vector3> DayTags = new Dictionary<string, Vector3> {
        { "open", new Vector3(630f, 290f, 8f) }, { "star3", new Vector3(600f, 900f, -6f) }, { "icecream", new Vector3(206f, 1030f, 4f) },
        { "cake", new Vector3(640f, 290f, -7f) }, { "choux", new Vector3(630f, 190f, 6f) }, { "machine", new Vector3(630f, 290f, 8f) }, { "kitchen", new Vector3(630f, 290f, -7f) } };
    private static RectTransform lastRoot; private static List<Entry> lastOwned; private static TMP_FontAsset lastTitleFont, lastTextFont; private static Image lastPanelLook;

    private static RectTransform bookButton;
    private static GameObject bookPanel;

    /// The book's button: always in the shop, in the arrows' column just above the up arrow, as big as an arrow.
    public static void EnsureBookButton(RectTransform arrow, System.Func<bool> canOpen, System.Func<List<Entry>> owned, TMP_FontAsset titleFont, TMP_FontAsset textFont, Image panelLook)
    {
        if (arrow == null || bookButton != null) return;
        var sprite = Resources.Load<Sprite>("Cookies/Book"); if (sprite == null) return;
        var go = new GameObject("CookieBookButton", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(arrow.parent, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = arrow.anchorMin; rt.anchorMax = arrow.anchorMax; rt.pivot = arrow.pivot;
        var side = Mathf.Max(arrow.rect.width, arrow.rect.height) * 1.05f;
        rt.sizeDelta = new Vector2(side, side);
        rt.anchoredPosition = arrow.anchoredPosition + new Vector2(0f, arrow.rect.height / 2f + side / 2f + 26f);
        var img = go.GetComponent<Image>(); img.sprite = sprite; img.preserveAspect = true;
        go.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (Eating || (canOpen != null && !canOpen())) return;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            OpenBook(rt.GetComponentInParent<Canvas>().rootCanvas.transform as RectTransform, owned(), titleFont, textFont, panelLook);
        });
        bookButton = rt;
        RefreshBookTag();
    }

    private static bool bookGlowing;

    /// "NEW" on the book, and the new things' white glow with its little stars round it, while a journal page, a cookie
    /// or a staff member in it has not been looked at (owner, 18 November 2026; 19 November: "show a new label on the
    /// book when a journal event or new cookie or new staff is added. also show the new glow on it").
    public static void RefreshBookTag()
    {
        if (bookButton == null) return;
        var any = SessionManager.Instance != null && (SessionManager.Instance.AnyNewInBook || SessionManager.Instance.AnyNewJournal);
        if (any) NewTag.OnUI(bookButton); else NewTag.Remove(bookButton);
        var img = bookButton.GetComponent<Image>();
        if (any && !bookGlowing) { GlowFx.New(img); bookGlowing = true; }
        else if (!any && bookGlowing) { GlowFx.Remove(img); bookGlowing = false; }
    }

    /// A small hop of the book's button: the thing just eaten went in there.
    public static void BumpBook(MonoBehaviour host)
    {
        if (host != null && bookButton != null && host.isActiveAndEnabled) host.StartCoroutine(Bump(bookButton));
    }

    private static IEnumerator Bump(RectTransform rt)
    {
        for (var t = 0f; t < 0.45f; t += Time.unscaledDeltaTime)
        {
            if (rt == null) yield break;
            var k = t / 0.45f; rt.localScale = Vector3.one * (1f + 0.28f * Mathf.Sin(k * Mathf.PI) * (1f - k * 0.4f));
            yield return null;
        }
        if (rt != null) rt.localScale = Vector3.one;
    }

    public static bool BookOpen => bookPanel != null;

    private static readonly Color Ink = new Color(0.36f, 0.20f, 0.10f);

    private static void OpenBook(RectTransform root, List<Entry> owned, TMP_FontAsset titleFont, TMP_FontAsset textFont, Image panelLook, int section = -1)
    {
        if (root == null || bookPanel != null) return;
        lastRoot = root; lastOwned = owned; lastTitleFont = titleFont; lastTextFont = textFont; lastPanelLook = panelLook;
        var bookLook = Resources.Load<Sprite>("Cookies/book_page");
        var sm = SessionManager.Instance;
        var tabs = bookLook != null && Resources.Load<Sprite>(TabArt[0]) != null;
        // what is new in which part (the keys of bought things are in the save's list until their part has been opened)
        var newStaff = new List<string>(); var newCookies = new List<string>();
        foreach (var e in owned) if (sm != null && !string.IsNullOrEmpty(e.key) && sm.IsNewInBook(e.key)) (e.staff ? newStaff : newCookies).Add(e.key);
        var newJournal = sm != null && sm.AnyNewJournal;
        // which part opens: the one with something new (journal first, then staff, then cookies); else the journal
        if (!tabs) section = Cookies; else if (section < 0) section = newJournal ? Journal : newStaff.Count > 0 ? Staff : newCookies.Count > 0 ? Cookies : Journal;
        var journalPages = sm != null ? sm.JournalPages() : new List<string>();
        // the staff part exists once somebody is hired (owner, 19 November: "if not hired, don't show staff in the book")
        var anyStaff = false; foreach (var e in owned) if (e.staff) anyStaff = true;
        if (section == Staff && !anyStaff) section = Journal;
        if (tabs)
        {
            var part = new List<Entry>(); foreach (var e in owned) if (e.staff == (section == Staff)) part.Add(e);
            owned = section == Journal ? new List<Entry>() : part;
        }
        var go = new GameObject("CookieBook", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(root, false); go.transform.SetAsLastSibling();
        var rt = (RectTransform)go.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var dim = go.GetComponent<Image>(); dim.color = new Color(0f, 0f, 0f, 0.55f);
        var dimButton = go.GetComponent<Button>(); dimButton.transition = Selectable.Transition.None;
        dimButton.onClick.AddListener(CloseBook);   // a tap beside the book closes it
        bookPanel = go;

        // the book keeps its picture's shape (960 x 1320 since the journal; it was 960 x 1560) and leaves room at the right for the tabs
        float artW = bookLook != null ? bookLook.rect.width : 960f, artH = bookLook != null ? bookLook.rect.height : 1560f;
        float W = Mathf.Min(artW, root.rect.width - (tabs ? 190f : 80f)), H = Mathf.Min(artH, root.rect.height - 240f);
        if (bookLook != null) { var fit = Mathf.Min(W / artW, H / artH); W = artW * fit; H = artH * fit; }
        var pageAt = new Vector2(tabs ? -45f : 0f, 0f);
        var page = Child(rt, "Page", new Vector2(0.5f, 0.5f), new Vector2(W, H), pageAt);
        var pageImg = page.gameObject.AddComponent<Image>();
        // The open book (owner, 19 November 2026: "use 5 for cookies book": a red book bound on the left, a cream page,
        // the page block's edges at the right and bottom; Store/ui/paper_panels_2026-11-19/book_5_game.png, 960 x 1560).
        // Everything keeps clear of the spine and the page's edges ("the icons are too close to the left and right
        // edge. need more space. close icon need some space around it too"). Without the picture: the shop panel's cream.
        float left, right, top, bottom;   // the room for cookies and words, in the page's own space
        if (bookLook != null)
        {
            pageImg.sprite = bookLook; pageImg.type = Image.Type.Simple; pageImg.color = Color.white;
            float kx = W / artW, ky = H / artH;
            left = -W / 2f + (104f + 64f) * kx; right = W / 2f - (56f + 64f) * kx; top = H / 2f - 48f * ky; bottom = -H / 2f + 66f * ky;
        }
        else
        {
            if (panelLook != null) { pageImg.sprite = panelLook.sprite; pageImg.type = panelLook.type; pageImg.pixelsPerUnitMultiplier = panelLook.pixelsPerUnitMultiplier; }
            pageImg.color = new Color(1f, 0.97f, 0.89f, 0.98f);   // the shop panel's cream, nearly solid so the cookies read
            left = -W / 2f + 40f; right = W / 2f - 40f; top = H / 2f; bottom = -H / 2f;
        }
        float wide = right - left, middle = (left + right) / 2f;
        // the cream page from the spine to its right edge: what a turning page covers, and what a journal page fills
        var sheetArea = bookLook != null
            ? Rect.MinMaxRect(-W / 2f + 104f * W / artW, -H / 2f + 60f * H / artH, W / 2f - 56f * W / artW, H / 2f - 48f * H / artH)
            : Rect.MinMaxRect(-W / 2f + 12f, -H / 2f + 12f, W / 2f - 12f, H / 2f - 12f);
        var paper = bookLook != null ? SectionPaper[tabs ? section : 0] : pageImg.color;
        if (tabs && section != Journal)
        {
            // the staff's and the cookies' pages have their own paper (pale blue, pale pink), as their tabs do
            var tint = Child(page, "Paper", new Vector2(0.5f, 0.5f), sheetArea.size, sheetArea.center);
            var ti = tint.gameObject.AddComponent<Image>(); ti.color = SectionPaper[section]; ti.raycastTarget = false;   // the very colour of the part's tab, so the two are one piece
        }

        if (tabs)
        {
            // The index tabs: every one lies over the red cover, because it is cut from a page's paper (owner, 19 November:
            // "show the other two book mark above the red cover since book marks is on paper"), its straight end exactly
            // on the paper's right edge so tab and page are one piece with no line between them. The open part's is in
            // front and as bright as its page; the others a shade darker.
            var k = W / artW; var slot = 0;
            for (var i = 0; i < TabArt.Length; i++)
            {
                if (i == Staff && !anyStaff) continue;
                var at = new Vector2(pageAt.x + sheetArea.xMax - 1f, H / 2f - (150f + slot * 138f) * k); slot++;
                var tab = MakeTab(rt, i, i == section, at, k);
                if (tab != null && i != section) tab.SetSiblingIndex(page.GetSiblingIndex() + 1);   // behind the open part's tab, in front of the book
                // a part with something not yet looked at wears a small NEW on its tab
                // (the open part's too, this once: its things are being looked at now; owner, 19 November: "show a tiny new
                // label on the book mark that contains new item ... possibly more than one book mark")
                if (tab != null && (i == Journal ? newJournal : i == Staff ? newStaff.Count > 0 : newCookies.Count > 0)) NewTag.OnUI(tab);
            }
        }

        var title = Label(page, "Title", section == Staff ? "MY STAFF" : "MY COOKIES", titleFont, 78f * Mathf.Min(1f, W / 900f), new Vector2(middle - 40f, top - 96f), new Vector2(wide, 110f));
        title.color = Ink;

        // four cookies in a row, and as many rows as fit above the description; a full page is followed by another, turned
        // like a book's with the arrows under the grid (owner, 19 November 2026: "put four cookies in a row, we will
        // have a page flip effect when a page is full, and a next arrow to click on to flip the page. So it's like a book")
        var infoH = artH < 1500f ? 260f : 330f; const float arrowRow = 110f; const int columns = 4;   // the shorter book: a lower description, closer rows
        var gridTop = top - 176f; var gridBottom = bottom + infoH + 60f + arrowRow;
        var cell = wide / columns; var rowPitch = Mathf.Max(cell, artH < 1500f ? 150f : 190f);
        var rowsPerPage = Mathf.Max(1, Mathf.FloorToInt((gridTop - gridBottom) / rowPitch)); var perPage = rowsPerPage * columns;
        rowPitch = Mathf.Min((gridTop - gridBottom) / rowsPerPage, cell * 1.3f);   // the rows share the height, not further apart than looks right
        var pageCount = section == Journal && tabs ? Mathf.Max(1, journalPages.Count) : Mathf.Max(1, Mathf.CeilToInt(owned.Count / (float)perPage));
        var icon = Mathf.Min(cell, rowPitch) * 0.88f;
        // the chosen thing: its cookie, its name and what it does, on the lower part of the page
        var chosen = Child(page, "Chosen", new Vector2(0.5f, 0.5f), new Vector2(200f, 200f), new Vector2(left + 100f, bottom + infoH / 2f + 20f));
        var chosenImg = chosen.gameObject.AddComponent<Image>(); chosenImg.preserveAspect = true; chosenImg.raycastTarget = false; chosenImg.enabled = false;
        float textLeft = left + 226f, textWide = right - textLeft;
        var name = Label(page, "Name", "", titleFont, 58f, new Vector2(textLeft + textWide / 2f, bottom + infoH - 30f), new Vector2(textWide, 90f));
        name.color = Ink; name.alignment = TextAlignmentOptions.Left;
        name.enableAutoSizing = true; name.fontSizeMax = 58f; name.fontSizeMin = 34f;   // a long name ("Kitchen upgrade") stays on the page
        var info = Label(page, "Info", "", textFont != null ? textFont : titleFont, bookLook != null ? (artH < 1500f ? 38f : 44f) : 50f, new Vector2(textLeft + textWide / 2f, bottom + infoH / 2f - 20f - ShopManager.HandTopFix * (bookLook != null ? 44f : 50f)), new Vector2(textWide, 210f));   // three lines fit above the page's edge
        info.color = Ink; info.alignment = TextAlignmentOptions.TopLeft; info.enableWordWrapping = true; info.lineSpacing = -14f + ShopManager.HandLineFix * 0.6f;   // the hand font (Delius): a little closer than the old font's lines
        var rule = Child(page, "Rule", new Vector2(0.5f, 0.5f), new Vector2(wide, 4f), new Vector2(middle, bottom + infoH + 40f));
        var ruleImg = rule.gameObject.AddComponent<Image>(); ruleImg.color = new Color(Ink.r, Ink.g, Ink.b, 0.25f); ruleImg.raycastTarget = false;

        var cells = new List<RectTransform>(); var newest = owned.Count - 1;
        var leaves = new List<RectTransform>();
        for (var k = 0; k < pageCount; k++) { var leaf = Child(page, "Leaf" + k, new Vector2(0.5f, 0.5f), new Vector2(W, H), Vector2.zero); leaf.gameObject.SetActive(false); leaves.Add(leaf); }
        if (tabs && section == Journal)
        {
            // the journal: one whole page per event, a picture, with the day written on its blank tag
            title.gameObject.SetActive(false); rule.gameObject.SetActive(false); chosen.gameObject.SetActive(false); name.gameObject.SetActive(false); info.gameObject.SetActive(false);
            for (var k = 0; k < journalPages.Count; k++)
            {
                var bits = journalPages[k].Split('|'); var id = bits[0]; var day = 0; if (bits.Length > 1) int.TryParse(bits[1], out day);
                var art = Resources.Load<Sprite>("Journal/page_" + id);
                var sheet = Child(leaves[k], "JournalPage", new Vector2(0.5f, 0.5f), sheetArea.size, sheetArea.center);
                var si = sheet.gameObject.AddComponent<Image>(); si.sprite = art; si.color = art != null ? Color.white : paper; si.raycastTarget = false;
                if (day > 0 && DayTags.TryGetValue(id, out var tag))
                {
                    var at = new Vector2(sheetArea.xMin + tag.x / 800f * sheetArea.width, sheetArea.yMax - tag.y / 1212f * sheetArea.height);
                    // the day as a red ink stamp, laid on only when the day is known (owner, 19 November: no brown tag on the pages)
                    var unit = sheetArea.width / 800f; var red = new Color(0.77f, 0.23f, 0.20f, 0.9f);
                    var stamp = Child(leaves[k], "DayStamp", new Vector2(0.5f, 0.5f), new Vector2(150f, 150f) * unit, at);
                    var sti = stamp.gameObject.AddComponent<Image>(); sti.sprite = Resources.Load<Sprite>("Journal/day_stamp"); sti.raycastTarget = false; sti.enabled = sti.sprite != null;
                    stamp.localRotation = Quaternion.Euler(0f, 0f, tag.z);
                    var dl = Label(stamp, "Day", "DAY\n" + day, titleFont, 34f * unit, Vector2.zero, new Vector2(140f, 120f) * unit);
                    dl.color = red; dl.lineSpacing = -30f;
                }
            }
            if (journalPages.Count == 0)
            {
                var empty = Label(leaves[0], "Empty", "Finish your first day\nto start the journal.", textFont != null ? textFont : titleFont, 48f, sheetArea.center, new Vector2(sheetArea.width - 120f, 300f));
                empty.color = Ink;
            }
        }
        else if (owned.Count == 0 && section == Staff)
        {
            var none = Label(leaves[0], "Empty", "No staff yet.", textFont != null ? textFont : titleFont, 48f, new Vector2(middle, (gridTop + gridBottom) / 2f), new Vector2(wide, 120f));
            none.color = Ink;
        }
        for (var i = 0; i < owned.Count; i++)
        {
            var e = owned[i];
            var onPage = i / perPage; var slot = i % perPage;
            var pos = new Vector2(left + cell * (slot % columns + 0.5f), gridTop - rowPitch * (slot / columns + 0.5f));
            var c = Child(leaves[onPage], "Owned" + i, new Vector2(0.5f, 0.5f), new Vector2(icon, icon), pos);
            var ci = c.gameObject.AddComponent<Image>(); ci.sprite = e.cookie; ci.preserveAspect = true;
            var cb = c.gameObject.AddComponent<Button>(); cb.transition = Selectable.Transition.None;
            cells.Add(c);
            if (SessionManager.Instance != null && SessionManager.Instance.IsNewInBook(e.key)) { NewTag.OnUI(c); newest = i; }
            cb.onClick.AddListener(() =>
            {
                // a crunch, one of the owner's three, as for the cookie being eaten (owner, 19 November 2026: "add crunch
                // sound effect when i click on icon in my cookies book")
                var crunch = Resources.Load<AudioClip>(BiteCrunches[UnityEngine.Random.Range(0, BiteCrunches.Length)]);
                if (AudioManager.Instance != null) { if (crunch != null) AudioManager.Instance.PlaySFX(crunch); else AudioManager.Instance.PlayButtonClick(); }
                foreach (var other in cells) if (other != null) other.localScale = Vector3.one;
                c.localScale = Vector3.one * 1.14f;
                chosenImg.sprite = e.cookie; chosenImg.enabled = true; name.text = TextStars.Mark(e.name ?? ""); info.text = TextStars.Mark(e.info ?? "");
                TextStars.Place(name); TextStars.Place(info);
            });
        }
        // the page itself swallows taps, so only a tap beside it closes the book
        var guard = page.gameObject.AddComponent<Button>(); guard.transition = Selectable.Transition.None;

        var cross = Resources.Load<Sprite>("UI/cross");
        if (cross != null)
        {
            var x = Child(page, "Close", new Vector2(0.5f, 0.5f), new Vector2(96f, 96f), bookLook != null ? new Vector2(right - 30f, top - 92f) : new Vector2(W / 2f - 76f, H / 2f - 82f));   // on the page, with room round it
            var xi = x.gameObject.AddComponent<Image>(); xi.sprite = cross; xi.preserveAspect = true;
            x.gameObject.AddComponent<Button>().onClick.AddListener(CloseBook);
        }
        // the pages: the one with the newest cookie first; arrows and "1 / 2" under the grid when there is more than one
        var turner = go.AddComponent<BookTurner>();
        // the whole page turns (owner, 19 November 2026: "when flip the page, the entire page flips"): the cream page from
        // the spine to its right edge, carrying the title, the cookies, the arrows and the description
        turner.Set(page, leaves, tabs && section == Journal ? pageCount - 1 : owned.Count > 0 ? newest / perPage : 0, sheetArea, paper);   // the turning sheet in the page's own paper; the journal opens on its newest page
        var furniture = tabs && section == Journal ? new List<RectTransform>() : new List<RectTransform> { title.rectTransform, rule, chosen, name.rectTransform, info.rectTransform };
        if (pageCount > 1)
        {
            var arrowY = tabs && section == Journal ? sheetArea.yMin + 74f : gridBottom - arrowRow / 2f + 6f;
            const float ArrowW = 230f, ArrowH = 150f;   // the map arrow's picture has wide margins
            var count = Label(page, "PageCount", "", titleFont, 58f, new Vector2(middle, arrowY), new Vector2(240f, 70f)); count.color = new Color(Ink.r, Ink.g, Ink.b, 0.7f);
            var next = Child(page, "NextPage", new Vector2(0.5f, 0.5f), new Vector2(ArrowW, ArrowH), new Vector2(right - 80f, arrowY));
            var ni = next.gameObject.AddComponent<Image>(); ni.sprite = Resources.Load<Sprite>("Map/arrow_right"); ni.preserveAspect = true;
            var prev = Child(page, "PrevPage", new Vector2(0.5f, 0.5f), new Vector2(ArrowW, ArrowH), new Vector2(left + 80f, arrowY));
            var pi = prev.gameObject.AddComponent<Image>(); pi.sprite = Resources.Load<Sprite>("Map/arrow_left"); pi.preserveAspect = true;
            var nb = next.gameObject.AddComponent<Button>(); nb.transition = Selectable.Transition.None; nb.onClick.AddListener(() => turner.Turn(1));
            var pb = prev.gameObject.AddComponent<Button>(); pb.transition = Selectable.Transition.None; pb.onClick.AddListener(() => turner.Turn(-1));
            turner.Controls(count, next.gameObject, prev.gameObject);
            furniture.Add(count.rectTransform); furniture.Add(next); furniture.Add(prev);
        }
        // what stays in place over a turning page: the ribbon (the book's own bookmark) and the close button
        var keep = new List<RectTransform>();
        var ribbonSprite = bookLook != null ? Resources.Load<Sprite>("Cookies/book_ribbon") : null;
        if (ribbonSprite != null)
        {
            float kx = W / artW, ky = H / artH;
            var ribbon = Child(page, "Ribbon", new Vector2(0.5f, 0.5f), new Vector2(ribbonSprite.rect.width * kx, ribbonSprite.rect.height * ky),
                               new Vector2(-W / 2f + (156f + ribbonSprite.rect.width / 2f) * kx, H / 2f - (40f + ribbonSprite.rect.height / 2f) * ky));
            var ri = ribbon.gameObject.AddComponent<Image>(); ri.sprite = ribbonSprite; ri.raycastTarget = false;
            keep.Add(ribbon);
        }
        var closeT = page.Find("Close") as RectTransform; if (closeT != null) keep.Add(closeT);
        turner.Furniture(furniture, keep);
        if (owned.Count > 0) cells[newest].GetComponent<Button>().onClick.Invoke();   // a new one is shown first
        // looked at: the labels stay on this open page, the book's own label goes
        // looked at: only the open part's things; the other parts keep their labels (and the book's button its NEW) until they are opened
        if (SessionManager.Instance != null)
        {
            if (!tabs) SessionManager.Instance.ClearNewInBook();
            else if (section == Cookies) SessionManager.Instance.ClearNewInBook(newCookies);
            else if (section == Staff) SessionManager.Instance.ClearNewInBook(newStaff);
            else SessionManager.Instance.ClearNewJournal();
        }
        RefreshBookTag();
    }

    /// One index tab: a square of its part's paper with its picture, its left end at `at` (the page's right edge). The
    /// open part's is full bright; the others are a shade darker and open their part on a tap, with the page's sound.
    private static RectTransform MakeTab(RectTransform parent, int index, bool open, Vector2 at, float k)
    {
        var sprite = Resources.Load<Sprite>(TabArt[index]); if (sprite == null) return null;
        var size = new Vector2(sprite.rect.width, sprite.rect.height) * 0.5f * k;
        var t = Child(parent, "Tab" + index, new Vector2(0.5f, 0.5f), size, at + new Vector2(size.x / 2f, 0f));
        var ti = t.gameObject.AddComponent<Image>(); ti.sprite = sprite; ti.color = open ? Color.white : new Color(0.94f, 0.94f, 0.94f, 1f);
        var b = t.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
        if (open) return t;
        b.onClick.AddListener(() =>
        {
            var flip = Resources.Load<AudioClip>("UI/page_flip");
            if (AudioManager.Instance != null) { if (flip != null) AudioManager.Instance.PlaySFX(flip); else AudioManager.Instance.PlayButtonClick(); }
            CloseBook(); OpenBook(lastRoot, lastOwned, lastTitleFont, lastTextFont, lastPanelLook, index);
        });
        return t;
    }

    public static void CloseBook()
    {
        if (bookPanel != null) Object.Destroy(bookPanel);
        bookPanel = null;
    }

    private static RectTransform Child(RectTransform parent, string name, Vector2 anchor, Vector2 size, Vector2 at)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size; rt.anchoredPosition = at;
        return rt;
    }

    private static TextMeshProUGUI Label(RectTransform parent, string name, string text, TMP_FontAsset font, float size, Vector2 at, Vector2 box)
    {
        var rt = Child(parent, name, new Vector2(0.5f, 0.5f), box, at);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text; t.fontSize = size; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; t.richText = true;
        t.enableWordWrapping = false;
        return t;
    }
}

/// The cookie book's pages: one leaf of cookies shown at a time. A turn is the whole page as a sheet of the book's own
/// paper, hinged at the spine, that carries the leaving page (its cookies, and copies of the title, the description and
/// the arrows): it swings over with its free edge lifting towards the reader (a little
/// taller there), darker along that edge like a bent page, a bright rim on the edge itself, and a soft shadow thrown on
/// the page under it; the next leaf lies ready below (owner, 19 November 2026: "change the page flip animation, it looks
/// bad. It can be simple but needs to look good"). Back: the earlier leaf swings down over the page. Unscaled time.
public class BookTurner : MonoBehaviour
{
    private RectTransform page; private List<RectTransform> leaves; private int current; private Rect grid; private Color paper;
    private TextMeshProUGUI count; private GameObject next, prev; private bool turning;
    private const float Seconds = 0.55f;
    private static Sprite rampSprite;

    public void Set(RectTransform bookPage, List<RectTransform> all, int first, Rect gridArea, Color paperColour)
    {
        page = bookPage; leaves = all; grid = gridArea; paper = new Color(paperColour.r, paperColour.g, paperColour.b, 1f);
        current = Mathf.Clamp(first, 0, leaves.Count - 1);
        for (var i = 0; i < leaves.Count; i++) leaves[i].gameObject.SetActive(i == current);
    }

    public void Controls(TextMeshProUGUI pageCount, GameObject nextArrow, GameObject prevArrow) { count = pageCount; next = nextArrow; prev = prevArrow; Show(current); }

    private List<RectTransform> furniture = new List<RectTransform>(), keep = new List<RectTransform>();
    /// What lies on every page and turns with it (title, description, arrows), and what stays over a turning page.
    public void Furniture(List<RectTransform> onThePage, List<RectTransform> staysOnTop) { furniture = onThePage; keep = staysOnTop; }

    /// The page number and the arrows as they are on page `n`.
    private void Show(int n)
    {
        if (count != null) count.text = (n + 1) + " / " + leaves.Count;
        if (next != null) next.SetActive(n < leaves.Count - 1);
        if (prev != null) prev.SetActive(n > 0);
    }

    public void Turn(int by)
    {
        var to = current + by;
        if (turning || to < 0 || to >= leaves.Count) return;
        StartCoroutine(Flip(to, by > 0));
    }

    /// White, clear at the left and solid at the right: tinted for the page's shade and for the shadow it throws.
    private static Sprite Ramp
    {
        get
        {
            if (rampSprite != null) return rampSprite;
            const int n = 64; var t = new Texture2D(n, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * 2];
            for (var x = 0; x < n; x++) { var a = (byte)(255f * Mathf.Pow(x / (n - 1f), 1.6f)); px[x] = px[n + x] = new Color32(255, 255, 255, a); }
            t.SetPixels32(px); t.Apply(false);
            return rampSprite = Sprite.Create(t, new Rect(0, 0, n, 2), new Vector2(0.5f, 0.5f), 100f);
        }
    }

    private static Image Layer(string name, RectTransform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
        var img = go.GetComponent<Image>(); img.sprite = sprite; img.raycastTarget = false;
        return img;
    }

    private IEnumerator Flip(int to, bool forward)
    {
        turning = true;
        var from = current;
        var onSheet = forward ? from : to;                // the page that rides on the turning sheet
        var below = forward ? to : from;                  // the page that lies flat under it
        leaves[from].gameObject.SetActive(true); leaves[to].gameObject.SetActive(true);

        // no taps while a page turns
        var guard = Layer("TurnGuard", page, null); guard.color = new Color(0f, 0f, 0f, 0f); guard.raycastTarget = true;
        var guardRt = guard.rectTransform; guardRt.anchorMin = Vector2.zero; guardRt.anchorMax = Vector2.one; guardRt.offsetMin = guardRt.offsetMax = Vector2.zero;

        // the shadow the sheet throws on the page under it, just past its free edge
        var cast = Layer("TurnShadow", page, Ramp); var castRt = cast.rectTransform;
        castRt.anchorMin = castRt.anchorMax = new Vector2(0.5f, 0.5f); castRt.pivot = new Vector2(0f, 0.5f); castRt.localScale = new Vector3(-1f, 1f, 1f);   // dark at the sheet's edge, fading away to the right

        // the sheet: the whole page in the book's paper, hinged at the spine
        var paperSprite = Resources.Load<Sprite>("Cookies/book_paper");
        var sheetImg = Layer("TurningSheet", page, paperSprite); var sheet = sheetImg.rectTransform;
        sheet.anchorMin = sheet.anchorMax = new Vector2(0.5f, 0.5f); sheet.pivot = new Vector2(0f, 0.5f);
        sheet.sizeDelta = grid.size; sheet.anchoredPosition = new Vector2(grid.xMin, grid.center.y);
        sheetImg.color = paperSprite != null ? Color.white : paper;
        var bend = sheet.gameObject.AddComponent<PageBend>();

        // on the sheet: that page's title, description, number and arrows as copies, and its own cookies
        Show(onSheet);
        foreach (var f in furniture)
        {
            if (f == null || !f.gameObject.activeSelf) continue;
            var copy = Instantiate(f.gameObject, sheet, true);
            foreach (var b in copy.GetComponentsInChildren<Selectable>(true)) Destroy(b);
            foreach (var g in copy.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            foreach (var c in copy.GetComponentsInChildren<CookiePress>(true)) Destroy(c);
        }
        leaves[onSheet].SetParent(sheet, true);
        Show(below);                                      // the real ones show the page that lies under the sheet

        var shade = Layer("Shade", sheet, Ramp); var shadeRt = shade.rectTransform;
        shadeRt.anchorMin = Vector2.zero; shadeRt.anchorMax = Vector2.one; shadeRt.offsetMin = shadeRt.offsetMax = Vector2.zero;
        var shadeBend = shade.gameObject.AddComponent<PageBend>();
        var rim = Layer("Rim", sheet, null); var rimRt = rim.rectTransform;
        rimRt.anchorMin = new Vector2(1f, 0f); rimRt.anchorMax = new Vector2(1f, 1f); rimRt.pivot = new Vector2(1f, 0.5f); rimRt.sizeDelta = new Vector2(12f, 0f); rimRt.anchoredPosition = Vector2.zero;

        // order: page things, the shadow, the sheet, then what stays on top (ribbon, close), then the guard
        castRt.SetAsLastSibling(); sheet.SetAsLastSibling();
        foreach (var k in keep) if (k != null) k.SetAsLastSibling();
        guardRt.SetAsLastSibling();

        var clip = Resources.Load<AudioClip>("UI/page_flip"); if (clip == null) clip = Resources.Load<AudioClip>("Audio/paper_flip");
        if (AudioManager.Instance != null) { if (clip != null) AudioManager.Instance.PlaySFX(clip); else AudioManager.Instance.PlayButtonClick(); }

        for (var t = 0f; t < Seconds; t += Mathf.Min(Time.unscaledDeltaTime, 0.05f))   // a hitch never skips the turn
        {
            var k = Mathf.SmoothStep(0f, 1f, t / Seconds); if (!forward) k = 1f - k;     // 0: flat on the page, 1: standing on its edge at the spine
            var flat = Mathf.Cos(k * Mathf.PI * 0.5f);                                   // the sheet's width as seen from above
            var up = Mathf.Sin(k * Mathf.PI);                                            // how far it is off the page: most half way
            sheet.localScale = new Vector3(Mathf.Max(0.001f, flat), 1f, 1f);
            bend.Lift = shadeBend.Lift = 0.035f * Mathf.Sin(k * Mathf.PI * 0.5f);         // the free edge comes towards the reader
            shade.color = new Color(0.30f, 0.18f, 0.08f, 0.5f * Mathf.Sin(k * Mathf.PI * 0.5f));   // none while it lies flat
            rim.color = new Color(1f, 1f, 1f, 0.75f * up);
            castRt.sizeDelta = new Vector2(30f + 170f * up, grid.height * (1f + 0.05f * up));
            castRt.anchoredPosition = new Vector2(grid.xMin + grid.width * flat + castRt.sizeDelta.x, grid.center.y);
            cast.color = new Color(0.25f, 0.14f, 0.06f, 0.34f * up);
            yield return null;
        }
        var leaf = leaves[onSheet]; leaf.SetParent(page, true); leaf.localScale = Vector3.one; leaf.anchoredPosition = Vector2.zero;
        leaf.SetSiblingIndex(leaves[below].GetSiblingIndex());
        Destroy(sheet.gameObject); Destroy(cast.gameObject); Destroy(guard.gameObject);
        current = to;
        for (var i = 0; i < leaves.Count; i++) leaves[i].gameObject.SetActive(i == current);
        Show(current); turning = false;
    }
}

/// A picture whose right edge is drawn taller (Lift as a share of its height): the look of a page's free edge
/// coming towards the reader while it turns.
public class PageBend : BaseMeshEffect
{
    private float lift;
    public float Lift { get { return lift; } set { if (Mathf.Approximately(lift, value)) return; lift = value; if (graphic != null) graphic.SetVerticesDirty(); } }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || lift == 0f || vh.currentVertCount == 0) return;
        var r = graphic.rectTransform.rect; var v = new UIVertex();
        for (var i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            var across = r.width > 0f ? Mathf.InverseLerp(r.xMin, r.xMax, v.position.x) : 0f;
            v.position.y = r.center.y + (v.position.y - r.center.y) * (1f + lift * 2f * across);
            vh.SetUIVertex(v, i);
        }
    }
}
