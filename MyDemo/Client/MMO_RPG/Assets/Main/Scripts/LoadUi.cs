using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>显示启动进度和状态文字。</summary>
public sealed class LoadUi : MonoBehaviour
{
    /// <summary>以 Image.fillAmount 显示真实 AB 下载进度，范围 0 到 1。</summary>
    [SerializeField] private Image imgProgress;
    /// <summary>启动和资源下载流程入口；UI 通过事件获取状态。</summary>
    [SerializeField] private MyYooAsset yooAsset;
    /// <summary>当前启动步骤、下载数量或失败提示的文字。</summary>
    [SerializeField] private TMP_Text txtProgress;
    /// <summary>显示真实下载进度的百分比，独立于当前步骤文字。</summary>
    [SerializeField] private TMP_Text txtPercent;
    /// <summary>启动失败后显示的重试按钮；成功或正在处理时隐藏。</summary>
    [SerializeField] private Button retryButton;

    /// <summary>绑定加载事件，并刷新当前进度。</summary>
    private void OnEnable()
    {
        yooAsset.DownloadProgressChanged += OnDownloadProgress;
        yooAsset.StatusChanged += OnStatusChanged;
        yooAsset.RetryAvailabilityChanged += OnRetryAvailabilityChanged;
        retryButton.onClick.AddListener(yooAsset.RetryStartup);
        OnRetryAvailabilityChanged(yooAsset.CanRetry);
        OnDownloadProgress(yooAsset.DownloadProgress, yooAsset.ProgressMessage);
    }

    /// <summary>更新下载进度和提示文字。</summary>
    private void OnDownloadProgress(float progress, string message)
    {
        imgProgress.fillAmount = progress;
        txtPercent.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        OnStatusChanged(message);
    }

    /// <summary>显示初始化、加载和失败提示。</summary>
    private void OnStatusChanged(string message) => txtProgress.text = message;

    private void OnRetryAvailabilityChanged(bool available)
    {
        retryButton.gameObject.SetActive(available);
        retryButton.interactable = available;
    }

    /// <summary>界面关闭时解除订阅，避免启动入口持有已销毁的 UI。</summary>
    private void OnDisable()
    {
        yooAsset.DownloadProgressChanged -= OnDownloadProgress;
        yooAsset.StatusChanged -= OnStatusChanged;
        yooAsset.RetryAvailabilityChanged -= OnRetryAvailabilityChanged;
        retryButton.onClick.RemoveListener(yooAsset.RetryStartup);
    }
}
