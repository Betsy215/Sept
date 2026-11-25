// Read the version and build number the next export will carry (python3 Docs/tools/unity_mcp.py exec Docs/tools/recipes/getver.cs).
return UnityEditor.PlayerSettings.bundleVersion + " (" + UnityEditor.PlayerSettings.iOS.buildNumber + ")";
