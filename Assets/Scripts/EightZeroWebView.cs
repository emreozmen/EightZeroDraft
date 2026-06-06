using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

[ExecuteAlways]
public sealed class EightZeroWebView : MonoBehaviour
{
    private const string GameRelativePath = "Game/index.html";
    private Texture2D splashIcon;
    private GUIStyle splashTitleStyle;
    private GUIStyle splashBodyStyle;
#if UNITY_EDITOR
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
#endif

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void EightZero_ShowWebView(string url);

    [DllImport("__Internal")]
    private static extern void EightZero_HideWebView();
#endif

    private void Start()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = new Color32(209, 212, 209, 255);
        }
        StartCoroutine(OpenGameWhenReady());
    }

    private void OnApplicationPause(bool paused)
    {
        if (!paused)
        {
            StartCoroutine(OpenGameWhenReady());
        }
    }

    private void OnDestroy()
    {
#if UNITY_IOS && !UNITY_EDITOR
        EightZero_HideWebView();
#endif
    }

    private void OpenGame()
    {
        var indexPath = Path.Combine(Application.streamingAssetsPath, GameRelativePath);

#if UNITY_IOS && !UNITY_EDITOR
        if (!File.Exists(indexPath))
        {
            Debug.LogError($"8-0 Draft missing game entrypoint: {indexPath}");
            return;
        }

        EightZero_ShowWebView(indexPath);
#else
        Debug.Log($"8-0 Draft WebView wrapper ready: {indexPath}");
#endif
    }

    private IEnumerator OpenGameWhenReady()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        OpenGame();
    }

    private void OnGUI()
    {
#if UNITY_EDITOR
        DrawEditorPreview();
#else
        DrawSplash();
#endif
    }

    private void DrawSplash()
    {
        EnsureSplashStyles();

        var iconSize = Mathf.Min(148, Screen.width * 0.34f);
        var iconRect = new Rect((Screen.width - iconSize) * 0.5f, Screen.height * 0.34f, iconSize, iconSize);
        if (splashIcon != null)
        {
            GUI.DrawTexture(iconRect, splashIcon, ScaleMode.ScaleToFit, true);
        }

        GUI.Label(new Rect(24, iconRect.yMax + 22, Screen.width - 48, 44), "Draft Game", splashTitleStyle);
        GUI.Label(new Rect(24, iconRect.yMax + 62, Screen.width - 48, 28), "8-0 World Cup Draft", splashBodyStyle);
    }

    private void EnsureSplashStyles()
    {
        if (splashIcon == null)
        {
            splashIcon = Resources.Load<Texture2D>("SplashLogo");
        }

        splashTitleStyle ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(Mathf.Min(34, Screen.width * 0.08f)),
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color32(71, 74, 74, 255) }
        };

        splashBodyStyle ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(Mathf.Min(16, Screen.width * 0.04f)),
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color32(42, 57, 141, 255) }
        };
    }

#if UNITY_EDITOR
    private void DrawEditorPreview()
    {
        EnsureStyles();

        var width = Mathf.Min(520, Screen.width - 40);
        var rect = new Rect((Screen.width - width) * 0.5f, 80, width, 260);

        GUI.Box(rect, GUIContent.none);
        GUILayout.BeginArea(new Rect(rect.x + 22, rect.y + 22, rect.width - 44, rect.height - 44));
        GUILayout.Label("8-0 World Cup Draft", titleStyle);
        GUILayout.Space(8);
        GUILayout.Label(
            "This Unity project is an iOS WebView wrapper. The game appears inside WKWebView on iPhone/iPad builds. In the Unity Editor, use the preview button to open the same local game build in your browser.",
            bodyStyle
        );
        GUILayout.Space(18);
        if (GUILayout.Button("Open Game Preview", GUILayout.Height(44)))
        {
            var indexPath = Path.Combine(Application.streamingAssetsPath, GameRelativePath);
            Application.OpenURL($"file:///{indexPath.Replace("\\", "/")}");
        }
        GUILayout.Space(8);
        GUILayout.Label("For iOS: File > Build Profiles > iOS > Build", bodyStyle);
        GUILayout.EndArea();
    }

    private void EnsureStyles()
    {
        titleStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            wordWrap = true
        };

        bodyStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = new Color(0.9f, 0.94f, 1f) },
            wordWrap = true
        };
    }
#endif
}
