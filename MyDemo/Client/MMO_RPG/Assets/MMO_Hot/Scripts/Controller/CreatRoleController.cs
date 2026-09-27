using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using UnityEngine;

public class CreatRoleController : Singleton<CreatRoleController>
{
    /// <summary>
    /// 登录成功后调用：请求进入游戏，服务器会判断有没有角色。
    /// 有角色 → 拿到自己的数据（位置等，后续状态同步用）；无角色 → 去创建角色界面。
    /// </summary>
    public async FTask<AccountErrorCode> EnterGameAsync()
    {
        var gateSession = NetworkManager.Instance.GateSession;
        if (gateSession == null || gateSession.IsDisposed)
        {
            Log.Error("Gate 连接不存在，无法进入游戏。");
            return AccountErrorCode.NoLogin;
        }

        using var response = await gateSession.C2G_EnterGameRequest();
        var errorCode = (AccountErrorCode)response.AccountErrorCode;
        //有角色
        if (errorCode == AccountErrorCode.HaveRole)
        {
             // 保存自己的角色数据（后续状态同步、移动上报都要用）
                if (response.Info != null)
                {
                    PlayerSelfModel.Instance.SetInfo(response.Info);
                    Debug.Log($"角色: RoleId={response.Info.RoleId} " +
                              $"Hp={response.Info.Hp}/{response.Info.MaxHp} " +
                              $"Pos=({response.Info.X},{response.Info.Y},{response.Info.Z})");
                }
        }
        return errorCode;
    }

    public async FTask<AccountErrorCode> CreatRoleAsync(string userName)
    {
        var gateSession=NetworkManager.Instance.GateSession;
        if (gateSession == null || gateSession.IsDisposed)
        {
            Log.Info("Gate连接不存在，创建失败");
            return AccountErrorCode.NoLogin;
        }
        using var response=await gateSession.C2G_CreateRoleRequest(userName);
        var errorcode=(AccountErrorCode)response.AccountErrorCode;
        //创建成功
        if (errorcode == AccountErrorCode.CreateRoleSuccess && response.Info != null)
        {
            PlayerSelfModel.Instance.SetInfo(response.Info);
        }
        return errorcode;
    }
}
