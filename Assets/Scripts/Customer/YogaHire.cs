using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// The yoga lady, hired in the star shop (owner, 14 November 2026): "after purchased, she shows up in the game once a
/// while, not always, but at least half of the game time for each level, she sits in the background and does yoga,
/// when she appears, she does each poses, every poses when finishes, all the customer in game gets one flower, we can
/// tune the timing by setting the duration for each pose, for now, do 4 seconds for each pose."
/// She sits on the grass (chapter 1) or on a float on the sea (chapter 2), behind the customers, at a spot of her own
/// per chapter. A day's plan: enough
/// poses to fill at least half the day, split into visits of at least one whole round of her five poses, with the
/// rest of the day as breaks of random length before, between and after. Frames: Resources/Yoga/sit_0..4 (ChatGPT's
/// sheet, cut by Store/characters/yoga_sit_2026-11-14/cut_frames.py).
public class YogaHire : MonoBehaviour
{
    public const string ItemName = "StarYoga";   // starts with "Star": the shop files it under the star tab
    public const int StarPrice = 15;
    public const float PoseSeconds = 4f;         // the tuning knob: how long she holds each pose
    public const float DayShare = 0.5f;          // she is there for at least this share of the day
    private const int Poses = 5;
    private const float Fade = 0.5f, Blend = 0.35f, FirstBreakMin = 1.5f;
    // Where her mat's lower edge sits, per chapter (owner, 14 November 2026: "can we put her in different position in
    // different chapter?"). Chapter 1: back on the grass and to the right, by the tree ("move her back (higher) a
    // little", "closer to the tree on the right"). Chapter 2: out on the water on a small float ("put her on a small
    // float in ocean instead"), a little smaller because she is further away, bobbing gently.
    // x, y in world units; the table's far edge is y = 0, the screen is 4.6 wide; chapter 2's water runs from about
    // y = 0.7 (the foam) to 1.95 (the horizon).
    private static readonly Vector2 MatChapter1 = new Vector2(1.7f, 0.8f), MatChapter2 = new Vector2(0.05f, 1.12f);
    private const float ScaleChapter2 = 0.78f;
    private const float MatToCentre = 0.715f;   // from the mat's lower edge up to the picture's centre
    private const float Behind = 0.5f;          // z behind the customers (same layer and order: the farther one is drawn first)
    private const float BobHeight = 0.03f, BobTilt = 1.5f;

    public static Vector3 SpotFor(int chapter)
    {
        var mat = chapter >= 2 ? MatChapter2 : MatChapter1;
        return new Vector3(mat.x, mat.y + MatToCentre * (chapter >= 2 ? ScaleChapter2 : 1f), Behind);
    }

    private LevelManager levelManager;
    private OrderSystem orderSystem;
    private SpriteRenderer front, back;          // two pictures so one pose melts into the next
    private SpriteRenderer floatPicture;         // chapter 2: the float under her mat (Resources/Yoga/float)
    private bool onWater;
    private Vector3 home;
    private readonly Sprite[] frames = new Sprite[Poses];

    public static bool Owned => SessionManager.Instance != null && SessionManager.Instance.OwnsStarItem(ItemName);

    /// Called when a day's play starts (not in the tutorials). Does nothing unless she was hired.
    public static void StartDay(LevelManager lm, OrderSystem os)
    {
        if (!Owned || lm == null || FindObjectOfType<YogaHire>() != null) return;
        var go = new GameObject("YogaHire");
        var chapter = SessionManager.CurrentChapter;
        go.transform.position = SpotFor(chapter);
        var y = go.AddComponent<YogaHire>();
        y.levelManager = lm; y.orderSystem = os; y.onWater = chapter >= 2; y.home = go.transform.position;
        if (y.onWater) go.transform.localScale = Vector3.one * ScaleChapter2;
    }

    /// The day's plan: how many poses each visit holds. At least DayShare of the day in all, each visit at least one
    /// whole round; a very short day gets one round or what fits.
    public static List<int> Plan(float daySeconds)
    {
        var plan = new List<int>();
        var room = Mathf.FloorToInt((daySeconds - FirstBreakMin - 2f * Fade) / PoseSeconds);
        var poses = Mathf.Min(room, Mathf.Max(Poses, Mathf.CeilToInt(daySeconds * DayShare / PoseSeconds)));
        if (poses <= 0) return plan;
        var visits = Mathf.Max(1, poses / Poses);
        for (var v = 0; v < visits; v++) plan.Add(poses / visits + (v < poses % visits ? 1 : 0));
        return plan;
    }

