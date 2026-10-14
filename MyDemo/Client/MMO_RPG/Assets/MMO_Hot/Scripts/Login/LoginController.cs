using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;

/// <summary>组织鉴权和 Gate 登录；请求互斥，短连接由 finally 释放。</summary>
public sealed class LoginController : Singleton<LoginController>
{
    private bool requesting;

    /// <summary>先取得 Token，再建立经过身份验证的 Gate 长连接。</summary>
    public async FTask<ResultCode> LoginAsync(string account, string password)
    {
        account = account?.Trim();
        if (!ValidCredentials(account, password)) return ResultCode.InvalidCredentials;
        if (requesting) return ResultCode.Busy;
        requesting = true;
        Session authentication = null;
        Session gate = null;
        try
        {
            NetworkManager.Instance.DisconnectGate();
            authentication = await NetworkManager.Instance.ConnectAsync(LoginServerConfig.GetAddress(account));
            string token;
            string gateAddress;
            using (var response = await authentication.C2A_LoginRequest(account, password))
            {
                if (response.ErrorCode != 0) return ResultCode.ServerError;
                if (response.Result != ResultCode.Success) return response.Result;
                if (string.IsNullOrEmpty(response.Token) || string.IsNullOrWhiteSpace(response.GateAddress))
                    return ResultCode.ServerError;
                token = response.Token;
                gateAddress = response.GateAddress;
            }
            authentication.Dispose();
            authentication = null;

            gate = await NetworkManager.Instance.ConnectAsync(gateAddress);
            using (var response = await gate.C2G_LoginRequest(token))
            {
                if (response.ErrorCode != 0) return ResultCode.ServerError;
                if (response.Result != ResultCode.Success) return response.Result;
            }
            NetworkManager.Instance.SetGateSession(gate);
            gate = null;
            return ResultCode.Success;
        }
        catch (Exception exception)
        {
            Log.Error(exception);
            return ResultCode.ServerError;
        }
        finally
        {
            if (authentication != null && !authentication.IsDisposed) authentication.Dispose();
            if (gate != null && !gate.IsDisposed) gate.Dispose();
            requesting = false;
        }
    }

    /// <summary>注册使用短连接；和登录共用请求保护。</summary>
    public async FTask<ResultCode> RegisterAsync(string account, string password)
    {
        account = account?.Trim();
        if (!ValidCredentials(account, password)) return ResultCode.InvalidCredentials;
        if (requesting) return ResultCode.Busy;
        requesting = true;
        Session session = null;
        try
        {
            session = await NetworkManager.Instance.ConnectAsync(LoginServerConfig.GetAddress(account));
            using var response = await session.C2A_RegisterRequest(account, password);
            return response.ErrorCode != 0 ? ResultCode.ServerError : response.Result;
        }
        catch (Exception exception)
        {
            Log.Error(exception);
            return ResultCode.ServerError;
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
