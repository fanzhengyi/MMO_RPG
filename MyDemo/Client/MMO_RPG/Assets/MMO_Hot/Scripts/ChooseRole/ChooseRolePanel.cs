using Fantasy;
using Fantasy.Async;
using TMPro;
using UnityEngine.UI;

/// <summary>昵称输入与角色创建结果展示。</summary>
public sealed class ChooseRolePanel : BasePanel
{
    public Button btnCreatRole;
    public TMP_InputField userName;
    private bool requesting;

    /// <summary>绑定创建按钮。</summary>
    protected override void OnInit()
    {
        btnCreatRole.onClick.AddListener(() => CreateRoleAsync().Coroutine());
    }

    /// <summary>创建期间禁用按钮；成功才隐藏面板，异常不会让按钮永久失效。</summary>
    private async FTask CreateRoleAsync()
    {
        if (requesting) return;
        requesting = true;
        btnCreatRole.interactable = false;
        try
        {
            var result = await CreatRoleController.Instance.CreatRoleAsync(userName.text);
            switch (result)
            {
                case AccountErrorCode.CreateRoleSuccess:
                    TipManager.Instance.ShowTip("角色创建成功");
                    Hide();
                    break;
                case AccountErrorCode.NickNameNull:
                    TipManager.Instance.ShowTip("昵称不能为空，且不能超过24个字符");
                    break;
                case AccountErrorCode.NoLogin:
                    Hide();
                    UIManager.Instance.ShowPanel<LoginPanel>();
                    TipManager.Instance.ShowTip("登录已失效，请重新登录");
                    break;
                default:
                    TipManager.Instance.ShowTip("服务器错误，请稍后重试");
                    break;
            }
        }
        finally
        {
            requesting = false;
            if (btnCreatRole != null) btnCreatRole.interactable = true;
        }
    }
}
