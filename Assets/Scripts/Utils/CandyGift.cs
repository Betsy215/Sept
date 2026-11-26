using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// Day 3's gift (owner, 24 November 2026: "only show the candy jar on table and the candies in store after day 3, when
/// day 3 starts, show the candy jar with animation, and give 2 candies in the jar, mention what it does, after day 3,
/// the shop has candies as new items"). The jar is not on the table before day 3 and the shop's candy card waits until
/// day 3 is done (a card the player never saw wears NEW then). When day 3's play begins, once: the jar arrives with two
/// candies and a line saying what it does; the day waits for it. Three ways to show it (`Current`; the owner picks):
///  - Card: a paper gift card like chapter 2's, the jar on it, two candies drop in; Let's go sends the jar to the table.
///  - Drop: the jar drops onto its spot with a bounce, two candies drop in, a note beside it says what it does.
///  - Sparkle: the jar appears on its spot in a burst of sparkles and a glow, the candies pop in, a banner says what it
///    does and a hand points at the jar.
public static class CandyGift
{
    public enum Style { Card = 1, Drop = 2, Sparkle = 3 }
    public static Style Current = Style.Card;   // the owner's pick, 24 November 2026 ("use A gift card")
    public const int Day = 2;          // day index: day 3
    public const int Gift = 2;         // candies given
    public static bool PreviewMode;    // recordings: game time, no pause, Let's go pressed by itself
    public const string Words = "Tap the jar to give customer\na candy to keep them happy";   // the owner's words, 24 November 2026

    private static SessionData S => SessionManager.Instance != null ? SessionManager.Instance.GetCurrentSession() : null;

    /// The jar stands on the table from day 3 (and for anyone who already has candies or plays chapter 2)
    public static bool JarOnTable
    {
        get
        {
            var sm = SessionManager.Instance; if (sm == null) return true;
            return sm.GetCurrentLevelIndex() >= Day || sm.Candies > 0 || sm.DaysDoneIn(1) >= Day || SessionManager.CurrentChapter >= 2;
        }
    }

    /// The shop sells candies once day 3 is done
    public static bool ShopOpen
    {
        get
        {
            var sm = SessionManager.Instance; if (sm == null) return true;
            return sm.DaysDoneIn(1) >= Day + 1 || SessionManager.ChaptersOpen >= 2 || sm.Candies > 0;
        }
    }

    /// Day 3 of chapter 1, the gift not given yet
    public static bool Due
    {
        get
        {
            var s = S; var sm = SessionManager.Instance;
            // the first time day 3 is reached, the jar still empty (a replay of day 3, or candies bought under an older
            // version, would turn the gift into a loss: review, 24 November 2026)
            return s != null && sm != null && !s.candyGiftSeen && sm.GetCurrentLevelIndex() == Day && sm.DaysDoneIn(1) == Day && sm.Candies == 0;
        }
    }

    private static float Dt => PreviewMode ? Time.deltaTime : Time.unscaledDeltaTime;
    private static IEnumerator Wait(float sec) { for (var t = 0f; t < sec; t += Dt) yield return null; }
    private static float Back(float k) { const float c = 1.7f; k -= 1f; return 1f + (c + 1f) * k * k * k + c * k * k; }

    /// Hidden until the show
    public static void HideJarForShow(CandyJar jar)
    {
        if (jar == null) return;
        var sr = jar.GetComponent<SpriteRenderer>(); if (sr != null) sr.enabled = false;
        var col = jar.GetComponent<Collider>(); if (col != null) col.enabled = false;
    }

    public static IEnumerator Run()
    {
        var jar = Object.FindObjectOfType<CandyJar>(); var s = S;
        if (jar == null || s == null) yield break;
        var before = s.candies;
        var paused = !PreviewMode; var oldScale = Time.timeScale; if (paused) Time.timeScale = 0f;
        var sr = jar.GetComponent<SpriteRenderer>(); var col = jar.GetComponent<Collider>();
        var ui = Overlay();
        var block = Img("Block", ui, null, new Color(0f, 0f, 0f, 0f)); Stretch(block.rectTransform); block.raycastTarget = true;   // no taps through (the pause button)
        if (paused) KitchenClock.Pause();
        switch (Current)
        {
            case Style.Card: yield return CardShow(jar, sr, ui); break;
            case Style.Drop: yield return DropShow(jar, sr, ui); break;
            default: yield return SparkleShow(jar, sr, ui); break;
        }
        if (sr != null) sr.enabled = true; if (col != null) col.enabled = true;
        SetCandies(jar, before + Gift);
        SessionManager.Instance.MarkCandyGiftSeen();   // after the show: a show left half way comes again next time
        Object.Destroy(ui.gameObject);
        if (paused) { Time.timeScale = oldScale == 0f ? 1f : oldScale; KitchenClock.Resume(); }
    }

