using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>单条提示动画；连续提示先停止旧动画，隐藏和销毁时释放 Tween。</summary>
public sealed class TipPanel : BasePanel
{
    /// <summary>显示本次提示内容的文本控件。</summary>
    public TMP_Text textTip;
    /// <summary>提示面板的 UI 位置，Tween 通过它实现上浮动画。</summary>
    private RectTransform rectTransform;

    /// <summary>缓存文字和动画使用的 RectTransform。</summary>
    protected override void OnInit()
    {
        if (textTip == null) textTip = GetComponentInChildren<TMP_Text>();
        rectTransform = (RectTransform)transform;
    }

    /// <summary>每条新提示重置位置；登录界面和暂停时也能正常播放。</summary>
    public void PlayTip(string text, float duration = 1.5f)
    {
        rectTransform.DOKill();
        textTip.text = text;
        rectTransform.anchoredPosition = new Vector2(0f, 180f);
        rectTransform.DOAnchorPosY(300f, duration).SetEase(Ease.OutQuad).SetUpdate(true).OnComplete(Hide);
    }

    /// <summary>隐藏时停止旧动画，避免旧完成回调影响下一条提示。</summary>
    protected override void OnHide() => rectTransform.DOKill();
    /// <summary>销毁时释放提示动画。</summary>
    private void OnDestroy() { if (rectTransform != null) rectTransform.DOKill(); }
}
