using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// The day-end card's stars celebrate (owner, 24 November 2026: "I want to see different effect include visual and sound
/// when player getting one star, two star, or three star, each one should be more rewarding feeling"). Three proposals,
/// each growing with the stars earned; `Current` picks one (Classic = the plain pop of 14 November).
///  - Chimes: each star rings a higher bell note and bursts sparkles in a glow ring; two stars twinkle together; three
///    stars light golden rays behind the middle star, rain tiny gold stars and play a fanfare.
///  - Cookie: each star lands with a crumbly pop and a burst of sprinkles in the game's pink, mint and yellow; two stars
///    float hearts up; three stars add a sprinkle shower, a shine over the stars and a jingle.
///  - Fireworks: each star bursts like a firework; two stars send one firework over the card; three stars send three
///    (mint, pink, gold), confetti and a big chord.
/// Everything is drawn in a layer over the card (no taps taken) that goes away with the card. Sounds: Resources/Audio/Stars.
public static class StarCelebration
{
    public enum Style { Classic = 0, Chimes = 1, Cookie = 2, Fireworks = 3 }
    public static Style Current = Style.Cookie;   // the owner's pick, 24 November 2026 ("B. Cookie sprinkles")

    private static readonly Color Gold = new Color(1f, 0.83f, 0.33f), Cream = new Color(1f, 0.97f, 0.88f);
    private static readonly Color Pink = new Color(0.92f, 0.65f, 0.66f), Mint = new Color(0.49f, 0.78f, 0.67f), Yellow = new Color(0.96f, 0.75f, 0.33f);

    private static string Prefix => Current == Style.Chimes ? "chime" : Current == Style.Cookie ? "cookie" : "fire";

    /// The last sound asked for and when (game time): the preview recordings line their soundtrack up with it
    public static string LastSound; public static float LastSoundAt; public static int SoundCount;

