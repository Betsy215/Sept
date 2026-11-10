using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Shop touches picked by the owner for the November 2026 release (idea e, 9 November): a NEW tag on a card during the
/// first visit in which it is open to buy, and the bought item flying out of the popup to the Next button, the way
/// to the table. Seen cards are kept in PlayerPrefs; the very first visit (new player, or a player updating to this
/// version) counts everything then open as seen, so day 1 shows no wall of tags.
public static class ShopFlourish
{
    private const string SeenKey = "ftc_shop_seen_";     // + card name
    private const string InitKey = "ftc_shop_seen_init";
    private static readonly HashSet<string> taggedThisVisit = new HashSet<string>();
    public static float FlyDuration = 0.7f;              // the editor capture slows this down

    public static void BeginVisit() => taggedThisVisit.Clear();

    private static bool Open(ShopItemController c) =>
        c != null && c.gameObject.activeInHierarchy && !c.isPurchased
        && !SessionManager.IsLockedInShop(c.itemName) && !SessionManager.IsLockedInShop(c.gameObject.name);

    /// After the cards are laid out (and after every tab switch): tag the open cards not seen before.
    public static void ApplyTags(IEnumerable<ShopItemController> cards)
    {
        var init = PlayerPrefs.GetInt(InitKey, 0) == 1;
        foreach (var c in cards)
        {
            if (c == null) continue;
            var show = false;
            if (!init) { if (Open(c)) PlayerPrefs.SetInt(SeenKey + c.itemName, 1); }
            else if (Open(c) && (PlayerPrefs.GetInt(SeenKey + c.itemName, 0) == 0 || taggedThisVisit.Contains(c.itemName)))
            { show = true; taggedThisVisit.Add(c.itemName); }
            SetTag(c, show);
        }
        if (!init) { PlayerPrefs.SetInt(InitKey, 1); PlayerPrefs.Save(); }
    }

    /// On leaving the shop: every card that was open this visit is seen; cards on tabs never opened stay new.
    public static void MarkSeen(IEnumerable<ShopItemController> cards)
    {
        foreach (var c in cards)
            if (c != null && c.gameObject.activeInHierarchy && !SessionManager.IsLockedInShop(c.itemName) && !SessionManager.IsLockedInShop(c.gameObject.name))
                PlayerPrefs.SetInt(SeenKey + c.itemName, 1);
        PlayerPrefs.Save();
    }

    private static void SetTag(ShopItemController card, bool show)
    {
        var icon = card.transform.Find("ItemIcon") as RectTransform; if (icon == null) return;
        var tag = icon.Find("NewTag");
        if (tag == null)
        {
            if (!show) return;
            var go = new GameObject("NewTag", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(icon, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-18f, -14f); rt.sizeDelta = new Vector2(132f, 50f);
            rt.localRotation = Quaternion.Euler(0f, 0f, 12f);
            var img = go.GetComponent<Image>(); img.sprite = null; img.color = new Color(0.87f, 0.30f, 0.32f); img.raycastTarget = false;
            var lgo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            lgo.transform.SetParent(go.transform, false);
            var lrt = lgo.GetComponent<RectTransform>(); lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var t = lgo.GetComponent<TextMeshProUGUI>(); t.text = "NEW"; t.fontSize = 34f; t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center; t.color = Color.white; t.raycastTarget = false;
            var cardFont = card.GetComponentInChildren<TextMeshProUGUI>(true); if (cardFont != null && cardFont != t) t.font = cardFont.font;
            tag = go.transform;
        }
        tag.gameObject.SetActive(show);
    }

    /// The bought item hops out of the popup, shrinks and lands on the Next button, which gives a small bounce.
    public static void Fly(MonoBehaviour host, Image from, RectTransform to)
    {
        if (host == null || from == null || to == null || from.sprite == null || from.canvas == null) return;
        host.StartCoroutine(FlyRoutine(host, from, to));
    }

    private static IEnumerator FlyRoutine(MonoBehaviour host, Image from, RectTransform to)
    {
        var canvas = from.canvas;
        var go = new GameObject("FlyingItem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvas.transform, false); go.transform.SetAsLastSibling();
        var img = go.GetComponent<Image>(); img.sprite = from.sprite; img.preserveAspect = true; img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        var k = canvas.transform.lossyScale.x > 0f ? from.rectTransform.lossyScale.x / canvas.transform.lossyScale.x : 1f;
        rt.sizeDelta = from.rectTransform.rect.size * k;
        var start = from.rectTransform.TransformPoint(from.rectTransform.rect.center);
        var end = to.TransformPoint(to.rect.center);
        rt.position = start;
        var lift = ((RectTransform)canvas.transform).rect.height * 0.10f * canvas.transform.lossyScale.y;
        var d = Mathf.Max(0.1f, FlyDuration); var t = 0f;
        while (t < d)
        {
            t += Time.unscaledDeltaTime; var u = Mathf.Clamp01(t / d); var e = u * u * (3f - 2f * u);
            rt.position = Vector3.Lerp(start, end, e) + Vector3.up * (Mathf.Sin(u * Mathf.PI) * lift);
            rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.3f, e);
            img.color = new Color(1f, 1f, 1f, u < 0.85f ? 1f : 1f - (u - 0.85f) / 0.15f);
            yield return null;
        }
        Object.Destroy(go);
        var baseScale = to.localScale; var p = 0f;
        while (p < 0.25f)
        {
            p += Time.unscaledDeltaTime; var u = Mathf.Clamp01(p / 0.25f);
            to.localScale = baseScale * (1f + 0.12f * Mathf.Sin(u * Mathf.PI));
            yield return null;
        }
        to.localScale = baseScale;
    }
}
