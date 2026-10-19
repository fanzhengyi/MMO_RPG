using Fantasy;
using Fantasy.Async;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>只负责输入、按钮状态和提示，连接与角色流程由 Controller 执行。</summary>
public sealed class LoginPanel : BasePanel
{
    [Header("注册界面")] public GameObject registerView;
    [Header("登录")]
    public TMP_InputField inputLoginAccount;
    public TMP_InputField inputLoginPassword;
    public Button btnLogin;
    public Button btnLogin_Register;
    public GameObject objDragon;
    [Header("注册")]
    public TMP_InputField inputRegisterAccount;
    public TMP_InputField inputRegisterPassWord;
    public TMP_InputField inputRegisterSurePassWord;
    public Button btnRegister;
    public Button btnCancel;
    private bool requesting;

    /// <summary>监听按钮；注册页每次打开时保持可预期的初始状态。</summary>
    protected override void OnInit()
    {
        btnLogin.onClick.AddListener(() => LoginAsync().Coroutine());
        btnRegister.onClick.AddListener(() => RegisterAsync().Coroutine());
        btnLogin_Register.onClick.AddListener(() => registerView.SetActive(true));
        btnCancel.onClick.AddListener(() => registerView.SetActive(false));
        if (objDragon == null) objDragon = GameObject.Find("Dragon");
        registerView.SetActive(false);
    }

    /// <summary>恢复当前请求的按钮状态，重新显示不能绕过请求互斥。</summary>
    protected override void OnShow()
    {
        SetRequesting(requesting);
        registerView.SetActive(false);
    }

    /// <summary>登录与进入世界期间禁用按钮，失败后恢复登录界面。</summary>
    private async FTask LoginAsync()
    {
        if (requesting) return;
        SetRequesting(true);
        Debug.Log($"LoginAsync: {inputLoginAccount.text} {inputLoginPassword.text}");
        try
        {
            var result = await LoginController.Instance.LoginAsync(inputLoginAccount.text, inputLoginPassword.text);
            ShowTip(result, "登录成功");
            if (result != ResultCode.Success) return;

            var gate = NetworkManager.Instance.GateSession;
            if (gate == null || gate.IsDisposed) return;
            var gateRuntimeId = gate.RuntimeId;
            result = await RoleController.Instance.EnterGameAsync();
            if (!NetworkManager.Instance.IsCurrentGate(gate, gateRuntimeId))
            {
                if (NetworkManager.Instance.GateSession == null && result != ResultCode.NotAuthenticated
                    && result != ResultCode.Success) ShowTip(result);
                return;
            }
            if (result == ResultCode.Success)
                Hide();
            else if (result == ResultCode.RoleNotFound)
            {
                var choosePanel = await UIManager.Instance.ShowAsync<ChooseRolePanel>();
                if (!NetworkManager.Instance.IsCurrentGate(gate, gateRuntimeId))
                {
                    choosePanel.Hide();
                    return;
                }
                Hide();
            }
            else
            {
                NetworkManager.Instance.DisconnectGate();
                ShowTip(result);
                Show();
            }
        }
        finally { SetRequesting(false); }
    }

    /// <summary>验证确认密码，再发送注册请求。</summary>
    private async FTask RegisterAsync()
    {
        if (requesting) return;
        if (inputRegisterPassWord.text != inputRegisterSurePassWord.text)
        {
            TipManager.Instance.ShowTip("两次输入的密码不一致");
            return;
        }
        SetRequesting(true);
        try
        {
            var result = await LoginController.Instance.RegisterAsync(
                inputRegisterAccount.text, inputRegisterPassWord.text);
            ShowTip(result, "注册成功");
            if (result == ResultCode.Success) registerView.SetActive(false);
        }
        finally { SetRequesting(false); }
    }

    /// <summary>登录与注册共用按钮保护，避免同时发起两条连接。</summary>
    private void SetRequesting(bool value)
    {
        requesting = value;
        btnLogin.interactable = btnRegister.interactable = !value;
        btnLogin_Register.interactable = btnCancel.interactable = !value;
    }

    /// <summary>将账号错误码转换成界面提示。</summary>
    private void ShowTip(ResultCode code, string successText = "操作成功")
    {
        var text = code switch
        {
            ResultCode.Success => successText,
            ResultCode.IncorrectCredentials => "账号不存在或密码错误",
            ResultCode.AccountExists => "注册账号已存在",
            ResultCode.WrongAuthenticationServer => "认证错误",
            ResultCode.InvalidCredentials => "账号或密码为空或超过长度限制",
            ResultCode.NotAuthenticated => "登录已失效，请重新登录",
            ResultCode.InvalidToken => "登录凭证已失效，请重新登录",
            ResultCode.UnsupportedProtocol => "客户端版本与服务器不匹配，请更新客户端",
            ResultCode.Busy => "正在处理请求，请稍候",
            ResultCode.RateLimited => "操作过于频繁，请稍后重试",
            _ => "服务器错误，请稍后重试",
        };
        TipManager.Instance.ShowTip(text);
    }

    //关闭销毁龙
    public override void Hide()
    {
        base.Hide();
        if (objDragon != null) Destroy(objDragon);
    }
}
