using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using YooAsset;

/// <summary>
/// 游戏启动入口。
/// 编辑器中默认使用本地模拟资源；打包后的客户端使用 CDN 热更新资源。
/// </summary>
public class MyYooAsset : MonoBehaviour
{
    public EPlayMode playMode = EPlayMode.HostPlayMode;

    [Tooltip("YooAsset 资源包名称，必须和 Bundle Collector 中的包名一致。")]
    public string packageName = "DefaultPackage";

    // 初始化完成后赋值。其他脚本需要加载资源时可使用它。
    public ResourcePackage package;

    //服务器地址
    public string defaultHostServer = "http://127.0.0.1/CDN/PC/v1.0";
    //备用地址
    public string fallbackHostServer = "";

    [Header("下载设置")]
    [Tooltip("同时下载的文件数。移动端建议 4 到 6，PC 可尝试 8 到 10。")]
    [Range(1, 32)] public int downloadingMaxNum = 6;
    [Tooltip("单个文件下载失败后的重试次数。")]
    [FormerlySerializedAs("filedTryAgain")]
    [Min(0)] public int failedTryAgain = 3;

    public string mainSceneLocation = "Assets/MMO_Hot/Scenes/Login.unity";
    //真实下载进度
    public float DownloadProgress { get; private set; }
    //下载消息提示
    public string ProgressMessage { get; private set; }

    // AB 下载时触发。第一个参数就是 Slider 要使用的真实下载进度（0 到 1）。
    public event Action<float, string> DownloadProgressChanged;

    // 状态文字变化时触发。可用于显示“请求版本”“清理旧资源”等提示。
    public event Action<string> StatusChanged;