    private static void Play(string name)
    {
        LastSound = Prefix + "_" + name; LastSoundAt = Time.time; SoundCount++;
        var clip = Resources.Load<AudioClip>("Audio/Stars/" + Prefix + "_" + name);
        if (clip != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(clip);
    }

    /// Star k (0-based) of `total` pops now.
    public static void OnStar(MonoBehaviour host, RectTransform star, int k, int total)
    {
        if (Current == Style.Classic || star == null) return;
        var fx = Fx.For(star);
        var at = fx.Local(star.position);
        Play((k + 1).ToString());
        Haptics.Tap();
        switch (Current)
        {
            case Style.Chimes:
                fx.Ring(at, Gold, 0.45f, 2.3f + 0.3f * k);
                fx.Burst(at, Sprites.Sparkle, 6 + 3 * k, 360f, 560f + 60f * k, 0f, 0.65f, 34f, 54f, new[] { Gold, Cream, Color.white });
                break;
            case Style.Cookie:
                fx.Burst(at, Sprites.Sprinkle, 9 + 5 * k, 380f, 700f + 80f * k, -1500f, 0.95f, 26f, 40f, new[] { Pink, Mint, Yellow, Cream }, up: 380f);
                fx.Burst(at, Sprites.Dot, 5 + 2 * k, 200f, 360f, -1100f, 0.7f, 14f, 22f, new[] { new Color(0.86f, 0.66f, 0.42f) }, up: 250f);   // crumbs
                break;
            case Style.Fireworks:
                fx.Ring(at, new Color(1f, 0.92f, 0.7f), 0.35f, 2.0f + 0.25f * k);
                fx.Burst(at, Sprites.Dot, 12 + 5 * k, 520f, 760f + 80f * k, -500f, 0.85f, 14f, 22f, new[] { Gold, Color.white, Cream }, drag: 2.2f);
                break;
        }
    }

    /// After the last star: the reward that grows with the count.
    public static IEnumerator Finale(MonoBehaviour host, RectTransform row, int total)
    {
        if (Current == Style.Classic || row == null || total < 2) yield break;
        var fx = Fx.For(row);
        var centre = fx.Local(row.position);
        yield return new WaitForSeconds(0.2f);
        Play("fin" + total);
        if (total >= 3) Haptics.DoubleTap(host); else Haptics.Tap();
        switch (Current)
        {
            case Style.Chimes:
                if (total == 2)
                {
                    for (var k = 0; k < 2; k++) fx.Pulse(GoldOf(row, k), 1.14f, 0.35f);
                    fx.Burst(centre + new Vector2(-75f, 0f), Sprites.Sparkle, 8, 120f, 260f, 120f, 1.1f, 26f, 40f, new[] { Gold, Color.white });
                }
                else
                {
                    fx.Rays(row, 1, Gold);
                    for (var k = 0; k < 3; k++) host.StartCoroutine(PulseLater(fx, GoldOf(row, k), 0.08f * k));
                    fx.Rain(centre + new Vector2(0f, 330f), 900f, Sprites.Sparkle, 38, 2.4f, new[] { Gold, Cream, Color.white }, 26f, 44f);
                }
                break;
            case Style.Cookie:
                fx.Hearts(centre, total == 2 ? 3 : 6, Pink);
                if (total >= 3)
                {
                    fx.Rain(centre + new Vector2(0f, 340f), 950f, Sprites.Sprinkle, 50, 2.4f, new[] { Pink, Mint, Yellow, Cream }, 24f, 38f);
                    for (var k = 0; k < 3; k++) host.StartCoroutine(ShineLater(fx, GoldOf(row, k), 0.12f * k));
                }
                break;
            case Style.Fireworks:
                if (total == 2) host.StartCoroutine(fx.Rocket(centre, centre + new Vector2(0f, 300f), Gold));
                else
                {
                    host.StartCoroutine(fx.Rocket(centre + new Vector2(-150f, 0f), centre + new Vector2(-250f, 280f), Mint));
                    host.StartCoroutine(RocketLater(fx, centre, centre + new Vector2(0f, 360f), Pink, 0.25f));
                    host.StartCoroutine(RocketLater(fx, centre + new Vector2(150f, 0f), centre + new Vector2(250f, 290f), Gold, 0.5f));
                    fx.Rain(centre + new Vector2(0f, 360f), 950f, Sprites.Confetti, 48, 2.6f, new[] { Pink, Mint, Yellow, Color.white }, 22f, 34f);
                }
                break;
        }
    }

    private static RectTransform GoldOf(RectTransform row, int k)
    {
        if (row == null || k >= row.childCount) return null;
        var g = row.GetChild(k).Find("Gold"); return g as RectTransform;
    }
    private static IEnumerator PulseLater(Fx fx, RectTransform t, float wait) { yield return new WaitForSeconds(wait); fx.Pulse(t, 1.18f, 0.32f); }
    private static IEnumerator ShineLater(Fx fx, RectTransform t, float wait) { yield return new WaitForSeconds(wait); fx.Shine(t); }
    private static IEnumerator RocketLater(Fx fx, Vector2 from, Vector2 to, Color c, float wait) { yield return new WaitForSeconds(wait); yield return fx.Rocket(from, to, c); }

    // ------------------------------------------------------------------------------------------------------------------
    /// The effects layer: a full-screen rect at the top of the card's canvas, moving its pieces itself each frame.
    private class Fx : MonoBehaviour
    {
        private class Piece { public RectTransform rt; public Image img; public Vector2 v; public float spin, age, life, s0, s1, gravity, drag, sway, swayPhase, fadeFrom = 0.6f; public Color c; public bool fadeIn; }
        private readonly List<Piece> pieces = new List<Piece>();
        private RectTransform layer;

        public static Fx For(Transform near)
        {
            var canvas = near.GetComponentInParent<Canvas>(); if (canvas == null) return null;
            var root = canvas.rootCanvas.transform;
            var t = root.Find("StarFx");
            if (t == null)
            {
                var go = new GameObject("StarFx", typeof(RectTransform)); go.transform.SetParent(root, false);
                var rt = (RectTransform)go.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
                t = rt; go.AddComponent<Fx>().layer = rt;
                // its own canvas, sorted over the card's (the card may sit in a nested, override-sorted canvas)
                var c = go.AddComponent<Canvas>(); c.overrideSorting = true; c.sortingLayerID = canvas.sortingLayerID; c.sortingOrder = canvas.sortingOrder + 20;
            }
            t.SetAsLastSibling();
            return t.GetComponent<Fx>();
        }

        public Vector2 Local(Vector3 world) => (Vector2)layer.InverseTransformPoint(world);

        private Piece Add(Sprite sprite, Vector2 at, float size, Color c)
        {
            var go = new GameObject("p", typeof(RectTransform), typeof(Image)); go.transform.SetParent(layer, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(size, size); rt.anchoredPosition = at;
            var img = go.GetComponent<Image>(); img.sprite = sprite; img.color = c; img.raycastTarget = false; img.preserveAspect = true;
            var p = new Piece { rt = rt, img = img, c = c, s0 = 1f, s1 = 1f };
            pieces.Add(p); return p;
        }

        public void Burst(Vector2 at, Sprite sprite, int n, float vMin, float vMax, float gravity, float life, float sMin, float sMax, Color[] colours, float up = 0f, float drag = 1.6f)
        {
            for (var i = 0; i < n; i++)
            {
                var a = (i + Random.Range(-0.35f, 0.35f)) / n * Mathf.PI * 2f; var v = Random.Range(vMin, vMax);
                var p = Add(sprite, at, Random.Range(sMin, sMax), colours[Random.Range(0, colours.Length)]);
                p.v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * v + new Vector2(0f, up); p.gravity = gravity; p.drag = drag;
                p.spin = Random.Range(-360f, 360f); p.life = life * Random.Range(0.8f, 1.15f); p.s0 = 1f; p.s1 = 0.3f;
                p.rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            }
        }

        public void Ring(Vector2 at, Color c, float life, float grow)
        {
            var p = Add(Sprites.Ring, at, 150f, new Color(c.r, c.g, c.b, 0.85f)); p.life = life; p.s0 = 0.5f; p.s1 = grow;
        }

        public void Rain(Vector2 top, float width, Sprite sprite, int n, float over, Color[] colours, float sMin, float sMax)
        {
            StartCoroutine(RainRun(top, width, sprite, n, over, colours, sMin, sMax));
        }
        private IEnumerator RainRun(Vector2 top, float width, Sprite sprite, int n, float over, Color[] colours, float sMin, float sMax)
        {
            for (var i = 0; i < n; i++)
            {
                var p = Add(sprite, top + new Vector2(Random.Range(-width / 2f, width / 2f), Random.Range(-40f, 40f)), Random.Range(sMin, sMax), colours[Random.Range(0, colours.Length)]);
                p.v = new Vector2(Random.Range(-60f, 60f), Random.Range(-260f, -120f)); p.gravity = -420f; p.drag = 0.4f;
                p.spin = Random.Range(-240f, 240f); p.life = Random.Range(2.3f, 2.9f); p.sway = Random.Range(40f, 90f); p.swayPhase = Random.Range(0f, 6f); p.fadeFrom = 0.78f;
                p.fadeIn = true; p.s0 = 1f; p.s1 = 0.85f;
                yield return new WaitForSeconds(over * 0.55f / n);
            }
        }

        public void Hearts(Vector2 centre, int n, Color c)
        {
            for (var i = 0; i < n; i++)
            {
                var p = Add(Sprites.Heart, centre + new Vector2(Random.Range(-220f, 220f), Random.Range(-30f, 30f)), Random.Range(38f, 58f), c);
                p.v = new Vector2(Random.Range(-30f, 30f), Random.Range(170f, 260f)); p.gravity = 0f; p.drag = 0.2f; p.life = Random.Range(1.4f, 1.9f);
                p.sway = 30f; p.swayPhase = Random.Range(0f, 6f); p.fadeIn = true; p.s0 = 0.6f; p.s1 = 1.1f; p.spin = 0f;
            }
        }

        public void Pulse(RectTransform t, float to, float time) { if (t != null) StartCoroutine(PulseRun(t, to, time)); }
        private IEnumerator PulseRun(RectTransform t, float to, float time)
        {
            for (var x = 0f; x < time && t != null; x += Time.deltaTime) { t.localScale = Vector3.one * Mathf.Lerp(1f, to, Mathf.Sin(x / time * Mathf.PI)); yield return null; }
            if (t != null) t.localScale = Vector3.one;
        }

        /// A soft white glint crossing the star, like light on icing.
        public void Shine(RectTransform t) { if (t != null) StartCoroutine(ShineRun(t)); }
        private IEnumerator ShineRun(RectTransform t)
        {
            var go = new GameObject("Shine", typeof(RectTransform), typeof(Image)); go.transform.SetParent(t, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(34f, 150f); rt.localRotation = Quaternion.Euler(0f, 0f, -28f);
            var img = go.GetComponent<Image>(); img.sprite = Sprites.Glint; img.raycastTarget = false;
            for (var x = 0f; x < 0.45f && t != null; x += Time.deltaTime)
            {
                var k = x / 0.45f; rt.anchoredPosition = new Vector2(Mathf.Lerp(-70f, 70f, k), 0f); img.color = new Color(1f, 1f, 1f, 0.75f * Mathf.Sin(k * Mathf.PI)); yield return null;
            }
            if (go != null) Destroy(go);
        }

        /// Golden rays turning slowly behind star k of the row (they stay with the card).
        public void Rays(RectTransform row, int k, Color c)
        {
            if (row == null || k >= row.childCount) return;
            var go = new GameObject("Rays", typeof(RectTransform), typeof(Image)); go.transform.SetParent(row, false); go.transform.SetSiblingIndex(0);
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = ((RectTransform)row.GetChild(k + 1)).anchoredPosition; rt.sizeDelta = new Vector2(460f, 460f);
            var img = go.GetComponent<Image>(); img.sprite = Sprites.Rays; img.raycastTarget = false; img.color = new Color(c.r, c.g, c.b, 0f);
            go.AddComponent<Spin>().img = img;
        }

        public IEnumerator Rocket(Vector2 from, Vector2 to, Color c)
        {
            var head = Add(Sprites.Dot, from, 20f, Color.white); head.life = 0.45f; head.s0 = 1f; head.s1 = 0.8f;
            for (var x = 0f; x < 0.42f; x += Time.deltaTime)
            {
                if (head.rt == null) yield break;
                var k = 1f - (1f - x / 0.42f) * (1f - x / 0.42f); head.rt.anchoredPosition = Vector2.Lerp(from, to, k);
                var trail = Add(Sprites.Dot, head.rt.anchoredPosition, 12f, new Color(c.r, c.g, c.b, 0.8f)); trail.life = 0.35f; trail.s1 = 0.2f; trail.gravity = -200f;
                yield return null;
            }
            Ring(to, c, 0.45f, 2.6f);
            Burst(to, Sprites.Dot, 26, 380f, 620f, -380f, 1.1f, 14f, 22f, new[] { c, Color.white, Color.Lerp(c, Color.white, 0.5f) }, drag: 1.8f);
            Burst(to, Sprites.Sparkle, 8, 160f, 300f, -200f, 0.9f, 26f, 38f, new[] { Color.white, c });
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            for (var i = pieces.Count - 1; i >= 0; i--)
            {
                var p = pieces[i];
                if (p.rt == null) { pieces.RemoveAt(i); continue; }
                p.age += dt; var k = p.age / Mathf.Max(0.01f, p.life);
                if (k >= 1f) { Destroy(p.rt.gameObject); pieces.RemoveAt(i); continue; }
                p.v *= Mathf.Max(0f, 1f - p.drag * dt); p.v.y += p.gravity * dt;
                var sway = p.sway > 0f ? new Vector2(Mathf.Cos(p.age * 3f + p.swayPhase) * p.sway * dt, 0f) : Vector2.zero;
                p.rt.anchoredPosition += p.v * dt + sway;
                p.rt.localRotation = Quaternion.Euler(0f, 0f, p.rt.localEulerAngles.z + p.spin * dt);
                p.rt.localScale = Vector3.one * Mathf.Lerp(p.s0, p.s1, k);
                var a = (p.fadeIn ? Mathf.Clamp01(k / 0.12f) : 1f) * (1f - Mathf.Clamp01((k - p.fadeFrom) / (1f - p.fadeFrom)));
                p.img.color = new Color(p.c.r, p.c.g, p.c.b, p.c.a * a);
            }
        }
    }

    private class Spin : MonoBehaviour
    {
        public Image img; private float t;
        private void Update()
        {
            t += Time.deltaTime; transform.localRotation = Quaternion.Euler(0f, 0f, -22f * t);
            if (img != null) { var c = img.color; c.a = 0.5f * Mathf.Clamp01(t / 0.5f) * (0.85f + 0.15f * Mathf.Sin(t * 3f)); img.color = c; }
        }
    }

    // ------------------------------------------------------------------------------------------------------------------
    /// The pieces, drawn once in code: soft, round-edged shapes in the game's flat cookie look.
    private static class Sprites
    {
        private static Sprite sparkle, dot, sprinkle, confetti, heart, ring, rays, glint;
        public static Sprite Sparkle => sparkle ??= Make(64, (x, y) => { var ax = Mathf.Abs(x); var ay = Mathf.Abs(y); var d = Mathf.Pow(ax, 0.5f) + Mathf.Pow(ay, 0.5f); return Mathf.Clamp01((0.95f - d) * 6f) + 0.35f * Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y) / 0.45f); });
        public static Sprite Dot => dot ??= Make(32, (x, y) => Mathf.Clamp01((1f - Mathf.Sqrt(x * x + y * y)) * 5f));
        public static Sprite Sprinkle => sprinkle ??= Make(48, (x, y) => { var ex = Mathf.Max(0f, Mathf.Abs(x) - 0.55f); var d = Mathf.Sqrt(ex * ex + y * y); return Mathf.Clamp01((0.3f - d) * 14f); });
        public static Sprite Confetti => confetti ??= Make(48, (x, y) => { var ex = Mathf.Max(0f, Mathf.Abs(x) - 0.4f); var ey = Mathf.Max(0f, Mathf.Abs(y) - 0.15f); var d = Mathf.Sqrt(ex * ex + ey * ey); return Mathf.Clamp01((0.18f - d) * 16f); });
        public static Sprite Heart => heart ??= Make(64, (x, y) => { y = -y * 1.1f + 0.15f; var v = Mathf.Pow(x * x + y * y - 0.42f, 3f) - x * x * y * y * y; return Mathf.Clamp01(-v * 40f); });
        public static Sprite Ring => ring ??= Make(96, (x, y) => { var r = Mathf.Sqrt(x * x + y * y); return Mathf.Clamp01(1f - Mathf.Abs(r - 0.82f) / 0.1f); });
        public static Sprite Rays => rays ??= Make(256, (x, y) => { var r = Mathf.Sqrt(x * x + y * y); var a = Mathf.Atan2(y, x); var ray = Mathf.Pow(Mathf.Clamp01(Mathf.Cos(a * 6f)), 6f); return ray * Mathf.Clamp01((1f - r) * 1.6f) * Mathf.Clamp01(r * 4f - 0.3f); });
        public static Sprite Glint => glint ??= Make(32, (x, y) => Mathf.Clamp01(1f - Mathf.Abs(x) * 1.2f) * Mathf.Clamp01((1f - Mathf.Abs(y)) * 3f));

        /// A white sprite from an alpha function over [-1, 1]²; tinted by the Image colour.
        private static Sprite Make(int size, System.Func<float, float, float> alpha)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            for (var j = 0; j < size; j++)
                for (var i = 0; i < size; i++)
                    px[j * size + i] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha((i + 0.5f) / size * 2f - 1f, (j + 0.5f) / size * 2f - 1f)));
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}

