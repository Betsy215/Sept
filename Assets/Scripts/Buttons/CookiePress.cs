using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Cookie buttons press down and spring back (owner, 18 November 2026: "When pressed, cookie is pressed down, when
/// release, it bounce back", with a very short crunch on the press). Nothing is wired in scenes: a runner finds every
/// button whose picture is one of the cookie pictures (Store/cookies_2026-11-17/install_ui.py) and adds this to it,
/// also buttons made later in code (map, Daily Treats, menu book).
/// The squash multiplies whatever scale the button already has, so buttons that animate their own scale (the map's
/// day circles) keep doing so. Unscaled time: it works in the pause menu too.
public class CookiePress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public static readonly HashSet<string> CookieSprites = new HashSet<string> {
        "setting 1", "pause 1", "audio", "music 1", "home", "replay", "next 1", "up 1", "down 1", "yes", "no",
        "continue", "new", "collect", "ready", "coinsbutton", "btn_done", "btn_play", "btn_cancel",
        "day_done", "day_today", "arrow_right", "arrow_left",
    };
    static readonly Vector2 Pressed = new Vector2(1.06f, 0.88f);   // wider and lower: pushed into the table
    const float PressStiffness = 1400f, PressDamping = 60f;        // down fast, no wobble
    const float ReleaseStiffness = 420f, ReleaseDamping = 12f;     // back up past its size once or twice, then still

    public static float LastCrunchAt = -10f;
    static AudioClip crunch; static bool crunchLoaded;
    public static AudioClip Crunch { get { if (!crunchLoaded) { crunch = Resources.Load<AudioClip>("UI/button_crunch"); crunchLoaded = true; } return crunch; } }

    Vector2 value = Vector2.one, velocity, target = Vector2.one;
    float stiffness = ReleaseStiffness, damping = ReleaseDamping;
    bool pressed, moving;
    Vector3 baseScale, lastWritten;
    Selectable selectable;

    void Awake() { selectable = GetComponent<Selectable>(); }

    public void OnPointerDown(PointerEventData e)
    {
        if (selectable != null && !selectable.IsInteractable()) return;
        pressed = true; target = Pressed; stiffness = PressStiffness; damping = PressDamping; Begin();
        if (Crunch != null && AudioManager.Instance != null) { LastCrunchAt = Time.unscaledTime; AudioManager.Instance.PlaySFX(Crunch); }
    }
    public void OnPointerUp(PointerEventData e) { Release(); }
    public void OnPointerExit(PointerEventData e) { if (pressed) Release(); }

    void Release()
    {
        if (!pressed) return;
        pressed = false; target = Vector2.one; stiffness = ReleaseStiffness; damping = ReleaseDamping;
    }

    void Begin()
    {
        if (moving) return;
        moving = true; baseScale = transform.localScale; lastWritten = baseScale; value = Vector2.one; velocity = Vector2.zero;
    }

    void LateUpdate()
    {
        if (!moving) return;
        var dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        var a = (target - value) * stiffness - velocity * damping;
        velocity += a * dt; value += velocity * dt;
        if (!pressed && (value - Vector2.one).sqrMagnitude < 1e-6f && velocity.sqrMagnitude < 1e-4f) { value = Vector2.one; moving = false; }
        Write();
    }

    void Write()
    {
        var t = transform;
        if (t.localScale != lastWritten) baseScale = t.localScale;      // someone else set the scale this frame: build on it
        var s = new Vector3(baseScale.x * value.x, baseScale.y * value.y, baseScale.z);
        t.localScale = s; lastWritten = s;
    }

    void OnDisable()
    {
        if (!moving) return;
        pressed = false; moving = false; value = Vector2.one; velocity = Vector2.zero; Write();
    }

    /// Adds the press to every cookie button on screen; looks again twice a second for buttons made later.
    class Runner : MonoBehaviour
    {
        float next;
        void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + 0.5f;
            foreach (var img in FindObjectsOfType<Image>())
            {
                if (img.sprite == null || !CookieSprites.Contains(img.sprite.name)) continue;
                var go = img.gameObject;
                if (go.GetComponent<CookiePress>() != null) continue;
                var pause = go.GetComponent<PauseButton>(); var cont = go.GetComponent<ClickContinue>(); var play = go.GetComponent<ClickPlay>();
                if (go.GetComponent<Selectable>() == null && pause == null && cont == null && play == null && go.GetComponent<SettingButton>() == null) continue;
                go.AddComponent<CookiePress>();
                if (Crunch == null) continue;                    // the crunch replaces their old press clicks
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
