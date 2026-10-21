using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

public sealed class C2G_WorldReadyHandler : MessageRPC<C2G_WorldReadyRequest, G2C_WorldReadyResponse>
{
    protected override async FTask Run(Session session, C2G_WorldReadyRequest request,
        G2C_WorldReadyResponse response, Action reply)
    {
        response.Result = ResultCode.NotAuthenticated;
        // info：网关绑定在该连接上的玩家身份、世界会话和路由信息。
        if (!GateSessionHelper.TryGetPlayer(session, out var info)) return;
        response.Result = ResultCode.InvalidWorldSession;
        // worldSessionId：本次进入世界的会话 ID，用于排除旧世界的迟到操作。
        var worldSessionId = request.WorldSessionId;
        if (worldSessionId == 0 || info.WorldSessionId != worldSessionId) return;
        // runtimeId：开始操作时记录的对象运行时 ID，用于识别销毁或对象池复用。
        var runtimeId = session.RuntimeId;
        // result：Game 返回的世界就绪响应，含其他玩家的初始列表。
        using var result = (Game2G_WorldReadyResponse)await session.Scene.Call(info.GameSceneAddress,
            new G2Game_WorldReadyRequest
            {
                UserName = info.UserName, GateSessionRuntimeId = runtimeId,
                GateSceneAddress = info.GateSceneAddress, WorldSessionId = worldSessionId,
            });
        if (session.RuntimeId != runtimeId || !GateSessionHelper.TryGetPlayer(session, out info)
            || info.WorldSessionId != worldSessionId) return;
        response.Result = result == null || result.ErrorCode != 0 ? ResultCode.ServerError : result.Result;
        if (response.Result != ResultCode.Success) return;
        info.IsWorldReady = true;
        response.Players.AddRange(result.Players);
    }
}