    // 当前下载器。下载中可以通过它暂停、恢复或取消下载。
    public ResourceDownloaderOperation downloader;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartGameAsync(this.GetCancellationTokenOnDestroy()).Forget(Debug.LogException);
    }

    /// 完整启动流程：初始化 -> 热更（仅客户端）-> 加载主场景。
    private async UniTask StartGameAsync(CancellationToken cancellationToken)
    {
        try
        {
            // 先让加载界面显示一帧，再开始耗时操作。
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

            SetStatus("正在初始化资源系统");
            if (!YooAssets.IsInitialized)
                YooAssets.Initialize();

            if (!YooAssets.TryGetPackage(packageName, out package))
                package = YooAssets.CreatePackage(packageName);

            var currentMode = playMode;
            await InitializePackageAsync(currentMode, cancellationToken);

            // 编辑器模拟模式没有 CDN 和热更文件，初始化后直接加载游戏场景。
            if (currentMode == EPlayMode.EditorSimulateMode)
            {
                SetDownloadProgress(1f, "编辑器模拟模式：跳过热更新");
                //加载场景
                await LoadMainSceneAsync(cancellationToken);
                return;
            }

            // 以下操作只在实际客户端的 HostPlayMode 执行。
            await UpdateRemoteResourcesAsync(cancellationToken);
            await ClearUnusedCacheAsync(cancellationToken);
            await LoadMainSceneAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            SetStatus("加载已取消");
        }
        catch (Exception exception)
        {
            SetStatus($"加载失败：{exception.Message}");
            Debug.LogException(exception);
        }
    }

    /// <summary>
    /// 根据编辑器或客户端模式初始化 YooAsset 包。
    /// </summary>
    private async UniTask InitializePackageAsync(EPlayMode currentMode, CancellationToken cancellationToken)
    {
        InitializePackageOperation operation;

#if UNITY_EDITOR
        if (currentMode == EPlayMode.EditorSimulateMode)
        {
            // 临时构建虚拟资源包，资源来自本地，不会访问 CDN。
            var buildResult = EditorSimulateBuildInvoker.Build(packageName, (int)EBundleType.VirtualAssetBundle);
            var options = new EditorSimulateModeOptions
            {
                EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(
                    buildResult.PackageRootDirectory)
            };
            operation = package.InitializePackageAsync(options);
        }
        else
#endif
        {
            // 客户端模式：内置资源 + CDN 下载缓存。
            var remoteService = new RemoteServices(defaultHostServer, fallbackHostServer);
            var options = new HostPlayModeOptions
            {
                BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(),
                CacheFileSystemParameters = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(remoteService)
            };
            operation = package.InitializePackageAsync(options);
        }

        await WaitYooOperationAsync(operation, cancellationToken);
        CheckOperation(operation, "初始化资源包");

#if UNITY_EDITOR
        // 编辑器模拟模式也需要加载模拟资源清单，否则无法根据地址查找场景和资源。
        if (currentMode == EPlayMode.EditorSimulateMode)
        {
            // EditorSimulateBuildInvoker 默认生成的资源版本名就是 Simulate。
            var manifestOperation = package.LoadPackageManifestAsync(
                new LoadPackageManifestOptions("Simulate", 60));
            await WaitYooOperationAsync(manifestOperation, cancellationToken);
            CheckOperation(manifestOperation, "加载编辑器资源清单");
        }
#endif

        SetStatus("资源系统初始化完成");
    }

    /// <summary>
    /// 客户端热更：请求版本 -> 下载清单 -> 下载资源。
    /// </summary>
    private async UniTask UpdateRemoteResourcesAsync(CancellationToken cancellationToken)
    {
        SetStatus("正在请求资源版本");
        var versionOperation = package.RequestPackageVersionAsync();
        await WaitYooOperationAsync(versionOperation, cancellationToken);
        CheckOperation(versionOperation, "请求资源版本");

        SetStatus("正在更新资源清单");
        var manifestOperation = package.LoadPackageManifestAsync(
            new LoadPackageManifestOptions(versionOperation.PackageVersion, 60));
        await WaitYooOperationAsync(manifestOperation, cancellationToken);
        CheckOperation(manifestOperation, "更新资源清单");

        downloader = package.CreateResourceDownloader(new ResourceDownloaderOptions(
            Mathf.Clamp(downloadingMaxNum, 1, 32),
            Mathf.Max(failedTryAgain, 0)));

        if (downloader.TotalDownloadCount == 0)
        {
            SetDownloadProgress(1f, "已经是最新版本");
            return;
        }

        downloader.DownloadProgressChanged += OnDownloadProgress;
        try
        {
            SetDownloadProgress(0f, $"需要下载 {downloader.TotalDownloadCount} 个文件");
            downloader.StartDownload();
            await WaitYooOperationAsync(downloader, cancellationToken);
            CheckOperation(downloader, "下载热更新资源");
            SetDownloadProgress(1f, "资源下载完成");
        }
        finally
        {
            downloader.DownloadProgressChanged -= OnDownloadProgress;

            // 对象销毁（如退出游戏）时停止还未完成的下载。
            if (cancellationToken.IsCancellationRequested && !downloader.IsDone)
                downloader.CancelDownload();
        }
    }

    //清理旧版本不再引用的 Bundle，避免缓存持续增大。
    private async UniTask ClearUnusedCacheAsync(CancellationToken cancellationToken)
    {
        SetStatus("正在清理旧资源");
        var clearOperation = package.ClearCacheAsync(new ClearCacheOptions(ClearCacheMethods.ClearUnusedBundleFiles));
        await WaitYooOperationAsync(clearOperation, cancellationToken);
        CheckOperation(clearOperation, "清理旧资源");
        SetStatus("资源准备完成");
    }

    /// <summary>
    /// 【热更完成后场景跳转的位置】
    /// </summary>
    private async UniTask LoadMainSceneAsync(CancellationToken cancellationToken)
    {
        SetStatus("正在加载主场景");
        await ResManager.Instance.LoadSceneAsync(mainSceneLocation);
        SetStatus("加载完成");
    }

    /// <summary>
    /// 【写下载进度的位置】
    /// args.Progress 是 YooAsset 给出的真实下载进度，范围为 0 到 1。
    /// </summary>
    private void OnDownloadProgress(DownloadProgressChangedEventArgs args)
    {
        // 这里不做任何百分比换算：args.Progress 就是 AB 包的真实下载进度。
        SetDownloadProgress(args.Progress, $"正在下载 {args.CurrentDownloadCount}/{args.TotalDownloadCount}");
    }

    /// <summary>
    /// 通知 UI 刷新 AB 下载进度条。progress 直接等于 YooAsset 的 args.Progress。
    /// </summary>
    private void SetDownloadProgress(float progress, string message)
    {
        DownloadProgress = Mathf.Clamp01(progress);
        SetStatus(message);
        DownloadProgressChanged?.Invoke(DownloadProgress, ProgressMessage);
    }

    // 只更新提示文字，不改变 AB 下载 Slider。
    private void SetStatus(string message)
    {
        ProgressMessage = message;
        StatusChanged?.Invoke(ProgressMessage);
    }

    // 等待任意 YooAsset 异步操作完成。
    private static UniTask WaitYooOperationAsync(AsyncOperationBase operation, CancellationToken cancellationToken)
    {
        return UniTask.WaitUntil(() => operation.IsDone, PlayerLoopTiming.Update, cancellationToken: cancellationToken);
    }

    // YooAsset 操作失败时，统一抛出带步骤名称的错误。
    private static void CheckOperation(AsyncOperationBase operation, string stepName)
    {
        if (operation.Status != EOperationStatus.Succeeded)
            throw new InvalidOperationException($"{stepName}失败：{operation.Error}");
    }
}

/// <summary>
/// 给 YooAsset 提供主、备用 CDN 地址。
/// </summary>
public class RemoteServices : IRemoteService
{
    private readonly string _defaultHostServer;
    private readonly string _fallbackHostServer;

    public RemoteServices(string defaultHostServer, string fallbackHostServer)
    {
        _defaultHostServer = NormalizeHost(defaultHostServer, nameof(defaultHostServer));
        _fallbackHostServer = string.IsNullOrWhiteSpace(fallbackHostServer)
            ? string.Empty
            : NormalizeHost(fallbackHostServer, nameof(fallbackHostServer));
    }

    public IReadOnlyList<string> GetRemoteUrls(string fileName)
    {
        var primaryUrl = $"{_defaultHostServer}/{fileName}";
        if (string.IsNullOrEmpty(_fallbackHostServer) || _fallbackHostServer == _defaultHostServer)
            return new[] { primaryUrl };

        return new[] { primaryUrl, $"{_fallbackHostServer}/{fileName}" };
    }

    private static string NormalizeHost(string host, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("CDN 地址不能为空。", parameterName);

        return host.Trim().TrimEnd('/');
    }
}
