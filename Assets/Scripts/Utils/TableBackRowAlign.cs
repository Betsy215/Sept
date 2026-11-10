using System.Collections.Generic;
using UnityEngine;

/// The fixed items on the table's back band (truck button, hen board, candy jar, two flower pots) were each placed by
/// their own constant, and their bottoms sat on four different lines (owner, 9 November 2026: "i want their bottom to
/// align strictly"). This snaps every item so that the LOWEST PAINTED PIXEL of its sprite sits on the flower pot's
/// lowest painted pixel, measured at runtime from the sprite's own texture (owner, 10 November: "use the very bottom
/// of the sprite content, no spacing"). The textures involved are import-readable for that; if one is not, the
/// sprite's rectangle bottom is used and a warning says so. Runs after the first frame and again half a second later.
public static class TableBackRowAlign
{
    static readonly Dictionary<Sprite, int> emptyRowsUnder = new Dictionary<Sprite, int>();

    /// Transparent rows under the drawing in the sprite's rectangle, from the texture pixels (alpha 16 or more counts).
    public static int EmptyRowsUnder(Sprite sprite)
    {
        if (sprite == null) return 0;
        if (emptyRowsUnder.TryGetValue(sprite, out var cached)) return cached;
        var rows = 0;
        try
        {
            var tex = sprite.texture; var r = sprite.textureRect;
            var x0 = Mathf.RoundToInt(r.x); var y0 = Mathf.RoundToInt(r.y); var w = Mathf.RoundToInt(r.width); var h = Mathf.RoundToInt(r.height);
            var px = tex.GetPixels32(); var tw = tex.width;
            for (var y = y0; y < y0 + h; y++)                 // texture rows run bottom-up
            {
                var painted = false;
                for (var x = x0; x < x0 + w; x++) if (px[y * tw + x].a >= 16) { painted = true; break; }
                if (painted) break;
                rows++;
            }
        }
        catch (UnityException)
        {
            Debug.LogWarning("TableBackRowAlign: texture of " + sprite.name + " is not readable; using its rectangle bottom");
            rows = 0;
        }
        emptyRowsUnder[sprite] = rows;
        return rows;
    }

    /// World y of the sprite's lowest painted pixel.
    public static float VisibleBottom(SpriteRenderer sr) =>
        sr.bounds.min.y + EmptyRowsUnder(sr.sprite) / sr.sprite.pixelsPerUnit * Mathf.Abs(sr.transform.lossyScale.y);

    /// World y of a UI Image's lowest painted pixel (the Image stretches the whole texture over its rectangle).
    public static float VisibleBottom(RectTransform rt, Sprite sprite, out bool ok)
    {
        var c = new Vector3[4]; rt.GetWorldCorners(c);           // 0 bottom-left, 1 top-left
        var worldHeight = c[1].y - c[0].y; ok = worldHeight > 0f && sprite != null;
        if (!ok) return c[0].y;
        return c[0].y + worldHeight * (EmptyRowsUnder(sprite) / sprite.rect.height);
    }

    public static void Snap()
    {
        var decor = GameObject.Find("TableDecor");
        var pot = decor != null ? decor.transform.Find("PotFlowers") : null;
        var potSr = pot != null ? pot.GetComponent<SpriteRenderer>() : null;
        if (potSr == null || potSr.sprite == null) return;
        var line = VisibleBottom(potSr);

        var green = decor.transform.Find("PotGreen");
        var greenSr = green != null ? green.GetComponent<SpriteRenderer>() : null;
        if (greenSr != null && greenSr.sprite != null) Lift(green, VisibleBottom(greenSr), line);

        var jar = Object.FindObjectOfType<CandyJar>();
        var jarSr = jar != null ? jar.GetComponent<SpriteRenderer>() : null;
        if (jarSr != null && jarSr.sprite != null) { Lift(jar.transform, VisibleBottom(jarSr), line); jar.Rebase(); }

        var clock = Object.FindObjectOfType<MoneyFlipClock>();
        var hen = clock != null ? clock.transform.Find("Hen") : null;
        var henSr = hen != null ? hen.GetComponent<SpriteRenderer>() : null;
        if (henSr != null && henSr.sprite != null) Lift(clock.transform, VisibleBottom(henSr), line);

        var truck = GameObject.Find("KitchenButton");
        var rt = truck != null ? truck.GetComponent<RectTransform>() : null;
        var img = truck != null ? truck.GetComponent<UnityEngine.UI.Image>() : null;
        if (rt != null && rt.rect.height > 0f)
        {
            var bottom = VisibleBottom(rt, img != null ? img.sprite : null, out var ok);
            if (ok)
            {
                var c = new Vector3[4]; rt.GetWorldCorners(c);
                var worldPerCanvasUnit = (c[1].y - c[0].y) / rt.rect.height;
                rt.anchoredPosition += new Vector2(0f, (line - bottom) / worldPerCanvasUnit);
                var wobble = truck.GetComponent<BalatroWobble>();
                if (wobble != null) wobble.Rebase();
            }
        }
    }

    static void Lift(Transform t, float bottom, float line)
    {
        var p = t.position; p.y += line - bottom; t.position = p;
    }
}
