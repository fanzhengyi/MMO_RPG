using Fantasy;
using Fantasy.Async;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>只负责输入、按钮状态和提示，连接与角色流程由 Controller 执行。</summary>
public sealed class LoginPanel : BasePanel
{
    /// <summary>注册子界面根节点；点击注册入口显示，关闭或完成注册后隐藏。</summary>
    [Header("注册界面")] public GameObject registerView;
    /// <summary>登录账号输入框；不是角色昵称。</summary>
    [Header("登录")]
    public TMP_InputField inputLoginAccount;
    /// <summary>登录密码输入框。</summary>
    public TMP_InputField inputLoginPassword;
    /// <summary>提交登录的按钮，登录和进入世界期间禁用。</summary>
    public Button btnLogin;
    /// <summary>打开注册子界面的按钮。</summary>
    public Button btnLogin_Register;
    /// <summary>登录场景中的龙展示对象；登录面板隐藏时销毁。</summary>
    public GameObject objDragon;
    /// <summary>注册账号输入框。</summary>
    [Header("注册")]
    public TMP_InputField inputRegisterAccount;
    /// <summary>注册密码输入框。</summary>
    public TMP_InputField inputRegisterPassWord;
    /// <summary>再次确认注册密码的输入框，只用于本地一致性检查。</summary>
    public TMP_InputField inputRegisterSurePassWord;
    /// <summary>提交注册请求的按钮。</summary>
    public Button btnRegister;
    /// <summary>关闭注册子界面的按钮。</summary>
    public Button btnCancel;
    /// <summary>界面是否正在登录、注册或进入世界；true 时拦截重复点击，finally 恢复为 false。</summary>
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
            // result：当前登录、进入世界或注册步骤的业务结果。
            var result = await LoginController.Instance.LoginAsync(inputLoginAccount.text, inputLoginPassword.text);
            ShowTip(result, "登录成功");
            //登录没有成功
            if (result != ResultCode.Success)
            {
                return;
            }

            //成功检查是否有角色
            // gate：本次登录或进入世界使用的 Gate 连接。
            var gate = NetworkManager.Instance.GateSession;
            if (gate == null || gate.IsDisposed) return;
            // gateRuntimeId：开始进入世界时的 Gate 连接运行时 ID，用于排除等待期间发生的重连。
            var gateRuntimeId = gate.RuntimeId;
            // 从这里开始 result 表示进入世界结果，不再是账号密码验证结果：Success 已就绪，RoleNotFound 需要创建角色。
            result = await RoleController.Instance.EnterGameAsync();
            if (!NetworkManager.Instance.IsCurrentGate(gate, gateRuntimeId))//gate不一致
            {
                if (NetworkManager.Instance.GateSession == null && result != ResultCode.NotAuthenticated && result != ResultCode.Success) ShowTip(result);
                return;
            }

            if (result == ResultCode.Success)//有角色
                Hide();
            else if (result == ResultCode.RoleNotFound)//没有角色，显示创建界面
            {
                // choosePanel：异步加载并打开的创建角色面板实例。
                var choosePanel = await UIManager.Instance.ShowAsync<ChooseRolePanel>();

                Hide();
            }
            else//其它错误
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
            // result：当前登录、进入世界或注册步骤的业务结果。
            var result = await LoginController.Instance.RegisterAsync(
                inputRegisterAccount.text, inputRegisterPassWord.text);
            ShowTip(result, "注册成功");
            if (result == ResultCode.Success) registerView.SetActive(false);
        }
        finally { SetRequesting(false); }
    }

    /// <summary>登录与注册共用按钮保护，避免同时发起两条连接。</summary>
    // 这个标记负责请求互斥，interactable 负责按钮显示状态；异步等待期间两者一起关闭，结束后一起恢复。
    private void SetRequesting(bool value)
    {
        requesting = value;
        btnLogin.interactable = btnRegister.interactable = !value;
        btnLogin_Register.interactable = btnCancel.interactable = !value;
    }

    /// <summary>将账号错误码转换成界面提示。</summary>
    private void ShowTip(ResultCode code, string successText = "操作成功")
    {
        // text：根据业务结果码转换出的玩家可读提示文字。
        var text = code switch
        {
            ResultCode.Success => successText,
            ResultCode.IncorrectCredentials => "账号不存在或密码错误",
            ResultCode.AccountExists => "注册账号已存在",
            ResultCode.WrongAuthenticationServer => "认证错误",
            ResultCode.InvalidCredentials => "账号或密码为空或超过长度限制",
            ResultCode.NotAuthenticated => "登录已失效，请重新登录", //session 过期或被踢
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
