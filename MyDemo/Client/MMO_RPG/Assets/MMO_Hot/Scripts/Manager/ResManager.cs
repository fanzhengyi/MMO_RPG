using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using YooAsset;
using SceneHandle = YooAsset.SceneHandle;

/// <summary>按路径和类型缓存 YooAsset 句柄；失败或卸载时清除同一个句柄。</summary>
public sealed class ResManager : Singleton<ResManager>
{
    private const string PackageName = "DefaultPackage";
    private readonly Dictionary<(Type Type, string Location), AssetHandle> handles =
        new Dictionary<(Type, string), AssetHandle>();
    private ResourcePackage Package => YooAssets.GetPackage(PackageName);

    /// <summary>同步加载或等待已有任务完成。</summary>
    public T Load<T>(string location) where T : UnityEngine.Object
    {
        var handle = GetOrLoad<T>(location, false);
        handle.WaitForAsyncComplete();
        return GetCachedAsset<T>(location, handle);
    }

    /// <summary>同一路径共用加载任务；加载中卸载会结束等待并返回取消错误。</summary>
    public async UniTask<T> LoadAsync<T>(string location) where T : UnityEngine.Object
    {
        var handle = GetOrLoad<T>(location, true);
        await UniTask.WaitUntil(() => !IsCurrent<T>(location, handle) || handle.IsDone);
        return GetCachedAsset<T>(location, handle);
    }

    /// <summary>回调入口与 await 入口共用加载逻辑。</summary>
    public void LoadAsync<T>(string location, Action<T> onComplete) where T : UnityEngine.Object
        => LoadAsync<T>(location).ContinueWith(onComplete).Forget(UnityEngine.Debug.LogException);

    /// <summary>加载场景并报告进度，失败向调用方抛出错误。</summary>
    public async UniTask<SceneHandle> LoadSceneAsync(string location,
        Action<float> onProgress = null, LoadSceneMode mode = LoadSceneMode.Single)
    {
        var handle = Package.LoadSceneAsync(location, mode);
        while (!handle.IsDone)
        {
            onProgress?.Invoke(handle.Progress);
            await UniTask.Yield();
        }
        if (handle.Status != EOperationStatus.Succeeded)
            throw new InvalidOperationException($"场景加载失败：{location}\n{handle.Error}");
        onProgress?.Invoke(1f);
        return handle;
    }

    /// <summary>释放指定类型；正在等待的调用会收到取消结果。</summary>
    public void Unload<T>(string location) where T : UnityEngine.Object
    {
        var key = (typeof(T), location);
        if (handles.TryGetValue(key, out var handle)) Release(key, handle);
    }

    /// <summary>释放同一路径的所有类型缓存。</summary>
    public void Unload(string location)
    {
        var keys = new List<(Type, string)>();
        foreach (var key in handles.Keys)
            if (key.Location == location) keys.Add(key);
        foreach (var key in keys) Release(key, handles[key]);
    }

    /// <summary>释放所有资源句柄；调用前先销毁仍在使用这些资源的实例。</summary>
    public void UnloadAll()
    {
        foreach (var handle in handles.Values) handle.Release();
        handles.Clear();
    }

    /// <summary>同类型同路径只创建一个加载句柄。</summary>
    private AssetHandle GetOrLoad<T>(string location, bool asynchronous) where T : UnityEngine.Object
    {
        var key = (typeof(T), location);
        if (handles.TryGetValue(key, out var existing)) return existing;
        var handle = asynchronous ? Package.LoadAssetAsync<T>(location) : Package.LoadAssetSync<T>(location);
        handles.Add(key, handle);
        return handle;
    }

    /// <summary>确认等待的句柄仍属于本次加载，而不是已被取消的旧任务。</summary>
    private bool IsCurrent<T>(string location, AssetHandle handle)
        => handles.TryGetValue((typeof(T), location), out var current) && ReferenceEquals(handle, current);

    /// <summary>失败的缓存只由首个等待者释放，后续等待者不会误删重试的新句柄。</summary>
    private T GetCachedAsset<T>(string location, AssetHandle handle) where T : UnityEngine.Object
    {
        if (!IsCurrent<T>(location, handle))
            throw new OperationCanceledException($"资源加载已取消：{location}");
        try
        {
            if (handle.Status != EOperationStatus.Succeeded)
                throw new InvalidOperationException($"资源加载失败：{location}\n{handle.Error}");
            var asset = handle.GetAssetObject<T>();
            if (asset == null) throw new InvalidOperationException($"资源类型不匹配：{location} / {typeof(T).Name}");
            return asset;
        }
        catch
        {
            Release((typeof(T), location), handle);
            throw;
        }
    }

    /// <summary>仅释放当前句柄，避免失败回调误删新的重试任务。</summary>
    private void Release((Type, string) key, AssetHandle handle)
    {
        if (!handles.TryGetValue(key, out var current) || !ReferenceEquals(current, handle)) return;
        handles.Remove(key);
        handle.Release();
    }
}
