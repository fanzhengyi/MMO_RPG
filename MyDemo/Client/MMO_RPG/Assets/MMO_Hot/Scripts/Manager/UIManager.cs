using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// UI 统一入口。面板预制体名必须与面板脚本名一致。
/// 例如 LoginPanel 对应 LoginPanel.prefab。
/// </summary>
public class UIManager : Singleton<UIManager>
{
    private const string UIPrefabPath = "Assets/MMO_Hot/Prefabs/UI/";

    private readonly Dictionary<Type, BasePanel> _panels = new Dictionary<Type, BasePanel>();
    private readonly HashSet<Type> _loadingPanels = new HashSet<Type>();
    private Transform _uiRoot;

    public UIManager()
    {
        GameObject rootPrefab = ResManager.Instance.Load<GameObject>(UIPrefabPath + "UIRoot");
        GameObject rootObject = GameObject.Instantiate(rootPrefab);
        _uiRoot = rootObject.transform;
        GameObject.DontDestroyOnLoad(rootObject);
    }

    /// <summary>
    /// 直接显示 UI。通常业务代码只需调用 UIManager.Instance.Show&lt;LoginPanel&gt;()。
    /// </summary>
    public void Show<T>(Action<T> onComplete = null) where T : BasePanel
    {
        ShowAsync<T>().ContinueWith(panel => onComplete?.Invoke(panel)).Forget(Debug.LogException);
    }

    public async UniTask<T> ShowAsync<T>() where T : BasePanel
    {
        Type type = typeof(T);

        if (_panels.TryGetValue(type, out BasePanel oldPanel))
        {
            oldPanel.Show();
            return (T)oldPanel;
        }

        // 同一帧连续 Show 时，只让第一个调用真正创建预制体。
        if (_loadingPanels.Contains(type))
        {
            await UniTask.WaitUntil(() => !_loadingPanels.Contains(type));
            return await ShowAsync<T>();
        }

        _loadingPanels.Add(type);
        try
        {
            string path = UIPrefabPath + type.Name + ".prefab";
            GameObject prefab = await ResManager.Instance.LoadAsync<GameObject>(path);
            GameObject panelObject = GameObject.Instantiate(prefab, _uiRoot, false);

            T panel = panelObject.GetComponent<T>();
            if (panel == null)
            {
                GameObject.Destroy(panelObject);
                throw new Exception($"UI 根节点没有挂 {type.Name}：{path}");
            }

            panel.Init();
            panel.Show();
            _panels.Add(type, panel);
            return panel;
        }
        finally
        {
            _loadingPanels.Remove(type);
        }
    }

    public void Hide<T>() where T : BasePanel
    {
        if (_panels.TryGetValue(typeof(T), out BasePanel panel))
            panel.Hide();
    }

    public T GetPanel<T>() where T : BasePanel
    {
        return _panels.TryGetValue(typeof(T), out BasePanel panel) ? panel as T : null;
    }

    /// <summary>
    /// 销毁 UI 实例；需要时 Show 会重新创建。常驻 UI 一般只 Hide，不 Close。
    /// </summary>
    public void Close<T>() where T : BasePanel
    {
        Type type = typeof(T);
        if (!_panels.TryGetValue(type, out BasePanel panel))
            return;

        _panels.Remove(type);
        GameObject.Destroy(panel.gameObject);
    }
}
