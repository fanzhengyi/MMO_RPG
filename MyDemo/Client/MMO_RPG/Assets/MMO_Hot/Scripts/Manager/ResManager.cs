using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
using SceneHandle = YooAsset.SceneHandle;

/// <summary>
/// YooAsset 资源加载入口。MyYooAsset 完成初始化与热更后即可直接使用。
/// </summary>
public class ResManager : Singleton<ResManager>
{
    private const string PackageName = "DefaultPackage";

    // 一个路径可按不同类型加载，所以 key 同时包含路径和资源类型。
    private readonly Dictionary<string, AssetHandle> _handles = new Dictionary<string, AssetHandle>();

    private ResourcePackage Package => YooAssets.GetPackage(PackageName);

    public T Load<T>(string location) where T : UnityEngine.Object
    {
        string key = GetKey<T>(location);
        if (_handles.TryGetValue(key, out AssetHandle oldHandle))
        {
            oldHandle.WaitForAsyncComplete();
            return GetAsset<T>(oldHandle, location);
        }

        AssetHandle handle = Package.LoadAssetSync<T>(location);
        _handles.Add(key, handle);

        try
        {
            return GetAsset<T>(handle, location);
        }
        catch
        {
            Release(key, handle);
            throw;
        }
    }

    /// <summary>
    /// 异步加载 GameObject、Sprite、AudioClip 等资源。
    /// </summary>
    public async UniTask<T> LoadAsync<T>(string location) where T : UnityEngine.Object
    {
        string key = GetKey<T>(location);
        if (_handles.TryGetValue(key, out AssetHandle oldHandle))
        {
            await UniTask.WaitUntil(() => oldHandle.IsDone);
            return GetAsset<T>(oldHandle, location);
        }

        AssetHandle handle = Package.LoadAssetAsync<T>(location);
        _handles.Add(key, handle);

        await UniTask.WaitUntil(() => handle.IsDone);
        try
        {
            return GetAsset<T>(handle, location);
        }
        catch
        {
            Release(key, handle);
            throw;
        }
    }

    /// <summary>
    /// 回调版异步加载。需要 await 时优先用上面的 LoadAsync。
    /// </summary>
    public void LoadAsync<T>(string location, Action<T> onComplete) where T : UnityEngine.Object
    {
        LoadAsync<T>(location).ContinueWith(onComplete).Forget(Debug.LogException);
    }

    /// <summary>
    /// 异步加载场景。这里的 progress 是场景加载进度，不是热更新下载 AB 的进度。
    /// </summary>
    public async UniTask<SceneHandle> LoadSceneAsync(string location,
        Action<float> onProgress = null, LoadSceneMode mode = LoadSceneMode.Single)
    {
        SceneHandle handle = Package.LoadSceneAsync(location, mode);
        while (!handle.IsDone)
        {
            onProgress?.Invoke(handle.Progress);
            await UniTask.Yield();
        }

        if (handle.Status != EOperationStatus.Succeeded)
            throw new Exception($"场景加载失败：{location}\n{handle.Error}");

        onProgress?.Invoke(1f);
        return handle;
    }

    public void Unload<T>(string location) where T : UnityEngine.Object
    {
        string key = GetKey<T>(location);
        if (_handles.TryGetValue(key, out AssetHandle handle))
            Release(key, handle);
    }

    /// <summary>
    /// 卸载同一路径下所有类型的缓存资源。
    /// </summary>
    public void Unload(string location)
    {
        List<string> keys = new List<string>();
        foreach (string key in _handles.Keys)
        {
            if (key.EndsWith("|" + location))
                keys.Add(key);
        }

        foreach (string key in keys)
            Release(key, _handles[key]);
    }

    public void UnloadAll()
    {
        foreach (AssetHandle handle in _handles.Values)
            handle.Release();

        _handles.Clear();
    }

    private static string GetKey<T>(string location)
    {
        return typeof(T).FullName + "|" + location;
    }

    private void Release(string key, AssetHandle handle)
    {
        handle.Release();
        _handles.Remove(key);
    }

    private static T GetAsset<T>(AssetHandle handle, string location) where T : UnityEngine.Object
    {
        if (handle.Status != EOperationStatus.Succeeded)
            throw new Exception($"资源加载失败：{location}\n{handle.Error}");

        T asset = handle.GetAssetObject<T>();
        if (asset == null)
            throw new Exception($"资源类型不对：{location} 不是 {typeof(T).Name}");

        return asset;
    }
}
