using System.Collections.Generic;
using UnityEngine;

/// Visible patience (12 October 2026): a daisy above the order bubble that loses one petal per second of the
/// customer's patience. Petals = whole seconds of the level's timer plus the decoration bonus, so a vase or a
/// bouquet on the table adds a petal; a patient customer's petals fall slower, a hurried one's faster. The tip is
/// 10% of the order per second left, so petals left is the tip. Built from runtime sprites (no assets), shown by
/// OrderSystem.DisplayOrder and hidden by CompleteOrder / ExpireOrder. Ticks with scaled time, like the order timer.
public class PatienceMeter : MonoBehaviour
{
    public static readonly Vector3 WorldPosition = new Vector3(-1.0f, 3.35f, 0f);   // above Bubble1 (-1, 2.5)
    const float PetalRadius = 0.30f;      // centre of each petal from the daisy centre, world units
    const float FallSeconds = 0.7f;

    static readonly Dictionary<OrderSystem.ActiveOrder, PatienceMeter> meters = new Dictionary<OrderSystem.ActiveOrder, PatienceMeter>();
    static Sprite petalSprite, centreSprite;

    OrderSystem.ActiveOrder order;
    int petals;
    readonly List<SpriteRenderer> petalRenderers = new List<SpriteRenderer>();
    readonly List<float> fallStart = new List<float>();     // Time.time a petal started falling, or -1
    SpriteRenderer centre;

    /// One daisy per open order, at the given world position (each slot's bubble root decides where)
    public static void Show(OrderSystem.ActiveOrder order, Vector3 position)
    {
        Hide(order);
        var petalCount = Mathf.Clamp(order.petals > 0 ? order.petals : Mathf.RoundToInt(order.displayTime), 1, 12);
        var go = new GameObject("PatienceMeter" + order.slot);
        go.transform.position = position;
        var m = go.AddComponent<PatienceMeter>();
        m.order = order;
        m.Build(petalCount);
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

    void Build(int count)
    {
        petals = count;
        for (var i = 0; i < count; i++)
        {
            var angle = 90f - 360f * i / count;             // first petal at the top, then clockwise
            var p = new GameObject("Petal" + i);
            p.transform.SetParent(transform, false);
            var rad = angle * Mathf.Deg2Rad;
            p.transform.localPosition = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * PetalRadius;
            p.transform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
            p.transform.localScale = Vector3.one * (count <= 6 ? 1f : 6f / count);
            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite = PetalSprite(); sr.sortingOrder = 5;
            petalRenderers.Add(sr); fallStart.Add(-1f);
        }
        var c = new GameObject("Centre");
        c.transform.SetParent(transform, false);
        centre = c.AddComponent<SpriteRenderer>();
        centre.sprite = CentreSprite(); centre.sortingOrder = 6;
    }

    void Update()
    {
        if (order == null || !order.active) return;
        var total = Mathf.Max(order.displayTime, 0.01f);
        var f = Mathf.Clamp01(order.timer / total);                     // share of patience left
        var alive = f * petals;                                         // petals still on, fractional
        for (var i = 0; i < petals; i++)
        {
            var sr = petalRenderers[i];
            var left = alive - i;                                       // > 1 whole, 0..1 draining, <= 0 gone
            if (left >= 1f) continue;
            if (left > 0f)
            {
                // the draining petal fades a little and shrinks, so you can see it going
                sr.color = new Color(1f, 1f, 1f, 0.45f + 0.55f * left);
                sr.transform.localScale = Vector3.one * (0.8f + 0.2f * left);
                continue;
            }
            if (fallStart[i] < 0f) fallStart[i] = Time.time;
            var t = (Time.time - fallStart[i]) / FallSeconds;
            if (t >= 1f) { if (sr.gameObject.activeSelf) sr.gameObject.SetActive(false); continue; }
            sr.color = new Color(1f, 1f, 1f, 1f - t);
            sr.transform.localPosition += Vector3.down * (Time.deltaTime * 0.9f);
            sr.transform.Rotate(0f, 0f, Time.deltaTime * 120f);
        }
        // the centre warms from yellow to orange-red in the last quarter
        if (centre != null) centre.color = Color.Lerp(new Color(1f, 0.55f, 0.25f), new Color(1f, 0.82f, 0.25f), Mathf.Clamp01(f / 0.25f));
    }

    static Sprite PetalSprite()
    {
        if (petalSprite != null) return petalSprite;
        const int w = 24, h = 40;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color[w * h];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var dx = (x + 0.5f - w / 2f) / (w / 2f);
            var dy = (y + 0.5f - h / 2f) / (h / 2f);
            var d = dx * dx + dy * dy;
            var a = Mathf.Clamp01((1f - d) * 6f);                    // soft edge
            var rim = d > 0.72f ? 0.85f : 1f;                        // slightly darker rim, like the kitchen's painted petals
            px[y * w + x] = new Color(1f * rim, 0.98f * rim, 0.92f * rim, a);
        }
        tex.SetPixels(px); tex.Apply();
        petalSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        return petalSprite;
    }

    static Sprite CentreSprite()
    {
        if (centreSprite != null) return centreSprite;
        const int n = 26;
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
}
