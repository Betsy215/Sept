using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// The candy jar on the table's back band (owner, 26 October 2026): an empty jar from the start; the shop sells candies
/// (three fill it); every tap on the jar gives one candy away: the customer with the least patience left gets one whole
/// flower of time back. Built at runtime by Ensure() from LevelManager.Start, next to the flower pots under TableDecor;
/// pictures Resources/Decor/candy_jar_0..3 (Store/redesign/candy_jar/jar.py). Candies live in the session (SessionManager.Candies).
public class CandyJar : MonoBehaviour, IPointerClickHandler
{
    public const int Full = 3;
    public static readonly Vector3 LocalPosition = new Vector3(0.62f, 0.22f, 0f);   // on the back band between the hen and the pots
    public const float Scale = 0.30f;

    private SpriteRenderer sr; private OrderSystem orderSystem; private Coroutine shake; private Vector3 rest;

    public static void Ensure()
    {
        if (FindObjectOfType<CandyJar>() != null) return;
        var decor = GameObject.Find("TableDecor"); if (decor == null) return;
        var pot = decor.transform.Find("PotFlowers"); var potSr = pot != null ? pot.GetComponent<SpriteRenderer>() : null;
        var go = new GameObject("CandyJar", typeof(SpriteRenderer), typeof(BoxCollider), typeof(CandyJar)); go.transform.SetParent(decor.transform, false);
        go.transform.localPosition = LocalPosition; go.transform.localScale = Vector3.one * Scale;
        var sr = go.GetComponent<SpriteRenderer>(); sr.sortingLayerID = potSr != null ? potSr.sortingLayerID : 0; sr.sortingOrder = potSr != null ? potSr.sortingOrder : 1;
        var jar = go.GetComponent<CandyJar>(); jar.sr = sr; jar.Refresh();
        var s = sr.sprite != null ? sr.sprite.bounds.size : new Vector3(2.4f, 2.4f, 0f); go.GetComponent<BoxCollider>().size = new Vector3(s.x, s.y, 0.2f);
    }

    private void Start() { orderSystem = FindObjectOfType<OrderSystem>(); rest = transform.localPosition; Refresh(); }

    public static int Candies => SessionManager.Instance != null ? SessionManager.Instance.Candies : 0;

    public void Refresh()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        var s = Resources.Load<Sprite>("Decor/candy_jar_" + Mathf.Clamp(Candies, 0, Full)); if (s != null) sr.sprite = s;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.pointerCurrentRaycast.gameObject != gameObject) return;
        Tap();
    }

    /// One candy to the customer lowest on patience: a whole flower of time back; nothing to give or no customer: a shake
    public bool Tap()
    {
        if (Time.timeScale == 0f) return false;
        if (orderSystem == null) orderSystem = FindObjectOfType<OrderSystem>();
        if (Candies <= 0 || orderSystem == null || !orderSystem.GiveFlowerToLowest()) { Reject(); return false; }
        SessionManager.Instance.UseCandy(); Refresh();
        StartCoroutine(Pop(transform));
        var give = Resources.Load<AudioClip>("Decor/candy_give"); if (give != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(give);
        return true;
    }

    private void Reject()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayWrongItemSFX();
        if (shake != null) StopCoroutine(shake); shake = StartCoroutine(Shake());
    }

    private IEnumerator Pop(Transform t)
    {
        var s0 = t.localScale; var e = 0f;
        while (e < 0.18f) { e += Time.deltaTime; var k = Mathf.Sin(Mathf.Clamp01(e / 0.18f) * Mathf.PI); t.localScale = s0 * (1f + 0.22f * k); yield return null; }
        t.localScale = s0;
    }

    private IEnumerator Shake()
    {
        for (var i = 0; i < 3; i++)
        {
            transform.localPosition = rest + Vector3.right * 0.06f; yield return new WaitForSeconds(0.06f);
            transform.localPosition = rest + Vector3.left * 0.06f; yield return new WaitForSeconds(0.06f);
        }
        transform.localPosition = rest; shake = null;
    }
}
