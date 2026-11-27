using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Four proposed animations for the day-end card, as a preview (owner, 26 November 2026: "for day end card animation,
/// show me these on report"). Off in the game: LevelManager hands the card's money part over to this only when
/// PreviewStyle is not 0. In every look the day's amounts stay written (today they count down to $ 0 into Earned), the
/// stars finish before the money starts, and a "3 stars +$ 100" line shows when the three-star bonus was paid.
///  1 A. Coin cookies into the hen: the lines pop in; coin cookies jump out of each amount and arc down to the hen
///       painted at the bottom of the page; each landing bumps the hen and ticks the ribbon's total up. The bonus is a
///       big coin dropping from the stars.
///  2 B. Printed receipt with a stamp: a receipt strip prints down over the page line by line, ends in "TODAY $ n",
///       a round red stamp thumps on it, then the ribbon's total rolls up once.
///  3 C. Hand-written tally: each line writes itself in the journal's hand (Delius), gets underlined; "Today $ n" is
///       written and circled in red; the total counts up gently.
///  4 D. Sticker lines: each line slaps on as a taped paper slip (the bonus slip with a star cookie); the slips pulse
///       in turn as the total jumps up in three steps.
///  5 E. Slips, hand and hen: C + A + D in one (the owner, 26 November 2026: "i like hand writing as well as the flying
///       to chicken effect, combine both and redesign", then "i like the sticker lines effect too, try to add this as
///       well"): each line slaps on as a taped slip (mint, yellow, pink with the star cookie), its words and amount write
///       themselves on it in the journal's hand, then coin cookies hop out of the amount into the page's hen (one coin per
///       $ 100 or so, two to four), the hen bumps and Earned ticks up with each landing; the bonus line's big coin drops
///       from the stars; "Today $ n" is written last and circled in red. The amounts stay written.
/// Coordinates are the panel's (the PopupCanvas, 1080 x 1920 reference): the page (paper.png, 984 x 1456) is shown at
/// 860 x 1272.5 around (-14, 215); its painted hen sits at (-252, -217), the ribbon from y -19 to 135.
/// Replay(style) re-runs the card from its stars for the frame-by-frame recordings (style 0 = the card as it is).
/// </summary>
public class DayEndPreview : MonoBehaviour
{
    public static int PreviewStyle = 0;
    /// Set when a look (or a replay) has run to its end; the recordings stop on it.
    public static bool Finished;

    private LevelManager lm;
    private RectTransform root;
    private TextMeshProUGUI sale, tip, total, bonusLine;
    private float saleAmt, tipAmt, bonusAmt, wallet, shown;
    private Image hen; private Vector2 henRest;

    private static readonly Color Ink = new Color(0.36f, 0.20f, 0.09f), Red = new Color(0.83f, 0.20f, 0.18f);
    private static readonly Color Mint = C(190, 228, 212), Yellow = C(251, 235, 169), Blue = C(197, 221, 241), Pinkish = C(250, 208, 212);
    private static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);
    private const float RowX = -14f;
    // the rows: today's size, 90 apart (C keeps them closer, its "Today" line goes under them)
    private static bool Tight => PreviewStyle == 3 || PreviewStyle == 5;
    private static float SaleY => PreviewStyle == 5 ? 432f : Tight ? 420f : 400f;
    private static float TipY => PreviewStyle == 5 ? 350f : Tight ? 345f : 310f;
    private static float BonusY => PreviewStyle == 5 ? 268f : Tight ? 270f : 220f;
    private static float RowSize => Tight ? 54f : 58f;
    private static readonly Vector2 HenAt = new Vector2(-252f, -215f), HenTop = new Vector2(-250f, -170f);

    // the rows as the game leaves them (StyleDayEnd), restored before every replay
    private static bool cached; private static TMP_FontAsset rowFont; private static Material rowMat; private static Color totalColor;

    // ---------------------------------------------------------------- entry points

    /// From LevelManager.ShowLevelCompletePopup (PreviewStyle 1-4): the card's money part.
    /// The day's stars, set by LevelManager when the day ends: the bonus line names them (the star bonus is paid for one,
    /// two and three stars since 26 November 2026)
    public static int StarsToday = 3;
    private static string StarsLabel => StarsToday == 1 ? "1 star" : StarsToday + " stars";

    public static IEnumerator Play(LevelManager lm, Coroutine stars, float sale, float tips, float bonus, float wallet)
    {
        var p = Prepare(lm, sale, tips, bonus, wallet);
        p.Hide();
        if (stars != null) yield return stars;   // the stars finish before the money starts
        yield return new WaitForSeconds(0.25f);
        switch (PreviewStyle)
        {
            case 1: yield return p.CoinsIntoHen(); break;
            case 2: yield return p.Receipt(); break;
            case 3: yield return p.Tally(); break;
            case 4: yield return p.Stickers(); break;
            case 5: yield return p.WrittenIntoHen(); break;
        }
        Finished = true;
    }

    /// Editor recordings: the card re-runs from its stars with the day's own numbers. Style 0 = the card as it is today.
    public static string Replay(int style) => Replay(style, -1f, -1f, -1f);

    /// The same, with the money of an earlier recorded day (sale, tips, wallet; -1 = the day's own), so a look recorded
    /// on another three-star day shows the same numbers as the others.
    public static string Replay(int style, float useSale, float useTips, float useWallet)
    {
        var lm = FindObjectOfType<LevelManager>(); if (lm == null || lm.levelCompletePanel == null) return "no card";
        var row = GameObject.Find("StarsRow"); if (row == null) return "no stars row";
        const System.Reflection.BindingFlags BF = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        lm.StopAllCoroutines();
        var rays = row.transform.Find("Rays"); if (rays != null) DestroyImmediate(rays.gameObject);
        foreach (Transform s in row.transform)
        {
            var g = s.Find("Gold"); if (g == null) continue;
            g.localScale = Vector3.zero; g.localRotation = Quaternion.identity;
            var sh = g.Find("Shine"); if (sh != null) DestroyImmediate(sh.gameObject);
        }
        var fx = row.GetComponentInParent<Canvas>().rootCanvas.transform.Find("StarFx"); if (fx != null) DestroyImmediate(fx.gameObject);
        var sm = lm.scoreManager;
        var stars = sm != null && sm.starProgressBar != null ? sm.starProgressBar.GetEarnedStarsCount() : 0;
        float sale = sm.PaidSaleToday, tips = sm.PaidTipsToday, bonus = (int)typeof(LevelManager).GetField("threeStarBonusPaid", BF).GetValue(lm);
        var wallet = SessionManager.Instance.GetTotalScore();
        if (useSale >= 0f) sale = useSale; if (useTips >= 0f) tips = useTips; if (useWallet >= 0f) wallet = useWallet;
        var co = lm.StartCoroutine((IEnumerator)typeof(LevelManager).GetMethod("PopStars", BF).Invoke(lm, new object[] { row.transform, stars }));
        typeof(LevelManager).GetField("starsPop", BF).SetValue(lm, co);
        PreviewStyle = style; Finished = false;
        lm.StartCoroutine(style == 0 ? Today(lm, sale, tips, bonus, wallet) : Play(lm, co, sale, tips, bonus, wallet));
        return $"replay {style}: stars {stars} sale {sale} tips {tips} bonus {bonus} wallet {wallet}";
    }

    /// Style 0 for the recordings: what ShowLevelCompletePopup does today (the amounts count down into Earned).
    private static IEnumerator Today(LevelManager lm, float sale, float tips, float bonus, float wallet)
    {
        var p = Prepare(lm, sale, tips, bonus, wallet);
        p.bonusLine.gameObject.SetActive(false);
        Row(p.sale, SaleY, 0); p.sale.rectTransform.anchoredPosition = new Vector2(RowX, 352f); p.sale.fontSize = 58f;
        Row(p.tip, TipY, 0); p.tip.rectTransform.anchoredPosition = new Vector2(RowX, 242f); p.tip.fontSize = 58f;
        p.sale.transform.localScale = p.tip.transform.localScale = Vector3.one;
        if (p.hen != null) p.hen.gameObject.SetActive(false);
        yield return new WaitForSeconds(1f);
        yield return lm.StartCoroutine(lm.AnimateFullTransfer(p.sale, p.total, "Order Sale:", "Earned:", sale, wallet - tips - bonus));
        yield return new WaitForSeconds(1f);
        yield return lm.StartCoroutine(lm.AnimateFullTransfer(p.tip, p.total, "Tips:", "Earned:", tips, wallet - bonus));
        if (bonus > 0) LevelManager.SetMoneyLine(p.total, "Earned:", Money.Format(wallet));
        Finished = true;
    }

    /// Puts the card back to its start for this look: rows restored and filled, a fresh layer for the pieces.
    private static DayEndPreview Prepare(LevelManager lm, float sale, float tips, float bonus, float wallet)
    {
        var panel = lm.levelCompletePanel.transform;
        foreach (var n in new[] { "DayEndPreview", "DayEndSlips" }) { var old = panel.Find(n); if (old != null) DestroyImmediate(old.gameObject); }
        var go = new GameObject("DayEndPreview", typeof(RectTransform));
        go.transform.SetParent(panel, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var p = go.AddComponent<DayEndPreview>();
        p.lm = lm; p.root = rt; p.sale = lm.todaySale; p.tip = lm.todayTip; p.total = lm.totalEarned;
        p.saleAmt = sale; p.tipAmt = tips; p.bonusAmt = bonus; p.wallet = wallet;
        if (!cached) { cached = true; rowFont = p.sale.font; rowMat = p.sale.fontSharedMaterial; totalColor = p.total.color; }
        foreach (var l in new[] { p.sale, p.tip })
        {
            l.font = rowFont; l.fontSharedMaterial = rowMat; l.color = Ink; l.maxVisibleCharacters = 99999;
            l.transform.localScale = Vector3.one; l.transform.localRotation = Quaternion.identity; l.gameObject.SetActive(true);
        }
        p.total.color = totalColor; p.total.transform.localScale = Vector3.one;
        LevelManager.SetMoneyLine(p.sale, "Today Sale:", Money.Format(sale));
        LevelManager.SetMoneyLine(p.tip, "Tips Earned:", Money.Format(tips));
        p.shown = Mathf.Max(0f, wallet - sale - tips - bonus);
        LevelManager.SetMoneyLine(p.total, "Earned:", Money.Format(p.shown));
        // the bonus row: a copy of the tips row, under it
        var b = p.tip.transform.parent.Find("BonusLine"); if (b != null) DestroyImmediate(b.gameObject);
        p.bonusLine = Instantiate(p.tip.gameObject, p.tip.transform.parent).GetComponent<TextMeshProUGUI>();
        p.bonusLine.name = "BonusLine"; p.bonusLine.transform.SetSiblingIndex(p.tip.transform.GetSiblingIndex() + 1);
        LevelManager.SetMoneyLine(p.bonusLine, StarsLabel, "+" + Money.Format(bonus));
        Row(p.sale, SaleY, RowSize); Row(p.tip, TipY, RowSize); Row(p.bonusLine, BonusY, RowSize);
        p.bonusLine.gameObject.SetActive(bonus > 0);
        // the page's own hen, cut out so it can bump (A)
        var paper = panel.Find("FinalPaper");
        var henSprite = paper != null ? HenCut(paper.GetComponent<Image>().sprite) : null;
        if (henSprite != null)
        {
            p.hen = Img("Hen", rt, henSprite, Color.white);
            p.hen.rectTransform.pivot = new Vector2(0.5f, 0.1f);
            Place(p.hen.rectTransform, HenAt.x, HenAt.y - 0.4f * HenH, HenW, HenH, new Vector2(0.5f, 0.1f));
            p.henRest = p.hen.rectTransform.anchoredPosition;
            p.hen.gameObject.SetActive(PreviewStyle == 1 || PreviewStyle == 5);
        }
        return p;
    }

    /// What waits for the stars: A's rows pop in later, B's are on the receipt, C's are written, D's slap on.
    private void Hide()
    {
        foreach (var r in new[] { sale, tip, bonusLine })
        {
            if (PreviewStyle == 1) r.transform.localScale = Vector3.zero;
            else if (Tight) r.maxVisibleCharacters = 0;
            else r.gameObject.SetActive(false);
        }
    }

    private static void Row(TextMeshProUGUI l, float y, float size)
    {
        if (l == null) return;
        l.rectTransform.anchoredPosition = new Vector2(RowX, y);
        if (size > 0f) l.fontSize = size;
    }

    // ---------------------------------------------------------------- A. coin cookies into the hen

    private IEnumerator CoinsIntoHen()
    {
        StartCoroutine(PopIn(sale.transform, 0f, 1f, 0.3f));
        yield return PopIn(tip.transform, 0.15f, 1f, 0.3f);
        yield return Wait(0.15f);
        yield return CoinsFrom(sale, saleAmt, SaleY);
        yield return Wait(0.1f);
        yield return CoinsFrom(tip, tipAmt, TipY);
        if (bonusAmt > 0)
        {
            yield return Wait(0.2f);
            Sfx("Audio/pop");
            yield return PopIn(bonusLine.transform, 0f, 1f, 0.3f);
            // the big coin drops from the stars
            var landed = false;
            StartCoroutine(Coin(new Vector2(RowX, 560f), 0f, 0.85f, 130f, (int)bonusAmt, 160f, () => landed = true, true));
            while (!landed) yield return null;
        }
        yield return Wait(0.2f);
        CoinSound();
    }

    private IEnumerator CoinsFrom(TextMeshProUGUI row, float amount, float y)
    {
        var whole = Mathf.FloorToInt(amount + 0.0001f);
        var n = Mathf.Clamp(Mathf.RoundToInt(amount / 70f), 4, 7);
        var landed = 0;
        StartCoroutine(Pulse(row.transform, 0.08f, 0.3f));
        for (var i = 0; i < n; i++)
        {
            var share = whole * (i + 1) / n - whole * i / n;
            StartCoroutine(Coin(new Vector2(250f + Random.Range(-20f, 20f), y), i * 0.11f, 0.62f, 70f, share, 220f, () => landed++, false));
        }
        while (landed < n) yield return null;
    }

    private IEnumerator Coin(Vector2 from, float delay, float seconds, float size, int share, float arc, System.Action onLand, bool big)
    {
        if (delay > 0f) yield return Wait(delay);
        var c = Img("Coin", root, Resources.Load<Sprite>("DailyTreats/glyph_coin"), Color.white);
        Place(c.rectTransform, from.x, from.y, size, size);
        var to = HenTop + new Vector2(Random.Range(-18f, 18f), 0f);
        var mid = (from + to) * 0.5f + Vector2.up * arc + (big ? Vector2.zero : Vector2.right * 60f);
        if (!big) Sfx("Audio/pop");
        var spin = big ? 0f : Random.Range(-540f, -300f);
        yield return Tween(seconds, k =>
        {
            var e = big ? k * k : k;
            c.rectTransform.anchoredPosition = Bezier(from, mid, to, e);
            var s = big ? Mathf.Lerp(1.15f, 0.8f, k) : (k < 0.2f ? Mathf.Lerp(0.5f, 1.1f, k / 0.2f) : Mathf.Lerp(1.1f, 0.7f, (k - 0.2f) / 0.8f));
            c.rectTransform.localScale = Vector3.one * s;
            c.rectTransform.localRotation = Quaternion.Euler(0, 0, spin * k);
        });
        Destroy(c.gameObject);
        shown += share; LevelManager.SetMoneyLine(total, "Earned:", Money.Format(shown));
        StartCoroutine(Pulse(total.transform, big ? 0.16f : 0.07f, 0.22f));
        StartCoroutine(HenBump(big ? 0.22f : 0.1f));
        Burst(to + Vector2.up * 10f, Sparkle, big ? 14 : 4, 120f, big ? 520f : 300f, -400f, big ? 0.9f : 0.5f, 18f, big ? 46f : 30f,
            new[] { new Color(1f, 0.85f, 0.35f), new Color(1f, 0.97f, 0.8f), new Color(0.98f, 0.62f, 0.66f) }, big ? 160f : 80f);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMoneyCount();
        onLand();
    }

    private IEnumerator HenBump(float amount)
    {
        if (hen == null) yield break;
        var rt = hen.rectTransform;
        yield return Tween(0.24f, k =>
        {
            var s = Mathf.Sin(k * Mathf.PI);
            rt.localScale = new Vector3(1f + amount * 0.6f * s, 1f + amount * s - amount * 0.5f * Mathf.Sin(k * Mathf.PI * 2f) * (1f - k), 1f);
            rt.anchoredPosition = henRest + Vector2.up * (amount * 60f * s);
        });
        rt.localScale = Vector3.one; rt.anchoredPosition = henRest;
    }

    // ---------------------------------------------------------------- B. printed receipt with a stamp

    private const float RcW = 520f, RcTop = 474f;

    private IEnumerator Receipt()
    {
        sale.gameObject.SetActive(false); tip.gameObject.SetActive(false); bonusLine.gameObject.SetActive(false);
        var src = Resources.Load<Sprite>("UI/receipt_paper"); if (src == null) yield break;
        var k = RcW / src.rect.width;
        // the strip: a mask that grows down from the printer's edge, the paper inside it
        var holder = new GameObject("Receipt", typeof(RectTransform)).GetComponent<RectTransform>();
        holder.SetParent(root, false); Place(holder, RowX, RcTop, RcW + 40f, 0f, new Vector2(0.5f, 1f));
        holder.localRotation = Quaternion.Euler(0, 0, -1.5f);
        holder.gameObject.AddComponent<RectMask2D>();
        var paper = new GameObject("Paper", typeof(RectTransform)).GetComponent<RectTransform>();
        paper.SetParent(holder, false); paper.anchorMin = paper.anchorMax = new Vector2(0.5f, 1f); paper.pivot = new Vector2(0.5f, 1f);
        paper.anchoredPosition = Vector2.zero;
        // pieces of the receipt picture (958 x 1042): the torn top with its dashes, plain paper, the dash line, the torn bottom
        float topH = 120f * k, dashH = 28f * k, botH = 82f * k;
        var lines = new List<(string words, string money, float size)> { ("Order sale", Money.Format(saleAmt), 44f), ("Tips", Money.Format(tipAmt), 44f) };
        if (bonusAmt > 0) lines.Add((StarsLabel, "+" + Money.Format(bonusAmt), 44f));
        const float lineH = 54f, todayH = 78f;
        var y = topH - 12f;
        var stops = new List<float> { topH };
        var texts = new List<TextMeshProUGUI>();
        foreach (var l in lines) { texts.Add(ReceiptText(paper, l.words, l.money, l.size, y + lineH * 0.5f)); y += lineH; stops.Add(y + 4f); }
        y += 8f; var dashY = y; y += dashH + 4f; stops.Add(y);
        var todayY = y + todayH * 0.5f;
        var today = ReceiptText(paper, "TODAY", Money.Format(saleAmt + tipAmt + bonusAmt), 58f, todayY); y += todayH;
        var paperH = y + 10f + botH; stops.Add(paperH);
        paper.sizeDelta = new Vector2(RcW, paperH);
        var top = Piece(paper, src, 0f, 120f, 0f, topH); top.transform.SetAsFirstSibling();
        var mid = Piece(paper, src, 420f, 40f, topH, paperH - topH - botH); mid.transform.SetSiblingIndex(1);
        var dash = Piece(paper, src, 728f, 28f, dashY, dashH); dash.transform.SetSiblingIndex(2);
        var bottom = Piece(paper, src, 960f, 82f, paperH - botH, botH); bottom.transform.SetSiblingIndex(3);
        // print: the strip grows a line at a time
        var shownH = 0f;
        foreach (var s in stops)
        {
            var from = shownH; var to = s;
            Sfx("UI/button_crunch_tap");
            yield return Tween(0.16f, e => holder.sizeDelta = new Vector2(RcW + 40f, Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, e))));
            shownH = to;
            yield return Wait(0.14f);
        }
        yield return Wait(0.2f);
        // the stamp thumps on
        var stamp = Img("Stamp", root, Resources.Load<Sprite>("Journal/day_stamp"), new Color(1f, 1f, 1f, 0f));
        var stampAt = new Vector2(RowX + 10f, RcTop - todayY - 8f);
        Place(stamp.rectTransform, stampAt.x, stampAt.y, 136f, 136f);
        stamp.rectTransform.localRotation = Quaternion.Euler(0, 0, -14f);
        var word = Text(stamp.transform, "PAID", 38f, Red, BeachFont(), 0f, 0f, 130f);
        word.rectTransform.localRotation = Quaternion.identity;
        yield return Tween(0.13f, e =>
        {
            stamp.rectTransform.localScale = Vector3.one * Mathf.Lerp(2.4f, 1f, e * e);
            stamp.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, 0.95f, e)); word.alpha = Mathf.Lerp(0f, 0.95f, e);
        });
        Sfx("Audio/pop"); Haptics.Tap();
        Burst(stampAt, Dot, 10, 160f, 320f, -300f, 0.45f, 8f, 16f, new[] { new Color(0.83f, 0.2f, 0.18f, 0.8f) }, 0f);
        yield return Shake(holder, 0.3f, 10f);
        yield return Wait(0.3f);
        // the total rolls up once
        yield return RollTotal(wallet, 1.1f);
    }

    private TextMeshProUGUI ReceiptText(RectTransform paper, string words, string money, float size, float centreY)
    {
        var t = Text(paper, "", size, Ink, BeachFont(), 0f, -centreY, RcW - 70f);
        t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        t.alignment = TextAlignmentOptions.Center;
        t.text = "<align=left>" + words + "<line-height=0>\n<align=right>" + money + "<line-height=1em>";
        return t;
    }

    /// A band of the receipt picture (rows from its top, in its pixels), laid at y from the paper's top
    private static Image Piece(RectTransform paper, Sprite src, float fromTop, float rows, float y, float h)
    {
        var r = src.rect;   // the whole picture (textureRect is trimmed)
        var s = Sprite.Create(src.texture, new Rect(r.x, r.y + r.height - fromTop - rows, r.width, rows), new Vector2(0.5f, 0.5f), 100f);
        var img = Img("Band", paper, s, Color.white);
        var rt = img.rectTransform; rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -y); rt.sizeDelta = new Vector2(0f, h);
        return img;
    }

    // ---------------------------------------------------------------- C. hand-written tally

    private IEnumerator Tally()
    {
        var hand = Resources.Load<TMP_FontAsset>("Fonts & Materials/Delius SDF");
        var todayLine = Instantiate(tip.gameObject, tip.transform.parent).GetComponent<TextMeshProUGUI>();
        todayLine.name = "TodayLine"; todayLine.transform.SetParent(root, true);
        var rows = new List<(TextMeshProUGUI l, string words, string money, float y)> { (sale, "Order sale", Money.Format(saleAmt), SaleY), (tip, "Tips", Money.Format(tipAmt), TipY) };
        if (bonusAmt > 0) rows.Add((bonusLine, StarsLabel, "+" + Money.Format(bonusAmt), BonusY));
        foreach (var r in rows) { Hand(r.l, hand, r.words, r.money, RowSize); r.l.maxVisibleCharacters = 0; }
        const float TodayY = 178f;
        Hand(todayLine, hand, "Today", Money.Format(saleAmt + tipAmt + bonusAmt), 62f);
        todayLine.rectTransform.anchoredPosition = new Vector2(RowX, TodayY); todayLine.maxVisibleCharacters = 0;
        foreach (var r in rows)
        {
            yield return Write(r.l, 26f);
            var u = Img("Underline", root, Stroke, new Color(Ink.r, Ink.g, Ink.b, 0.8f));
            Place(u.rectTransform, RowX, r.y - 34f, 620f, 14f);
            u.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(-0.6f, 0.6f));
            u.type = Image.Type.Filled; u.fillMethod = Image.FillMethod.Horizontal; u.fillOrigin = 0; u.fillAmount = 0f;
            yield return Tween(0.28f, e => u.fillAmount = Mathf.SmoothStep(0f, 1f, e));
            yield return Wait(0.12f);
        }
        yield return Write(todayLine, 22f);
        // circled in red round the money
        todayLine.ForceMeshUpdate();
        var circle = Img("Circle", root, Loop, new Color(Red.r, Red.g, Red.b, 0.9f));
        Place(circle.rectTransform, RowX + 222f, TodayY + 2f, 250f, 112f);
        circle.rectTransform.localRotation = Quaternion.Euler(0, 0, 4f);
        circle.type = Image.Type.Filled; circle.fillMethod = Image.FillMethod.Radial360; circle.fillOrigin = (int)Image.Origin360.Left; circle.fillClockwise = true; circle.fillAmount = 0f;
        yield return Tween(0.55f, e => circle.fillAmount = Mathf.SmoothStep(0f, 1f, e));
        yield return Wait(0.3f);
        yield return RollTotal(wallet, 1.3f);
    }

    private static void Hand(TextMeshProUGUI l, TMP_FontAsset font, string words, string money, float size)
    {
        if (font != null) { l.font = font; l.fontSharedMaterial = font.material; }
        l.fontSize = size; l.color = Ink;
        l.text = "<align=left>" + words + "<line-height=0>\n<align=right>" + money + "<line-height=1em>";
        l.transform.localScale = Vector3.one; l.gameObject.SetActive(true);
    }

    private static IEnumerator Write(TextMeshProUGUI l, float charsPerSecond)
    {
        l.ForceMeshUpdate(); var n = l.textInfo.characterCount;
        var t = 0f;
        while (l.maxVisibleCharacters < n) { t += Time.deltaTime; l.maxVisibleCharacters = Mathf.Min(n, Mathf.FloorToInt(t * charsPerSecond) + 1); yield return null; }
        l.maxVisibleCharacters = 99999;
    }

    // ---------------------------------------------------------------- E. written, then into the hen

    private IEnumerator WrittenIntoHen()
    {
        var hand = Resources.Load<TMP_FontAsset>("Fonts & Materials/Delius SDF");
        var todayLine = Instantiate(tip.gameObject, tip.transform.parent).GetComponent<TextMeshProUGUI>();
        todayLine.name = "TodayLine"; todayLine.transform.SetParent(root, true);
        var rows = new List<(TextMeshProUGUI l, string words, string money, float y, float amount, Color c, float tilt)>
            { (sale, "Order sale", Money.Format(saleAmt), SaleY, saleAmt, Mint, 1.2f), (tip, "Tips", Money.Format(tipAmt), TipY, tipAmt, Yellow, -1.1f) };
        if (bonusAmt > 0) rows.Add((bonusLine, StarsLabel, "+" + Money.Format(bonusAmt), BonusY, bonusAmt, Pinkish, 0.9f));
        var layer = new GameObject("DayEndSlips", typeof(RectTransform)).GetComponent<RectTransform>();
        layer.SetParent(sale.transform.parent, false); layer.anchorMin = Vector2.zero; layer.anchorMax = Vector2.one; layer.offsetMin = layer.offsetMax = Vector2.zero;
        layer.SetSiblingIndex(sale.transform.GetSiblingIndex());   // under the rows' words
        foreach (var r in rows) { Hand(r.l, hand, r.words, r.money, RowSize); r.l.maxVisibleCharacters = 0; }
        const float TodayY = 178f;
        Hand(todayLine, hand, "Today", Money.Format(saleAmt + tipAmt + bonusAmt), 62f);
        todayLine.rectTransform.anchoredPosition = new Vector2(RowX, TodayY); todayLine.maxVisibleCharacters = 0;
        var flying = 0;
        foreach (var r in rows)
        {
            // the slip slaps on, the words write themselves on it
            var slip = Img("Slip", layer, RoundRect, new Color(r.c.r, r.c.g, r.c.b, 0f)); slip.type = Image.Type.Sliced;
            Place(slip.rectTransform, RowX - 6f, r.y, 690f, 70f);
            foreach (var tx in new[] { -322f, 326f })
            {
                var tape = Img("Tape", slip.transform, Rect, new Color(1f, 0.98f, 0.9f, 0.72f));
                Place(tape.rectTransform, tx, tx < 0f ? 24f : -22f, 84f, 28f); tape.rectTransform.localRotation = Quaternion.Euler(0, 0, -24f);
            }
            if (r.l == bonusLine)
            {
                var star = Img("Star", slip.transform, StarSprite(), Color.white);
                Place(star.rectTransform, -100f, 4f, 56f, 56f); star.preserveAspect = true;
            }
            var st = slip.rectTransform; var l = r.l; var c = r.c; var tilt = r.tilt;
            l.transform.localRotation = Quaternion.Euler(0, 0, tilt);
            Sfx("Audio/pop");
            yield return Tween(0.18f, e =>
            {
                st.localScale = Vector3.one * Mathf.Lerp(1.55f, 1f, e * e);
                st.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(tilt + 9f, tilt, e));
                slip.color = new Color(c.r, c.g, c.b, e);
            });
            Haptics.Tap();
            StartCoroutine(Squash(st, l.transform));
            Burst(st.anchoredPosition + new Vector2(-330f, 0f), Dot, 5, 120f, 260f, -200f, 0.35f, 8f, 14f, new[] { new Color(1f, 1f, 1f, 0.9f) }, 0f);
            yield return Wait(0.08f);
            yield return Write(r.l, 38f);
            if (r.l == bonusLine)
            {
                // the bonus: one big coin from the stars
                flying++;
                StartCoroutine(Coin(new Vector2(RowX, 560f), 0.05f, 0.7f, 120f, Mathf.FloorToInt(r.amount + 0.0001f), 150f, () => flying--, true));
                yield return Wait(0.3f);
                continue;
            }
            var whole = Mathf.FloorToInt(r.amount + 0.0001f);
            if (whole <= 0) { yield return Wait(0.15f); continue; }
            var n = Mathf.Clamp(Mathf.RoundToInt(r.amount / 100f), 2, 4);
            if (whole < 10) n = 1;
            StartCoroutine(Pulse(r.l.transform, 0.05f, 0.25f));
            for (var i = 0; i < n; i++)
            {
                flying++;
                var share = whole * (i + 1) / n - whole * i / n;
                StartCoroutine(Coin(new Vector2(250f + Random.Range(-16f, 16f), r.y), i * 0.09f, 0.5f, 64f, share, 200f, () => flying--, false));
            }
            yield return Wait(0.22f);   // the next line starts while these coins are still in the air
        }
        while (flying > 0) yield return null;
        if (Mathf.Abs(shown - wallet) > 0.5f) { shown = wallet; LevelManager.SetMoneyLine(total, "Earned:", Money.Format(shown)); }
        yield return Wait(0.12f);
        yield return Write(todayLine, 30f);
        var circle = Img("Circle", root, Loop, new Color(Red.r, Red.g, Red.b, 0.9f));
        Place(circle.rectTransform, RowX + 222f, TodayY + 2f, 250f, 112f);
        circle.rectTransform.localRotation = Quaternion.Euler(0, 0, 4f);
        circle.type = Image.Type.Filled; circle.fillMethod = Image.FillMethod.Radial360; circle.fillOrigin = (int)Image.Origin360.Left; circle.fillClockwise = true; circle.fillAmount = 0f;
        yield return Tween(0.45f, e => circle.fillAmount = Mathf.SmoothStep(0f, 1f, e));
        StartCoroutine(Pulse(total.transform, 0.12f, 0.28f));
        StartCoroutine(HenBump(0.12f));
        CoinSound();
        yield return Wait(0.3f);
    }

    // ---------------------------------------------------------------- D. sticker lines

    private IEnumerator Stickers()
    {
        var layer = new GameObject("DayEndSlips", typeof(RectTransform)).GetComponent<RectTransform>();
        layer.SetParent(sale.transform.parent, false); layer.anchorMin = Vector2.zero; layer.anchorMax = Vector2.one; layer.offsetMin = layer.offsetMax = Vector2.zero;
        layer.SetSiblingIndex(sale.transform.GetSiblingIndex());   // under the rows' words
        var rows = new List<(TextMeshProUGUI l, float y, Color c, float tilt, float amount)> { (sale, SaleY, Mint, 1.2f, saleAmt), (tip, TipY, Yellow, -1.1f, tipAmt) };
        if (bonusAmt > 0) rows.Add((bonusLine, BonusY, Pinkish, 0.9f, bonusAmt));
        var slips = new List<RectTransform>();
        foreach (var r in rows)
        {
            var slip = Img("Slip", layer, RoundRect, r.c); slip.type = Image.Type.Sliced;
            Place(slip.rectTransform, RowX - 6f, r.y, 690f, 74f);
            var tape = Img("Tape", slip.transform, Rect, new Color(1f, 0.98f, 0.9f, 0.72f));
            Place(tape.rectTransform, -322f, 26f, 84f, 28f); tape.rectTransform.localRotation = Quaternion.Euler(0, 0, -24f);
            var tape2 = Img("Tape", slip.transform, Rect, new Color(1f, 0.98f, 0.9f, 0.72f));
            Place(tape2.rectTransform, 326f, -24f, 84f, 28f); tape2.rectTransform.localRotation = Quaternion.Euler(0, 0, -24f);
            if (r.l == bonusLine)
            {
                var star = Img("Star", slip.transform, StarSprite(), Color.white);
                Place(star.rectTransform, -92f, 4f, 60f, 60f); star.preserveAspect = true;
            }
            slip.gameObject.SetActive(false); r.l.gameObject.SetActive(false);
            slips.Add(slip.rectTransform);
        }
        for (var i = 0; i < rows.Count; i++)
        {
            var slip = slips[i]; var l = rows[i].l; var tilt = rows[i].tilt;
            slip.gameObject.SetActive(true); l.gameObject.SetActive(true);
            var img = slip.GetComponent<Image>(); var c = rows[i].c;
            Sfx("Audio/pop");
            yield return Tween(0.2f, e =>
            {
                var s = Mathf.Lerp(1.55f, 1f, e * e);
                slip.localScale = l.transform.localScale = Vector3.one * s;
                slip.localRotation = l.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(tilt + 9f, tilt, e));
                img.color = new Color(c.r, c.g, c.b, e); l.alpha = e;
            });
            Haptics.Tap();
            StartCoroutine(Squash(slip, l.transform));
            Burst(slip.anchoredPosition + new Vector2(-330f, 0f), Dot, 5, 120f, 260f, -200f, 0.35f, 8f, 14f, new[] { new Color(1f, 1f, 1f, 0.9f) }, 0f);
            yield return Wait(0.32f);
        }
        yield return Wait(0.25f);
        for (var i = 0; i < rows.Count; i++)
        {
            StartCoroutine(Pulse(slips[i], 0.1f, 0.34f)); StartCoroutine(Pulse(rows[i].l.transform, 0.1f, 0.34f));
            yield return Wait(0.12f);
            shown += Mathf.Floor(rows[i].amount + 0.0001f); LevelManager.SetMoneyLine(total, "Earned:", Money.Format(shown));
            StartCoroutine(Pulse(total.transform, 0.16f, 0.26f));
            Burst(new Vector2(200f, 60f), Sparkle, 8, 160f, 380f, -260f, 0.6f, 18f, 34f, new[] { new Color(1f, 0.85f, 0.35f), new Color(1f, 0.97f, 0.8f) }, 60f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMoneyCount();
            yield return Wait(0.5f);
        }
        if (Mathf.Abs(shown - wallet) > 0.5f) { shown = wallet; LevelManager.SetMoneyLine(total, "Earned:", Money.Format(shown)); }
        CoinSound();
    }

    private static IEnumerator Squash(Transform a, Transform b)
    {
        yield return Tween(0.18f, e =>
        {
            var s = Mathf.Sin(e * Mathf.PI) * (1f - e);
            var v = new Vector3(1f + 0.05f * s, 1f - 0.08f * s, 1f);
            a.localScale = v; b.localScale = v;
        });
    }

    // ---------------------------------------------------------------- shared motions

    private IEnumerator RollTotal(float to, float seconds)
    {
        var from = shown; var nextTick = 0f;
        yield return Tween(seconds, e =>
        {
            shown = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, e));
            LevelManager.SetMoneyLine(total, "Earned:", Money.Format(e >= 1f ? to : shown));
            if (e >= nextTick) { nextTick += 0.34f; if (AudioManager.Instance != null) AudioManager.Instance.PlayMoneyCount(); }
        });
        shown = to;
        StartCoroutine(Pulse(total.transform, 0.14f, 0.28f));
        Burst(new Vector2(200f, 60f), Sparkle, 10, 160f, 420f, -260f, 0.7f, 18f, 36f, new[] { new Color(1f, 0.85f, 0.35f), new Color(1f, 0.97f, 0.8f) }, 60f);
        CoinSound();
    }

    private static IEnumerator Pulse(Transform t, float amount, float seconds)
    {
        if (t == null) yield break;
        yield return Tween(seconds, k => { if (t != null) t.localScale = Vector3.one * (1f + amount * Mathf.Sin(k * Mathf.PI)); });
    }

    private static IEnumerator Shake(RectTransform t, float seconds, float amp)
    {
        var b = t.anchoredPosition;
        yield return Tween(seconds, k => t.anchoredPosition = b + new Vector2(Mathf.Sin(k * 40f), Mathf.Cos(k * 31f) * 0.6f) * amp * (1f - k));
        t.anchoredPosition = b;
    }

    private static float Back(float k) { const float c = 1.7f; var m = k - 1f; return 1f + (c + 1f) * m * m * m + c * m * m; }
    private static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float t) => (1 - t) * (1 - t) * a + 2 * (1 - t) * t * b + t * t * c;
    private static IEnumerator Wait(float s) { var t = 0f; while (t < s) { t += Time.deltaTime; yield return null; } }

    private static IEnumerator Tween(float seconds, System.Action<float> f)
    {
        var t = 0f;
        while (t < seconds) { t += Time.deltaTime; f(Mathf.Clamp01(t / seconds)); yield return null; }
        f(1f);
    }

    private static IEnumerator PopIn(Transform tr, float delay, float to, float seconds)
    {
        if (tr == null) yield break;
        tr.localScale = Vector3.zero;
        if (delay > 0f) yield return Wait(delay);
        Sfx("Audio/pop");
        yield return Tween(seconds, k => { if (tr != null) tr.localScale = Vector3.one * (to * Back(k)); });
    }

    // sparkles and dots, flying out and fading
    private class Bit { public RectTransform rt; public Image img; public Vector2 v; public float spin, age, life, gravity; public Color c; }
    private readonly List<Bit> bits = new List<Bit>();

    private void Burst(Vector2 at, Sprite sprite, int n, float vMin, float vMax, float gravity, float life, float sMin, float sMax, Color[] colours, float up)
    {
        for (var i = 0; i < n; i++)
        {
            var a = Random.Range(0f, Mathf.PI * 2f); var sp = Random.Range(vMin, vMax); var s = Random.Range(sMin, sMax);
            var img = Img("Bit", root, sprite, colours[i % colours.Length]);
            Place(img.rectTransform, at.x, at.y, s, s);
            bits.Add(new Bit { rt = img.rectTransform, img = img, v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp + Vector2.up * up, spin = Random.Range(-360f, 360f), life = life * Random.Range(0.8f, 1.15f), gravity = gravity, c = img.color });
        }
    }

    private void Update()
    {
        var dt = Time.deltaTime;
        for (var i = bits.Count - 1; i >= 0; i--)
        {
            var p = bits[i];
            if (p.rt == null) { bits.RemoveAt(i); continue; }
            p.age += dt;
            if (p.age >= p.life) { Destroy(p.rt.gameObject); bits.RemoveAt(i); continue; }
            p.v.y += p.gravity * dt; p.v *= Mathf.Max(0f, 1f - 1.6f * dt);
            p.rt.anchoredPosition += p.v * dt;
            p.rt.localRotation = Quaternion.Euler(0, 0, p.rt.localEulerAngles.z + p.spin * dt);
            var k = p.age / p.life;
            p.img.color = new Color(p.c.r, p.c.g, p.c.b, p.c.a * (k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f));
        }
    }

    // ---------------------------------------------------------------- pieces

    private static void Sfx(string path)
    {
        var clip = Resources.Load<AudioClip>(path);
        if (clip != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clip);
    }

    private static void CoinSound() { if (AudioManager.Instance != null) AudioManager.Instance.PlayMoneyTransferComplete(); }

    private static TMP_FontAsset BeachFont() => Resources.Load<TMP_FontAsset>("Fonts & Materials/beachday SDF");

    private static TextMeshProUGUI Text(Transform parent, string words, float size, Color colour, TMP_FontAsset font, float x, float y, float width)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        var plain = Resources.Load<Material>("Fonts & Materials/beachday SDF - Plain"); if (plain != null && font == BeachFont()) t.fontSharedMaterial = plain;
        t.text = words; t.fontSize = size; t.color = colour; t.alignment = TextAlignmentOptions.Center; t.enableWordWrapping = false; t.raycastTarget = false;
        t.overflowMode = TextOverflowModes.Overflow; t.richText = true;
        var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(width, size * 1.4f);
        return t;
    }

    private static Image Img(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>(); img.sprite = sprite; img.color = color; img.raycastTarget = false;
        return img;
    }

    private static void Place(RectTransform rt, float x, float y, float w, float h, Vector2? pivot = null)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
    }

    private static Sprite StarSprite()
    {
        var g = GameObject.Find("StarsRow");
        var gold = g != null && g.transform.childCount > 0 ? g.transform.GetChild(0).Find("Gold") : null;
        var img = gold != null ? gold.GetComponent<Image>() : null;
        return img != null && img.sprite != null ? img.sprite : Resources.Load<Sprite>("UI/star_icon");
    }

    // The hen painted on the page (paper.png pixels 100-340 across, 1100-1340 down), copied out of the page through a
    // render texture, with a soft oval edge.
    private const float PaperK = 860f / 984f, HenW = 240f * PaperK, HenH = 240f * PaperK;
    private static Sprite henCut;
    private static Sprite HenCut(Sprite paper)
    {
        if (henCut != null) return henCut;
        if (paper == null) return null;
        var tex = paper.texture; var tr = paper.rect; var f = tr.width / 984f;   // rect: the whole picture (textureRect is trimmed)
        int w = Mathf.RoundToInt(240 * f), h = Mathf.RoundToInt(240 * f), x0 = Mathf.RoundToInt(tr.x + 100 * f), y0 = Mathf.RoundToInt(tr.y + tr.height - 1340 * f);
        var rtex = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(tex, rtex);
        var prev = RenderTexture.active; RenderTexture.active = rtex;
        var cut = new Texture2D(w, h, TextureFormat.RGBA32, false);
        cut.ReadPixels(new Rect(x0, y0, w, h), 0, 0); cut.Apply();
        RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rtex);
        // a soft oval round the hen: the paper at its edge fades into the page, so the bump shows no box
        var px = cut.GetPixels32();
        for (var q = 0; q < px.Length; q++)
        {
            float ex = (q % w + 0.5f) / w * 2f - 1f, ey = (q / w + 0.5f) / h * 2f - 1f;
            var r = Mathf.Sqrt(ex * ex + ey * ey);
            px[q].a = (byte)(255 * Mathf.Clamp01((1f - r) / 0.16f));
        }
        cut.SetPixels32(px); cut.Apply();
        henCut = Sprite.Create(cut, new Rect(0, 0, w, h), new Vector2(0.5f, 0.1f), 100f);
        return henCut;
    }

    private static Sprite stroke, loop, roundRect, rect, sparkle, dot;
    private static Sprite Sparkle => sparkle ??= Make(64, 64, (x, y) => { var ax = Mathf.Abs(x); var ay = Mathf.Abs(y); var d = Mathf.Pow(ax, 0.5f) + Mathf.Pow(ay, 0.5f); return Mathf.Clamp01((0.95f - d) * 6f) + 0.35f * Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y) / 0.45f); });
    private static Sprite Dot => dot ??= Make(32, 32, (x, y) => Mathf.Clamp01((1f - Mathf.Sqrt(x * x + y * y)) * 5f));
    private static Sprite Rect => rect ??= Make(8, 8, (x, y) => 1f);
    /// A pen line: thicker in the middle, a slight wave, round ends
    private static Sprite Stroke => stroke ??= Make(512, 24, (x, y) =>
    {
        var c = 0.18f * Mathf.Sin(x * 2.6f + 0.4f); var half = 0.42f * (1f - 0.55f * x * x);
        var end = Mathf.Clamp01((1f - Mathf.Abs(x)) * 30f);
        return Mathf.Clamp01((half - Mathf.Abs(y - c)) * 9f) * end;
    });
    /// A hand-drawn loop: an ellipse whose end runs on past its start, a little wider
    private static Sprite Loop => loop ??= Make(320, 144, (x, y) =>
    {
        var a = Mathf.Atan2(y, x); var r = Mathf.Sqrt(x * x + y * y);
        var t = Mathf.Repeat(Mathf.PI - a, Mathf.PI * 2f) / (Mathf.PI * 2f);   // 0 at the left, going round
        var r1 = 0.86f + 0.04f * Mathf.Sin(t * Mathf.PI * 2f + 0.8f) - 0.03f * t;
        var w1 = 0.045f;
        var v = Mathf.Clamp01(1f - Mathf.Abs(r - r1) / w1);
        if (t < 0.18f) { var r2 = r1 + 0.09f * (1f - t / 0.18f); v = Mathf.Max(v, Mathf.Clamp01(1f - Mathf.Abs(r - r2) / w1) * (1f - t / 0.18f)); }
        return v;
    });
    private static Sprite RoundRect => roundRect ??= Make(48, 48, (x, y) =>
    {
        // a paper slip's corner: 8 px round, a soft edge (sliced, 16 px borders)
        var px = Mathf.Abs(x) * 24f; var py = Mathf.Abs(y) * 24f;
        var qx = Mathf.Max(px - 15f, 0f); var qy = Mathf.Max(py - 15f, 0f);
        var d = Mathf.Sqrt(qx * qx + qy * qy) - 8f;
        return Mathf.Clamp01(-d + 0.5f);
    }, 16f);

    /// A white picture whose alpha is f(x, y), x and y from -1 to 1
    private static Sprite Make(int w, int h, System.Func<float, float, float> alpha, float border = 0f)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color32[w * h];
        for (var j = 0; j < h; j++)
            for (var i = 0; i < w; i++)
                px[j * w + i] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha((i + 0.5f) / w * 2f - 1f, (j + 0.5f) / h * 2f - 1f)));
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.one * border);
    }
}