    private void Start()
    {
        for (var k = 0; k < Poses; k++) frames[k] = Resources.Load<Sprite>("Yoga/sit_" + k);
        if (frames[0] == null) { Destroy(gameObject); return; }
        back = NewPicture("Back", 0); front = NewPicture("Front", 1);
        if (onWater)
        {
            floatPicture = NewPicture("Float", -1);
            floatPicture.sprite = Resources.Load<Sprite>("Yoga/float");
            floatPicture.transform.localPosition = new Vector3(0f, -MatToCentre + 0.0125f, 0.01f);   // the mat rests on the middle of its top
        }
        StartCoroutine(Day());
    }

    /// On the water she and her float rise, sink and tip a little; the float shows whenever she does.
    private void Update()
    {
        if (!onWater || front == null) return;
        var shown = Mathf.Max(front.color.a, back.color.a);
        if (floatPicture != null) floatPicture.color = new Color(1f, 1f, 1f, shown);
        if (shown <= 0f) return;
        transform.position = home + new Vector3(0f, Mathf.Sin(Time.time * 1.6f) * BobHeight, 0f);
        transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.1f + 0.7f) * BobTilt);
    }

    private SpriteRenderer NewPicture(string name, int nearer)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, -0.01f * nearer);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(1f, 1f, 1f, 0f);
        return sr;
    }

    private IEnumerator Day()
    {
        var day = levelManager != null ? levelManager.DayDuration : 0f;
        var plan = Plan(day);
        if (plan.Count == 0) yield break;
        var busy = 0f; foreach (var n in plan) busy += n * PoseSeconds + 2f * Fade;
        // the free time, cut into breaks of random length: one before each visit and one after the last
        var free = Mathf.Max(0f, day - busy - FirstBreakMin);
        var cuts = new float[plan.Count + 1]; var sum = 0f;
        for (var i = 0; i < cuts.Length; i++) { cuts[i] = Random.Range(0.4f, 1.6f); sum += cuts[i]; }
        var pose = 0;
        for (var v = 0; v < plan.Count; v++)
        {
            yield return Wait((v == 0 ? FirstBreakMin : 0f) + free * cuts[v] / sum);
            if (Ending) yield break;
            front.sprite = frames[pose % Poses]; back.sprite = null;
            yield return FadeTo(1f);
            for (var k = 0; k < plan[v]; k++)
            {
                yield return Wait(PoseSeconds - (k < plan[v] - 1 ? Blend : 0f));
                if (orderSystem != null) orderSystem.GiveFlowerToAll();   // the pose is done: a flower for everyone waiting
                pose++;
                if (Ending) break;
                if (k < plan[v] - 1) yield return BlendTo(frames[pose % Poses]);
            }
            yield return FadeTo(0f);
        }
    }

    private bool Ending => levelManager == null || (levelManager.levelCompletePanel != null && levelManager.levelCompletePanel.activeInHierarchy);

    private static IEnumerator Wait(float seconds)
    {
        for (var t = 0f; t < seconds; t += Time.deltaTime) yield return null;   // game time: a pause holds her too
    }

    private IEnumerator FadeTo(float alpha)
    {
        var from = front.color.a;
        for (var t = 0f; t < Fade; t += Time.deltaTime)
        {
            var a = Mathf.Lerp(from, alpha, t / Fade);
            front.color = new Color(1f, 1f, 1f, a); back.color = new Color(1f, 1f, 1f, 0f);
            yield return null;
        }
        front.color = new Color(1f, 1f, 1f, alpha);
    }

    /// The next pose fades in over the one she holds; the old one stays underneath until the new one is whole.
    private IEnumerator BlendTo(Sprite next)
    {
        back.sprite = front.sprite; back.color = Color.white;
        front.sprite = next; front.color = new Color(1f, 1f, 1f, 0f);
        for (var t = 0f; t < Blend; t += Time.deltaTime)
        {
            front.color = new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, t / Blend));
            yield return null;
        }
        front.color = Color.white; back.color = new Color(1f, 1f, 1f, 0f);
    }
}
