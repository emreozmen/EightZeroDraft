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

        var nativeSourcePath = Path.Combine(pathToBuiltProject, "Libraries", "EightZeroWebView.mm");
        Directory.CreateDirectory(Path.GetDirectoryName(nativeSourcePath));
        File.WriteAllText(nativeSourcePath, NativeSource);

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

    private const string NativeSource = @"
#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <WebKit/WebKit.h>

static WKWebView *EightZeroWebView = nil;

static UIViewController *EightZeroRootViewController(void) {
    UIWindow *window = UIApplication.sharedApplication.keyWindow;
    if (window == nil) {
        window = UIApplication.sharedApplication.windows.firstObject;
    }
    return window.rootViewController;
}

extern ""C"" void EightZero_ShowWebView(const char *urlCString) {
    dispatch_async(dispatch_get_main_queue(), ^{
        UIViewController *root = EightZeroRootViewController();
        if (root == nil || urlCString == nil) {
            return;
        }

        NSString *urlString = [NSString stringWithUTF8String:urlCString];
        NSURL *fileURL = [NSURL fileURLWithPath:urlString];
        NSURL *readAccessURL = [fileURL URLByDeletingLastPathComponent];

        if (EightZeroWebView == nil) {
            WKWebViewConfiguration *configuration = [[WKWebViewConfiguration alloc] init];
            configuration.allowsInlineMediaPlayback = YES;
            configuration.preferences.javaScriptCanOpenWindowsAutomatically = YES;

            EightZeroWebView = [[WKWebView alloc] initWithFrame:root.view.bounds configuration:configuration];
            EightZeroWebView.autoresizingMask = UIViewAutoresizingFlexibleWidth | UIViewAutoresizingFlexibleHeight;
            EightZeroWebView.backgroundColor = UIColor.clearColor;
            EightZeroWebView.opaque = NO;
            EightZeroWebView.scrollView.bounces = NO;
            EightZeroWebView.scrollView.contentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentNever;

            [root.view addSubview:EightZeroWebView];
        }

        [EightZeroWebView loadFileURL:fileURL allowingReadAccessToURL:readAccessURL];
    });
}

extern ""C"" void EightZero_HideWebView(void) {
    dispatch_async(dispatch_get_main_queue(), ^{
        if (EightZeroWebView != nil) {
            [EightZeroWebView removeFromSuperview];
            EightZeroWebView = nil;
        }
    });
}
";
}
