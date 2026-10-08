#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

/// <summary>
/// Runs after every iOS export and patches the generated Info.plist.
/// Without NSUserTrackingUsageDescription, iOS silently skips the App Tracking
/// Transparency prompt that AdsInitializer requests, and Unity Ads gets no tracking consent.
/// </summary>
public static class IOSPostBuild
{
    private const string TrackingUsageDescription =
        "This lets us show ads that are more relevant to you and keeps the game free.";

    [PostProcessBuild(100)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS) return;

        var plistPath = System.IO.Path.Combine(pathToBuiltProject, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        plist.root.SetString("NSUserTrackingUsageDescription", TrackingUsageDescription);

        plist.WriteToFile(plistPath);
        UnityEngine.Debug.Log("[IOSPostBuild] Added NSUserTrackingUsageDescription to Info.plist");
    }
}
#endif
