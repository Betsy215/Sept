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

/// The day is over, or it is given up (restart, back to the menu): everything the kitchen was doing stops (owner,
/// 23 November 2026: "if day ends, then all action should be canceled or terminated in the kitchen etc."). No oven
/// ding, no mixer or knife sound, no dessert turning up after the day; the next day starts with empty ovens, empty
/// plates and full trees. Before this the ovens and the dessert steps ran on (they live on objects that outlast the
/// scene), so their sounds could play over the day-end panel or in the shop.
public static class KitchenDay
{
    public static void End()
    {
        KitchenTimerHelper.CancelAll();                    // ovens, regrowing fruit: stopped, their callbacks dropped
        CooldownRegistry.ClearAll();
        OvenKitchenBase.Recipes.Clear();
        AppleInKitchen.Count = AppleInKitchen.Full; CoconutInKitchen.Count = CoconutInKitchen.Full;
        KitchenFlow.Reset();                               // a dessert at the mixer or the board: dropped
    }
}
