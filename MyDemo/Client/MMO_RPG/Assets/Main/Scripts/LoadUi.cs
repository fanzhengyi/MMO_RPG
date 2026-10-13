using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>显示启动进度和状态文字。</summary>
public sealed class LoadUi : MonoBehaviour
{
    [SerializeField] private Image imgProgress;
    [SerializeField] private MyYooAsset yooAsset;
    [SerializeField] private TMP_Text txtProgress;

    /// <summary>绑定加载事件，并刷新当前进度。</summary>
    private void OnEnable()
    {
        if (yooAsset == null || imgProgress == null || txtProgress == null)
        {
            Debug.LogError("LoadUi: 请配置加载入口、进度条和状态文字。", this);
            enabled = false;
            return;
        }

        yooAsset.DownloadProgressChanged += OnDownloadProgress;
        yooAsset.StatusChanged += OnStatusChanged;
        OnDownloadProgress(yooAsset.DownloadProgress, yooAsset.ProgressMessage);
    }

    /// <summary>界面关闭时解除订阅，避免启动入口持有已销毁的 UI。</summary>
    private void OnDisable()
    {
        if (yooAsset == null) return;
        yooAsset.DownloadProgressChanged -= OnDownloadProgress;
        yooAsset.StatusChanged -= OnStatusChanged;
    }

    /// <summary>更新下载进度和提示文字。</summary>
    private void OnDownloadProgress(float progress, string message)
    {
        imgProgress.fillAmount = progress;
        OnStatusChanged(message);
    }

    /// <summary>显示初始化、加载和失败提示。</summary>
    private void OnStatusChanged(string message) => txtProgress.text = message;
}
