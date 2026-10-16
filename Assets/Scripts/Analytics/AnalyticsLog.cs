using System;
using System.Collections.Generic;
using Unity.Services.Analytics;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

/// <summary>
/// Player analytics through Unity Analytics (owner, 16 October 2026: see how long people play and where they stop).
/// Unity records sessions on its own (start, length, new player, device). The game adds a few events of its own:
/// tutorialStep, tutorialFinished, dayStarted, dayFinished, dayLeft, shopPurchase, adWatched. Their schemas live in
/// the Unity dashboard's Event Manager; an event or parameter not defined there is dropped by Unity.
/// Store builds report to the "production" environment; the editor and TestFlight builds to "development", so our
/// own testing never mixes with real players. Nothing here may ever break the game: every call is guarded.
/// </summary>
public static class AnalyticsLog
{
    private static bool ready;
    private static bool starting;
    private static readonly List<CustomEvent> Waiting = new List<CustomEvent>();   // recorded before Unity was ready

    private static bool IsTest => Application.isEditor || AdsInitializer.IsTestFlightBuild();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot() => Start();

    private static async void Start()
    {
        if (ready || starting) return;
        starting = true;
        try
        {
            var options = new InitializationOptions().SetEnvironmentName(IsTest ? "development" : "production");
            await UnityServices.InitializeAsync(options);
            AnalyticsService.Instance.StartDataCollection();
            ready = true;
            foreach (var e in Waiting) AnalyticsService.Instance.RecordEvent(e);
            Waiting.Clear();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Analytics] not started: " + ex.Message);
        }
        starting = false;
    }

    private static void Record(string name, params (string key, object value)[] parameters)
    {
        try
        {
            var e = new CustomEvent(name);
            foreach (var (key, value) in parameters) if (value != null) e.Add(key, value);
            if (ready) AnalyticsService.Instance.RecordEvent(e);
            else if (Waiting.Count < 50) Waiting.Add(e);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Analytics] " + name + ": " + ex.Message);
        }
    }

    /// Day numbers as the player sees them: day 1 is level index 0.
    private static int Day(int levelIndex) => levelIndex + 1;

    private static string EnabledFoods()
    {
        var sm = SessionManager.Instance;
        if (sm == null || sm.GetCurrentSession() == null || sm.GetCurrentSession().enabledFoodItems == null) return "";
        return string.Join(",", sm.GetCurrentSession().enabledFoodItems);
    }

    public static void TutorialStep(int step, string text) => Record("tutorialStep", ("stepNumber", step), ("stepText", text));
    public static void TutorialFinished(int captionsShown) => Record("tutorialFinished", ("stepNumber", captionsShown));
    public static void DayStarted(int levelIndex) => Record("dayStarted", ("day", Day(levelIndex)), ("foodsOn", EnabledFoods()));
    public static void DayFinished(int levelIndex, int stars, float earnings) =>
        Record("dayFinished", ("day", Day(levelIndex)), ("stars", stars), ("dayEarnings", earnings));
    public static void DayLeft(int levelIndex, string reason) => Record("dayLeft", ("day", Day(levelIndex)), ("reason", reason));
    public static void ShopPurchase(string item, int price) => Record("shopPurchase", ("itemName", item ?? ""), ("price", price));
    public static void AdWatched(string adType, bool completed) => Record("adWatched", ("adType", adType), ("completed", completed));
}
