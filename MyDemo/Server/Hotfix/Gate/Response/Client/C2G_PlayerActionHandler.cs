using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>动作请求只附加已验证身份并转发，Gate 不决定技能冷却或连击时序。</summary>
public sealed class C2G_PlayerActionHandler : MessageRPC<C2G_PlayerActionRequest, G2C_PlayerActionResponse>
{
    protected override async FTask Run(Session session, C2G_PlayerActionRequest request,
        G2C_PlayerActionResponse response, Action reply)
    {
        response.Sequence = request.Sequence;
        response.Result = ResultCode.NotAuthenticated;
        if (!GateSessionHelper.TryGetPlayer(session, out var info)) return;
        response.Result = ResultCode.InvalidWorldSession;
        // worldSessionId：等待 Game 返回前固定本次世界身份，防止旧响应写回新世界。
        var worldSessionId = request.WorldSessionId;
        if (!info.IsWorldReady || worldSessionId == 0 || info.WorldSessionId != worldSessionId) return;
        response.Result = ResultCode.RateLimited;
        if (!info.TryAcceptAction(Environment.TickCount64)) return;

        // runtimeId：本次来源连接的运行时 ID，等待期间连接可能被替换或回收。
        var runtimeId = session.RuntimeId;
        using var result = (Game2G_PlayerActionResponse)await session.Scene.Call(info.GameSceneAddress,
            new G2Game_PlayerActionRequest
            {
                AccountUser = info.AccountUser,
                GateSessionRuntimeId = runtimeId,
                GateSceneAddress = info.GateSceneAddress,
                WorldSessionId = worldSessionId,
                Sequence = request.Sequence,
                Kind = request.Kind,
                RotationY = request.RotationY,
                MovementEpoch = request.MovementEpoch,
                CancelSequence = request.CancelSequence,
            });
        response.Result = ResultCode.InvalidWorldSession;
        if (session.RuntimeId != runtimeId || !GateSessionHelper.TryGetPlayer(session, out info)
            || info.WorldSessionId != worldSessionId) return;
        response.Result = result == null || result.ErrorCode != 0 ? ResultCode.ServerError : result.Result;
        if (result == null || result.ErrorCode != 0) return;
        response.Action = result.Action;
        response.ServerTimeMs = result.ServerTimeMs;
    }
}
