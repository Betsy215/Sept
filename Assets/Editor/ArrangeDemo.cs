using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Editor-only helper that moves table items through DraggableFood's real pointer handlers,
/// either instantly (store screenshots) or animated over frames (preview video of the
/// arrangement phase). Driven from Claude Code through execute_code; no menu item.
/// </summary>
public static class ArrangeDemo
{
    private struct Move { public string food; public Vector2 target; }

    private static readonly List<Move> plan = new();
    private static int index, frame, moveFrames, pauseFrames;
    private static DraggableFood current;
    private static Vector3 from;
    private static PointerEventData ed;
    private static bool running;

    public static bool IsRunning => running;
    public static string Status => $"running={running} move={index}/{plan.Count} frame={frame}";

    private static DraggableFood Find(string food)
    {
        foreach (var it in Object.FindObjectsOfType<ServeableItem>())
            if (it.GetFoodType().ToString() == food) return it.GetComponent<DraggableFood>() ?? it.GetComponentInParent<DraggableFood>();
        return null;
    }

    private static PointerEventData Down(DraggableFood d)
    {
        var e = new PointerEventData(EventSystem.current) { pointerId = 0, position = Camera.main.WorldToScreenPoint(d.transform.position) };
        d.OnPointerDown(e); d.OnBeginDrag(e);
        return e;
    }

    private static void DragTo(DraggableFood d, PointerEventData e, Vector3 world)
    {
        e.position = Camera.main.WorldToScreenPoint(world);
        d.OnDrag(e);
    }

    private static void Up(DraggableFood d, PointerEventData e)
    {
        d.OnPointerUp(e); d.OnEndDrag(e);
    }

    /// <summary>Move every listed item to its target in one go (converges the smoothed drag).</summary>
    public static string Instant(Dictionary<string, Vector2> targets)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var kv in targets)
        {
            var d = Find(kv.Key); if (d == null) { sb.Append(kv.Key + ":missing "); continue; }
            var e = Down(d);
            var world = new Vector3(kv.Value.x, kv.Value.y, d.transform.position.z);
            for (var i = 0; i < 80; i++) DragTo(d, e, world);
            Up(d, e);
            sb.Append(kv.Key + "->" + ((Vector2)d.transform.position).ToString("F2") + " ");
        }
        return sb.ToString();
    }

    /// <summary>Animate the moves one after another, framesPerMove frames each with a pause between.</summary>
    public static void Start(List<KeyValuePair<string, Vector2>> moves, int framesPerMove = 36, int pause = 10)
    {
        Stop();
        plan.Clear();
        foreach (var m in moves) plan.Add(new Move { food = m.Key, target = m.Value });
        index = 0; frame = 0; moveFrames = framesPerMove; pauseFrames = pause; current = null;
        running = true;
        EditorApplication.update += Tick;
    }

    public static void Stop()
    {
        if (!running) return;
        EditorApplication.update -= Tick;
        if (current != null && ed != null) Up(current, ed);
        current = null; running = false;
    }

    private static float Ease(float p) => p * p * (3f - 2f * p);

    private static void Tick()
    {
        if (!Application.isPlaying) { Stop(); return; }
        if (index >= plan.Count) { Stop(); return; }
        if (current == null)
        {
            if (frame < pauseFrames) { frame++; return; }
            current = Find(plan[index].food);
            if (current == null) { index++; frame = 0; return; }
            from = current.transform.position; ed = Down(current); frame = 0;
            return;
        }
        var t = Mathf.Clamp01((float)frame / moveFrames);
        var target = new Vector3(plan[index].target.x, plan[index].target.y, from.z);
        var p = Vector3.Lerp(from, target, Ease(t));
        // a little arc so the lift reads as a pick-up
        p.y += Mathf.Sin(t * Mathf.PI) * 0.25f;
        DragTo(current, ed, p);
        frame++;
        if (frame > moveFrames + 8)
        {
            for (var i = 0; i < 20; i++) DragTo(current, ed, target);
            Up(current, ed); current = null; index++; frame = 0;
        }
    }
}
