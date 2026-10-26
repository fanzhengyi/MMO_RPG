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
            
            .TryValidateToken(request.Token, session.Scene.SceneConfigId, out var accountUser)) return;

        // existing：当前连接上已绑定的玩家会话组件。
        var existing = session.GetComponent<PlayerSessionComponent>();
        //客户端可能用同一个Session点了两次登录
        if (existing != null)
        {
            if (existing.AccountUser == accountUser) response.Result = ResultCode.Success;
            return;
        }
        // gameScenes：配置中的 Game 场景列表，按账号哈希选择角色所在 Game。
        var gameScenes = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Game);
        if (gameScenes.Count == 0) { response.Result = ResultCode.ServerError; return; }
        
        // cache：当前 Gate 的账号到在线连接映射。
        var cache = session.Scene.GetComponent<GateLoginCache>();
        // oldId：同一账号先前连接的运行时 ID。
        if (cache.Sessions.TryGetValue(accountUser, out var oldId)
            // oldSession：同一账号先前使用的客户端连接，新登录将替换它。
            && session.Scene.TryGetEntity<Session>(oldId, out var oldSession))
        {
            GateSessionHelper.Detach(oldSession.GetComponent<PlayerSessionComponent>());
            oldSession.Send(new G2C_RepeatLogin());
            oldSession.SetTimeout(3000);
        }
        // info：本次登录新绑定的玩家会话组件，保存已验证账号和路由。
        var info = session.AddComponent<PlayerSessionComponent>();
        info.ResetWorldState();
        info.AccountUser = accountUser;
        // 当前是单个公共世界，同场景玩家不能按账号分到不同 Game。
        info.GameSceneAddress = gameScenes[0].Address;
        info.GateSceneAddress = SceneConfigData.Instance.Get(session.Scene.SceneConfigId).Address;
        info.SessionRuntimeId = session.RuntimeId;
        cache.Sessions[accountUser] = session.RuntimeId;
        response.Result = ResultCode.Success;
        await FTask.CompletedTask;
    }
}
