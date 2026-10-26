using UnityEngine;

/// The ice cream in a customer's order bubble, built from the parts (owner, 17 October: the order says how many
/// scoops and which flavours; their order does not matter). Since the glass-dish flow (owner, 26 October 2026) it is
/// the dish with the scoops side by side, the waffle bowl under them when the order's variant ends in 'w'; the parts
/// come from Resources/IceCream (dish_back, bowl, dish_front_*, scoop_small_*). Without them the old cone stack shows.
public class IceCreamOrderIcon : MonoBehaviour
{
    public SpriteRenderer cone; public SpriteRenderer[] scoops = new SpriteRenderer[IceCreamStation.MaxScoops];
    public Sprite[] scoopSprites = new Sprite[IceCreamStation.Flavours];      // vanilla, strawberry, chocolate, mint
    public float scoopStep = 0.62f, firstScoopDrop = 0.22f;
    public float dishScale = 0.85f;                                             // the dish against the cone's size in the bubble
    public float scoopSpread = 0.72f, scoopTop = 0.669f, scoopZig = 0.014f;    // as on the station
    private SpriteRenderer bowl, front;

    /// variant: the order letters (v s c m), one per scoop, then 'w' for a waffle bowl
    public void Show(string variant)
    {
        var dishBack = Resources.Load<Sprite>("IceCream/dish_back");
        if (dishBack == null || cone == null) { ShowCone(variant); return; }
        var waffle = variant != null && variant.IndexOf(IceCreamStation.WaffleLetter) >= 0;
        var letters = variant != null ? variant.Replace(IceCreamStation.WaffleLetter.ToString(), "") : "";
        var parts = cone.transform.parent; if (parts != null) { parts.localScale = new Vector3(dishScale, dishScale, 1f); parts.localPosition = Vector3.zero; }
        cone.sprite = dishBack; cone.transform.localPosition = Vector3.zero;
        if (bowl == null) bowl = Part("Bowl", cone.sortingOrder + 1);
        if (front == null) front = Part("Front", cone.sortingOrder + 6);
        bowl.sprite = Resources.Load<Sprite>("IceCream/bowl"); bowl.enabled = waffle && bowl.sprite != null;
        front.sprite = Resources.Load<Sprite>("IceCream/dish_front_" + (waffle ? "bowl" : "empty")); front.enabled = front.sprite != null;
        var n = letters.Length; var dh = dishBack.bounds.size.y; var dmin = dishBack.bounds.min.y;
        for (var i = 0; i < scoops.Length; i++)
        {
            if (scoops[i] == null) continue;
            var on = i < n; scoops[i].enabled = on; if (!on) continue;
            var f = Mathf.Max(0, IceCreamStation.Letters.IndexOf(letters[i]));
            var s = Resources.Load<Sprite>("IceCream/scoop_small_" + IceCreamStation.Letters[f]); scoops[i].sprite = s != null ? s : scoopSprites[f];
            scoops[i].sortingOrder = cone.sortingOrder + 2 + i;
            var sw = scoops[i].sprite != null ? scoops[i].sprite.bounds.size.x : 0.4f;
            scoops[i].transform.localPosition = new Vector3((i - (n - 1) * 0.5f) * sw * scoopSpread, dmin + scoopTop * dh - (i % 2 == 1 ? scoopZig * dh : 0f), -0.001f * (i + 2));
        }
    }

    private SpriteRenderer Part(string name, int sortingOrder)
    {
        var go = new GameObject(name, typeof(SpriteRenderer)); go.transform.SetParent(cone.transform.parent, false); go.transform.localPosition = new Vector3(0f, 0f, -0.001f * (sortingOrder - cone.sortingOrder));
        var sr = go.GetComponent<SpriteRenderer>(); sr.sortingLayerName = cone.sortingLayerName; sr.sortingOrder = sortingOrder; return sr;
    }

    /// The cone flow's icon: the scoops stacked on the cone
    private void ShowCone(string variant)
    {
        var coneH = cone != null && cone.sprite != null ? cone.sprite.bounds.size.y : 1f;
        var scoopH = scoopSprites[0] != null ? scoopSprites[0].bounds.size.y : 0.6f;
        for (var i = 0; i < scoops.Length; i++)
        {
            if (scoops[i] == null) continue;
            var on = variant != null && i < variant.Length;
            scoops[i].enabled = on;
            if (!on) continue;
            var f = Mathf.Max(0, IceCreamStation.Letters.IndexOf(variant[i]));
            scoops[i].sprite = scoopSprites[f];
            scoops[i].transform.localPosition = new Vector3(0f, coneH * 0.5f - firstScoopDrop * scoopH + i * scoopStep * scoopH, -0.001f * (i + 1));
        }
        // the whole icon is taller with more scoops: keep its middle where the order layout put it
        var n = variant != null ? variant.Length : 0;
        var shift = (coneH + Mathf.Max(0, n - 1) * scoopStep * scoopH + (n > 0 ? scoopH * (1f - firstScoopDrop) : 0f)) * 0.5f - coneH * 0.5f;
        if (cone != null) cone.transform.parent.localPosition = new Vector3(0f, -shift * 0.5f, 0f);
    }
}
