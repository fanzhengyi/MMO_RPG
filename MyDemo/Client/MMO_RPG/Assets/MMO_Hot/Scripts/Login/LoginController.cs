using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;

/// <summary>组织鉴权和 Gate 登录；请求互斥，短连接由 finally 释放。</summary>
public sealed class LoginController : Singleton<LoginController>
{
    /// <summary>登录和注册共用的请求占用标记；即使不经 UI 调用，也不允许并发操作，finally 会恢复为 false。</summary>
    private bool requesting;

    /// <summary>先取得 Token，再建立经过身份验证的 Gate 长连接。</summary>
    public async FTask<ResultCode> LoginAsync(string account, string password)
    {
        account = account?.Trim();
        //长度限制和判空
        if (!ValidCredentials(account, password)) return ResultCode.InvalidCredentials;
        //请求中
        if (requesting) return ResultCode.Busy;
        requesting = true;
        // authentication：临时连接的鉴权服，用完 Token 后关闭，不作为游戏长连接。
        Session authentication = null;
        // gate：本次登录或进入世界使用的 Gate 连接。
        Session gate = null;
        try
        {
            NetworkManager.Instance.DisconnectGate();
            authentication = await NetworkManager.Instance.ConnectAsync(LoginServerConfig.GetAddress(account));
            // token：鉴权服务签发的登录凭证，客户端原样转交给 Gate。
            string token;
            // gateAddress：鉴权服务返回的网关连接地址，格式为主机:端口。
            string gateAddress;
            // response：本次请求返回的响应对象，using 结束后交回协议对象池。
            using (var response = await authentication.C2A_LoginRequest(account, password))
            {
                if (response.ErrorCode != 0) return ResultCode.ServerError;
                if (response.Result != ResultCode.Success) return response.Result;
                //登录成功
                if (string.IsNullOrEmpty(response.Token) || string.IsNullOrWhiteSpace(response.GateAddress))
                    return ResultCode.ServerError;
                token = response.Token;
                gateAddress = response.GateAddress;
            }
            // Token 和 Gate 地址已复制出来，鉴权短连接使命完成；游戏运行期间只保留 Gate 长连接。
            authentication.Dispose();
            authentication = null;
            //连接Gate
            gate = await NetworkManager.Instance.ConnectAsync(gateAddress);
            // response：本次请求返回的响应对象，using 结束后交回协议对象池。
            using (var response = await gate.C2G_LoginRequest(token))
            {
                if (response.ErrorCode != 0) return ResultCode.ServerError;
                if (response.Result != ResultCode.Success) return response.Result;
            }
            NetworkManager.Instance.SetGateSession(gate);
            // 所有权交给 NetworkManager；将局部变量清空，避免 finally 把刚登录成功的长连接释放。
            gate = null;
            return ResultCode.Success;
        }
        // exception：本次操作抛出的异常，用于记录具体失败原因。
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
        // session：本次操作使用的客户端网络连接。
        Session session = null;
        try
        {
            session = await NetworkManager.Instance.ConnectAsync(LoginServerConfig.GetAddress(account));
            // response：本次请求返回的响应对象，using 结束后交回协议对象池。
            using var response = await session.C2A_RegisterRequest(account, password);
            return response.ErrorCode != 0 ? ResultCode.ServerError : response.Result;
        }
        // exception：本次操作抛出的异常，用于记录具体失败原因。
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

    //客户端限制输入长度，服务端仍会独立验证
    private static bool ValidCredentials(string account, string password)
        => !string.IsNullOrWhiteSpace(account) && account.Length <= 64
            && !string.IsNullOrEmpty(password) && password.Length <= 128;
}
