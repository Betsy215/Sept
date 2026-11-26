using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Three proposed looks for the Daily Treats card, as a preview (owner, 26 November 2026: "on daily treats modal, I want
/// animation / effect to make it more clear which sticker we are picking up and which ones already picked up etc.";
/// "show me demos of the proposed daily treats modal"). Off in the game: only used when DailyTreats.PreviewStyle is
///  1 Stamp card: collected notes keep their colour under a tilted red stamp; today's note breathes and rocks over a soft
///    glow; later notes are pale and smaller. Collect thumps the stamp down, shakes the card, a coin pops out of the note
///    and flies to the +50, the trail fills up to today.
///  2 Peel and take: collected notes keep only a dotted outline where the coin was; today's coin hops every 2 s; later
///    coins are pale. Collect peels the coin off, it arcs down to the +50 with a sprinkle burst.
///  3 Spotlight and trail: a dot on the trail under every note (gold and ticked, pulsing for today, empty for later);
///    every note but today's under a cream veil. Collect flips today's note to its ticked back, its dot turns gold and
///    the trail slides one step; on the last day the gift shakes and opens.
/// Laid over the paper card's pieces after DailyTreats.ShowOnPaper; measured from the note pictures (340 x 460, shown
/// at 140 x 190, today's 16 % bigger): the coin's middle sits 18 px below the note's middle (13 px above on today's).
/// </summary>
public class DailyTreatsPreview : MonoBehaviour
{
    private int style, day;
    private RectTransform canvas;
    private RectTransform todayNote; private float todayTilt; private Vector2 todayCoinAt;
    private Image glow, todayCoin, todayRing, fill;
    private GameObject todayBack;
    private float fillTo, idleT; private bool collected;
    private Vector2 coinBase;

    private static readonly float[] Tilt = { 3f, -5f, 2f, -3f, 4f };
    private static readonly Color[] Paper = { C(190, 228, 212), C(251, 235, 169), C(197, 221, 241), C(250, 208, 212) };
    private static readonly Color Brown = new Color(0.36f, 0.20f, 0.10f), Gold = new Color(1f, 0.80f, 0.30f), GoldRim = new Color(0.82f, 0.52f, 0.16f);
    private static readonly Color Cream = new Color(1f, 0.975f, 0.91f), StampRed = new Color(1f, 1f, 1f, 0.95f);
    private static readonly Color Pink = new Color(0.92f, 0.65f, 0.66f), Mint = new Color(0.49f, 0.78f, 0.67f), Yellow = new Color(0.96f, 0.75f, 0.33f);
    private static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

    public static DailyTreatsPreview Dress(RectTransform canvas, int day, int style)
    {
        var p = canvas.gameObject.AddComponent<DailyTreatsPreview>();
        p.canvas = canvas; p.day = day; p.style = style;
        p.Build();
        return p;
    }

    private RectTransform Find(string name) => canvas.Find(name) as RectTransform;

    /// The trail's fill as far as the dot under note d (the trail runs from x -330 to 270)
    private static float FillAt(float x) => Mathf.Clamp01((x + 330f) / 600f);

    private void Build()
    {
        fill = Find("TrailFill") != null ? Find("TrailFill").GetComponent<Image>() : null;
        for (var d = 1; d <= DailyTreats.RowDays; d++)
        {
            var note = Find("Note" + d); if (note == null) continue;
            var img = note.GetComponent<Image>(); var tick = Find("Tick" + d);
            var done = d < day; var today = d == day; var tilt = Tilt[(d - 1) % Tilt.Length];
            var x = note.anchoredPosition.x; var coinY = today ? 6.2f : -7.4f;   // the coin's middle in the note
            var coinAt = note.anchoredPosition + new Vector2(0f, coinY);
            var paper = Paper[(d - 1) % Paper.Length];
            if (today) { todayNote = note; todayTilt = tilt; todayCoinAt = coinAt; }
            switch (style)
            {
                case 1:   // Stamp card
                    if (done)
                    {
                        img.color = Color.white;
                        var st = Img("Stamp" + d, canvas, Resources.Load<Sprite>("Journal/day_stamp"), StampRed);
                        Place(st.rectTransform, coinAt.x, coinAt.y, 100f, 100f); st.rectTransform.localRotation = Quaternion.Euler(0, 0, d % 2 == 0 ? 12f : -14f);
                        if (tick != null) { st.transform.SetSiblingIndex(tick.GetSiblingIndex()); Place(tick, coinAt.x, coinAt.y, 58f, 58f); }
                    }
                    else if (today)
                    {
                        glow = Img("Glow", canvas, Load("glow"), new Color(1f, 1f, 1f, 0.7f));
                        Place(glow.rectTransform, x, note.anchoredPosition.y, 214f, 290f);
                        glow.rectTransform.localRotation = note.localRotation;
                        glow.transform.SetSiblingIndex(note.GetSiblingIndex());
                    }
                    else { img.color = new Color(1f, 1f, 1f, 0.55f); note.localScale = Vector3.one * 0.9f; }
                    break;
                case 2:   // Peel and take
                    if (done)
                    {
                        img.color = Color.white;
                        Child(note, "Cover", Disc, paper, 0f, coinY, 102f, 102f);
                        Child(note, "Outline", DottedRing, new Color(Brown.r, Brown.g, Brown.b, 0.45f), 0f, coinY, 86f, 86f);
                        if (tick != null) Place(tick, coinAt.x, coinAt.y, 60f, 60f);
                    }
                    else if (today && day < DailyTreats.RowDays)
                    {
                        Child(note, "Cover", Disc, paper, 0f, coinY, 100f, 100f);
                        Child(note, "Outline", DottedRing, new Color(Brown.r, Brown.g, Brown.b, 0.45f), 0f, coinY, 86f, 86f);
                        todayCoin = Child(note, "TodayCoin", Load("glyph_coin"), Color.white, 0f, coinY, 92f, 92f);
                        coinBase = todayCoin.rectTransform.anchoredPosition;
                    }
                    else if (!today)
                    {
                        var gift = d == DailyTreats.RowDays;
                        Child(note, "Pale", Disc, new Color(paper.r, paper.g, paper.b, 0.62f), 0f, gift ? -6f : coinY, gift ? 120f : 102f, gift ? 120f : 102f);
                    }
                    break;
                case 3:   // Spotlight and trail
                    if (!today)
                    {
                        img.color = Color.white;
                        Child(note, "Veil", RoundRect, new Color(Cream.r, Cream.g, Cream.b, 0.66f), 0f, 0f, 150f, 200f);
                    }
                    else
                    {
                        todayBack = new GameObject("Back", typeof(RectTransform)); todayBack.transform.SetParent(note, false);
                        var back = (RectTransform)todayBack.transform; Place(back, 0f, 0f, 10f, 10f);
                        Child(back, "Cover", RoundRect, paper, 0f, -16f, 104f, 138f);
                        Child(back, "BigTick", Load("tick"), Color.white, 0f, -12f, 84f, 84f);
                        todayBack.SetActive(false);
                    }
                    // the dot on the trail under this note
                    if (done) { Dot(x, true); var t = Img("DotTick" + d, canvas, Load("tick"), Color.white); Place(t.rectTransform, x, -88f, 28f, 28f); }
                    else
                    {
                        if (today) { todayRing = Img("TodayRing", canvas, Ring, GoldRim); Place(todayRing.rectTransform, x, -88f, 40f, 40f); }
                        Dot(x, false);
                    }
                    break;
            }
        }
        // the trail fill lines up with the notes (1 and 3): as far as the last collected note
        if (fill != null && style != 2)
        {
            var prev = Find("Note" + (day - 1)); fill.fillAmount = prev != null ? FillAt(prev.anchoredPosition.x) : 0f;
            fillTo = todayNote != null ? FillAt(todayNote.anchoredPosition.x) : fill.fillAmount;
        }
    }

    private void Dot(float x, bool gold)
    {
        var rim = Img("DotRim", canvas, Disc, gold ? GoldRim : new Color(0.78f, 0.66f, 0.50f)); Place(rim.rectTransform, x, -88f, gold ? 40f : 30f, gold ? 40f : 30f);
        var dot = Img("Dot", canvas, Disc, gold ? Gold : Cream); Place(dot.rectTransform, x, -88f, gold ? 32f : 22f, gold ? 32f : 22f);
    }

    // ---- idle motion ----

    private void Update()
    {
        var dt = Time.deltaTime; idleT += dt;
        if (!collected)
        {
            if (style == 1 && todayNote != null)
            {
                todayNote.localScale = Vector3.one * (1.02f + 0.02f * Mathf.Sin(idleT * Mathf.PI * 2f / 1.6f));
                todayNote.localRotation = Quaternion.Euler(0, 0, todayTilt + 2.2f * Mathf.Sin(idleT * Mathf.PI * 2f / 2.6f));
                if (glow != null) { glow.color = new Color(1f, 1f, 1f, 0.55f + 0.25f * Mathf.Sin(idleT * Mathf.PI * 2f / 1.6f)); glow.rectTransform.localRotation = todayNote.localRotation; }
            }
            else if (style == 2 && todayCoin != null)
            {
                var ph = (idleT + 1.4f) % 2f;   // the first hop 0.6 s after the card opens, then every 2 s
                var rt = todayCoin.rectTransform;
                if (ph < 0.5f)
                {
                    var k = ph / 0.5f;
                    rt.anchoredPosition = coinBase + Vector2.up * (Mathf.Sin(k * Mathf.PI) * 20f);
                    var squash = k > 0.85f ? Mathf.Sin((k - 0.85f) / 0.15f * Mathf.PI) * 0.1f : 0f;
                    rt.localScale = new Vector3(1f + squash, 1f - squash, 1f);
                }
                else { rt.anchoredPosition = coinBase; rt.localScale = Vector3.one; }
            }
            else if (style == 3 && todayRing != null)
            {
                var k = (idleT % 1.2f) / 1.2f;
                todayRing.rectTransform.localScale = Vector3.one * (1f + 0.8f * k);
                todayRing.color = new Color(GoldRim.r, GoldRim.g, GoldRim.b, 0.95f * (1f - k));
            }
        }
        UpdatePieces(dt);
    }

    // ---- Collect ----

    public IEnumerator Collect(Image reward, Image sparkles, Image gift)
    {
        collected = true;
        if (style == 1) yield return StampCollect(reward, sparkles, gift);
        else if (style == 2) yield return PeelCollect(reward, sparkles, gift);
        else yield return FlipCollect(reward, sparkles, gift);
    }

    private IEnumerator StampCollect(Image reward, Image sparkles, Image gift)
    {
        // the note settles; the stamp drops from 1.6x and lands with a thump
        var s0 = todayNote != null ? todayNote.localScale.x : 1f; var r0 = todayNote != null ? Signed(todayNote.localEulerAngles.z) : 0f;
        var stamp = Img("StampToday", canvas, Resources.Load<Sprite>("Journal/day_stamp"), new Color(1f, 1f, 1f, 0f));
        Place(stamp.rectTransform, todayCoinAt.x, todayCoinAt.y, 108f, 108f); stamp.rectTransform.localRotation = Quaternion.Euler(0, 0, -12f);
        yield return Tween(0.17f, k =>
        {
            var e = k * k;
            stamp.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, e);
            stamp.color = new Color(1f, 1f, 1f, Mathf.Clamp01(k * 2.5f) * 0.95f);
            if (todayNote != null) { todayNote.localScale = Vector3.one * Mathf.Lerp(s0, 1f, k); todayNote.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(r0, todayTilt, k)); }
            if (glow != null) glow.color = new Color(1f, 1f, 1f, Mathf.Lerp(glow.color.a, 0f, k));
        });
        Sfx("UI/button_crunch_go"); Haptics.Tap();
        StartCoroutine(Shake(0.24f, 9f));
        var tick = Img("TickToday", canvas, Load("tick"), Color.white); Place(tick.rectTransform, todayCoinAt.x, todayCoinAt.y, 60f, 60f);
        StartCoroutine(PopIn(tick.transform, 0.06f, 1f, 0.22f));
        yield return Tween(0.16f, k => stamp.rectTransform.localScale = Vector3.one * (1f - 0.08f * Mathf.Sin(k * Mathf.PI)));
        if (glow != null) glow.gameObject.SetActive(false);
        yield return Wait(0.08f);

        // the coin pops out of the note and flies to the +50
        var target = RewardCoinAt(reward);
        var coin = Img("FlyCoin", canvas, Load("glyph_coin"), Color.white); Place(coin.rectTransform, todayCoinAt.x, todayCoinAt.y, 92f, 92f);
        Sfx("Audio/pop");
        yield return Tween(0.14f, k => { coin.rectTransform.localScale = Vector3.one * Back(k) * 1.15f; coin.rectTransform.anchoredPosition = todayCoinAt + Vector2.up * (30f * k); });
        var from = coin.rectTransform.anchoredPosition;
        yield return Tween(0.42f, k =>
        {
            var e = k * k * (3f - 2f * k);
            coin.rectTransform.anchoredPosition = Bezier(from, (from + target) / 2f + Vector2.up * 150f, target, e);
            coin.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1.07f, e);
            coin.rectTransform.localRotation = Quaternion.Euler(0, 0, -25f * Mathf.Sin(e * Mathf.PI));
        });
        Destroy(coin.gameObject);
        if (gift != null) gift.gameObject.SetActive(false);
        ShowReward(reward, false);
        sparkles.gameObject.SetActive(true); StartCoroutine(PopIn(sparkles.transform, 0f, 1f, 0.3f));
        Burst(target, Sprites.Sparkle, 8, 260f, 420f, 0f, 0.55f, 22f, 36f, new[] { Gold, Color.white, Cream }, 0f);
        CoinSound();
        if (fill != null) { var f0 = fill.fillAmount; StartCoroutine(Tween(0.4f, k => fill.fillAmount = Mathf.Lerp(f0, fillTo, 1f - (1f - k) * (1f - k)))); }
        yield return Wait(0.55f);
    }

    private IEnumerator PeelCollect(Image reward, Image sparkles, Image gift)
    {
        var target = RewardCoinAt(reward);
        if (todayCoin == null) { yield return GiftOpen(gift); ShowReward(reward, true); Burst(target, Sprites.Sprinkle, 18, 380f, 760f, -1500f, 0.95f, 26f, 40f, new[] { Pink, Mint, Yellow, Cream }, 380f); CoinSound(); yield return Wait(0.8f); yield break; }
        var coin = todayCoin.rectTransform;
        coin.SetParent(canvas, true); coin.SetAsLastSibling(); coin.localScale = Vector3.one;
        var p0 = coin.anchoredPosition; var r0 = Signed(coin.localEulerAngles.z);
        Sfx("Audio/pop");
        // peel: up off the note, a little bigger
        yield return Tween(0.18f, k =>
        {
            var e = 1f - (1f - k) * (1f - k);
            coin.anchoredPosition = p0 + Vector2.up * (34f * e); coin.localScale = Vector3.one * Mathf.Lerp(1f, 1.22f, e);
            coin.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(r0, -10f, e));
        });
        // the note keeps its dotted outline and gets the tick
        var tick = Img("TickToday", canvas, Load("tick"), Color.white); Place(tick.rectTransform, todayCoinAt.x, todayCoinAt.y, 60f, 60f);
        StartCoroutine(PopIn(tick.transform, 0.1f, 1f, 0.22f));
        var from = coin.anchoredPosition;
        yield return Tween(0.46f, k =>
        {
            var e = k * k * (3f - 2f * k);
            coin.anchoredPosition = Bezier(from, new Vector2((from.x + target.x) / 2f, from.y + 110f), target, e);
            coin.localScale = Vector3.one * Mathf.Lerp(1.22f, 1.07f, e);
            coin.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-10f, 0f, e) + 18f * Mathf.Sin(e * Mathf.PI * 2f));
        });
        Destroy(coin.gameObject);
        if (gift != null) gift.gameObject.SetActive(false);
        ShowReward(reward, true);
        Burst(target, Sprites.Sprinkle, 18, 380f, 760f, -1500f, 0.95f, 26f, 40f, new[] { Pink, Mint, Yellow, Cream }, 380f);
        Burst(target, Sprites.Dot, 6, 200f, 360f, -1100f, 0.7f, 14f, 22f, new[] { new Color(0.86f, 0.66f, 0.42f) }, 250f);
        CoinSound();
        yield return Wait(0.7f);
    }

    private IEnumerator FlipCollect(Image reward, Image sparkles, Image gift)
    {
        if (gift != null) yield return GiftOpen(gift);
        Sfx("Audio/paper_flip");
        if (todayNote != null)
        {
            yield return Tween(0.13f, k => todayNote.localScale = new Vector3(1f - k * k, 1f + 0.05f * k, 1f));
            if (todayBack != null) todayBack.SetActive(true);
            yield return Tween(0.16f, k => { var e = 1f - (1f - k) * (1f - k); todayNote.localScale = new Vector3(e, 1.05f - 0.05f * e, 1f); });
            todayNote.localScale = Vector3.one;
        }
        // today's dot turns gold, the trail slides one step
        var x = todayNote != null ? todayNote.anchoredPosition.x : 0f;
        if (todayRing != null) todayRing.gameObject.SetActive(false);
        var rim = Img("DotRimToday", canvas, Disc, GoldRim); Place(rim.rectTransform, x, -88f, 40f, 40f);
        var dot = Img("DotToday", canvas, Disc, Gold); Place(dot.rectTransform, x, -88f, 32f, 32f);
        var t = Img("DotTickToday", canvas, Load("tick"), Color.white); Place(t.rectTransform, x, -88f, 28f, 28f);
        foreach (var p in new Transform[] { rim.transform, dot.transform, t.transform }) StartCoroutine(PopIn(p, 0f, 1f, 0.24f));
        if (fill != null) { var f0 = fill.fillAmount; StartCoroutine(Tween(0.38f, k => fill.fillAmount = Mathf.Lerp(f0, fillTo, 1f - (1f - k) * (1f - k)))); }
        Burst(new Vector2(x, -88f), Sprites.Sparkle, 6, 200f, 320f, 0f, 0.45f, 18f, 28f, new[] { Gold, Color.white }, 0f);
        yield return Wait(0.18f);
        ShowReward(reward, false);
        sparkles.gameObject.SetActive(true); StartCoroutine(PopIn(sparkles.transform, 0f, 1f, 0.3f));
        CoinSound();
        yield return Wait(0.8f);
    }

    /// The last day's gift: shakes harder and harder, then opens in a burst
    private IEnumerator GiftOpen(Image gift)
    {
        if (gift == null) yield break;
        var rt = gift.rectTransform;
        yield return Tween(0.5f, k => { rt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * Mathf.PI * 10f) * 12f * k); rt.localScale = Vector3.one * (1f + 0.08f * k); });
        Sfx("Audio/pop");
        Burst(rt.anchoredPosition, Sprites.Sprinkle, 16, 380f, 720f, -1400f, 0.9f, 26f, 40f, new[] { Pink, Mint, Yellow, Cream }, 360f);
        gift.gameObject.SetActive(false);
    }

    /// The +50 shows: the coin at once, the letters pop (hop = they also jump up one after another)
    private void ShowReward(Image reward, bool hop)
    {
        reward.gameObject.SetActive(true);
        var i = 0;
        foreach (Transform g in reward.transform)
        {
            if (!g.name.StartsWith("G")) continue;
            StartCoroutine(PopIn(g, 0.05f * i, 1f, 0.28f));
            if (hop) StartCoroutine(Hop(g as RectTransform, 0.12f + 0.06f * i, 26f));
            i++;
        }
    }

    private Vector2 RewardCoinAt(Image reward)
    {
        var c = reward.transform.Find("Coin");
        return (Vector2)canvas.InverseTransformPoint((c != null ? c : reward.transform).position);
    }

    // ---- small motions ----

    private static float Signed(float a) => a > 180f ? a - 360f : a;
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
        yield return Tween(seconds, k => { if (tr != null) tr.localScale = Vector3.one * (to * Back(k)); });
    }

    private static IEnumerator Hop(RectTransform rt, float delay, float height)
    {
        if (rt == null) yield break;
        var start = rt.anchoredPosition;
        if (delay > 0f) yield return Wait(delay);
        yield return Tween(0.3f, k => { if (rt != null) rt.anchoredPosition = start + Vector2.up * (Mathf.Sin(k * Mathf.PI) * height); });
    }

    /// The whole card shakes a little (everything but the dim behind it)
    private IEnumerator Shake(float seconds, float amp)
    {
        var parts = new List<RectTransform>(); var bases = new List<Vector2>();
        foreach (RectTransform c in canvas) { if (c.name == "Dim") continue; parts.Add(c); bases.Add(c.anchoredPosition); }
        yield return Tween(seconds, k =>
        {
            var off = new Vector2(Mathf.Sin(k * 38f), Mathf.Cos(k * 29f) * 0.6f) * amp * (1f - k);
            for (var i = 0; i < parts.Count; i++) if (parts[i] != null) parts[i].anchoredPosition = bases[i] + off;
        });
    }

    // ---- sprinkles and sparkles (the same shapes as StarCelebration's, drawn in code) ----

    private class Piece { public RectTransform rt; public Image img; public Vector2 v; public float spin, age, life, gravity; public Color c; }
    private readonly List<Piece> pieces = new List<Piece>();

    private void Burst(Vector2 at, Sprite sprite, int n, float vMin, float vMax, float gravity, float life, float sMin, float sMax, Color[] colours, float up)
    {
        for (var i = 0; i < n; i++)
        {
            var a = Random.Range(0f, Mathf.PI * 2f); var sp = Random.Range(vMin, vMax); var s = Random.Range(sMin, sMax);
            var img = Img("Bit", canvas, sprite, colours[i % colours.Length]);
            Place(img.rectTransform, at.x, at.y, s, s);
            img.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            pieces.Add(new Piece { rt = img.rectTransform, img = img, v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp + Vector2.up * up, spin = Random.Range(-360f, 360f), life = life * Random.Range(0.8f, 1.15f), gravity = gravity, c = img.color });
        }
    }

    private void UpdatePieces(float dt)
    {
        for (var i = pieces.Count - 1; i >= 0; i--)
        {
            var p = pieces[i];
            if (p.rt == null) { pieces.RemoveAt(i); continue; }
            p.age += dt;
            if (p.age >= p.life) { Destroy(p.rt.gameObject); pieces.RemoveAt(i); continue; }
            p.v.y += p.gravity * dt; p.v *= Mathf.Max(0f, 1f - 1.6f * dt);
            p.rt.anchoredPosition += p.v * dt;
            p.rt.localRotation = Quaternion.Euler(0, 0, p.rt.localEulerAngles.z + p.spin * dt);
            var k = p.age / p.life;
            p.img.color = new Color(p.c.r, p.c.g, p.c.b, p.c.a * (k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f));
        }
    }

    // ---- pieces ----

    private static void Sfx(string path)
    {
        var clip = Resources.Load<AudioClip>(path);
        if (clip != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clip);
    }

    private static void CoinSound() { if (AudioManager.Instance != null) AudioManager.Instance.PlayMoneyTransferComplete(); }

    private static Sprite Load(string name) => Resources.Load<Sprite>("DailyTreats/" + name);

    private static Image Img(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>(); img.sprite = sprite; img.color = color; img.raycastTarget = false;
        return img;
    }

    private static Image Child(Transform parent, string name, Sprite sprite, Color color, float x, float y, float w, float h)
    {
        var img = Img(name, parent, sprite, color); Place(img.rectTransform, x, y, w, h); return img;
    }

    private static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
    }

    private static Sprite disc, ring, dotted, roundRect;
    private static Sprite Disc => disc ??= Sprites.Make(128, 128, (x, y) => Mathf.Clamp01((1f - Mathf.Sqrt(x * x + y * y)) * 7f));
    private static Sprite Ring => ring ??= Sprites.Make(96, 96, (x, y) => Mathf.Clamp01(1f - Mathf.Abs(Mathf.Sqrt(x * x + y * y) - 0.84f) / 0.11f));
    private static Sprite DottedRing => dotted ??= Sprites.Make(128, 128, (x, y) =>
    {
        var r = Mathf.Sqrt(x * x + y * y); var band = Mathf.Clamp01(1f - Mathf.Abs(r - 0.88f) / 0.07f);
        var dash = Mathf.Clamp01(Mathf.Cos(Mathf.Atan2(y, x) * 18f) * 3f + 0.4f);
        return band * dash;
    });
    private static Sprite RoundRect => roundRect ??= Sprites.Make(96, 128, (x, y) =>
    {
        // soft-edged rounded box filling the square -1..1
        const float rad = 0.18f; var qx = Mathf.Max(Mathf.Abs(x) - (1f - rad - 0.06f), 0f); var qy = Mathf.Max(Mathf.Abs(y) - (1f - rad - 0.06f), 0f);
        var d = Mathf.Sqrt(qx * qx + qy * qy) - rad;
        return Mathf.Clamp01(-d * 16f + 0.5f);
    });

    private static class Sprites
    {
        private static Sprite sparkle, sprinkle, dot;
        public static Sprite Sparkle => sparkle ??= Make(64, 64, (x, y) => { var ax = Mathf.Abs(x); var ay = Mathf.Abs(y); var d = Mathf.Pow(ax, 0.5f) + Mathf.Pow(ay, 0.5f); return Mathf.Clamp01((0.95f - d) * 6f) + 0.35f * Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y) / 0.45f); });
        public static Sprite Dot => dot ??= Make(32, 32, (x, y) => Mathf.Clamp01((1f - Mathf.Sqrt(x * x + y * y)) * 5f));
        public static Sprite Sprinkle => sprinkle ??= Make(48, 48, (x, y) => { var ex = Mathf.Max(0f, Mathf.Abs(x) - 0.55f); var d = Mathf.Sqrt(ex * ex + y * y); return Mathf.Clamp01((0.3f - d) * 14f); });

        /// A white picture whose alpha is f(x, y), x and y from -1 to 1
        public static Sprite Make(int w, int h, System.Func<float, float, float> alpha)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[w * h];
            for (var j = 0; j < h; j++)
                for (var i = 0; i < w; i++)
                    px[j * w + i] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha((i + 0.5f) / w * 2f - 1f, (j + 0.5f) / h * 2f - 1f)));
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
