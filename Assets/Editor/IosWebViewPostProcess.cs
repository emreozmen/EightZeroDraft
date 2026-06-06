using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public static class IosWebViewPostProcess
{
    [PostProcessBuild(1000)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS)
        {
            return;
        }

        var pluginSourcePath = Path.Combine(Application.dataPath, "Plugins", "iOS", "EightZeroWebView.mm");
        if (!File.Exists(pluginSourcePath))
        {
            throw new FileNotFoundException("Missing iOS WebView native source.", pluginSourcePath);
        }

        var nativeSourcePath = Path.Combine(pathToBuiltProject, "Libraries", "EightZeroWebView.mm");
        Directory.CreateDirectory(Path.GetDirectoryName(nativeSourcePath));
        File.Copy(pluginSourcePath, nativeSourcePath, true);

        var projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        var project = new PBXProject();
        project.ReadFromFile(projectPath);

        var unityFrameworkGuid = project.GetUnityFrameworkTargetGuid();
        var mainTargetGuid = project.GetUnityMainTargetGuid();
        var fileGuid = project.AddFile("Libraries/EightZeroWebView.mm", "Libraries/EightZeroWebView.mm", PBXSourceTree.Source);

        project.AddFileToBuild(unityFrameworkGuid, fileGuid);
        project.AddFrameworkToProject(unityFrameworkGuid, "WebKit.framework", false);
        project.AddFrameworkToProject(mainTargetGuid, "WebKit.framework", false);
        project.WriteToFile(projectPath);

        InjectAppStoreIcon(pathToBuiltProject);
    }

    private static void InjectAppStoreIcon(string pathToBuiltProject)
    {
        var sourceIcon = Path.Combine(Application.dataPath, "AppIcon", "AppIcon1024.png");
        if (!File.Exists(sourceIcon))
        {
            throw new FileNotFoundException("Missing iOS App Store icon source.", sourceIcon);
        }

        var appIconSet = FindAppIconSet(pathToBuiltProject);
        Directory.CreateDirectory(appIconSet);

        const string iconFileName = "Icon-App-1024x1024@1x.png";
        File.Copy(sourceIcon, Path.Combine(appIconSet, iconFileName), true);

        var contentsPath = Path.Combine(appIconSet, "Contents.json");
        var entry = $@"    {{
      ""idiom"" : ""ios-marketing"",
      ""size"" : ""1024x1024"",
      ""scale"" : ""1x"",
      ""filename"" : ""{iconFileName}""
    }}";

        if (!File.Exists(contentsPath))
        {
            File.WriteAllText(contentsPath, $@"{{
  ""images"" : [
{entry}
  ],
  ""info"" : {{
    ""author"" : ""xcode"",
    ""version"" : 1
  }}
}}
");
            return;
        }

        var contents = File.ReadAllText(contentsPath);
        if (contents.Contains(@"""ios-marketing"""))
        {
            return;
        }

        var imageArrayEnd = contents.LastIndexOf("  ]");
        if (imageArrayEnd < 0)
        {
            imageArrayEnd = contents.LastIndexOf("]");
        }

        if (imageArrayEnd < 0)
        {
            File.WriteAllText(contentsPath, $@"{{
  ""images"" : [
{entry}
  ],
  ""info"" : {{
    ""author"" : ""xcode"",
    ""version"" : 1
  }}
}}
");
            return;
        }

        var separator = contents.Contains(@"""filename""") ? ",\n" : string.Empty;
        contents = contents.Insert(imageArrayEnd, separator + entry + "\n");
        File.WriteAllText(contentsPath, contents);
    }

    private static string FindAppIconSet(string pathToBuiltProject)
    {
        var matches = Directory.GetDirectories(pathToBuiltProject, "AppIcon.appiconset", SearchOption.AllDirectories);
        if (matches.Length > 0)
        {
            return matches[0];
        }

        return Path.Combine(pathToBuiltProject, "Unity-iPhone", "Images.xcassets", "AppIcon.appiconset");
    }

}
