using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildIos
{
    public static void PerformBuild()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var outputPath = Path.Combine(projectRoot, "Builds", "iOS");
        Directory.CreateDirectory(outputPath);

        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.eightzero.worldcupdraft");
        PlayerSettings.bundleVersion = Environment.GetEnvironmentVariable("APP_VERSION") ?? "1.0";
        PlayerSettings.iOS.buildNumber = Environment.GetEnvironmentVariable("CM_BUILD_NUMBER") ?? "1";
        PlayerSettings.iOS.requiresFullScreen = true;

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = outputPath,
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new Exception($"iOS build failed: {report.summary.result}");
        }
    }
}
