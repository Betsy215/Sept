using System.Collections;
using UnityEngine;

/// What shows on the yoga lady when she finishes a pose and every waiting customer gets a flower (owner, 15 November
/// 2026: "I want there to be an effect on yoga lady too to show that she is giving out patience effects, try add some
/// glow or sparks when that happens, do a few different variations"). Four styles, picked by Style until the owner
/// chooses one; each lasts about a second and is drawn as children of her, so it moves, bobs and fades with her.
///   1 Glow:    a warm golden glow swells behind her and fades.
///   2 Sparks:  little four-point sparkles burst out from her heart and fade.
///   3 Petals:  the patience flower's own petals rise from her and drift up, like the flowers she gives.
///   4 Shine:   a soft golden outline grows around her shape and fades.
public static class YogaGlow
{
    public static int Style = 1;
    private static Sprite glow, spark;
    private static readonly Color Gold = new Color(1f, 0.86f, 0.42f);

    public static void Play(MonoBehaviour her, SpriteRenderer pose)
    {
        if (her == null || pose == null || pose.sprite == null || pose.color.a < 0.5f) return;
        switch (Style)
        {
            case 2: her.StartCoroutine(Sparks(her.transform)); break;
            case 3: her.StartCoroutine(Petals(her.transform)); break;
            case 4: her.StartCoroutine(Shine(her.transform, pose)); break;
            default: her.StartCoroutine(Glow(her.transform)); break;
        }
    }

