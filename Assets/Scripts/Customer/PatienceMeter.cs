using System.Collections.Generic;
using UnityEngine;

/// Visible patience (13 October 2026, owner's design): three flowers above the customer's order bubble, one per
/// chapter kind (chapter 1 a daisy). Patience runs through them one at a time: the first flower wilts gradually
/// (petals fade and the head droops), then the second, then the third. When an item of the order is served, the
/// flower that was draining fills up again (the "hearts" mechanic), with a bounce and a sparkle; the order's timer
/// gets that time back too (OrderSystem.RefillPatience). The tip is the time left, so flowers still fresh are the tip.
/// Built from runtime sprites, no assets. Shown by OrderSystem.StartOrderForCustomer, hidden by CompleteOrder / ExpireOrder.
public class PatienceMeter : MonoBehaviour
{
    public static readonly Vector3 WorldPosition = new Vector3(-0.75f, 3.43f, 0f);   // above the bubble, a little right of its centre so the left set clears the settings button; with two slots at 0.7 that is y 4.2
    public const int Flowers = 3;
    const float Spacing = 0.45f;           // between flower centres, world units
    const float PetalRadius = 0.15f;
    const float FallSeconds = 0.8f;

    static readonly Dictionary<OrderSystem.ActiveOrder, PatienceMeter> meters = new Dictionary<OrderSystem.ActiveOrder, PatienceMeter>();
    static Sprite petalSprite, centreSprite, sparkSprite;

    OrderSystem.ActiveOrder order;
    readonly Flower[] flowers = new Flower[Flowers];
    float sway;

    class Flower
    {
        public Transform root, head;
        public SpriteRenderer[] petals;
        public SpriteRenderer centre;
        public float shown = 1f;           // the fill the picture shows (eases toward the real fill)
        public float bounce;               // seconds left of the refill bounce
        public float fallStart = -1f;      // Time.time the wilted head started to fall, or -1
        public bool gone;
    }

    /// One meter per open order, at the given world position (each slot's bubble root decides where)
    public static void Show(OrderSystem.ActiveOrder order, Vector3 position)
    {
        Hide(order);
        var go = new GameObject("PatienceMeter" + order.slot);
        go.transform.position = position;
        var m = go.AddComponent<PatienceMeter>();
        m.order = order;
        m.Build();
        meters[order] = m;
    }

    public static void Hide(OrderSystem.ActiveOrder order)
    {
        if (order != null && meters.TryGetValue(order, out var m))
        {
            if (m != null) Destroy(m.gameObject);
            meters.Remove(order);
        }
    }

    public static void HideAll()
    {
        foreach (var m in meters.Values) if (m != null) Destroy(m.gameObject);
        meters.Clear();
    }

    /// An item was served: the flower that was draining is full again (OrderSystem already gave the time back)
    public static void OnItemServed(OrderSystem.ActiveOrder order)
    {
        if (order != null && meters.TryGetValue(order, out var m) && m != null) m.Refill();
    }

    /// Patience as fills per flower, 1 = fresh, 0 = gone, from the order's time left
    static float Fill(OrderSystem.ActiveOrder o, int i)
    {
        var total = Mathf.Max(o.displayTime, 0.01f);
        var per = total / Flowers;
        return Mathf.Clamp01((o.timer - i * per) / per);
    }

    void Build()
    {
        for (var i = 0; i < Flowers; i++)
        {
            var f = new Flower();
            f.root = new GameObject("Flower" + i).transform;
            f.root.SetParent(transform, false);
            f.root.localPosition = new Vector3((i - (Flowers - 1) / 2f) * Spacing, 0f, 0f);
            f.head = new GameObject("Head").transform;
            f.head.SetParent(f.root, false);
            f.head.localPosition = new Vector3(0f, 0.04f, 0f);
            f.petals = new SpriteRenderer[8];
            for (var k = 0; k < 8; k++)
            {
                var angle = 90f - 45f * k;
                var p = new GameObject("Petal" + k);
                p.transform.SetParent(f.head, false);
                var rad = angle * Mathf.Deg2Rad;
                p.transform.localPosition = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * PetalRadius;
                p.transform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
                var sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = PetalSprite(); sr.sortingOrder = 5;
                f.petals[k] = sr;
            }
            var c = new GameObject("Centre");
            c.transform.SetParent(f.head, false);
            f.centre = c.AddComponent<SpriteRenderer>();
            f.centre.sprite = CentreSprite(); f.centre.sortingOrder = 6;
            f.centre.color = new Color(1f, 0.82f, 0.25f);
            flowers[i] = f;
        }
    }

