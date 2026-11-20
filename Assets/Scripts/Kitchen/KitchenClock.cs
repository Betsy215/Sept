using System.Collections;
using UnityEngine;

/// The kitchen's clock: real time (so ovens, regrowing fruit and refills keep going while the kitchen scene is closed,
/// as they always have), but stopped while the game is paused (owner, 20 November 2026: "When paused, kitchen should
/// pause too, I heard ding sound from kitchen during pause"). LevelManager calls Pause and Resume with the pause menu.
public static class KitchenClock
{
    private static float pausedTotal, pauseStart = -1f;
    public static bool Paused => pauseStart >= 0f;
    /// Seconds since start-up, without the time spent paused.
    public static float Now => Time.realtimeSinceStartup - pausedTotal - (Paused ? Time.realtimeSinceStartup - pauseStart : 0f);
    /// This frame's real seconds, or 0 while paused.
    public static float Delta => Paused ? 0f : Time.unscaledDeltaTime;
    public static void Pause() { if (!Paused) pauseStart = Time.realtimeSinceStartup; }
    public static void Resume() { if (Paused) { pausedTotal += Time.realtimeSinceStartup - pauseStart; pauseStart = -1f; } }
    /// Waits the given kitchen seconds (stands still while paused).
    public static IEnumerator Wait(float seconds) { var end = Now + seconds; while (Now < end) yield return null; }
}
