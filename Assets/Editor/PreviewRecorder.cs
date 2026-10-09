using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only frame recorder for store preview videos. Call Start(folder, fps) while in play mode:
/// Unity then advances game time by exactly 1/fps per rendered frame (Time.captureFramerate) and
/// one PNG of the Game view is written per frame, so the result is a smooth 30 fps sequence no
/// matter how slow the editor renders. Stop() restores normal time. Assemble with ffmpeg.
/// Driven from Claude Code through execute_code; no menu item needed.
/// </summary>
public static class PreviewRecorder
{
    private static string folder;
    private static int frame;
    private static bool recording;

    public static int FrameCount => frame;
    public static bool IsRecording => recording;

    public static void Start(string outputFolder, int fps = 30)
    {
        Stop();
        folder = outputFolder;
        Directory.CreateDirectory(folder);
        frame = 0;
        Time.captureFramerate = fps;
        recording = true;
        EditorApplication.update += Tick;
    }

    public static void Stop()
    {
        if (!recording) return;
        EditorApplication.update -= Tick;
        Time.captureFramerate = 0;
        recording = false;
    }

    private static void Tick()
    {
        if (!Application.isPlaying) { Stop(); return; }
        ScreenCapture.CaptureScreenshot(Path.Combine(folder, $"f{frame:D5}.png"));
        frame++;
    }
}
