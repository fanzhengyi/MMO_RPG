using Fantasy;
using Fantasy.Async;
using TMPro;
using UnityEngine.UI;

/// <summary>昵称输入与角色创建结果展示。</summary>
public sealed class ChooseRolePanel : BasePanel
{
    /// <summary>提交创建角色的按钮，等待期间禁用。</summary>
    public Button createRoleButton;
    /// <summary>游戏内角色昵称输入框，不是登录账号。</summary>
    public TMP_InputField nicknameInput;
    /// <summary>是否正在创建角色并进入游戏；防止重复提交昵称。</summary>
    private bool requesting;

    /// <summary>绑定创建按钮。</summary>
    protected override void OnInit()
    {
        createRoleButton.onClick.AddListener(() => CreateRoleAsync().Coroutine());
    }

    /// <summary>创建期间禁用按钮；成功才隐藏面板，异常不会让按钮永久失效。</summary>
    private async FTask CreateRoleAsync()
    {
        if (requesting) return;
        requesting = true;
        createRoleButton.interactable = false;
        try
        {
            // result：创建角色并进入世界的最终业务结果。
            var result = await RoleController.Instance.CreateRoleAsync(nicknameInput.text);
            switch (result)
            {
                case ResultCode.Success:
                    TipManager.Instance.ShowTip("角色创建成功");
                    Hide();
                    break;
                case ResultCode.InvalidNickname:
                    TipManager.Instance.ShowTip("昵称不能为空，且不能超过24个字符");
                    break;
                case ResultCode.NotAuthenticated:
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
            if (createRoleButton != null) createRoleButton.interactable = true;
        }
    }
}
