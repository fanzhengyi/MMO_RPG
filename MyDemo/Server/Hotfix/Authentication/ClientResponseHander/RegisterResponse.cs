using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>注册请求由 Authentication 完成，响应后关闭短连接。</summary>
public sealed class RegisterResponse : MessageRPC<C2A_RegisterRequest, A2C_RegisterResponse>
{
    /// <summary>在账号锁内完成注册，响应后安排短连接关闭。</summary>
    protected override async FTask Run(Session session, C2A_RegisterRequest request, A2C_RegisterResponse response, Action reply)
    {
        response.Result = ResultCode.ServerError;
        if (!session.CheckInterval(2000)) { response.Result = ResultCode.RateLimited; return; }
        // sessionRuntimeId：开始操作时的连接运行时 ID，用于判断等待后是否仍是原连接。
        var sessionRuntimeId = session.RuntimeId;
        response.Result = await session.Scene.GetComponent<AuthenticationComponent>().Register(request.AccountUser, request.Password);
        if (session.IsDisposed || session.RuntimeId != sessionRuntimeId) return;
        session.SetTimeout(3000);
    }
}
