using UnityEngine;

/// <summary>
/// Chapters and their days (chapter 2, the beach, is on since 18 October 2026; plan in Docs/chapter2-plan.md).
/// Chapter 1 has 12 days since 25 October 2026 (owner: "reduce the days in level 1 to be 12 days"), chapter 2 keeps 16,
/// so the days are per chapter here and every chapter sum goes through FirstDay / DaysIn / TotalDays.
/// No save field: the chapter comes from the day index and levelsCompleted the save already has.
/// Art() returns null in chapter 1 so the scenes keep their own art.
/// </summary>
public static class Chapter
{
    /// Days per chapter, chapter 1 first. The scene's LevelManager.allLevels holds the days in this order.
    public static readonly int[] DaysPerChapter = { 12, 16 };
    public static int Count = 2;                   // chapter 2 is on since 18 October 2026 (the owner: release with chapter 2); a field so a recipe can still flip it

    /// Days in a chapter (1-based).
    public static int DaysIn(int chapter) => DaysPerChapter[Mathf.Clamp(chapter - 1, 0, DaysPerChapter.Length - 1)];

    /// The day index (0-based) of a chapter's first day.
    public static int FirstDay(int chapter)
    {
        var first = 0;
        for (var c = 1; c < chapter && c - 1 < DaysPerChapter.Length; c++) first += DaysPerChapter[c - 1];
        return first;
    }

    /// Every day of every chapter that is on.
    public static int TotalDays
    {
        get { var n = 0; for (var c = 1; c <= Count; c++) n += DaysIn(c); return n; }
    }

    /// Chapter (1-based) of a day index (0-based); past the last chapter it stays the last.
    public static int Of(int dayIndex)
    {
        var d = Mathf.Max(0, dayIndex);
        for (var c = 1; c <= DaysPerChapter.Length; c++) { d -= DaysIn(c); if (d < 0) return c; }
        return DaysPerChapter.Length;
    }

    /// True when the day index is the last day of its chapter.
    public static bool IsLastDay(int dayIndex) => dayIndex + 1 == FirstDay(Of(dayIndex)) + DaysIn(Of(dayIndex));

    /// Chapter art from Resources/Chapter{n}/name, or null in chapter 1 (keep the scene's own art).
    public static T Art<T>(int chapter, string name) where T : Object
        => chapter <= 1 ? null : Resources.Load<T>($"Chapter{chapter}/{name}");
}
