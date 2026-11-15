using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// A try-out (owner, 14 November 2026: "can we try out the swipe for kitchen?"): a swipe to the right on the table
/// opens the kitchen, which slides in from the truck's side; a swipe to the left in the kitchen slides it back out.
/// The truck button and the kitchen's own close button stay and work as before; this is a second way in and out.
/// It only listens during play: not while arranging (items are dragged there), not in a tutorial, not while paused,
/// not with the menu book or the day-end card open. A swipe is a fast, long, mostly sideways move, so a tap that
/// slips a little never opens the kitchen.
public class KitchenSwipe : MonoBehaviour
{
    private const float MinWidthShare = 0.22f;   // of the screen width
    private const float MaxSeconds = 0.6f;
    private const float Sideways = 2.2f;         // at least this many times wider than tall
    private const float SlideIn = 0.22f, SlideOut = 0.18f;

    private static KitchenSwipe instance;
    private bool tracking, sliding;
    private Vector2 downAt;
    private float downTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (instance != null) return;
        var go = new GameObject("KitchenSwipe");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<KitchenSwipe>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) { tracking = true; downAt = Input.mousePosition; downTime = Time.unscaledTime; }
        if (!tracking || !Input.GetMouseButtonUp(0)) return;
        tracking = false;
        if (Input.touchCount > 1) return;
        Vector2 move = (Vector2)Input.mousePosition - downAt;
        if (Time.unscaledTime - downTime > MaxSeconds) return;
        if (Mathf.Abs(move.x) < Screen.width * MinWidthShare || Mathf.Abs(move.x) < Mathf.Abs(move.y) * Sideways) return;
        var direction = move.x > 0f ? 1 : -1;
        var tutorial = FindObjectOfType<TutorialDirector>();
        if (tutorial != null) tutorial.OnSwipe(direction);   // the tutorial opens and closes the kitchen itself, on its own steps
        else Swipe(direction);
    }

    /// For the tutorial's swipe steps: the same slide, without the play-phase checks.
    public static IEnumerator SlideOpen()
    {
        var ksm = KitchenSceneManager.Instance;
        if (ksm == null) yield break;
        if (instance == null) { ksm.OpenKitchen(); yield break; }
        yield return instance.Open(ksm);
    }

    public static IEnumerator SlideClose()
    {
        var ksm = KitchenSceneManager.Instance;
        if (ksm == null) yield break;
        if (instance == null) { ksm.CloseKitchen(); yield break; }
        yield return instance.Close(ksm);
    }

    /// +1: a swipe to the right (open the kitchen), -1: to the left (close it). Returns what it did.
    public static string Swipe(int direction)
    {
        if (instance == null) return "no swipe reader";
        var ksm = KitchenSceneManager.Instance;
        if (ksm == null || instance.sliding || !instance.MayListen()) return "not now";
        if (direction > 0 && !ksm.IsKitchenOpen) { instance.StartCoroutine(instance.Open(ksm)); return "opening"; }
        if (direction < 0 && ksm.IsKitchenOpen) { instance.StartCoroutine(instance.Close(ksm)); return "closing"; }
        return "nothing to do";
    }

    private bool MayListen()
    {
        if (Time.timeScale == 0f) return false;
        if (FindObjectOfType<TutorialDirector>() != null) return false;
        var lm = FindObjectOfType<LevelManager>();
        if (lm == null || !lm.DayStarted) return false;
        if (lm.levelCompletePanel != null && lm.levelCompletePanel.activeInHierarchy) return false;
        var pm = FindObjectOfType<GamePhaseManager>();
        if (pm != null && pm.GetCurrentPhase() == GamePhase.ARRANGEMENT) return false;
        var book = FindObjectOfType<MenuBook>();
        if (book != null && book.IsOpen) return false;
        return true;
    }

    private IEnumerator Open(KitchenSceneManager ksm)
    {
        sliding = true;
        ksm.OpenKitchen();
        yield return null;   // the scene is there on the next frame, before it is drawn
        var parts = KitchenParts(ksm, out var width);
        if (ksm.IsKitchenOpen && parts.Count > 0)
        {
            var home = new List<Vector2>();
            foreach (var p in parts) home.Add(p.anchoredPosition);
            for (var t = 0f; t < SlideIn; t += Time.unscaledDeltaTime)
            {
                var left = 1f - Mathf.Clamp01(t / SlideIn); left *= left;   // fast first, soft landing
                for (var i = 0; i < parts.Count; i++) if (parts[i] != null) parts[i].anchoredPosition = home[i] + new Vector2(-width * left, 0f);
                yield return null;
            }
            for (var i = 0; i < parts.Count; i++) if (parts[i] != null) parts[i].anchoredPosition = home[i];
        }
        sliding = false;
    }

    private IEnumerator Close(KitchenSceneManager ksm)
    {
        sliding = true;
        var parts = KitchenParts(ksm, out var width);
        var home = new List<Vector2>();
        foreach (var p in parts) home.Add(p.anchoredPosition);
        for (var t = 0f; t < SlideOut && ksm.IsKitchenOpen; t += Time.unscaledDeltaTime)
        {
            var gone = Mathf.Clamp01(t / SlideOut); gone *= gone;
            for (var i = 0; i < parts.Count; i++) if (parts[i] != null) parts[i].anchoredPosition = home[i] + new Vector2(-width * gone, 0f);
            yield return null;
        }
        ksm.CloseKitchen();
        sliding = false;
    }

    /// Everything the kitchen draws hangs under its canvas (one child, "bg"): those are moved, the canvas stays.
    private static List<RectTransform> KitchenParts(KitchenSceneManager ksm, out float width)
    {
        var parts = new List<RectTransform>(); width = Screen.width;
        var scene = SceneManager.GetSceneByName(ksm.kitchenSceneName);
        if (!scene.isLoaded) return parts;
        foreach (var root in scene.GetRootGameObjects())
        {
            var canvas = root.GetComponent<Canvas>();
            if (canvas == null) continue;
            width = ((RectTransform)canvas.transform).rect.width;
            foreach (Transform child in canvas.transform) if (child is RectTransform rt) parts.Add(rt);
        }
        return parts;
    }
}
