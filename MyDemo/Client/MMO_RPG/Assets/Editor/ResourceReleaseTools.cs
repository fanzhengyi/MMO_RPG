using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;
using YooAsset.Editor;

/// <summary>项目的 Windows 资源构建和本地发布入口；不改动游戏服务器。</summary>
public static class ResourceReleaseTools
{
    public const string InitScene = "Assets/Main/Init.unity";
    public const string PackageName = "DefaultPackage";
    public const string LocalHost = "http://127.0.0.1/CDN/StandaloneWindows64/DefaultPackage";
    private static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);
    public static string ServerRoot => Path.GetFullPath(Path.Combine(ProjectRoot, "../../ResourceServer"));

    [MenuItem("Tools/Resources/Prepare Bootstrap")]
    public static void PrepareBootstrap()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("退出播放模式后配置启动场景。");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(InitScene, true) };
        PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
        var scene = EditorSceneManager.GetSceneByPath(InitScene);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(InitScene, OpenSceneMode.Additive);
        var roots = scene.GetRootGameObjects();
        var startup = roots.SelectMany(r => r.GetComponentsInChildren<MyYooAsset>(true)).Single();
        var ui = roots.SelectMany(r => r.GetComponentsInChildren<LoadUi>(true)).Single();
        startup.defaultHostServer = LocalHost;
        var serialized = new SerializedObject(ui);
        var label = (TMP_Text)serialized.FindProperty("txtProgress").objectReferenceValue;
        var existing = label.transform.parent.Find("RetryDownload");
        Button button;
        if (existing != null)
            button = existing.GetComponent<Button>();
        else
        {
            var go = new GameObject("RetryDownload", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(label.transform.parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -100f);
            rect.sizeDelta = new Vector2(220f, 56f);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.12f, 0.32f, 0.56f, 1f);
            button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.font = label.font;
            text.fontSize = 26;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.text = "重试下载";
        }
        serialized.FindProperty("retryButton").objectReferenceValue = button;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        button.gameObject.SetActive(false);
        EditorUtility.SetDirty(startup);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (opened) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("启动配置完成：Init 入口、开发包 HTTP 策略、重试按钮。");
    }

    [MenuItem("Tools/Resources/Build and Stage Windows Bundles")]
    public static void BuildAndStageWindows()
    {
        BuildAndStage(DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
    }

    /// <summary>版本号在构建完成后不可复用；所有资源就绪后才原子切换版本文件。</summary>
    public static string BuildAndStage(string version)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("退出播放模式后构建资源。");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
            throw new InvalidOperationException("请先把构建目标切换到 Windows x86_64。");
        var parameters = new ScriptableBuildParameters
        {
            BuildOutputRoot = Path.Combine(ProjectRoot, "Bundles"),
            BundledFileRoot = Path.Combine(Application.streamingAssetsPath, "yoo"),
            BuildPipeline = EBuildPipeline.ScriptableBuildPipeline.ToString(),
            BuildBundleType = (int)EBundleType.AssetBundle,
            BuildTarget = BuildTarget.StandaloneWindows64,
            PackageName = PackageName,
            PackageVersion = version,
            CompressOption = ECompressOption.LZ4,
            FileNameStyle = EFileNameStyle.HashName,
            EnableSharePackRule = true,
            SingleReferencedPackAlone = false,
            VerifyBuildingResult = true,
            BundledCopyOption = EBundledCopyOption.None,
            BuiltinShadersBundleName = "defaultpackage_unityshaders",
            MonoScriptsBundleName = "defaultpackage_monoscripts",
            WriteLinkXML = true
        };
        var result = new YooAsset.Editor.ScriptableBuildPipeline().Run(parameters, true);
        if (!result.Success) throw new BuildFailedException($"资源构建失败：{result.FailedTask}\n{result.ErrorInfo}");
        // AB 场景不在 Player 场景列表中，保留其中引用的脚本类型，防止 IL2CPP 裁剪。
        var preservationDirectory = Path.Combine(Application.dataPath, "ResourceBuild");
        Directory.CreateDirectory(preservationDirectory);
        File.Copy(Path.Combine(result.OutputPackageDirectory, "link.xml"), Path.Combine(preservationDirectory, "link.xml"), true);
        AssetDatabase.ImportAsset("Assets/ResourceBuild/link.xml", ImportAssetOptions.ForceSynchronousImport);
        var destination = Path.Combine(ServerRoot, "CDN/StandaloneWindows64", PackageName);
        Directory.CreateDirectory(destination);
        string versionFile = YooAssetConfiguration.GetPackageVersionFileName(PackageName);
        foreach (string source in Directory.GetFiles(result.OutputPackageDirectory))
        {
            string name = Path.GetFileName(source);
            var extension = Path.GetExtension(name).ToLowerInvariant();
            if (name == versionFile || extension == ".report" || extension == ".json" || extension == ".xml") continue;
            File.Copy(source, Path.Combine(destination, name), true);
        }
        string publishedVersion = Path.Combine(destination, versionFile);
        string pendingVersion = publishedVersion + ".pending";
        File.Copy(Path.Combine(result.OutputPackageDirectory, versionFile), pendingVersion, true);
        if (File.Exists(publishedVersion)) File.Replace(pendingVersion, publishedVersion, null);
        else File.Move(pendingVersion, publishedVersion);
        File.WriteAllText(Path.Combine(ProjectRoot, "Library/LastResourceRelease.txt"), destination);
        Debug.Log($"资源发布目录已准备：{destination}\n版本：{version}\n将 {ServerRoot} 内的 CDN 文件夹映射到静态服务器 /CDN。");
        return destination;
    }
}

/// <summary>阻止跳过资源初始化入口的安装包被误发出去。</summary>
public sealed class ResourceBootstrapBuildCheck : IPreprocessBuildWithReport
{
    public int callbackOrder => -100;

    public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
        if (scenes.Length != 1 || scenes[0].path != ResourceReleaseTools.InitScene)
            throw new BuildFailedException("客户端应只内置 Assets/Main/Init.unity；登录与游戏场景由 YooAsset 加载。请运行 Tools/Resources/Prepare Bootstrap。");
        if (!File.Exists(Path.Combine(Application.dataPath, "ResourceBuild/link.xml")))
            throw new BuildFailedException("缺少 AB 脚本类型的防裁剪清单。请先运行 Tools/Resources/Build and Stage Windows Bundles，再打客户端。");
    }
}