    void Update()
    {
        if (order == null || !order.active) return;
        sway += Time.deltaTime;
        for (var i = 0; i < Flowers; i++)
        {
            var f = flowers[i];
            var target = Fill(order, i);
            // the picture eases toward the real fill: a slow wilt, a quick fill-up
            f.shown = target > f.shown ? Mathf.MoveTowards(f.shown, target, Time.deltaTime * 3f) : Mathf.MoveTowards(f.shown, target, Time.deltaTime * 1.2f);
            var s = f.shown;
            if (s > 0.02f) { f.gone = false; f.fallStart = -1f; }

            // petals fade one after another from the top as the flower drains; the head droops and dulls
            for (var k = 0; k < f.petals.Length; k++)
            {
                var pf = Mathf.Clamp01((s - k / 8f) * 8f);          // this petal's own fill
                var tint = Mathf.Lerp(0.72f, 1f, s);                   // dull as it wilts
                f.petals[k].color = new Color(tint, tint, tint * 0.98f, 0.25f + 0.75f * pf);
                f.petals[k].transform.localScale = Vector3.one * (0.75f + 0.25f * pf);
            }
            f.centre.color = Color.Lerp(new Color(0.75f, 0.6f, 0.35f), new Color(1f, 0.82f, 0.25f), s);
            var droop = Mathf.Lerp(-42f, 0f, Mathf.SmoothStep(0f, 1f, s));             // head leans over as it wilts
            var alive = Mathf.Sin(sway * 2.2f + i * 1.3f) * 3f * s;                      // a gentle sway while fresh
            f.head.localRotation = Quaternion.Euler(0f, 0f, droop + alive);
            var scale = 1f;
            if (f.bounce > 0f)
            {
                f.bounce -= Time.deltaTime;
                scale = 1f + 0.35f * Mathf.Sin(Mathf.Clamp01(f.bounce / 0.45f) * Mathf.PI);   // pop and settle
            }
            f.head.localScale = Vector3.one * scale * Mathf.Lerp(0.8f, 1f, s);

            // a flower that is all gone drops its head and fades out
            if (s <= 0.02f && !f.gone)
            {
                if (f.fallStart < 0f) f.fallStart = Time.time;
                var t = (Time.time - f.fallStart) / FallSeconds;
                f.head.localPosition = new Vector3(0f, 0.04f - 0.35f * t, 0f);
                var a = 1f - Mathf.Clamp01(t);
                foreach (var p in f.petals) { var c = p.color; c.a *= a; p.color = c; }
                var cc = f.centre.color; cc.a = a; f.centre.color = cc;
                if (t >= 1f) f.gone = true;
            }
            else if (s > 0.02f) f.head.localPosition = new Vector3(0f, 0.04f, 0f);
        }
    }

    /// The draining flower pops back to full with a little sparkle
    void Refill()
    {
        if (order == null) return;
        for (var i = Flowers - 1; i >= 0; i--)
        {
            var f = flowers[i];
            var target = Fill(order, i);
            if (target >= 0.999f && f.shown < 0.98f)
            {
                f.bounce = 0.45f;
                f.shown = Mathf.Max(f.shown, 0.6f);
                StartCoroutine(Sparkle(f.root.position));
                break;
            }
        }
    }

    System.Collections.IEnumerator Sparkle(Vector3 at)
    {
        var sparks = new List<Transform>();
        for (var k = 0; k < 6; k++)
        {
            var g = new GameObject("Spark");
            g.transform.SetParent(transform, true);
            g.transform.position = at;
            var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = SparkSprite(); sr.sortingOrder = 7; sr.color = new Color(1f, 0.95f, 0.6f);
            sparks.Add(g.transform);
        }
        var t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            var k = 0;
            foreach (var s in sparks)
            {
                if (s == null) continue;
                var a = k * 60f * Mathf.Deg2Rad; k++;
                s.position = at + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * (0.18f + 0.35f * t);
                s.localScale = Vector3.one * (1f - t / 0.5f) * 0.9f;
            }
            yield return null;
        }
        foreach (var s in sparks) if (s != null) Destroy(s.gameObject);
    }

    static Sprite PetalSprite()
    {
        if (petalSprite != null) return petalSprite;
        const int w = 20, h = 34;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color[w * h];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var dx = (x + 0.5f - w / 2f) / (w / 2f);
            var dy = (y + 0.5f - h / 2f) / (h / 2f);
            var d = dx * dx + dy * dy;
            var a = Mathf.Clamp01((1f - d) * 6f);
            var rim = d > 0.72f ? 0.86f : 1f;                        // a slightly darker rim, like the painted petals
            px[y * w + x] = new Color(1f * rim, 0.98f * rim, 0.92f * rim, a);
        }
        tex.SetPixels(px); tex.Apply();
        petalSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        return petalSprite;
    }

    static Sprite CentreSprite()
    {
        if (centreSprite != null) return centreSprite;
        const int n = 18;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color[n * n];
        for (var y = 0; y < n; y++)
        for (var x = 0; x < n; x++)
        {
            var dx = (x + 0.5f - n / 2f) / (n / 2f);
            var dy = (y + 0.5f - n / 2f) / (n / 2f);
            var d = Mathf.Sqrt(dx * dx + dy * dy);
            var a = Mathf.Clamp01((1f - d) * 7f);
            var shade = d > 0.75f ? 0.8f : 1f;
            px[y * n + x] = new Color(shade, shade, shade, a);        // tinted by the renderer colour
        }
        tex.SetPixels(px); tex.Apply();
        centreSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return centreSprite;
    }

    static Sprite SparkSprite()
    {
        if (sparkSprite != null) return sparkSprite;
        const int n = 10;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color[n * n];
        for (var y = 0; y < n; y++)
        for (var x = 0; x < n; x++)
        {
            var dx = Mathf.Abs(x + 0.5f - n / 2f) / (n / 2f); var dy = Mathf.Abs(y + 0.5f - n / 2f) / (n / 2f);
            var a = Mathf.Clamp01(1f - Mathf.Min(dx + dy * 3f, dy + dx * 3f));   // a little four-point star
            px[y * n + x] = new Color(1f, 1f, 1f, a);
        }
        tex.SetPixels(px); tex.Apply();
        sparkSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return sparkSprite;
    }
}
