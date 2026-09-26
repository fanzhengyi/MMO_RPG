using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using UnityEngine;

public class LoginController : Singleton<LoginController>
{
    public async FTask<AccountErrorCode> LoginAsync(string account, string password)
    {

        Session loginSession = null;
        Session gateSession = null;

        try
        {
            // 登录前断开旧 Gate，避免同一客户端保留多个长期连接。
            NetworkManager.Instance.DisconnectGate();

            // 第一步：连接 Authentication，验证账号密码并取得 Token。
            loginSession = await NetworkManager.Instance.ConnectAsync(LoginServerConfig.GetAddress(account));
            string token;
            string gateAddress;

            using (var request = C2A_LoginRequest.Create())
            {
                request.Username = account;
                request.Password = password;

                using var response = (A2C_LoginResponse)await loginSession.Call(request);
                if (response.LoginError != AccountErrorCode.LoginSuccess)
                    return response.LoginError;

                if (!JwtHelper.TryGetPayload(response.Token, out JwtPayload payload))
                {
                    Log.Error("登录 Token 格式错误，无法取得 Gate 地址。");
                    return AccountErrorCode.ServerError;
                }

                token = response.Token;
                gateAddress = payload.Address;
            }

            // Authentication 连接只负责登录，拿到 Token 后立即关闭。
            loginSession.Dispose();

            // 第二步：连接 Gate，验证 Token 后才成为长期游戏连接。
            gateSession = await NetworkManager.Instance.ConnectAsync(gateAddress);
            using (var request = C2G_LoginRequest.Create())
            {
                request.Token = token;
                request.UserName = account;

                using var response = (G2C_LoginResponse)await gateSession.Call(request);
                if (response.LoginError != AccountErrorCode.LoginSuccess)
                {
                    gateSession.Dispose();
                    return AccountErrorCode.ServerError;
                }
            }

            NetworkManager.Instance.SetGateSession(gateSession);
            gateSession = null;
            return AccountErrorCode.LoginSuccess;
        }
        catch (Exception exception)
        {
            Log.Error(exception);
            NetworkManager.Instance.DisconnectGate();
            return AccountErrorCode.ServerError;
        }
        finally
        {
            if (loginSession != null && !loginSession.IsDisposed)
                loginSession.Dispose();

            // Gate 验证失败或中途异常时
            if (gateSession != null && !gateSession.IsDisposed)
                gateSession.Dispose();

        }
    }

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

    public async FTask<AccountErrorCode> RegisterAsync(string account, string password)
    {
        Session loginSession = null;

        try
        {
            loginSession = await NetworkManager.Instance.ConnectAsync(LoginServerConfig.GetAddress(account));
            using (var request = C2A_RegisterRequest.Create())
            {
                request.Username = account;
                request.Password = password;

                using var response = (A2C_RegisterResponse)await loginSession.Call(request);
                return response.LoginError;
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return AccountErrorCode.ServerError;
        }
        finally
        {

            if (loginSession != null && !loginSession.IsDisposed)
                loginSession.Dispose();
        }
    }
}
