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

        // The game only uses standard HTTPS, so it is exempt from export compliance.
        // Answering here keeps TestFlight builds from sitting in "Missing Compliance".
        plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);

        plist.WriteToFile(plistPath);
        UnityEngine.Debug.Log("[IOSPostBuild] Added NSUserTrackingUsageDescription and ITSAppUsesNonExemptEncryption to Info.plist");

        // Xcode 26 embeds a Swift back-deployment library inside UnityFramework when the minimum
        // iOS is below 15. App Store validation rejects the upload ("contains disallowed file
        // 'Frameworks'"). Turning off forced Swift library embedding on both targets fixes it.
        var pbxPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        var proj = new PBXProject();
        proj.ReadFromFile(pbxPath);
        foreach (var guid in new[] { proj.GetUnityFrameworkTargetGuid(), proj.GetUnityMainTargetGuid() })
            proj.SetBuildProperty(guid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
        proj.WriteToFile(pbxPath);
        UnityEngine.Debug.Log("[IOSPostBuild] Set ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES=NO on UnityFramework and Unity-iPhone");
    }
}
#endif
