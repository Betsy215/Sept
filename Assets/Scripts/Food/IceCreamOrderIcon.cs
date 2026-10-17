using UnityEngine;

/// The ice cream in a customer's order bubble: a cone with the ordered scoops stacked on it, built from the parts
/// (owner, 17 October: the order says how many scoops and which flavours; their order on the cone does not matter).
public class IceCreamOrderIcon : MonoBehaviour
{
    public SpriteRenderer cone; public SpriteRenderer[] scoops = new SpriteRenderer[IceCreamStation.MaxScoops];
    public Sprite[] scoopSprites = new Sprite[IceCreamStation.Flavours];      // vanilla, strawberry, chocolate, mint
    public float scoopStep = 0.62f, firstScoopDrop = 0.22f;

    /// variant: the order letters (v s c m), one per scoop
    public void Show(string variant)
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