    /// The jar's picture only (nothing saved): the show counts the candies in
    private static void ShowCount(SpriteRenderer sr, int n) { if (sr != null) { var sp = JarSprite(n); if (sp != null) sr.sprite = sp; } }

    private static void SetCandies(CandyJar jar, int n)
    {
        if (SessionManager.Instance == null) return;
        SessionManager.Instance.SetCandies(n); jar.Refresh();
    }

    private static void Sound(string path)
    {
        StarCelebration.LastSound = path; StarCelebration.LastSoundAt = Time.time; StarCelebration.SoundCount++;   // for the preview recordings
        var c = Resources.Load<AudioClip>(path); if (c != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(c);
    }

    // ---------------------------------------------------------------- the three shows

    private static IEnumerator CardShow(CandyJar jar, SpriteRenderer sr, RectTransform ui)
    {
        ShowCount(sr, 0); if (sr != null) sr.enabled = false;
        var dim = Img("Dim", ui, null, new Color(0f, 0f, 0f, 0f)); Stretch(dim.rectTransform); dim.raycastTarget = true;   // nothing under the card takes a tap (the pause button)
        var panel = Img("Panel", ui, Resources.Load<Sprite>("Map/day_paper"), Color.white); panel.rectTransform.sizeDelta = new Vector2(780f, 820f);
        var title = Txt("Title", panel.rectTransform, Title(), 78f, Ink, "A GIFT FOR YOU!"); Place(title.rectTransform, new Vector2(0f, 262f), new Vector2(700f, 120f));
        var jarImg = Img("Jar", panel.rectTransform, JarSprite(0), Color.white); jarImg.preserveAspect = true; Place(jarImg.rectTransform, new Vector2(0f, 70f), new Vector2(250f, 250f));
        var line = Txt("Line", panel.rectTransform, Hand(), 38f, Soft, "Candies!\n" + Words); Place(line.rectTransform, new Vector2(0f, -118f), new Vector2(680f, 150f)); line.alpha = 0f;
        var letsGo = Resources.Load<Sprite>("UI/cookie_letsgo");
        var go = Img("LetsGo", panel.rectTransform, letsGo, Color.white); go.preserveAspect = true; Place(go.rectTransform, new Vector2(0f, -262f), new Vector2(340f, 122f));   // inside the paper's border (owner, 24 November 2026)
        var pressed = false; go.gameObject.AddComponent<Button>().onClick.AddListener(() => pressed = true); go.transform.localScale = Vector3.zero;
        jarImg.transform.localScale = Vector3.zero; panel.transform.localScale = Vector3.one * 0.6f;
        for (var t = 0f; t < 1f; t += Dt / 0.28f) { var k = Mathf.Clamp01(t); dim.color = new Color(0f, 0f, 0f, 0.55f * k); panel.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, Back(k)); yield return null; }
        Sound("Audio/flower_pop");
        for (var t = 0f; t < 1f; t += Dt / 0.4f) { var k = Mathf.Clamp01(t); jarImg.transform.localScale = Vector3.one * Back(k); yield return null; }
        // two candies drop in
        for (var i = 1; i <= Gift; i++)
        {
            yield return FallInto(panel.rectTransform, jarImg.rectTransform, new Vector2((i == 1 ? -40f : 40f), 330f));
            jarImg.sprite = JarSprite(i); Sound("Audio/pop"); Haptics.Tap();
            yield return Squash(jarImg.transform);
        }
        for (var t = 0f; t < 1f; t += Dt / 0.3f) { line.alpha = Mathf.Clamp01(t); go.transform.localScale = Vector3.one * Back(Mathf.Clamp01(t)); yield return null; }
        var w = 0f; var auto = PreviewMode ? 1.6f : float.MaxValue;
        while (!pressed && w < auto) { w += Dt; jarImg.transform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(w * 4f)); yield return null; }
        // the paper goes, the jar flies to its spot on the table
        var target = ScreenOf(jar.transform.position, ui); var from = jarImg.rectTransform.anchoredPosition;
        jarImg.transform.SetParent(ui, true); var start = jarImg.rectTransform.anchoredPosition; var endSize = JarScreenHeight(sr, ui);
        var group = panel.gameObject.AddComponent<CanvasGroup>();
        for (var t = 0f; t < 1f; t += Dt / 0.6f)
        {
            var k = Mathf.Clamp01(t); var e = Mathf.SmoothStep(0f, 1f, k);
            group.alpha = 1f - Mathf.Clamp01(k / 0.3f); dim.color = new Color(0f, 0f, 0f, 0.55f * (1f - k));
            jarImg.rectTransform.anchoredPosition = Vector2.Lerp(start, target, e) + new Vector2(0f, 120f * Mathf.Sin(k * Mathf.PI));
            var size = Mathf.Lerp(250f, endSize, e); jarImg.rectTransform.sizeDelta = new Vector2(size, size); jarImg.transform.localScale = Vector3.one;
            yield return null;
        }
        if (sr != null) sr.enabled = true; ShowCount(sr, Gift); Sound("Audio/pop");
        Object.Destroy(jarImg.gameObject);
        yield return WorldSquash(jar.transform);
    }

    private static IEnumerator DropShow(CandyJar jar, SpriteRenderer sr, RectTransform ui)
    {
        ShowCount(sr, 0); if (sr != null) sr.enabled = true;
        var rest = jar.transform.localPosition; var drop = rest + new Vector3(0f, 6f, 0f);
        jar.transform.localPosition = drop;
        yield return Wait(0.2f);
        for (var t = 0f; t < 1f; t += Dt / 0.45f) { var k = Mathf.Clamp01(t); jar.transform.localPosition = Vector3.LerpUnclamped(drop, rest, k * k); yield return null; }
        jar.transform.localPosition = rest; Sound("Audio/pop"); Haptics.Tap();
        yield return WorldSquash(jar.transform);
        for (var i = 1; i <= Gift; i++)
        {
            yield return FallInto(ui, null, ScreenOf(jar.transform.position, ui) + new Vector2(i == 1 ? -30f : 30f, 300f), ScreenOf(jar.transform.position, ui));
            ShowCount(sr, i); Sound("Audio/pop"); Haptics.Tap();
            yield return WorldSquash(jar.transform, 0.12f);
        }
        // the note beside the jar
        var at = ScreenOf(jar.transform.position, ui);
        var note = Img("Note", ui, Resources.Load<Sprite>("Map/day_paper"), Color.white); Place(note.rectTransform, at + new Vector2(at.x > 0f ? -300f : 300f, -210f), new Vector2(520f, 300f));
        var t1 = Txt("Line", note.rectTransform, Hand(), 30f, Soft, "2 free candies!\n" + Words); Stretch(t1.rectTransform, 40f);
        var hand = HandPointer(ui, at);
        note.transform.localScale = Vector3.zero;
        for (var t = 0f; t < 1f; t += Dt / 0.3f) { note.transform.localScale = Vector3.one * Back(Mathf.Clamp01(t)); yield return null; }
        yield return HoldUntilTap(ui, hand, at, PreviewMode ? 2.2f : 5f);
        for (var t = 0f; t < 1f; t += Dt / 0.2f) { note.transform.localScale = Vector3.one * (1f - Mathf.Clamp01(t)); yield return null; }
    }

    private static IEnumerator SparkleShow(CandyJar jar, SpriteRenderer sr, RectTransform ui)
    {
        ShowCount(sr, 0); if (sr != null) sr.enabled = true;
        var at = ScreenOf(jar.transform.position, ui);
        var glow = Img("Glow", ui, SoftGlow(), new Color(1f, 0.9f, 0.55f, 0f)); Place(glow.rectTransform, at, new Vector2(420f, 420f)); glow.transform.SetAsFirstSibling();
        var baseScale = jar.transform.localScale; jar.transform.localScale = Vector3.zero;
        Sound("Audio/Stars/chime_fin2");
        Burst(ui, at, 14);
        for (var t = 0f; t < 1f; t += Dt / 0.5f)
        {
            var k = Mathf.Clamp01(t); jar.transform.localScale = baseScale * Back(k); glow.color = new Color(1f, 0.9f, 0.55f, 0.8f * Mathf.Sin(k * Mathf.PI * 0.5f)); yield return null;
        }
        jar.transform.localScale = baseScale;
        for (var i = 1; i <= Gift; i++)
        {
            yield return Wait(0.25f); ShowCount(sr, i); Sound("Audio/Stars/chime_" + i); Burst(ui, at + new Vector2(0f, 40f), 6); yield return WorldSquash(jar.transform, 0.1f);
        }
        // the banner and the hand
        var banner = Img("Banner", ui, Resources.Load<Sprite>("Map/day_paper"), Color.white);
        banner.rectTransform.anchorMin = banner.rectTransform.anchorMax = new Vector2(0.5f, 1f); banner.rectTransform.pivot = new Vector2(0.5f, 1f);
        banner.rectTransform.anchoredPosition = new Vector2(0f, -360f); banner.rectTransform.sizeDelta = new Vector2(860f, 270f);
        var t2 = Txt("Line", banner.rectTransform, Hand(), 34f, Soft, "A gift: 2 candies!\n" + Words); Stretch(t2.rectTransform, 40f);
        banner.transform.localScale = Vector3.zero;
        var hand = HandPointer(ui, at);
        for (var t = 0f; t < 1f; t += Dt / 0.3f) { banner.transform.localScale = Vector3.one * Back(Mathf.Clamp01(t)); yield return null; }
        yield return HoldUntilTap(ui, hand, at, PreviewMode ? 2.2f : 5f, glow);
        for (var t = 0f; t < 1f; t += Dt / 0.25f) { var k = Mathf.Clamp01(t); banner.transform.localScale = Vector3.one * (1f - k); glow.color = new Color(1f, 0.9f, 0.55f, 0.8f * (1f - k)); yield return null; }
    }

    // ---------------------------------------------------------------- pieces

    private static readonly Color Ink = new Color(0.36f, 0.20f, 0.09f), Soft = new Color(0.47f, 0.31f, 0.20f);
    private static TMP_FontAsset Title() => Resources.Load<TMP_FontAsset>("Fonts & Materials/beachday SDF");
    private static TMP_FontAsset Hand() { var f = Resources.Load<TMP_FontAsset>("Fonts & Materials/Delius SDF"); return f != null ? f : Title(); }
    // ---------------------------------------------------------------- the count card (26 November 2026)
    /// A tap on the jar while arranging: a paper card with the jar and how many candies are in it (there is no upper
    /// limit any more, and the jar's picture is full from nine up). The DONE cookie or a tap beside the card closes it.
    private static bool countCardOpen;
    public static void ShowCountCard(CandyJar jar)
    {
        if (countCardOpen || jar == null) return;
        jar.StartCoroutine(CountCard());
    }
    private static IEnumerator CountCard()
    {
        countCardOpen = true;
        var ui = Overlay(); ui.gameObject.name = "CandyCount";
        var n = CandyJar.Candies; var closed = false;
        var dim = Img("Tap", ui, null, new Color(0f, 0f, 0f, 0f)); Stretch(dim.rectTransform); dim.gameObject.AddComponent<Button>().onClick.AddListener(() => closed = true);
        var panel = Img("Panel", ui, Resources.Load<Sprite>("Map/day_paper"), Color.white); panel.rectTransform.sizeDelta = new Vector2(700f, 740f); panel.raycastTarget = true;
        var title = Txt("Title", panel.rectTransform, Title(), 78f, Ink, "CANDIES"); Place(title.rectTransform, new Vector2(0f, 232f), new Vector2(620f, 110f));
        var jarImg = Img("Jar", panel.rectTransform, JarSprite(n), Color.white); jarImg.preserveAspect = true; Place(jarImg.rectTransform, new Vector2(-110f, 30f), new Vector2(250f, 250f));
        var count = Txt("Count", panel.rectTransform, Title(), 130f, Ink, "x " + n); Place(count.rectTransform, new Vector2(120f, 30f), new Vector2(330f, 170f)); count.enableWordWrapping = false;
        var ok = Img("LetsGo", panel.rectTransform, Resources.Load<Sprite>("UI/btn_done"), Color.white); ok.preserveAspect = true; Place(ok.rectTransform, new Vector2(0f, -228f), new Vector2(300f, 200f));
        ok.gameObject.AddComponent<Button>().onClick.AddListener(() => closed = true);
        Sound("Audio/flower_pop");
        panel.transform.localScale = Vector3.one * 0.6f;
        for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.22f) { var k = Mathf.Clamp01(t); dim.color = new Color(0f, 0f, 0f, 0.5f * k); panel.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, Back(k)); yield return null; }
        panel.transform.localScale = Vector3.one;
        while (!closed && ui != null) yield return null;
        if (ui != null)
        {
            var group = ui.gameObject.AddComponent<CanvasGroup>();
            for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.15f) { group.alpha = 1f - Mathf.Clamp01(t); yield return null; }
            Object.Destroy(ui.gameObject);
        }
        countCardOpen = false;
    }

    private static Sprite JarSprite(int n) => Resources.Load<Sprite>("Decor/candy_jar_" + Mathf.Clamp(n, 0, CandyJar.Full));

    private static RectTransform Overlay()
    {
        var go = new GameObject("CandyGift", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var c = go.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 900;
        var sc = go.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1080f, 1920f); sc.matchWidthOrHeight = 0.5f;
        return (RectTransform)go.transform;
    }

    private static Vector2 ScreenOf(Vector3 world, RectTransform ui)
    {
        var cam = Camera.main; if (cam == null) return Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(ui, cam.WorldToScreenPoint(world), null, out var p); return p;
    }

    private static float JarScreenHeight(SpriteRenderer sr, RectTransform ui)
    {
        if (sr == null || Camera.main == null) return 160f;
        if (sr.sprite == null) return 160f;
        var h = sr.sprite.bounds.size.y * Mathf.Abs(sr.transform.lossyScale.y); var p = sr.transform.position;
        return Mathf.Abs(ScreenOf(p + Vector3.up * h, ui).y - ScreenOf(p, ui).y);
    }

    private static IEnumerator FallInto(RectTransform parent, RectTransform jar, Vector2 from, Vector2? toOverride = null)
    {
        var candy = Img("Candy", parent, Resources.Load<Sprite>("Cookies/Candies"), Color.white); candy.preserveAspect = true;
        var to = toOverride ?? (jar != null ? jar.anchoredPosition + new Vector2(0f, 40f) : Vector2.zero);
        Place(candy.rectTransform, from, new Vector2(90f, 90f));
        for (var t = 0f; t < 1f; t += Dt / 0.35f)
        {
            var k = Mathf.Clamp01(t); candy.rectTransform.anchoredPosition = Vector2.Lerp(from, to, k * k);
            candy.transform.localRotation = Quaternion.Euler(0f, 0f, 360f * k); candy.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, k); yield return null;
        }
        Object.Destroy(candy.gameObject);
    }

    private static IEnumerator Squash(Transform t, float time = 0.18f)
    {
        var s0 = t.localScale;
        for (var x = 0f; x < 1f; x += Dt / time) { var k = Mathf.Sin(Mathf.Clamp01(x) * Mathf.PI); t.localScale = new Vector3(s0.x * (1f + 0.12f * k), s0.y * (1f - 0.12f * k), s0.z); yield return null; }
        t.localScale = s0;
    }
    private static IEnumerator WorldSquash(Transform t, float time = 0.22f) => Squash(t, time);

    private static Image HandPointer(RectTransform ui, Vector2 at)
    {
        var h = Img("Hand", ui, Resources.Load<Sprite>("Tutorial/pointer_hand"), Color.white); h.preserveAspect = true;
        Place(h.rectTransform, at + new Vector2(40f, -120f), new Vector2(150f, 150f)); return h;
    }

    private static IEnumerator HoldUntilTap(RectTransform ui, Image hand, Vector2 at, float max, Image glow = null)
    {
        var tapped = false; var catcher = Img("Tap", ui, null, new Color(0f, 0f, 0f, 0f)); Stretch(catcher.rectTransform); catcher.transform.SetAsLastSibling();
        catcher.gameObject.AddComponent<Button>().onClick.AddListener(() => tapped = true);
        var w = 0f;
        while (!tapped && w < max)
        {
            w += Dt; if (hand != null) hand.rectTransform.anchoredPosition = at + new Vector2(40f, -120f + 22f * Mathf.Sin(w * 6.5f));
            if (glow != null) { var c = glow.color; c.a = 0.55f + 0.25f * Mathf.Sin(w * 3f); glow.color = c; }
            yield return null;
        }
        if (hand != null) Object.Destroy(hand.gameObject); Object.Destroy(catcher.gameObject);
    }

    private static void Burst(RectTransform ui, Vector2 at, int n)
    {
        var host = ui.GetComponent<BurstRunner>(); if (host == null) host = ui.gameObject.AddComponent<BurstRunner>();
        for (var i = 0; i < n; i++)
        {
            var a = i / (float)n * Mathf.PI * 2f; var sp = Img("Spark", ui, Spark(), new Color(1f, 0.92f, 0.6f)); Place(sp.rectTransform, at, new Vector2(46f, 46f));
            host.Add(sp.rectTransform, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(300f, 520f));
        }
    }

    private class BurstRunner : MonoBehaviour
    {
        private readonly System.Collections.Generic.List<(RectTransform rt, Vector2 v, float age)> parts = new System.Collections.Generic.List<(RectTransform, Vector2, float)>();
        public void Add(RectTransform rt, Vector2 v) => parts.Add((rt, v, 0f));
        private void Update()
        {
            var dt = Dt;
            for (var i = parts.Count - 1; i >= 0; i--)
            {
                var p = parts[i]; if (p.rt == null) { parts.RemoveAt(i); continue; }
                p.age += dt; var k = p.age / 0.7f; if (k >= 1f) { Destroy(p.rt.gameObject); parts.RemoveAt(i); continue; }
                p.rt.anchoredPosition += p.v * dt * (1f - k); p.rt.localScale = Vector3.one * (1f - k * 0.7f); p.rt.localRotation = Quaternion.Euler(0f, 0f, 300f * k);
                var img = p.rt.GetComponent<Image>(); var c = img.color; c.a = 1f - Mathf.Clamp01((k - 0.5f) / 0.5f); img.color = c; parts[i] = p;
            }
        }
    }

    private static Sprite spark, softGlow;
    private static Sprite Spark()
    {
        if (spark != null) return spark;
        const int n = 64; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false); var px = new Color[n * n];
        for (var y = 0; y < n; y++) for (var x = 0; x < n; x++)
        {
            float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f; var d = Mathf.Pow(Mathf.Abs(u), 0.5f) + Mathf.Pow(Mathf.Abs(v), 0.5f);
            px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01((0.95f - d) * 6f) + 0.3f * Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v) / 0.45f));
        }
        tex.SetPixels(px); tex.Apply(); return spark = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }
    private static Sprite SoftGlow()
    {
        if (softGlow != null) return softGlow;
        const int n = 64; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false); var px = new Color[n * n];
        for (var y = 0; y < n; y++) for (var x = 0; x < n; x++)
        {
            float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f; var a = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v)); px[y * n + x] = new Color(1f, 1f, 1f, a * a);
        }
        tex.SetPixels(px); tex.Apply(); return softGlow = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }

    private static Image Img(string name, RectTransform parent, Sprite sprite, Color colour)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>(); img.sprite = sprite; img.color = colour; img.raycastTarget = name == "Tap" || name == "LetsGo"; return img;
    }
    private static TextMeshProUGUI Txt(string name, RectTransform parent, TMP_FontAsset font, float size, Color colour, string words)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>(); if (font != null) t.font = font; t.fontSize = size; t.color = colour; t.text = words;
        t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; t.enableWordWrapping = true; return t;
    }
    private static void Place(RectTransform rt, Vector2 pos, Vector2 size) { rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.anchoredPosition = pos; rt.sizeDelta = size; }
    private static void Stretch(RectTransform rt, float pad = 0f) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = new Vector2(pad, pad); rt.offsetMax = new Vector2(-pad, -pad); }
}
