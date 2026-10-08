using UnityEngine;

/// <summary>
/// Turns off Debug.Log output in release builds. The project logs heavily (every tap,
/// every SFX), and on iOS each log line goes through NSLog, which is slow.
/// Development builds and the Editor keep full logging. Warnings and errors always show.
/// </summary>
public static class LogControl
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Configure()
    {
        if (Application.isEditor || Debug.isDebugBuild) return;
        Debug.unityLogger.filterLogType = LogType.Warning;
    }
}
