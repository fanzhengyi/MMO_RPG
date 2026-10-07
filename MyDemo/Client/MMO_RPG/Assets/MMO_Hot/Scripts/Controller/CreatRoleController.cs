using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreatRoleController : Singleton<CreatRoleController>
{
    private const string GameSceneLocation = "Assets/MMO_Hot/Scenes/Scene Night.unity";

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

        try
        {
            using var response = await gateSession.C2G_EnterGameRequest();
            var errorCode = (AccountErrorCode)response.AccountErrorCode;
            //没角色就返回错误码
            if (errorCode != AccountErrorCode.HaveRole || response.Info == null)
            {
                return errorCode;
            }
            //有角色
            PlayerSelfModel.Instance.SetInfo(response.Info);
            Debug.Log($"角色: RoleId={response.Info.RoleId} " +
                      $"Hp={response.Info.Hp}/{response.Info.MaxHp} " +
                      $"Pos=({response.Info.X},{response.Info.Y},{response.Info.Z})");

            return await EnterWorldAsync();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            return AccountErrorCode.ServerError;
        }
    }

    public async FTask<AccountErrorCode> CreatRoleAsync(string userName)
    {
        var gateSession = NetworkManager.Instance.GateSession;
        if (gateSession == null || gateSession.IsDisposed)
        {
            Log.Info("Gate连接不存在，创建失败");
            return AccountErrorCode.NoLogin;
        }
        AccountErrorCode errorCode;
        using (var response = await gateSession.C2G_CreateRoleRequest(userName))
        {
            errorCode = (AccountErrorCode)response.AccountErrorCode;
            if ((errorCode == AccountErrorCode.CreateRoleSuccess || errorCode == AccountErrorCode.HaveRole)
                && response.Info != null)
            {
                PlayerSelfModel.Instance.SetInfo(response.Info);
            }
        }

        if (errorCode == AccountErrorCode.CreateRoleSuccess || errorCode == AccountErrorCode.HaveRole)
        {
            var enterResult = await EnterGameAsync();
            return enterResult == AccountErrorCode.HaveRole ? AccountErrorCode.CreateRoleSuccess : enterResult;
        }

        return errorCode;
    }
    //进入游戏请求
    private async FTask<AccountErrorCode> EnterWorldAsync()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != GameSceneLocation)
        {
            await ResManager.Instance.LoadSceneAsync(GameSceneLocation);
        }

        var localPlayer = GameObject.Find("PlayerRoot");
        if (localPlayer == null)
        {
            Debug.LogError("游戏场景中找不到 PlayerRoot。");
            return AccountErrorCode.ServerError;
        }

        var player = PlayerSelfModel.Instance;
        localPlayer.transform.SetPositionAndRotation(new Vector3(player.X, player.Y, player.Z),
            Quaternion.Euler(0f, player.RotationY, 0f));
        RemotePlayerManager.Initialize(localPlayer);
        return AccountErrorCode.HaveRole;
    }
}
