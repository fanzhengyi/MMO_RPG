using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;

/// <summary>组织鉴权和 Gate 登录；请求互斥，短连接由 finally 释放。</summary>
public sealed class LoginController : Singleton<LoginController>
{
    private bool requesting;

    /// <summary>先取得 Token，再建立经过身份验证的 Gate 长连接。</summary>
    public async FTask<AccountErrorCode> LoginAsync(string account, string password)
    {
        account = account?.Trim();
        if (!ValidCredentials(account, password)) return AccountErrorCode.AccountPaawordEmpty;
        if (requesting) return AccountErrorCode.ServerError;
        requesting = true;
        Session authentication = null;
        Session gate = null;
        try
        {
            NetworkManager.Instance.DisconnectGate();
            PlayerSelfModel.Instance.Clear();
            RemotePlayerManager.Reset();
            authentication = await NetworkManager.Instance.ConnectAsync(LoginServerConfig.GetAddress(account));
            string token;
            string gateAddress;
            using (var response = await authentication.C2A_LoginRequest(account, password))
            {
                if (response.LoginError != AccountErrorCode.LoginSuccess) return response.LoginError;
                if (!JwtHelper.TryGetPayload(response.Token, out var payload)) return AccountErrorCode.ServerError;
                token = response.Token;
                gateAddress = payload.Address;
            }
            authentication.Dispose();
            authentication = null;

            gate = await NetworkManager.Instance.ConnectAsync(gateAddress);
            using (var response = await gate.C2G_LoginRequest(token, account))
            {
                if (response.LoginError != AccountErrorCode.LoginSuccess) return response.LoginError;
            }
            NetworkManager.Instance.SetGateSession(gate);
            gate = null;
            return AccountErrorCode.LoginSuccess;
        }
        catch (Exception exception)
        {
            Log.Error(exception);
            return AccountErrorCode.ServerError;
        }
        finally
        {
            if (authentication != null && !authentication.IsDisposed) authentication.Dispose();
            if (gate != null && !gate.IsDisposed) gate.Dispose();
            requesting = false;
        }
    }

    /// <summary>注册使用短连接；和登录共用请求保护。</summary>
    public async FTask<AccountErrorCode> RegisterAsync(string account, string password)
    {
        account = account?.Trim();
        if (!ValidCredentials(account, password)) return AccountErrorCode.AccountPaawordEmpty;
        if (requesting) return AccountErrorCode.ServerError;
        requesting = true;
        Session session = null;
        try
        {
            session = await NetworkManager.Instance.ConnectAsync(LoginServerConfig.GetAddress(account));
            using var response = await session.C2A_RegisterRequest(account, password);
            return response.LoginError;
        }
        catch (Exception exception)
        {
            Log.Error(exception);
            return AccountErrorCode.ServerError;
        }
        finally
        {
            if (session != null && !session.IsDisposed) session.Dispose();
            requesting = false;
        }
    }

    /// <summary>客户端限制输入长度，服务端仍会独立验证。</summary>
    private static bool ValidCredentials(string account, string password)
        => !string.IsNullOrWhiteSpace(account) && account.Length <= 64
            && !string.IsNullOrEmpty(password) && password.Length <= 128;
}
