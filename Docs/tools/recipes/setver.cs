// Set the version and build number for the next build (run in the open editor: python3 Docs/tools/unity_mcp.py exec Docs/tools/recipes/setver.cs).
// Edit the two constants first. Version rule (owner, 18 November 2026): YYYY.MM.DD of the build day, build 0, 1, 2 within that day;
// a version that went live closes its train, so the next test build takes the next day's date. Check with getver.cs.
const string Version = "2026.11.24";
const string Build = "1";
UnityEditor.PlayerSettings.bundleVersion = Version; UnityEditor.PlayerSettings.iOS.buildNumber = Build; UnityEditor.AssetDatabase.SaveAssets();
return UnityEditor.PlayerSettings.bundleVersion + " (" + UnityEditor.PlayerSettings.iOS.buildNumber + ")";
