using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 轻量飘字提示：向上缓慢移动 + 渐隐，结束后自动关闭面板。
/// 用法：TipManager.Instance.ShowTip("登录成功", Color.green);
/// </summary>
public class TipManager : Singleton<TipManager>
{
    public void ShowTip(string text )
    {
        UIManager.Instance.ShowPanel<TipPanel>(tipPanel =>
        {
            // 正在播上一条时先停掉旧动画，避免位移和透明度叠加
            tipPanel.transform.DOKill();
            tipPanel.PlayTip(text);
        });
    }
}
