using Fantasy;
using Fantasy.Async;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChooseRolePanel : BasePanel
{
    public Button btnCreatRole;
    public TMP_InputField userName;
    private bool _isRequesting;
    protected override void OnInit()
    {
        btnCreatRole.onClick.AddListener(() =>
        {
            OnCreateRoleClick();
        });
    }
    private void OnCreateRoleClick()
    {
        CreateRoleAsync().Coroutine();
    }
     private async FTask CreateRoleAsync()
    {
        if(_isRequesting)return;
        _isRequesting=true;
        btnCreatRole.interactable=false;
        try
        {
            //判空
            if (string.IsNullOrEmpty(userName.text))
            {
                TipManager.Instance.ShowTip("昵称不能为空");
            }
            AccountErrorCode errcode=await CreatRoleController.Instance.CreatRoleAsync(userName.text);
            Cheak(errcode);
            
        }
        finally
        {
            _isRequesting=false;
            btnCreatRole.interactable=true;
        }
    }


    private void Cheak(AccountErrorCode accountErrorCode)
    {
        switch (accountErrorCode)
        {
            case AccountErrorCode.CreateRoleSuccess:
                TipManager.Instance.ShowTip("已经拥有角色");
            break;
            case AccountErrorCode.NickNameNull:
                TipManager.Instance.ShowTip("角色名字不能为空");
            break;
            case AccountErrorCode.NoLogin:
                TipManager.Instance.ShowTip("登录失效，请重新登录");
            break;
            default:
                TipManager.Instance.ShowTip("服务器错误");
            break;
        }
    }
}
