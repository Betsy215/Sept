// iOS export from inside the open editor, through the bridge (18 October 2026): the batchmode export needs the
// licensing client, which the Hub only installs after a Mac admin prompt; the open editor is already licensed.
// Run with a long HTTP timeout (the call blocks until the build ends): sed s/timeout=120/timeout=2400/ unity_mcp.py
// and then `python3 <long copy> exec Docs/tools/build_in_editor.cs`. EditorApplication.delayCall does not fire from
// bridge code, so the build is synchronous. Edit the path and the result file for the version.
var target = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
if (target != UnityEditor.BuildTarget.iOS) return "active target is " + target;
if (UnityEditor.EditorApplication.isPlaying) return "still playing";
var path = "/Users/betzthebest/Desktop/Sept/Builds/iOS-26.10.18-1";
var scenes = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(System.Linq.Enumerable.Where(UnityEditor.EditorBuildSettings.scenes, s => s.enabled), s => s.path));
var options = new UnityEditor.BuildPlayerOptions { scenes = scenes, locationPathName = path, target = UnityEditor.BuildTarget.iOS, options = UnityEditor.BuildOptions.None };
var report = UnityEditor.BuildPipeline.BuildPlayer(options);
var line = report.summary.result + " " + report.summary.totalErrors + " errors " + report.summary.totalTime.TotalSeconds.ToString("F0") + " s";
System.IO.File.WriteAllText("/Users/betzthebest/Desktop/Sept/Builds/export-26.10.18-1.result", line + "\n");
return line;
