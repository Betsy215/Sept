using UnityEngine;

/// <summary>
/// Chapters of 16 days (chapter 2, the beach, planned for the next version; plan in Docs/chapter2-plan.md).
/// No save field: the chapter comes from the day index and levelsCompleted the save already has.
/// While Count is 1 everything behaves exactly as before; Art() returns null in chapter 1 so the scenes keep
/// their own art.
/// </summary>
public static class Chapter
{
    public const int Days = MapScreen.ChapterDays;
    public static int Count = 2;                   // chapter 2 is on since 18 October 2026 (the owner: release with chapter 2); a field so a recipe can still flip it
    public static int TotalDays => Days * Count;

    /// Chapter (1-based) of a day index (0-based).
    public static int Of(int dayIndex) => 1 + Mathf.Max(0, dayIndex) / Days;

    /// True when the day index is the last day of its chapter.
    public static bool IsLastDay(int dayIndex) => (dayIndex + 1) % Days == 0;

    /// Chapter art from Resources/Chapter{n}/name, or null in chapter 1 (keep the scene's own art).
    public static T Art<T>(int chapter, string name) where T : Object
        => chapter <= 1 ? null : Resources.Load<T>($"Chapter{chapter}/{name}");
}
