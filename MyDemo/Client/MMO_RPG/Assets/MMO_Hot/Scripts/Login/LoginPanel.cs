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
        try
        {
            var result = await LoginController.Instance.LoginAsync(inputLoginAccount.text, inputLoginPassword.text);
            ShowTip(result);
            if (result != AccountErrorCode.LoginSuccess) return;

            result = await CreatRoleController.Instance.EnterGameAsync();
            if (result == AccountErrorCode.HaveRole)
                Hide();
            else if (result == AccountErrorCode.NoRole)
            {
                await UIManager.Instance.ShowAsync<ChooseRolePanel>();
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
            ShowTip(result);
            if (result == AccountErrorCode.RegisterSuccess) registerView.SetActive(false);
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
    public void ShowTip(AccountErrorCode code)
    {
        var text = code switch
        {
            AccountErrorCode.LoginSuccess => "登录成功",
            AccountErrorCode.RegisterSuccess => "注册成功",
            AccountErrorCode.AccountNotExistOrPasswordError => "账号不存在或密码错误",
            AccountErrorCode.RegisterAccountExist => "注册账号已存在",
            AccountErrorCode.AuthenticationError => "认证错误",
            AccountErrorCode.AccountPaawordEmpty => "账号或密码为空或超过长度限制",
            AccountErrorCode.NoLogin => "登录已失效，请重新登录",
            _ => "服务器错误，请稍后重试",
        };
        TipManager.Instance.ShowTip(text);
    }

    /// <summary>隐藏登录界面并释放登录场景的展示模型。</summary>
    public override void Hide()
    {
        base.Hide();
        if (objDragon != null) Destroy(objDragon);
    }
}
