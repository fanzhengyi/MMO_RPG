using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>按面板类型缓存实例；并发打开共用创建任务。</summary>
public sealed class UIManager : Singleton<UIManager>
{
    private const string UIPrefabPath = "Assets/MMO_Hot/Prefabs/UI/";
    private readonly Dictionary<Type, BasePanel> panels = new Dictionary<Type, BasePanel>();
    private readonly Dictionary<Type, UniTask<BasePanel>> loading = new Dictionary<Type, UniTask<BasePanel>>();
    private readonly Transform uiRoot;

    public UIManager()
    {
        var prefab = ResManager.Instance.Load<GameObject>(UIPrefabPath + "UIRoot");
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
        var type = typeof(T);
        if (panels.TryGetValue(type, out var panel) && panel != null)
        {
            panel.Show();
            return (T)panel;
        }
        panels.Remove(type);
        if (!loading.TryGetValue(type, out var task))
        {
            task = CreateAsync<T>().Preserve();
            loading[type] = task;
        }
        try { return (T)await task; }
        finally
        {
            // 旧等待者不能移除失败重试时新建的任务。
            if (loading.TryGetValue(type, out var current) && current.Equals(task)) loading.Remove(type);
        }
    }

    /// <summary>创建失败时销毁实例，防止留下没有管理者的 UI。</summary>
    private async UniTask<BasePanel> CreateAsync<T>() where T : BasePanel
    {
        var path = UIPrefabPath + typeof(T).Name + ".prefab";
        var prefab = await ResManager.Instance.LoadAsync<GameObject>(path);
        var obj = GameObject.Instantiate(prefab, uiRoot, false);
        try
        {
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
        if (panels.TryGetValue(typeof(T), out var panel) && panel != null) panel.Hide();
    }

    /// <summary>读取已创建的面板，不触发加载。</summary>
    public T GetPanel<T>() where T : BasePanel
        => panels.TryGetValue(typeof(T), out var panel) ? panel as T : null;

    /// <summary>销毁缓存面板，下一次显示时重新创建。</summary>
    public void Close<T>() where T : BasePanel
    {
        if (!panels.TryGetValue(typeof(T), out var panel)) return;
        panels.Remove(typeof(T));
        if (panel != null) GameObject.Destroy(panel.gameObject);
    }
}
