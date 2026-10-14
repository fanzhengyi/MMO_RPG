using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>进入请求交给 Game，Gate 不访问角色数据库。</summary>
public sealed class C2G_EnterGameHandler : MessageRPC<C2G_EnterGameRequest, G2C_EnterGameResponse>
{
    /// <summary>核对连接，转发进入请求并返回角色 DTO。</summary>
    protected override async FTask Run(Session session, C2G_EnterGameRequest request,
        G2C_EnterGameResponse response, Action reply)
    {
        response.Result = ResultCode.NotAuthenticated;
        if (!GateSessionHelper.TryGetPlayer(session, out var info)) return;
        var runtimeId = session.RuntimeId;
        using var result = (Game2G_EnterGameResponse)await session.Scene.Call(
            info.GameSceneAddress, new G2Game_EnterGameRequest
            {
                UserName = info.UserName,
                GateSessionRuntimeId = runtimeId,
                GateSceneAddress = info.GateSceneAddress,
            });
        if (session.RuntimeId != runtimeId || !GateSessionHelper.TryGetPlayer(session, out _)) return;
        if (result == null || result.ErrorCode != 0
            || (result.Result == ResultCode.Success && result.Info?.Movement == null))
        {
            response.Result = ResultCode.ServerError;
            return;
        }
        response.Result = result.Result;
        response.Info = result.Info;
    }
}
