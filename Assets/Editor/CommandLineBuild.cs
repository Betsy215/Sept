using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// Command-line iOS export, for when the editor (and its MCP bridge) is not open (first used 12 October 2026):
///   Unity -batchmode -quit -projectPath <project> -buildTarget iOS -executeMethod CommandLineBuild.iOS
///         -exportPath <folder> -logFile <log>
/// Exports the scenes enabled in Build Settings to the folder; IOSPostBuild runs as usual afterwards.
/// Fails loudly (exit code 1) when the export does not succeed, so a shell can check `$?`.
public static class CommandLineBuild
{
    public static void iOS()
    {
        var args = Environment.GetCommandLineArgs();
        var at = Array.IndexOf(args, "-exportPath");
        if (at < 0 || at + 1 >= args.Length) throw new Exception("CommandLineBuild: -exportPath <folder> is required");
        var path = args[at + 1];
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var options = new BuildPlayerOptions { scenes = scenes, locationPathName = path, target = BuildTarget.iOS, options = BuildOptions.None };
        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"CommandLineBuild: {report.summary.result}, {report.summary.totalErrors} errors, {report.summary.totalTime.TotalSeconds:F0} s, {path}");
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
