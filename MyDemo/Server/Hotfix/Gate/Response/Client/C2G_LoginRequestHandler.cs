using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Platform.Net;

/// <summary>只信任已验签的身份和 Gate 路由，替换旧登录。</summary>
public sealed class C2G_LoginRequestHandler : MessageRPC<C2G_LoginRequest, G2C_LoginResponse>
{
    /// <summary>登记当前连接；同一连接重复登录同一账号是幂等操作。</summary>
    protected override async FTask Run(Session session, C2G_LoginRequest request,
        G2C_LoginResponse response, Action reply)
    {
        response.Result = ResultCode.UnsupportedProtocol;
        if (request.ProtocolVersion != (uint)ProtocolRevision.Current) return;
        response.Result = ResultCode.InvalidToken;
        if (!session.Scene.GetComponent<GateJwtComponent>()
            .TryValidateToken(request.Token, session.Scene.SceneConfigId, out var userName)) return;

        var existing = session.GetComponent<PlayerSessionComponent>();
        if (existing != null)
        {
            if (existing.UserName == userName) response.Result = ResultCode.Success;
            return;
        }
        var gameScenes = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Game);
        if (gameScenes.Count == 0) { response.Result = ResultCode.ServerError; return; }

        var cache = session.Scene.GetComponent<GateLoginCache>();
        if (cache.Sessions.TryGetValue(userName, out var oldId)
            && session.Scene.TryGetEntity<Session>(oldId, out var oldSession))
        {
            GateSessionHelper.Detach(oldSession.GetComponent<PlayerSessionComponent>());
            oldSession.Send(new G2C_RepeatLogin());
            oldSession.SetTimeout(3000);
        }
        var info = session.AddComponent<PlayerSessionComponent>();
        info.ResetWorldState();
        info.UserName = userName;
        // 当前是单个公共世界，同场景玩家不能按账号分到不同 Game。
        info.GameSceneAddress = gameScenes[0].Address;
        info.GateSceneAddress = SceneConfigData.Instance.Get(session.Scene.SceneConfigId).Address;
        info.SessionRuntimeId = session.RuntimeId;
        cache.Sessions[userName] = session.RuntimeId;
        response.Result = ResultCode.Success;
        await FTask.CompletedTask;
    }
}
