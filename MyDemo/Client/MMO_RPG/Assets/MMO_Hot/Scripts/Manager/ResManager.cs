using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using YooAsset;
using SceneHandle = YooAsset.SceneHandle;

/// <summary>按路径和类型缓存 YooAsset 句柄；失败或卸载时清除同一个句柄。</summary>
public sealed class ResManager : Singleton<ResManager>
{
    /// <summary>运行时加载的 YooAsset 包名，必须与启动脚本和资源收集配置一致。</summary>
    private const string PackageName = "DefaultPackage";
    /// <summary>按资源类型和地址缓存加载句柄；句柄持有资源引用，卸载时必须 Release。</summary>
    private readonly Dictionary<(Type Type, string Location), AssetHandle> handles =
        new Dictionary<(Type, string), AssetHandle>();
    /// <summary>获取启动流程已初始化的资源包，资源管理器本身不负责初始化或下载版本。</summary>
    private ResourcePackage Package => YooAssets.GetPackage(PackageName);

    /// <summary>同步加载或等待已有任务完成。</summary>
    public T Load<T>(string location) where T : UnityEngine.Object
    {
        // handle：当前资源或场景的 YooAsset 加载句柄，用它等待完成和释放引用。
        var handle = GetOrLoad<T>(location, false);
        handle.WaitForAsyncComplete();
        return GetCachedAsset<T>(location, handle);
    }

    /// <summary>同一路径共用加载任务；加载中卸载会结束等待并返回取消错误。</summary>
    public async UniTask<T> LoadAsync<T>(string location) where T : UnityEngine.Object
    {
        // handle：当前资源或场景的 YooAsset 加载句柄，用它等待完成和释放引用。
        var handle = GetOrLoad<T>(location, true);
        await UniTask.WaitUntil(() => !IsCurrent<T>(location, handle) || handle.IsDone);
        return GetCachedAsset<T>(location, handle);
    }

    /// <summary>加载场景并报告进度，失败向调用方抛出错误。</summary>
    public async UniTask<SceneHandle> LoadSceneAsync(string location,
        Action<float> onProgress = null, LoadSceneMode mode = LoadSceneMode.Single)
    {
        // handle：当前资源或场景的 YooAsset 加载句柄，用它等待完成和释放引用。
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
        // key：资源类型与地址组成的缓存键，或当前遍历的同类键。
        var key = (typeof(T), location);
        // handle：当前资源或场景的 YooAsset 加载句柄，用它等待完成和释放引用。
        if (handles.TryGetValue(key, out var handle)) Release(key, handle);
    }

    /// <summary>释放同一路径的所有类型缓存。</summary>
    public void Unload(string location)
    {
        // keys：待释放的缓存键副本，避免遍历字典时直接修改字典。
        var keys = new List<(Type, string)>();
        // key：资源类型与地址组成的缓存键，或当前遍历的同类键。
        foreach (var key in handles.Keys)
            if (key.Location == location) keys.Add(key);
        // key：资源类型与地址组成的缓存键，或当前遍历的同类键。
        foreach (var key in keys) Release(key, handles[key]);
    }

    /// <summary>释放所有资源句柄；调用前先销毁仍在使用这些资源的实例。</summary>
    public void UnloadAll()
    {
        // handle：当前资源或场景的 YooAsset 加载句柄，用它等待完成和释放引用。
        foreach (var handle in handles.Values) handle.Release();
        handles.Clear();
    }

    /// <summary>同类型同路径只创建一个加载句柄。</summary>
    private AssetHandle GetOrLoad<T>(string location, bool asynchronous) where T : UnityEngine.Object
    {
        // key：资源类型与地址组成的缓存键，或当前遍历的同类键。
        var key = (typeof(T), location);
        // existing：已缓存的资源加载句柄，同类型同地址复用它。
        if (handles.TryGetValue(key, out var existing)) return existing;
        // handle：当前资源或场景的 YooAsset 加载句柄，用它等待完成和释放引用。
        var handle = asynchronous ? Package.LoadAssetAsync<T>(location) : Package.LoadAssetSync<T>(location);
        handles.Add(key, handle);
        return handle;
    }

    /// <summary>确认等待的句柄仍属于本次加载，而不是已被取消的旧任务。</summary>
    private bool IsCurrent<T>(string location, AssetHandle handle)
        // current：当前字典里保存的句柄，用于判断是否仍是原来的加载任务。
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
            // asset：加载句柄返回的实际 Unity 资源对象。
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
        // current：当前字典里保存的句柄，用于判断是否仍是原来的加载任务。
        if (!handles.TryGetValue(key, out var current) || !ReferenceEquals(current, handle)) return;
        handles.Remove(key);
        handle.Release();
    }
}
