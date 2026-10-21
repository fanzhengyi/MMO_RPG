using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>按面板类型缓存实例；并发打开共用创建任务。</summary>
public sealed class UIManager : Singleton<UIManager>
{
    /// <summary>面板预制体资源目录；默认用面板类名拼接 .prefab 地址。</summary>
    private const string UIPrefabPath = "Assets/MMO_Hot/Prefabs/UI/";
    /// <summary>已创建的面板缓存，以面板类型为键；Hide 后保留，Close 才销毁。</summary>
    private readonly Dictionary<Type, BasePanel> panels = new Dictionary<Type, BasePanel>();
    /// <summary>正在创建的面板任务；同时打开同一个面板时共享任务，避免重复实例化。</summary>
    private readonly Dictionary<Type, UniTask<BasePanel>> loading = new Dictionary<Type, UniTask<BasePanel>>();
    /// <summary>所有面板挂载的常驻 UI 根节点，跨 Unity 场景保留。</summary>
    private readonly Transform uiRoot;

    public UIManager()
    {
        // prefab：从资源包加载的 UI 根节点或面板预制体。
        var prefab = ResManager.Instance.Load<GameObject>(UIPrefabPath + "UIRoot");
        // root：新实例化的常驻 UI 根物体。
        var root = GameObject.Instantiate(prefab);
        uiRoot = root.transform;
        GameObject.DontDestroyOnLoad(root);
    }

    /// <summary>不需要等待时使用回调入口，错误统一交给日志。</summary>
    public void ShowPanel<T>(Action<T> onComplete = null) where T : BasePanel
        => ShowAsync<T>().ContinueWith(panel => onComplete?.Invoke(panel)).Forget(Debug.LogException);

    /// <summary>多个打开请求共享成功或失败结果，避免每帧轮询和递归重试。</summary>
    public async UniTask<T> ShowAsync<T>() where T : BasePanel
    {
        // type：要打开的面板脚本类型，作为缓存和加载任务的字典键。
        var type = typeof(T);
        // panel：缓存中查到或新创建的具体面板组件。
        if (panels.TryGetValue(type, out var panel) && panel != null)
        {
            panel.Show();
            return (T)panel;
        }
        panels.Remove(type);
        // task：同类面板共享的异步创建任务，避免并发创建多个面板。
        if (!loading.TryGetValue(type, out var task))
        {
            task = CreateAsync<T>().Preserve();
            loading[type] = task;
        }
        try { return (T)await task; }
        finally
        {
            // 旧等待者不能移除失败重试时新建的任务。
            // current：字典当前登记的创建任务，避免旧等待者清除新的重试任务。
            if (loading.TryGetValue(type, out var current) && current.Equals(task)) loading.Remove(type);
        }
    }

    /// <summary>创建失败时销毁实例，防止留下没有管理者的 UI。</summary>
    private async UniTask<BasePanel> CreateAsync<T>() where T : BasePanel
    {
        // path：由面板类名拼出的 YooAsset 预制体地址。
        var path = UIPrefabPath + typeof(T).Name + ".prefab";
        // prefab：从资源包加载的 UI 根节点或面板预制体。
        var prefab = await ResManager.Instance.LoadAsync<GameObject>(path);
        // obj：新实例化的面板根物体。
        var obj = GameObject.Instantiate(prefab, uiRoot, false);
        try
        {
            // panel：缓存中查到或新创建的具体面板组件。
            var panel = obj.GetComponent<T>();
            if (panel == null) throw new InvalidOperationException($"UI 根节点没有挂 {typeof(T).Name}：{path}");
            panel.Init();
            panel.Show();
            panels[typeof(T)] = panel;
            return panel;
        }
        catch { GameObject.Destroy(obj); throw; }
    }

    /// <summary>隐藏已有面板，保留实例供下次显示。</summary>
    public void Hide<T>() where T : BasePanel
    {
        // panel：缓存中查到或新创建的具体面板组件。
        if (panels.TryGetValue(typeof(T), out var panel) && panel != null) panel.Hide();
    }

    /// <summary>读取已创建的面板，不触发加载。</summary>
    public T GetPanel<T>() where T : BasePanel
        // panel：缓存中查到或新创建的具体面板组件。
        => panels.TryGetValue(typeof(T), out var panel) ? panel as T : null;

    /// <summary>销毁缓存面板，下一次显示时重新创建。</summary>
    public void Close<T>() where T : BasePanel
    {
        // panel：缓存中查到或新创建的具体面板组件。
        if (!panels.TryGetValue(typeof(T), out var panel)) return;
        panels.Remove(typeof(T));
        if (panel != null) GameObject.Destroy(panel.gameObject);
    }
}
