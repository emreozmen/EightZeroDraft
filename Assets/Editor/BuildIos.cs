using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildIos
{
    public static void PerformBuild()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var outputPath = Path.Combine(projectRoot, "Builds", "iOS");
        Directory.CreateDirectory(outputPath);

        IosBuildSettings.Apply();

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

public sealed class IosBuildSettings : IPreprocessBuildWithReport
{
    private const string BundleId = "com.eightzero.worldcupdraft";
    private const string DefaultAppVersion = "1.0";
    private const string ProductName = "Draft Game";
    private const string AppIconPath = "Assets/AppIcon/AppIcon1024.png";

    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.iOS)
        {
            Apply();
        }
    }

    public static void Apply()
    {
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
        PlayerSettings.productName = ProductName;
        PlayerSettings.bundleVersion = GetAppVersion();
        PlayerSettings.iOS.buildNumber = GetBuildNumber();
        PlayerSettings.iOS.requiresFullScreen = true;
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        PlayerSettings.SplashScreen.backgroundColor = new Color32(209, 212, 209, 255);
        ApplyAppIcons();

        Debug.Log(
            $"8-0 Draft iOS settings: product={ProductName}, bundle={BundleId}, version={PlayerSettings.bundleVersion}, build={PlayerSettings.iOS.buildNumber}"
        );
    }

    private static void ApplyAppIcons()
    {
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
        if (icon == null)
        {
            throw new FileNotFoundException("Missing iOS app icon texture.", AppIconPath);
        }

        var sizes = PlayerSettings.GetIconSizes(NamedBuildTarget.iOS, IconKind.Application);
        if (sizes.Length == 0)
        {
            return;
        }

        PlayerSettings.SetIcons(NamedBuildTarget.iOS, sizes.Select(_ => icon).ToArray(), IconKind.Application);
    }

    private static string GetAppVersion()
    {
        return FirstEnvironmentValue("APP_VERSION", "IOS_APP_VERSION", "UNITY_APP_VERSION") ?? DefaultAppVersion;
    }

    private static string GetBuildNumber()
    {
        var value = FirstEnvironmentValue(
            "BUILD_NUMBER",
            "UCB_BUILD_NUMBER",
            "UNITY_CLOUD_BUILD_NUMBER",
            "UNITY_CLOUD_BUILD_ATTEMPT",
            "CM_BUILD_NUMBER",
            "GITHUB_RUN_NUMBER"
        );

        if (!string.IsNullOrWhiteSpace(value))
        {
            return SanitizeBuildNumber(value);
        }

        return DateTime.UtcNow.ToString("yyyyMMddHHmm");
    }

    private static string FirstEnvironmentValue(params string[] names)
    {
        foreach (var name in names)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string SanitizeBuildNumber(string value)
    {
        var chars = value.Trim().ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (!char.IsDigit(chars[i]) && chars[i] != '.')
            {
                chars[i] = '.';
            }
        }

        var cleaned = new string(chars).Trim('.');
        return string.IsNullOrWhiteSpace(cleaned) ? DateTime.UtcNow.ToString("yyyyMMddHHmm") : cleaned;
    }
}
