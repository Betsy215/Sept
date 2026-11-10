using UnityEngine;

/// The fixed items on the table's back band (truck button, hen board, candy jar, two flower pots) were each placed by
/// their own constant, and their bottoms sat on four different lines, about 26 px apart on the phone (owner,
/// 9 November 2026: "i want their bottom to align strictly"). This snaps every visible bottom onto the flower pot's
/// bottom line once the row is built, measured at runtime from the real renderers, so it holds on every screen size
/// (the truck is a UI button whose canvas scale changes with the device).
public static class TableBackRowAlign
{
    // transparent rows under each drawing in its sprite, in sprite pixels (the renderer bounds cover the whole rect)
    // measured 10 November 2026 (lowest row with alpha > 8): pot_flowers.png 37 of 398, pot_green.png 84 of 398, candy_jar_*.png 39 of 240,
    // UI/money_hen.png 6 of 222; the kitchen button's Image is decor/kit1_table.png, 3 empty rows of 406 (the Image stretches the whole texture)
    const int PotFlowersPad = 37, PotGreenPad = 84, JarPad = 39, HenPad = 6;
    const float TruckPadShare = 3f / 406f;

    public static void Snap()
    {
        var decor = GameObject.Find("TableDecor");
        var pot = decor != null ? decor.transform.Find("PotFlowers") : null;
        var potSr = pot != null ? pot.GetComponent<SpriteRenderer>() : null;
        if (potSr == null || potSr.sprite == null) return;
        var line = VisibleBottom(potSr, PotFlowersPad);

        var green = decor.transform.Find("PotGreen");
        var greenSr = green != null ? green.GetComponent<SpriteRenderer>() : null;
        if (greenSr != null && greenSr.sprite != null) Lift(green, VisibleBottom(greenSr, PotGreenPad), line);

        var jar = Object.FindObjectOfType<CandyJar>();
        var jarSr = jar != null ? jar.GetComponent<SpriteRenderer>() : null;
        if (jarSr != null && jarSr.sprite != null) { Lift(jar.transform, VisibleBottom(jarSr, JarPad), line); jar.Rebase(); }

        var clock = Object.FindObjectOfType<MoneyFlipClock>();
        var hen = clock != null ? clock.transform.Find("Hen") : null;
        var henSr = hen != null ? hen.GetComponent<SpriteRenderer>() : null;
        if (henSr != null && henSr.sprite != null) Lift(clock.transform, VisibleBottom(henSr, HenPad), line);

        var truck = GameObject.Find("KitchenButton");
        var rt = truck != null ? truck.GetComponent<RectTransform>() : null;
        if (rt != null && rt.rect.height > 0f)
        {
            var c = new Vector3[4]; rt.GetWorldCorners(c);           // 0 bottom-left, 1 top-left
            var worldHeight = c[1].y - c[0].y;
            if (worldHeight > 0f)
            {
                var bottom = c[0].y + worldHeight * TruckPadShare;
                var worldPerCanvasUnit = worldHeight / rt.rect.height;
                rt.anchoredPosition += new Vector2(0f, (line - bottom) / worldPerCanvasUnit);
                var wobble = truck.GetComponent<BalatroWobble>();
                if (wobble != null) wobble.Rebase();
            }
        }
    }

    static float VisibleBottom(SpriteRenderer sr, int padPx)
    {
        return sr.bounds.min.y + padPx / sr.sprite.pixelsPerUnit * Mathf.Abs(sr.transform.lossyScale.y);
    }

    static void Lift(Transform t, float bottom, float line)
    {
        var p = t.position; p.y += line - bottom; t.position = p;
    }
}
