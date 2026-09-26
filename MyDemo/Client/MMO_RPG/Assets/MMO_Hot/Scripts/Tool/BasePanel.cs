using UnityEngine;

/// <summary>
/// 所有 UI 面板的基类。挂到 UI 预制体的根节点。
/// </summary>
public abstract class BasePanel : MonoBehaviour
{
    private bool _inited;
    // 第一次创建面板时调用一次。写缓存控件、按钮监听。
    public void Init()
    {
        if (_inited) return;
        _inited = true;
        OnInit();
    }

    // 每次打开面板时调用。
    public virtual void Show()
    {
        gameObject.SetActive(true);
        OnShow();
    }

    // 每次关闭面板时调用。
    public virtual void Hide()
    {
        OnHide();
        gameObject.SetActive(false);
    }

    protected abstract void OnInit();
    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
}