    private static SpriteRenderer Make(Transform parent, string name, Sprite sprite, float z)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = new Vector3(0f, 0f, z);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.color = new Color(1f, 1f, 1f, 0f);
        return sr;
    }

    private static IEnumerator Glow(Transform her)
    {
        var sr = Make(her, "YogaGlow", GlowSprite(), 0.004f);   // just behind her: the background is about 0.01 further
        sr.transform.localPosition += new Vector3(0f, 0.05f, 0f);
        for (var t = 0f; t < 1.1f; t += Time.deltaTime)
        {
            var k = t / 1.1f; var up = Mathf.Sin(Mathf.PI * Mathf.Min(1f, k * 1.4f));
            sr.transform.localScale = Vector3.one * (2.0f + 0.9f * k);
            sr.color = new Color(1f, 0.82f, 0.3f, up);
            yield return null;
        }
        Object.Destroy(sr.gameObject);
    }

    private static IEnumerator Sparks(Transform her)
    {
        const int n = 9; var parts = new SpriteRenderer[n]; var dirs = new Vector2[n]; var spin = new float[n];
        for (var i = 0; i < n; i++)
        {
            parts[i] = Make(her, "YogaSpark", SparkSprite(), -0.03f);
            var a = (i / (float)n) * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
            dirs[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.85f) * Random.Range(0.75f, 1.05f); spin[i] = Random.Range(-90f, 90f);
        }
        var heart = new Vector3(0f, 0.15f, -0.03f);
        for (var t = 0f; t < 1f; t += Time.deltaTime)
        {
            var k = t / 1f; var out_ = 1f - (1f - k) * (1f - k);
            for (var i = 0; i < n; i++)
            {
                parts[i].transform.localPosition = heart + (Vector3)(dirs[i] * out_);
                parts[i].transform.localRotation = Quaternion.Euler(0f, 0f, spin[i] * k);
                parts[i].transform.localScale = Vector3.one * (0.55f * Mathf.Sin(Mathf.PI * Mathf.Min(1f, k * 1.2f)) + 0.05f);
                var c = i % 3 == 0 ? Color.white : Gold; parts[i].color = new Color(c.r, c.g, c.b, 1f - k * k);
            }
            yield return null;
        }
        foreach (var p in parts) Object.Destroy(p.gameObject);
    }

    private static IEnumerator Petals(Transform her)
    {
        var petal = Resources.Load<Sprite>("Patience/petal"); if (petal == null) petal = SparkSprite();
        const int n = 7; var parts = new SpriteRenderer[n]; var start = new Vector3[n]; var sway = new float[n]; var turn = new float[n];
        for (var i = 0; i < n; i++)
        {
            parts[i] = Make(her, "YogaPetal", petal, -0.03f);
            start[i] = new Vector3(Random.Range(-0.45f, 0.45f), Random.Range(-0.2f, 0.35f), -0.03f); sway[i] = Random.Range(0f, 6f); turn[i] = Random.Range(-160f, 160f);
        }
        var size = 0.18f / Mathf.Max(0.01f, petal.bounds.size.x);
        for (var t = 0f; t < 1.4f; t += Time.deltaTime)
        {
            var k = t / 1.4f;
            for (var i = 0; i < n; i++)
            {
                parts[i].transform.localPosition = start[i] + new Vector3(Mathf.Sin(sway[i] + k * 6f) * 0.08f, 0.9f * k, 0f);
                parts[i].transform.localRotation = Quaternion.Euler(0f, 0f, turn[i] * k);
                parts[i].transform.localScale = Vector3.one * size * Mathf.Min(1f, k * 5f);
                var pink = i % 2 == 0 ? new Color(1f, 0.62f, 0.72f) : new Color(1f, 0.78f, 0.84f);   // the patience flower's pinks
                parts[i].color = new Color(pink.r, pink.g, pink.b, k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f);
            }
            yield return null;
        }
        foreach (var p in parts) Object.Destroy(p.gameObject);
    }

    private static IEnumerator Shine(Transform her, SpriteRenderer pose)
    {
        // her own picture, a little bigger and tinted gold, behind her: reads as a glowing rim around her shape
        var sr = Make(her, "YogaShine", pose.sprite, 0.003f); var soft = Make(her, "YogaShineSoft", GlowSprite(), 0.006f);
        soft.transform.localPosition += new Vector3(0f, 0.05f, 0f);
        for (var t = 0f; t < 1.1f; t += Time.deltaTime)
        {
            var k = t / 1.1f; var up = Mathf.Sin(Mathf.PI * Mathf.Min(1f, k * 1.3f));
            sr.sprite = pose.sprite;
            sr.transform.localScale = Vector3.one * (1.06f + 0.06f * k);
            sr.color = new Color(1f, 0.88f, 0.4f, up);
            soft.transform.localScale = Vector3.one * 2.2f; soft.color = new Color(1f, 0.85f, 0.4f, 0.55f * up);
            yield return null;
        }
        Object.Destroy(sr.gameObject); Object.Destroy(soft.gameObject);
    }

    /// A soft round glow, white in the middle and clear at the edge (tinted by the renderer's colour).
    private static Sprite GlowSprite()
    {
        if (glow != null) return glow;
        const int n = 64; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n];
        for (var y = 0; y < n; y++) for (var x = 0; x < n; x++)
        {
            var dx = (x + 0.5f - n / 2f) / (n / 2f); var dy = (y + 0.5f - n / 2f) / (n / 2f);
            var a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy)); px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Pow(a, 0.8f));
        }
        tex.SetPixels(px); tex.Apply();
        return glow = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);   // one world unit across at scale 1
    }

    /// A small four-point sparkle with a soft core.
    private static Sprite SparkSprite()
    {
        if (spark != null) return spark;
        const int n = 64; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n];
        for (var y = 0; y < n; y++) for (var x = 0; x < n; x++)
        {
            var dx = Mathf.Abs((x + 0.5f - n / 2f) / (n / 2f)); var dy = Mathf.Abs((y + 0.5f - n / 2f) / (n / 2f));
            var star = Mathf.Clamp01(1f - (Mathf.Pow(dx, 0.5f) + Mathf.Pow(dy, 0.5f)) * 0.9f);   // a four-point star
            var core = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 2.2f);
            px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(star * 1.6f + core));
        }
        tex.SetPixels(px); tex.Apply();
        return spark = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
    }
}
