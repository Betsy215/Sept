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
    public static void Reset() { Eating = false; bookPanel = null; bookButton = null; }

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

    public struct Entry { public Sprite cookie; public string name; public string info; public string key; }

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

    /// "NEW" on the book while something bought has not been looked at in it (owner, 18 November 2026).
    public static void RefreshBookTag()
    {
        if (bookButton == null) return;
        if (SessionManager.Instance != null && SessionManager.Instance.AnyNewInBook) NewTag.OnUI(bookButton); else NewTag.Remove(bookButton);
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

    private static void OpenBook(RectTransform root, List<Entry> owned, TMP_FontAsset titleFont, TMP_FontAsset textFont, Image panelLook)
    {
        if (root == null || bookPanel != null) return;
        var go = new GameObject("CookieBook", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(root, false); go.transform.SetAsLastSibling();
        var rt = (RectTransform)go.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var dim = go.GetComponent<Image>(); dim.color = new Color(0f, 0f, 0f, 0.55f);
        var dimButton = go.GetComponent<Button>(); dimButton.transition = Selectable.Transition.None;
        dimButton.onClick.AddListener(CloseBook);   // a tap beside the book closes it
        bookPanel = go;

        float W = Mathf.Min(960f, root.rect.width - 80f), H = Mathf.Min(1560f, root.rect.height - 240f);
        var page = Child(rt, "Page", new Vector2(0.5f, 0.5f), new Vector2(W, H), Vector2.zero);
        var pageImg = page.gameObject.AddComponent<Image>();
        // The open book (owner, 19 November 2026: "use 5 for cookies book": a red book bound on the left, a cream page,
        // the page block's edges at the right and bottom; Store/ui/paper_panels_2026-11-19/book_5_game.png, 960 x 1560).
        // Everything keeps clear of the spine and the page's edges ("the icons are too close to the left and right
        // edge. need more space. close icon need some space around it too"). Without the picture: the shop panel's cream.
        var bookLook = Resources.Load<Sprite>("Cookies/book_page");
        float left, right, top, bottom;   // the room for cookies and words, in the page's own space
        if (bookLook != null)
        {
            pageImg.sprite = bookLook; pageImg.type = Image.Type.Simple; pageImg.color = Color.white;
            float kx = W / 960f, ky = H / 1560f;
            left = -W / 2f + (104f + 64f) * kx; right = W / 2f - (56f + 64f) * kx; top = H / 2f - 48f * ky; bottom = -H / 2f + 66f * ky;
        }
        else
        {
            if (panelLook != null) { pageImg.sprite = panelLook.sprite; pageImg.type = panelLook.type; pageImg.pixelsPerUnitMultiplier = panelLook.pixelsPerUnitMultiplier; }
            pageImg.color = new Color(1f, 0.97f, 0.89f, 0.98f);   // the shop panel's cream, nearly solid so the cookies read
            left = -W / 2f + 40f; right = W / 2f - 40f; top = H / 2f; bottom = -H / 2f;
        }
        float wide = right - left, middle = (left + right) / 2f;

        var title = Label(page, "Title", "MY COOKIES", titleFont, 78f, new Vector2(middle, top - 96f), new Vector2(wide, 110f));
        title.color = Ink;

        // four cookies in a row, and as many rows as fit above the description; a full page is followed by another, turned
        // like a book's with the arrows under the grid (owner, 19 November 2026: "put four cookies in a row, we will
        // have a page flip effect when a page is full, and a next arrow to click on to flip the page. So it's like a book")
        const float infoH = 330f; const float arrowRow = 110f; const int columns = 4;
        var gridTop = top - 176f; var gridBottom = bottom + infoH + 60f + arrowRow;
        var cell = wide / columns; var rowPitch = Mathf.Max(cell, 190f);
        var rowsPerPage = Mathf.Max(1, Mathf.FloorToInt((gridTop - gridBottom) / rowPitch)); var perPage = rowsPerPage * columns;
        rowPitch = Mathf.Min((gridTop - gridBottom) / rowsPerPage, cell * 1.3f);   // the rows share the height, not further apart than looks right
        var pageCount = Mathf.Max(1, Mathf.CeilToInt(owned.Count / (float)perPage));
        var icon = Mathf.Min(cell, rowPitch) * 0.88f;
        // the chosen thing: its cookie, its name and what it does, on the lower part of the page
        var chosen = Child(page, "Chosen", new Vector2(0.5f, 0.5f), new Vector2(200f, 200f), new Vector2(left + 100f, bottom + infoH / 2f + 20f));
        var chosenImg = chosen.gameObject.AddComponent<Image>(); chosenImg.preserveAspect = true; chosenImg.raycastTarget = false; chosenImg.enabled = false;
        float textLeft = left + 226f, textWide = right - textLeft;
        var name = Label(page, "Name", "", titleFont, 58f, new Vector2(textLeft + textWide / 2f, bottom + infoH - 30f), new Vector2(textWide, 90f));
        name.color = Ink; name.alignment = TextAlignmentOptions.Left;
        var info = Label(page, "Info", "", textFont != null ? textFont : titleFont, bookLook != null ? 44f : 50f, new Vector2(textLeft + textWide / 2f, bottom + infoH / 2f - 20f), new Vector2(textWide, 210f));   // three lines fit above the page's edge
        info.color = Ink; info.alignment = TextAlignmentOptions.TopLeft; info.enableWordWrapping = true; info.lineSpacing = -14f;
        var rule = Child(page, "Rule", new Vector2(0.5f, 0.5f), new Vector2(wide, 4f), new Vector2(middle, bottom + infoH + 40f));
        var ruleImg = rule.gameObject.AddComponent<Image>(); ruleImg.color = new Color(Ink.r, Ink.g, Ink.b, 0.25f); ruleImg.raycastTarget = false;

        var cells = new List<RectTransform>(); var newest = owned.Count - 1;
        var leaves = new List<RectTransform>();
        for (var k = 0; k < pageCount; k++) { var leaf = Child(page, "Leaf" + k, new Vector2(0.5f, 0.5f), new Vector2(W, H), Vector2.zero); leaf.gameObject.SetActive(false); leaves.Add(leaf); }
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
        turner.Set(page, leaves, owned.Count > 0 ? newest / perPage : 0, new Rect(left - 24f, gridBottom, wide + 48f, gridTop - gridBottom),
                   bookLook != null ? new Color(1f, 0.976f, 0.925f, 1f) : pageImg.color);   // the turning sheet in the page's own paper
        if (pageCount > 1)
        {
            var arrowY = gridBottom - arrowRow / 2f + 6f;
            const float ArrowW = 230f, ArrowH = 150f;   // the map arrow's picture has wide margins
            var count = Label(page, "PageCount", "", titleFont, 58f, new Vector2(middle, arrowY), new Vector2(240f, 70f)); count.color = new Color(Ink.r, Ink.g, Ink.b, 0.7f);
            var next = Child(page, "NextPage", new Vector2(0.5f, 0.5f), new Vector2(ArrowW, ArrowH), new Vector2(right - 80f, arrowY));
            var ni = next.gameObject.AddComponent<Image>(); ni.sprite = Resources.Load<Sprite>("Map/arrow_right"); ni.preserveAspect = true;
            var prev = Child(page, "PrevPage", new Vector2(0.5f, 0.5f), new Vector2(ArrowW, ArrowH), new Vector2(left + 80f, arrowY));
            var pi = prev.gameObject.AddComponent<Image>(); pi.sprite = Resources.Load<Sprite>("Map/arrow_left"); pi.preserveAspect = true;
            var nb = next.gameObject.AddComponent<Button>(); nb.transition = Selectable.Transition.None; nb.onClick.AddListener(() => turner.Turn(1));
            var pb = prev.gameObject.AddComponent<Button>(); pb.transition = Selectable.Transition.None; pb.onClick.AddListener(() => turner.Turn(-1));
            turner.Controls(count, next.gameObject, prev.gameObject);
        }
        if (owned.Count > 0) cells[newest].GetComponent<Button>().onClick.Invoke();   // a new one is shown first
        // looked at: the labels stay on this open page, the book's own label goes
        if (SessionManager.Instance != null) SessionManager.Instance.ClearNewInBook();
        RefreshBookTag();
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

/// The cookie book's pages: one leaf of cookies shown at a time. A turn is a sheet of the book's own paper, hinged at
/// the spine, that carries the leaving leaf: it swings over with its free edge lifting towards the reader (a little
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

    public void Controls(TextMeshProUGUI pageCount, GameObject nextArrow, GameObject prevArrow) { count = pageCount; next = nextArrow; prev = prevArrow; Show(); }

    private void Show()
    {
        if (count != null) count.text = (current + 1) + " / " + leaves.Count;
        if (next != null) next.SetActive(current < leaves.Count - 1);
        if (prev != null) prev.SetActive(current > 0);
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
        var carried = leaves[forward ? current : to];     // the leaf on the turning sheet
        var under = leaves[forward ? to : current];       // the leaf that lies flat
        under.gameObject.SetActive(true); carried.gameObject.SetActive(true);

        // the shadow the sheet throws on the page under it, just past its free edge
        var cast = Layer("TurnShadow", page, Ramp); var castRt = cast.rectTransform;
        castRt.anchorMin = castRt.anchorMax = new Vector2(0.5f, 0.5f); castRt.pivot = new Vector2(0f, 0.5f); castRt.localScale = new Vector3(-1f, 1f, 1f);   // dark at the sheet's edge, fading away to the right
        castRt.SetSiblingIndex(under.GetSiblingIndex() + 1);

        // the sheet: the book's paper over the grid's area, hinged on its left edge
        var paperSprite = Resources.Load<Sprite>("Cookies/book_paper");
        var sheetImg = Layer("TurningSheet", page, paperSprite); var sheet = sheetImg.rectTransform;
        sheet.anchorMin = sheet.anchorMax = new Vector2(0.5f, 0.5f); sheet.pivot = new Vector2(0f, 0.5f);
        sheet.sizeDelta = grid.size; sheet.anchoredPosition = new Vector2(grid.xMin, grid.center.y);
        sheetImg.color = paperSprite != null ? Color.white : paper; sheetImg.raycastTarget = true;   // no taps through a turning page
        var bend = sheet.gameObject.AddComponent<PageBend>();
        sheet.SetSiblingIndex(castRt.GetSiblingIndex() + 1);
        carried.SetParent(sheet, true);
        var shade = Layer("Shade", sheet, Ramp); var shadeRt = shade.rectTransform;
        shadeRt.anchorMin = Vector2.zero; shadeRt.anchorMax = Vector2.one; shadeRt.offsetMin = shadeRt.offsetMax = Vector2.zero;
        var shadeBend = shade.gameObject.AddComponent<PageBend>();
        var rim = Layer("Rim", sheet, null); var rimRt = rim.rectTransform;
        rimRt.anchorMin = new Vector2(1f, 0f); rimRt.anchorMax = new Vector2(1f, 1f); rimRt.pivot = new Vector2(1f, 0.5f); rimRt.sizeDelta = new Vector2(10f, 0f); rimRt.anchoredPosition = Vector2.zero;

        var clip = Resources.Load<AudioClip>("UI/page_flip"); if (clip == null) clip = Resources.Load<AudioClip>("Audio/paper_flip");
        if (AudioManager.Instance != null) { if (clip != null) AudioManager.Instance.PlaySFX(clip); else AudioManager.Instance.PlayButtonClick(); }

        for (var t = 0f; t < Seconds; t += Mathf.Min(Time.unscaledDeltaTime, 0.05f))   // a hitch never skips the turn
        {
            var k = Mathf.SmoothStep(0f, 1f, t / Seconds); if (!forward) k = 1f - k;     // 0: flat on the page, 1: standing on its edge at the spine
            var flat = Mathf.Cos(k * Mathf.PI * 0.5f);                                   // the sheet's width as seen from above
            var up = Mathf.Sin(k * Mathf.PI);                                            // how far it is off the page: most half way
            sheet.localScale = new Vector3(Mathf.Max(0.001f, flat), 1f, 1f);
            bend.Lift = shadeBend.Lift = 0.07f * Mathf.Sin(k * Mathf.PI * 0.5f);          // the free edge comes towards the reader
            shade.color = new Color(0.30f, 0.18f, 0.08f, 0.5f * Mathf.Sin(k * Mathf.PI * 0.5f));   // none while it lies flat
            rim.color = new Color(1f, 1f, 1f, 0.75f * up);
            castRt.sizeDelta = new Vector2(30f + 150f * up, grid.height * (1f + 0.10f * up));
            castRt.anchoredPosition = new Vector2(grid.xMin + grid.width * flat + castRt.sizeDelta.x, grid.center.y);
            cast.color = new Color(0.25f, 0.14f, 0.06f, 0.34f * up);
            yield return null;
        }
        carried.SetParent(page, true); carried.localScale = Vector3.one; carried.anchoredPosition = Vector2.zero;
        carried.SetSiblingIndex(under.GetSiblingIndex());
        Destroy(sheet.gameObject); Destroy(cast.gameObject);
        current = to;
        for (var i = 0; i < leaves.Count; i++) leaves[i].gameObject.SetActive(i == current);
        Show(); turning = false;
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
