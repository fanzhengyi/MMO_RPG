using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>将进入游戏请求交给 Game，并转换返回的角色数据。</summary>
public sealed class C2G_EnterGameHandler : MessageRPC<C2G_EnterGameRequest, G2C_EnterGameResponse>
{
    /// <summary>核对连接，转发进入请求并映射角色数据。</summary>
    protected override async FTask Run(Session session, C2G_EnterGameRequest request,
        G2C_EnterGameResponse response, Action reply)
    {
        response.AccountErrorCode = (int)AccountErrorCode.NoLogin;
        if (!GateSessionHelper.TryGetPlayer(session, out var info))
            return;

        var sessionRuntimeId = session.RuntimeId;
        using var gameResponse = (Game2G_EnterGameResponse)await session.Scene.Call(
            info.GameSceneAddress, new G2Game_EnterGameRequest
            {
                UserName = info.UserName,
                GateSessionRuntimeId = session.RuntimeId,
                GateSceneAddress = info.GateSceneAddress,
            });

        // 等待 RPC 时可能顶号；旧连接不能继续完成进入游戏。
        if (session.RuntimeId != sessionRuntimeId || !GateSessionHelper.TryGetPlayer(session, out _))
            return;
        if (gameResponse == null)
        {
            response.AccountErrorCode = (int)AccountErrorCode.ServerError;
            return;
        }

        response.AccountErrorCode = gameResponse.AccountErrorCode;
        if (gameResponse.AccountErrorCode == (int)AccountErrorCode.HaveRole)
        {
            if (gameResponse.Data == null)
            {
                response.AccountErrorCode = (int)AccountErrorCode.ServerError;
                return;
            }
            response.Info = PlayerMessageMapper.ToPlayerInfo(gameResponse.Data);
        }
    }
}
