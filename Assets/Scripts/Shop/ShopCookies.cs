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
        if (panelLook != null) { pageImg.sprite = panelLook.sprite; pageImg.type = panelLook.type; pageImg.pixelsPerUnitMultiplier = panelLook.pixelsPerUnitMultiplier; }
        pageImg.color = new Color(1f, 0.97f, 0.89f, 0.98f);   // the shop panel's cream, nearly solid so the cookies read

        var title = Label(page, "Title", "MY COOKIES", titleFont, 78f, new Vector2(0f, H / 2f - 90f), new Vector2(W - 60f, 110f));
        title.color = Ink;

        // as many columns as it takes for every owned cookie to fit above the description (18 can be owned; on an
        // iPad four columns hold only 16)
        const float infoH = 330f;
        var gridTop = H / 2f - 170f; var gridBottom = -H / 2f + infoH + 60f;
        var columns = 4; float cell;
        while (true)
        {
            cell = (W - 80f) / columns;
            var rows = Mathf.Max(1, Mathf.CeilToInt(owned.Count / (float)columns));
            if (rows * cell <= gridTop - gridBottom || columns >= 7) break;
            columns++;
        }
        var icon = cell * 0.86f;
        // the chosen thing: its cookie, its name and what it does, on the lower part of the page
        var chosen = Child(page, "Chosen", new Vector2(0.5f, 0.5f), new Vector2(230f, 230f), new Vector2(-W / 2f + 170f, -H / 2f + infoH / 2f + 30f));
        var chosenImg = chosen.gameObject.AddComponent<Image>(); chosenImg.preserveAspect = true; chosenImg.raycastTarget = false; chosenImg.enabled = false;
        var name = Label(page, "Name", "", titleFont, 60f, new Vector2(140f, -H / 2f + infoH - 40f), new Vector2(W - 360f, 90f));
        name.color = Ink; name.alignment = TextAlignmentOptions.Left;
        var info = Label(page, "Info", "", textFont != null ? textFont : titleFont, 50f, new Vector2(140f, -H / 2f + infoH / 2f - 30f), new Vector2(W - 360f, 200f));
        info.color = Ink; info.alignment = TextAlignmentOptions.TopLeft; info.enableWordWrapping = true; info.lineSpacing = -14f;
        var rule = Child(page, "Rule", new Vector2(0.5f, 0.5f), new Vector2(W - 120f, 4f), new Vector2(0f, -H / 2f + infoH + 40f));
        var ruleImg = rule.gameObject.AddComponent<Image>(); ruleImg.color = new Color(Ink.r, Ink.g, Ink.b, 0.25f); ruleImg.raycastTarget = false;

        var cells = new List<RectTransform>(); var newest = owned.Count - 1;
        for (var i = 0; i < owned.Count; i++)
        {
            var e = owned[i];
            var pos = new Vector2(-W / 2f + 40f + cell * (i % columns + 0.5f), gridTop - cell * (i / columns + 0.5f));
            var c = Child(page, "Owned" + i, new Vector2(0.5f, 0.5f), new Vector2(icon, icon), pos);
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
            var x = Child(page, "Close", new Vector2(0.5f, 0.5f), new Vector2(96f, 96f), new Vector2(W / 2f - 76f, H / 2f - 82f));
            var xi = x.gameObject.AddComponent<Image>(); xi.sprite = cross; xi.preserveAspect = true;
            x.gameObject.AddComponent<Button>().onClick.AddListener(CloseBook);
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
