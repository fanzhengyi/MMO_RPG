using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TipPanel : BasePanel
{
    private const float BackGroundAlpha = 0.392f; // 预制体里背景图的初始透明度

    public TMP_Text textTip;
    public Image bkImage;
    private RectTransform rectTransform;
    protected override void OnInit()
    {
        textTip ??= GetComponentInChildren<TMP_Text>();
        rectTransform=transform as RectTransform;
    }


    public void SetText(string text)
    {
        textTip.text = text;
    }

 public void PlayTip(string text, float duration = 1.5f)
{
    SetText(text);
    // 杀掉旧动画（Hide 回调挂在这些 tween 上，会一起被杀掉）
    rectTransform.DOKill();

    // 重置状态（位置 + 透明度，连续触发不会残留）
    rectTransform.anchoredPosition = new Vector2(0, 180);
    rectTransform.DOAnchorPosY(rectTransform.anchoredPosition.y + 120, duration)
        .SetEase(Ease.OutQuad)
        .OnComplete(Hide);
}
}
