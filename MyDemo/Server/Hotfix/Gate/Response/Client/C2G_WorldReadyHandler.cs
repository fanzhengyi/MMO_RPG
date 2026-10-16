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
        if (!GateSessionHelper.TryGetPlayer(session, out var info)) return;
        response.Result = ResultCode.InvalidWorldSession;
        var worldSessionId = request.WorldSessionId;
        if (worldSessionId == 0 || info.WorldSessionId != worldSessionId) return;
        var runtimeId = session.RuntimeId;
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
