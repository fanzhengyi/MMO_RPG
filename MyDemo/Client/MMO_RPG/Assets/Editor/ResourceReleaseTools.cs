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
    /// <summary>安装包内的启动场景，先下载资源，再进入 AB 中的登录场景。</summary>
    public const string InitScene = "Assets/Main/Init.unity";
    /// <summary>要构建的 YooAsset 包名，须与收集配置和客户端加载包名一致。</summary>
    public const string PackageName = "DefaultPackage";
    /// <summary>本机 HFS 测试用资源目录；不是线上发布域名。</summary>
    public const string LocalHost = "http://127.0.0.1/CDN/StandaloneWindows64/DefaultPackage";
    /// <summary>Unity 项目根目录，即包含 Assets、Packages 的 MMO_RPG 文件夹。</summary>
    private static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);
    /// <summary>整理发布文件的本地根目录；把其 CDN 子目录映射到 HFS 根路径后才能 HTTP 下载。</summary>
    public static string ServerRoot => Path.GetFullPath(Path.Combine(ProjectRoot, "../../ResourceServer"));

    // 首次配置或修复启动入口时使用：设置 Init 构建场景、下载重试按钮及本机资源地址，日常打 AB 不需要点它。
    [MenuItem("Tools/Resources/Prepare Bootstrap")]
    public static void PrepareBootstrap()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("退出播放模式后配置启动场景。");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(InitScene, true) };
        PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
        // scene：待配置的 Unity Init 场景。
        var scene = EditorSceneManager.GetSceneByPath(InitScene);
        // opened：Init 是否由本次工具临时打开，完成后据此决定是否关闭。
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(InitScene, OpenSceneMode.Additive);
        // roots：Init 场景的所有根物体，用于查找启动和加载界面组件。
        var roots = scene.GetRootGameObjects();
        // startup：Init 中负责资源初始化和下载的 MyYooAsset 组件。
        var startup = roots.SelectMany(r => r.GetComponentsInChildren<MyYooAsset>(true)).Single();
        // ui：Init 中显示下载状态的 LoadUi 组件。
        var ui = roots.SelectMany(r => r.GetComponentsInChildren<LoadUi>(true)).Single();
        startup.defaultHostServer = LocalHost;
        // serialized：加载界面的序列化编辑入口，用于设置私有控件引用。
        var serialized = new SerializedObject(ui);
        // label：现有进度文字，作为重试按钮布局和字体的参照。
        var label = (TMP_Text)serialized.FindProperty("txtProgress").objectReferenceValue;
        // existing：已经存在的 RetryDownload 子物体，避免重复创建按钮。
        var existing = label.transform.parent.Find("RetryDownload");
        // button：最终使用的下载重试按钮。
        Button button;
        if (existing != null)
            button = existing.GetComponent<Button>();
        else
        {
            // go：新创建的重试按钮物体。
            var go = new GameObject("RetryDownload", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(label.transform.parent, false);
            // rect：重试按钮的 UI 布局和尺寸组件。
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, -100f);
            rect.sizeDelta = new Vector2(220f, 56f);
            // image：重试按钮的背景图片组件。
            var image = go.GetComponent<Image>();
            image.color = new Color(0.12f, 0.32f, 0.56f, 1f);
            button = go.GetComponent<Button>();
            button.targetGraphic = image;
            // textGo：按钮文字所在的子物体。
            var textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            // textRect：按钮文字的 UI 布局组件。
            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            // text：显示“重试下载”的 TMP 文本组件。
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

    // 资源准备发布时使用：构建 Windows AB 并整理到 ResourceServer；不构建客户端 exe，也不自动更新正式远程服务器。
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
        // parameters：本次 Windows AB 构建的包名、版本、压缩和输出参数。
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
        // result：AB 构建结果，包含成功状态、错误信息和输出目录。
        var result = new YooAsset.Editor.ScriptableBuildPipeline().Run(parameters, true);
        if (!result.Success) throw new BuildFailedException($"资源构建失败：{result.FailedTask}\n{result.ErrorInfo}");
        // AB 场景不在 Player 场景列表中，保留其中引用的脚本类型，防止 IL2CPP 裁剪。
        // preservationDirectory：客户端代码裁剪保留配置 link.xml 的存放目录。
        var preservationDirectory = Path.Combine(Application.dataPath, "ResourceBuild");
        Directory.CreateDirectory(preservationDirectory);
        File.Copy(Path.Combine(result.OutputPackageDirectory, "link.xml"), Path.Combine(preservationDirectory, "link.xml"), true);
        AssetDatabase.ImportAsset("Assets/ResourceBuild/link.xml", ImportAssetOptions.ForceSynchronousImport);
        // destination：本次资源整理发布的本地目录，之后由 HFS 提供下载。
        var destination = Path.Combine(ServerRoot, "CDN/StandaloneWindows64", PackageName);
        Directory.CreateDirectory(destination);
        // versionFile：资源包版本入口文件名，例如 DefaultPackage.version。
        string versionFile = YooAssetConfiguration.GetPackageVersionFileName(PackageName);
        // source：正在从构建输出目录复制的源文件路径。
        foreach (string source in Directory.GetFiles(result.OutputPackageDirectory))
        {
            // name：当前源文件的文件名，不含目录。
            string name = Path.GetFileName(source);
            // extension：当前文件的小写扩展名，用于排除仅供诊断的报告。
            var extension = Path.GetExtension(name).ToLowerInvariant();
            if (name == versionFile || extension == ".report" || extension == ".json" || extension == ".xml") continue;
            File.Copy(source, Path.Combine(destination, name), true);
        }
        // publishedVersion：客户端会请求的正式版本入口文件路径。
        string publishedVersion = Path.Combine(destination, versionFile);
        // pendingVersion：准备替换正式版本入口的临时文件路径，资源复制完后才发布。
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
    /// <summary>打包前检查的执行顺序；数值较小的检查先执行。</summary>
    public int callbackOrder => -100;

    public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
    {
        // scenes：当前构建设置中启用的 Unity 场景列表。
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
        if (scenes.Length != 1 || scenes[0].path != ResourceReleaseTools.InitScene)
            throw new BuildFailedException("客户端应只内置 Assets/Main/Init.unity；登录与游戏场景由 YooAsset 加载。请运行 Tools/Resources/Prepare Bootstrap。");
        if (!File.Exists(Path.Combine(Application.dataPath, "ResourceBuild/link.xml")))
            throw new BuildFailedException("缺少 AB 脚本类型的防裁剪清单。请先运行 Tools/Resources/Build and Stage Windows Bundles，再打客户端。");
    }
}
