using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// The tutorial banner as a taped note (owner, 27 November 2026: "for tutorial i like the taped note, put the tape in the
/// middle. when a tutorial banner shows up, add an animation for it to show up"; picked animation C, slide and flutter).
/// A torn cream paper strip (Resources/Tutorial/note_paper, 9-sliced, 20 units of shadow on every side), one mint gingham
/// washi tape centred on its top edge at -4 degrees (Tutorial/note_tape), the words in Delius bold, dark brown.
/// Used by every tutorial banner: TutorialDirector's caption, the menu lesson (MenuBook) and the arrangement hint
/// (GamePhaseManager). Art and its script: Store/ui/tutorial_banner_2026-11-26 (game_art.py).
public class TapedNote : MonoBehaviour
{
    public const float Tilt = -1.2f;                                   // the note lies a little crooked
    public const float Pad = 20f;                                      // the picture's shadow margin, canvas units
    public static readonly Color Ink = new Color(0.24f, 0.12f, 0.04f);
    private const float SlideSeconds = 0.4f, FlutterFrom = 0.36f, FlutterSeconds = 0.36f, SlideFrom = 650f;

    private RectTransform rt;
    private CanvasGroup group;
    private bool playing;
    private float time;
    private Vector2 home, written;
    private Vector3 baseScale, writtenScale;

    /// Real seconds per frame, or the recorder's fixed step (Time.captureFramerate), as MenuBook does.
    private static float Dt => Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;

    /// Turns a banner (an Image with its words) into the taped note. Safe to call again.
    public static TapedNote Dress(Image card, TMP_Text words, float maxFont)
    {
        if (card == null) return null;
        var paper = Resources.Load<Sprite>("Tutorial/note_paper");
        if (paper != null) { card.sprite = paper; card.type = Image.Type.Sliced; card.pixelsPerUnitMultiplier = 1f; card.color = Color.white; }
        var rt = card.rectTransform;
        if (rt.Find("Tape") == null)
        {
            var tapeSprite = Resources.Load<Sprite>("Tutorial/note_tape");
            if (tapeSprite != null)
            {
                var go = new GameObject("Tape", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(rt, false);
                var tape = go.GetComponent<Image>(); tape.sprite = tapeSprite; tape.raycastTarget = false;
                var trt = tape.rectTransform; trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f); trt.pivot = new Vector2(0.5f, 0.5f);
                trt.anchoredPosition = new Vector2(0f, -Pad - 4f); trt.sizeDelta = new Vector2(222f, 78f); trt.localRotation = Quaternion.Euler(0f, 0f, -4f);
            }
        }
        if (words != null)
        {
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Delius SDF");
            if (font != null) { words.font = font; words.fontSharedMaterial = font.material; }
            words.fontStyle = FontStyles.Bold; words.color = Ink; words.characterSpacing = 0f;
            words.enableAutoSizing = true; words.fontSizeMax = maxFont; words.fontSizeMin = 36f;
            words.alignment = TextAlignmentOptions.Center; words.enableWordWrapping = true;
        }
        var note = card.GetComponent<TapedNote>(); if (note == null) note = card.gameObject.AddComponent<TapedNote>();
        note.rt = rt;
        if (!note.playing) rt.localRotation = Quaternion.Euler(0f, 0f, Tilt);
        return note;
    }

    /// The words inside the paper: clear of the torn edges, the shadow margin and the tape.
    public static void FitWords(RectTransform words)
    {
        if (words == null) return;
        words.anchorMin = Vector2.zero; words.anchorMax = Vector2.one; words.pivot = new Vector2(0.5f, 0.5f);
        words.offsetMin = new Vector2(Pad + 46f, Pad + 20f); words.offsetMax = new Vector2(-Pad - 46f, -Pad - 26f);
        words.localRotation = Quaternion.identity; words.localScale = Vector3.one;
    }

    /// Animation C: the note slides in from the left on a tilt and settles with a little paper flutter (a squash and
    /// two small wobbles), about 0.7 s. Call it after the note is placed; the place may still change while it plays.
    public void Appear()
    {
        if (rt == null) rt = (RectTransform)transform;
        if (group == null) { group = GetComponent<CanvasGroup>(); if (group == null) group = gameObject.AddComponent<CanvasGroup>(); }
        if (!playing) { home = rt.anchoredPosition; baseScale = rt.localScale; }
        time = 0f; playing = true;
        Pose();
    }

    private void OnDisable() { if (playing) Settle(); }

    private void Update()
    {
        if (!playing) return;
        time += Dt;
        Pose();
    }

    private void Pose()
    {
        // someone else moved or sized the note while it plays (its place for this screen): that is the new rest place
        if (time > 0f && rt.anchoredPosition != written) home = rt.anchoredPosition;
        if (time > 0f && rt.localScale != writtenScale) baseScale = rt.localScale;
        if (time >= FlutterFrom + FlutterSeconds) { Settle(); return; }
        var u = Mathf.Clamp01(time / SlideSeconds); var e = 1f - Mathf.Pow(1f - u, 3f);
        var offset = new Vector2(-SlideFrom * (1f - e), 40f * Mathf.Sin(Mathf.PI * u) * (1f - e));
        var rot = Tilt - 10f * (1f - e); var scale = Vector3.one;
        if (time > FlutterFrom)
        {
            var v = Mathf.Clamp01((time - FlutterFrom) / FlutterSeconds); var damp = Mathf.Pow(1f - v, 1.5f);
            rot = Tilt + 3f * Mathf.Sin(2f * Mathf.PI * 2f * v) * damp;
            var sq = 0.06f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(v * 2.2f)) * damp; scale = new Vector3(1f + sq, 1f - sq, 1f);
        }
        if (group != null) group.alpha = Mathf.Clamp01(time / 0.1f);
        written = home + offset; rt.anchoredPosition = written;
        writtenScale = Vector3.Scale(baseScale, scale); rt.localScale = writtenScale;
        rt.localRotation = Quaternion.Euler(0f, 0f, rot);
    }

    private void Settle()
    {
        playing = false;
        if (rt == null) return;
        if (rt.anchoredPosition != written) home = rt.anchoredPosition;
        if (rt.localScale != writtenScale) baseScale = rt.localScale;
        rt.anchoredPosition = home; rt.localScale = baseScale; rt.localRotation = Quaternion.Euler(0f, 0f, Tilt);
        if (group != null) group.alpha = 1f;
    }
}
