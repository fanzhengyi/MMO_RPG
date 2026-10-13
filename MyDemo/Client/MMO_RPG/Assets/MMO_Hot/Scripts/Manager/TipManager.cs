/// <summary>提示面板的业务入口；动画生命周期由面板自己负责。</summary>
public sealed class TipManager : Singleton<TipManager>
{
    /// <summary>打开提示面板并播放当前文本。</summary>
    public void ShowTip(string text)
        => UIManager.Instance.ShowPanel<TipPanel>(panel => panel.PlayTip(text));
}
