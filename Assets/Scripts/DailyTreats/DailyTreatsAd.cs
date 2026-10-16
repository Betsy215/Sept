using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.UI;

/// <summary>
/// "One more treat" (owner, 16 October 2026): after the daily coins are collected the board stays, and the Collect
/// button comes back as an ad button. Each watched ad pays a random 20 to 50 coins, shown with the coin and the
/// amount popping up with sparkles, and the button returns, so it repeats. A small cross closes the board.
/// Uses the Shop's rewarded ad unit; the coins take the same "held until Continue or New" path as the daily coins.
/// Motion: the button sinks on the tap and comes back as the ad button, then breathes; after a reward it hops once.
/// In the editor, where no ad can show, the ad is simulated after a second so the flow can be played and recorded.
/// </summary>
public class DailyTreatsAd : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    public const string AdUnitId = "Rewarded_iOS";
    public const int MinCoins = 20, MaxCoins = 50;

    private RectTransform canvasRect;
    private Image button, coin, sparkles;
    private RectTransform amount;                 // the "+N" glyphs, laid out at runtime
    private TextMeshProUGUI label, small;
    private GameObject close;
    private bool adReady, loading, showing, closed;
    private Coroutine breathing;
    private System.Action<int> onCoins;           // pays the coins (DailyTreats keeps the save path)
    private System.Action onClose;
    private Image dailyReward;                    // the +50 picture, replaced by the ad reward

    private static Sprite Load(string name) => Resources.Load<Sprite>("DailyTreats/" + name);

    /// Builds the ad button and the reward pieces on the Daily Treats canvas; call once the daily reward has popped.
    public void Begin(RectTransform canvas, Image coinAndAmountToHide, Image sparklesImage, Sprite buttonSprite,
                      System.Action<int> payCoins, System.Action closeBoard)
    {
        canvasRect = canvas; sparkles = sparklesImage; onCoins = payCoins; onClose = closeBoard; dailyReward = coinAndAmountToHide;
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/beachday SDF");
        var plain = Resources.Load<Material>("Fonts & Materials/beachday SDF - Plain");

        // The coin alone (the baked "+50" picture goes; the amount is composed per reward)
        coin = NewImage("AdCoin", Load("glyph_coin") != null ? Load("glyph_coin") : null, Color.white);
        if (coin.sprite == null) { coin.sprite = coinAndAmountToHide.sprite; }   // fallback: the whole +50 picture
        Place(coin.rectTransform, -200f, -190f, 150f, 150f);
        coin.gameObject.SetActive(false);
        var a = new GameObject("AdAmount", typeof(RectTransform)); a.transform.SetParent(canvasRect, false);
        amount = (RectTransform)a.transform; Place(amount, 60f, -190f, 400f, 180f); a.SetActive(false);

        // The button: the blank felt with the words on it (no extra layer behind the letters, owner)
        var blank = Load("button_blank"); button = NewImage("OneMoreTreat", blank != null ? blank : buttonSprite, Color.white);
        Place(button.rectTransform, 16f, -431f, 548f, 305f);
        button.raycastTarget = true;
        label = NewText("Label", button.rectTransform, font, 58f, new Color(0.8f, 0.47f, 0.16f));
        label.text = "ONE MORE TREAT"; Place(label.rectTransform, -8f, 22f, 520f, 90f);
        small = NewText("Small", button.rectTransform, font, 30f, new Color(0.28f, 0.15f, 0.055f));
        small.text = "watch an ad"; if (plain != null) small.fontSharedMaterial = plain; Place(small.rectTransform, -8f, -40f, 520f, 50f);
        var b = button.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.onClick.AddListener(OnTap);

        // A small cross at the board's top right corner
        close = new GameObject("Close", typeof(RectTransform), typeof(Image)); close.transform.SetParent(canvasRect, false);
        var ci = close.GetComponent<Image>(); var cross = Resources.Load<Sprite>("UI/cross"); if (cross != null) ci.sprite = cross; else ci.color = new Color(0.86f, 0.24f, 0.2f);
        Place((RectTransform)close.transform, 440f, 540f, 76f, 76f);
        close.AddComponent<Button>().onClick.AddListener(() => { if (!showing) { closed = true; onClose?.Invoke(); } });

        button.transform.localScale = Vector3.one * 0.6f;
        StartCoroutine(Pop(button.transform, 0.6f, 1f, 0.28f));
        breathing = StartCoroutine(Breathe());
        Preload();
    }

    #region Ads

    private void Preload()
    {
        if (adReady || loading) return;
        loading = true;
        if (Application.isEditor) { loading = false; adReady = true; return; }       // simulated in the editor
        if (!Advertisement.isInitialized) { loading = false; return; }
        Advertisement.Load(AdUnitId, this);
    }

    private void OnTap()
    {
        if (showing || closed) return;
        StartCoroutine(TapThenShow());
    }

    private IEnumerator TapThenShow()
    {
        showing = true;
        yield return Pop(button.transform, 1f, 0.9f, 0.1f);
        yield return Pop(button.transform, 0.9f, 1f, 0.14f);
        var waited = 0f;
        while (!adReady && waited < 8f) { Preload(); label.text = "LOADING..."; yield return new WaitForSecondsRealtime(0.5f); waited += 0.5f; }
        label.text = "ONE MORE TREAT";
        if (!adReady) { showing = false; yield return Fade(button, 0.4f); onClose?.Invoke(); yield break; }
        adReady = false;
        if (Application.isEditor) { yield return new WaitForSecondsRealtime(1f); OnUnityAdsShowComplete(AdUnitId, UnityAdsShowCompletionState.COMPLETED); yield break; }
        Advertisement.Show(AdUnitId, this);
    }

    public void OnUnityAdsAdLoaded(string id) { if (id == AdUnitId) { loading = false; adReady = true; } }
    public void OnUnityAdsFailedToLoad(string id, UnityAdsLoadError e, string m) { if (id == AdUnitId) { loading = false; adReady = false; } }
    public void OnUnityAdsShowStart(string id) { }
    public void OnUnityAdsShowClick(string id) { }
    public void OnUnityAdsShowFailure(string id, UnityAdsShowError e, string m) { if (id == AdUnitId) { showing = false; Preload(); } }

    public void OnUnityAdsShowComplete(string id, UnityAdsShowCompletionState state)
    {
        if (id != AdUnitId) return;
        var done = state == UnityAdsShowCompletionState.COMPLETED;
        AnalyticsLog.AdWatched("dailyTreat", done);
        if (!done) { showing = false; Preload(); return; }
        var coins = Random.Range(MinCoins, MaxCoins + 1);
        onCoins?.Invoke(coins);
        StartCoroutine(Reward(coins));
    }

    #endregion

    #region The reward effect

    /// The coin and "+N" pop up with the sparkles, the amount lands with a bounce, then the button hops back.
    private IEnumerator Reward(int coins)
    {
        // compose the amount from the glyph sprites
        for (var i = amount.childCount - 1; i >= 0; i--) Destroy(amount.GetChild(i).gameObject);
        var text = "+" + coins; var x = 0f; var widths = new float[text.Length]; var total = 0f;
        for (var i = 0; i < text.Length; i++) { var s = Load(text[i] == '+' ? "glyph_plus" : "glyph_" + text[i]); widths[i] = s != null ? 180f * s.rect.width / s.rect.height : 100f; total += widths[i]; }
        x = -total / 2f;
        for (var i = 0; i < text.Length; i++)
        {
            var s = Load(text[i] == '+' ? "glyph_plus" : "glyph_" + text[i]);
            var g = NewImage("G" + i, s, Color.white); g.transform.SetParent(amount, false);
            var rt = g.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0f); rt.sizeDelta = new Vector2(widths[i], 180f); x += widths[i] - 6f;
        }
        if (dailyReward != null) dailyReward.gameObject.SetActive(false);
        sparkles.gameObject.SetActive(true); sparkles.transform.localScale = Vector3.one * 0.6f;
        coin.gameObject.SetActive(true); amount.gameObject.SetActive(true);
        coin.transform.localScale = Vector3.zero; amount.localScale = Vector3.zero;
        StartCoroutine(Pop(sparkles.transform, 0.6f, 1f, 0.3f));
        StartCoroutine(Pop(coin.transform, 0f, 1f, 0.3f));
        yield return new WaitForSecondsRealtime(0.1f);
        // the amount: up from nothing with an overshoot, then a bounce and a little wobble
        var t = 0f;
        while (t < 0.45f)
        {
            t += Time.unscaledDeltaTime; var k = Mathf.Clamp01(t / 0.45f);
            var s = k < 0.6f ? Mathf.LerpUnclamped(0f, 1.35f, 1f - (1f - k / 0.6f) * (1f - k / 0.6f)) : Mathf.Lerp(1.35f, 1f, (k - 0.6f) / 0.4f);
            amount.localScale = Vector3.one * s; amount.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * Mathf.PI * 2f) * 6f * (1f - k));
            yield return null;
        }
        amount.localScale = Vector3.one; amount.localRotation = Quaternion.identity;
        for (var i = 0; i < amount.childCount; i++) StartCoroutine(Hop(amount.GetChild(i), 0.05f * i, 26f));   // the digits hop in turn
        yield return new WaitForSecondsRealtime(1.3f);
        showing = false;
        yield return Hop(button.transform, 0f, 30f);
        Preload();
    }

    #endregion

    #region Little motions

    private IEnumerator Breathe()
    {
        var t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime;
            if (!showing) button.transform.localScale = Vector3.one * (1f + 0.02f + 0.02f * Mathf.Sin(t * Mathf.PI));
            yield return null;
        }
    }

    private static IEnumerator Pop(Transform tr, float from, float to, float seconds)
    {
        var t = 0f;
        while (t < seconds && tr != null) { t += Time.unscaledDeltaTime; var k = Mathf.Clamp01(t / seconds); tr.localScale = Vector3.one * Mathf.LerpUnclamped(from, to, 1f - (1f - k) * (1f - k)); yield return null; }
        if (tr != null) tr.localScale = Vector3.one * to;
    }

    private static IEnumerator Hop(Transform tr, float delay, float height)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        var rt = tr as RectTransform; if (rt == null) yield break;
        var start = rt.anchoredPosition; var t = 0f;
        while (t < 0.32f) { t += Time.unscaledDeltaTime; var k = Mathf.Clamp01(t / 0.32f); rt.anchoredPosition = start + Vector2.up * (Mathf.Sin(k * Mathf.PI) * height); yield return null; }
        rt.anchoredPosition = start;
    }

    private static IEnumerator Fade(Image img, float seconds)
    {
        var t = 0f; var c = img.color;
        while (t < seconds && img != null) { t += Time.unscaledDeltaTime; img.color = new Color(c.r, c.g, c.b, 1f - t / seconds); yield return null; }
    }

    #endregion

    #region UI helpers

    private Image NewImage(string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvasRect, false);
        var img = go.GetComponent<Image>(); img.sprite = sprite; img.color = color; img.preserveAspect = true; img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI NewText(string name, RectTransform parent, TMP_FontAsset font, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = size; tmp.color = color; tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
        return tmp;
    }

    private static void Place(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
    }

    #endregion
}
