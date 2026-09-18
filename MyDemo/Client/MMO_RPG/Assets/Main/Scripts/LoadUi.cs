using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadUi : MonoBehaviour
{
    [SerializeField] private Image imgProgress;
    [SerializeField] private MyYooAsset yooAsset;
    [SerializeField]private TMP_Text txtProgress;
    void Start()
    {
        yooAsset.DownloadProgressChanged += YooAsset_DownloadProgressChanged;
    }

    private void YooAsset_DownloadProgressChanged(float arg1, string arg2)
    {
        imgProgress.fillAmount = arg1;
        txtProgress.text = arg2;
    }
}
