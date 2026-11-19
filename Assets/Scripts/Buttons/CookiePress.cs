using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Cookie buttons are pushed down and rise back, like a firm cookie (owner, 18 November 2026: "it gets pushed down and
/// rise back", no bounce), with a very short crunch on the press. Nothing is wired in scenes: a runner finds every
/// button whose picture is one of the cookie pictures (Store/cookies_2026-11-17/install_ui.py) and adds this to it,
/// also buttons made later in code (map, Daily Treats, menu book).
/// Three crunches (owner picked 1, 2 and 6 of Store/audio/button_crunch_2026-11-18, "use them for different kind of
/// actions"): a dry snap for going on, a nibble for going back or saying no, a light bite for small controls.
/// The push is laid on top of whatever scale and place the button already has, so buttons that move or animate
/// themselves (the pause button, the map's day circles) keep doing so. Unscaled time: it works in the pause menu too.
public class CookiePress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    enum Kind { Tap, Go, Back }
    static readonly Dictionary<string, Kind> CookieSprites = new Dictionary<string, Kind> {
        { "continue", Kind.Go }, { "new", Kind.Go }, { "ready", Kind.Go }, { "btn_play", Kind.Go }, { "btn_done", Kind.Go },
        { "collect", Kind.Go }, { "yes", Kind.Go }, { "next 1", Kind.Go }, { "coinsbutton", Kind.Go },
        { "no", Kind.Back }, { "btn_cancel", Kind.Back }, { "home", Kind.Back }, { "replay", Kind.Back },
        { "setting 1", Kind.Tap }, { "pause 1", Kind.Tap }, { "audio", Kind.Tap }, { "music 1", Kind.Tap },
        { "up 1", Kind.Tap }, { "down 1", Kind.Tap }, { "arrow_right", Kind.Tap }, { "arrow_left", Kind.Tap },
        { "day_done", Kind.Tap }, { "day_today", Kind.Tap },
    };
    const float Shrink = 0.05f;      // pushed in: 5 % smaller
    const float Sink = 0.06f;        // and lower by 6 % of its height
    const float PressRate = 45f, ReleaseRate = 16f;   // down at once; up smoothly in about a fifth of a second, never past its place

    public static float LastCrunchAt = -10f;
    static readonly AudioClip[] clips = new AudioClip[3]; static bool clipsLoaded;
    static AudioClip Clip(Kind k)
    {
        if (!clipsLoaded)
        {
            clipsLoaded = true;
            clips[(int)Kind.Tap] = Resources.Load<AudioClip>("UI/button_crunch_tap");
            clips[(int)Kind.Go] = Resources.Load<AudioClip>("UI/button_crunch_go");
            clips[(int)Kind.Back] = Resources.Load<AudioClip>("UI/button_crunch_back");
        }
        return clips[(int)k];
    }

    Kind kind;
    float amount, target;
    bool pressed, moving;
    Vector3 baseScale, lastScale, basePos, lastPos;
    Selectable selectable;
    RectTransform rt;

    void Awake() { selectable = GetComponent<Selectable>(); rt = transform as RectTransform; }

    public void OnPointerDown(PointerEventData e)
    {
        if (selectable != null && !selectable.IsInteractable()) return;
        pressed = true; target = 1f; Begin();
        var clip = Clip(kind);
        if (clip != null && AudioManager.Instance != null) { LastCrunchAt = Time.unscaledTime; AudioManager.Instance.PlaySFX(clip); }
    }
    public void OnPointerUp(PointerEventData e) { Release(); }
    public void OnPointerExit(PointerEventData e) { if (pressed) Release(); }

    void Release() { if (!pressed) return; pressed = false; target = 0f; }

    void Begin()
    {
        if (moving) return;
        moving = true; amount = 0f;
        baseScale = lastScale = transform.localScale; basePos = lastPos = transform.localPosition;
    }

    void LateUpdate()
    {
        if (!moving) return;
        var rate = pressed ? PressRate : ReleaseRate;
        amount += (target - amount) * (1f - Mathf.Exp(-rate * Time.unscaledDeltaTime));
        if (!pressed && amount < 0.002f) { amount = 0f; moving = false; }
        Write();
    }

    void Write()
    {
        var t = transform;
        // someone else set it this frame: build on it. With a tolerance: a RectTransform gives back what was written
        // with a little float rounding, which an exact compare took for a move (the map's Play sank further every
        // frame and stayed down, 18 November 2026)
        if ((t.localScale - lastScale).sqrMagnitude > 1e-6f) baseScale = t.localScale;
        if ((t.localPosition - lastPos).sqrMagnitude > 0.01f) basePos = t.localPosition;
        var k = 1f - Shrink * amount;
        var h = rt != null ? rt.rect.height * baseScale.y : 0f;
        t.localScale = lastScale = new Vector3(baseScale.x * k, baseScale.y * k, baseScale.z);
        t.localPosition = lastPos = basePos + Vector3.down * (h * Sink * amount);
    }

    void OnDisable()
    {
        if (!moving) return;
        pressed = false; moving = false; amount = 0f; Write();
    }

    /// Adds the press to every cookie button on screen: at once when a scene loads, then once a second for buttons
    /// made later. Only buttons are looked at (not every picture), so the look costs next to nothing.
    class Runner : MonoBehaviour
    {
        float next;
        readonly List<GameObject> found = new List<GameObject>();
        void Start() { Clip(Kind.Tap); UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => next = 0f; }
        void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + 1f;
            found.Clear();
            foreach (var s in FindObjectsOfType<Selectable>()) found.Add(s.gameObject);
            foreach (var b in FindObjectsOfType<PauseButton>()) found.Add(b.gameObject);
            foreach (var b in FindObjectsOfType<ClickContinue>()) found.Add(b.gameObject);
            foreach (var b in FindObjectsOfType<ClickPlay>()) found.Add(b.gameObject);
            foreach (var b in FindObjectsOfType<SettingButton>()) found.Add(b.gameObject);
            foreach (var go in found)
            {
                var img = go.GetComponent<Image>();
                if (img == null || img.sprite == null || !CookieSprites.TryGetValue(img.sprite.name, out var k)) continue;
                if (go.GetComponent<CookiePress>() != null) continue;
                var pause = go.GetComponent<PauseButton>(); var cont = go.GetComponent<ClickContinue>(); var play = go.GetComponent<ClickPlay>();
                if (go.GetComponent<Selectable>() == null && pause == null && cont == null && play == null && go.GetComponent<SettingButton>() == null) continue;
                go.AddComponent<CookiePress>().kind = k;
                if (Clip(k) == null) continue;                    // the crunch replaces their old press clicks
                if (pause != null) { pause._compressClip = null; pause._uncompressClip = null; }
                if (cont != null) { cont._compressClip = null; cont._uncompressClip = null; }
                if (play != null) { play._compressClip = null; play._uncompressClip = null; }
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var go = new GameObject("CookiePress");
        DontDestroyOnLoad(go);
        go.AddComponent<Runner>();
    }
}
