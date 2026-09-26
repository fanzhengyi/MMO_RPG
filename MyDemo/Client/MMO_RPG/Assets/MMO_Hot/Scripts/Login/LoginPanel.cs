using Fantasy;
using Fantasy.Async;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 登录界面只处理输入、按钮状态和显示结果。
/// 账号验证与网络连接都在 LoginController 中。
/// </summary>
public class LoginPanel : BasePanel
{
    [Header("注册界面")]
    public GameObject registerView;

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

    private bool _isRequesting;

    protected override void OnInit()
    {
        //登录
        btnLogin.onClick.AddListener(OnLoginClick);
        btnLogin_Register.onClick.AddListener(() => registerView.SetActive(true));
        //注册
        btnRegister.onClick.AddListener(OnRegisterClick);
        btnCancel.onClick.AddListener(() => registerView.SetActive(false));
        objDragon=GameObject.Find("Dragon");
        registerView.SetActive(false);
    }

    private void OnLoginClick()
    {
        LoginAsync().Coroutine();
    }

    private async FTask LoginAsync()
    {
        string account = inputLoginAccount.text.Trim();
        string password = inputLoginPassword.text;
        //判空
        if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(password))
        {
            TipManager.Instance.ShowTip("账号和密码不能为空");
            return;
        }

        AccountErrorCode result = await LoginController.Instance.LoginAsync(account, password);
        //飘字
        ShowTip(result);
        if (result == AccountErrorCode.LoginSuccess)
        {
             btnLogin.interactable = false;
             Hide();
             // 登录成功立刻请求进入游戏，判断有没有角色
            AccountErrorCode errorCode= await LoginController.Instance.EnterGameAsync();
             Debug.Log(errorCode);
             if(errorCode==AccountErrorCode.HaveRole)
             {
                 //有角色，进入游戏
                 
             }
             else if(errorCode==AccountErrorCode.NoRole)
             {
                 //没有角色，进入创建角色界面
                 UIManager.Instance.ShowPanel<ChooseRolePanel>();
                 Hide();
             }
             else
             {
                 TipManager.Instance.ShowTip("服务器错误");
             }
        }
    }

    private void OnRegisterClick()
    {
        RegisterAsync().Coroutine();
    }

    private async FTask RegisterAsync()
    {
        string account = inputRegisterAccount.text.Trim();
        string password = inputRegisterPassWord.text;
        if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(password))
        {
            TipManager.Instance.ShowTip("注册账号和密码不能为空");
            return;
        }

        if (password != inputRegisterSurePassWord.text)
        {
            TipManager.Instance.ShowTip("两次输入的密码不一致");
            return;
        }

        AccountErrorCode result = await LoginController.Instance.RegisterAsync(account, password);
        //飘字
        ShowTip(result);
        if (result == AccountErrorCode.RegisterSuccess)
           {
              registerView.SetActive(false);
              return;
           }

    }
    public void ShowTip(AccountErrorCode accountErrorCode)
    {
        switch (accountErrorCode)
        {
            case AccountErrorCode.LoginSuccess:
                TipManager.Instance.ShowTip("登录成功");
                break;
            case AccountErrorCode.RegisterSuccess:
                TipManager.Instance.ShowTip("注册成功");
                break;
            case AccountErrorCode.AccountNotExistOrPasswordError:
                TipManager.Instance.ShowTip("账号不存在或密码错误");
                break;
            case AccountErrorCode.ServerError:
                TipManager.Instance.ShowTip("服务器错误");
                break;
            case AccountErrorCode.RegisterAccountExist:
                TipManager.Instance.ShowTip("注册账号已存在");
                break;
            case AccountErrorCode.AuthenticationError:
                TipManager.Instance.ShowTip("认证错误");
                break;
            case AccountErrorCode.AccountPaawordEmpty:
                 TipManager.Instance.ShowTip("账号或密码为空");
                break;
        }
    }

    public override void Hide()
    {
        base.Hide();
        GameObject.Destroy(objDragon);
    }
}
