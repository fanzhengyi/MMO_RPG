using Fantasy;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Platform.Net;

/// <summary>验证登录凭证，替换旧连接，并缓存当前玩家的路由。</summary>
public sealed class C2G_LoginRequestHandler : MessageRPC<C2G_LoginRequest, G2C_LoginResponse>
{
    /// <summary>验证账号凭证和 Gate 路由，登记连接并替换旧登录。</summary>
    protected override async FTask Run(Session session, C2G_LoginRequest request,
        G2C_LoginResponse response, Action reply)
    {
        response.LoginError = AccountErrorCode.NoLogin;
        var gates = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Gate);
        if (string.IsNullOrWhiteSpace(request.UserName) || gates.Count == 0
            || !session.Scene.GetComponent<GateJwtComponent>().ValidateJwtTokens(request.Token, request.UserName))
            return;

        var gateIndex = (int)(HashCodeHelper.MurmurHash3(request.UserName) % gates.Count);
        if (gates[gateIndex].Id != session.Scene.SceneConfigId)
            return;

        var existingInfo = session.GetComponent<PlayerSessionComponent>();
        if (existingInfo != null)
        {
            // 同一连接重复登录是幂等操作；切换账号必须建立新连接。
            if (existingInfo.UserName == request.UserName)
                response.LoginError = AccountErrorCode.LoginSuccess;
            return;
        }

        var gameScenes = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Game);
        if (gameScenes.Count == 0)
        {
            response.LoginError = AccountErrorCode.ServerError;
            return;
        }

        var cache = session.Scene.GetComponent<GateLoginCache>();
        if (cache.Sessions.TryGetValue(request.UserName, out var oldRuntimeId)
            && session.Scene.TryGetEntity<Session>(oldRuntimeId, out var oldSession))
        {
            GateSessionHelper.Detach(oldSession.GetComponent<PlayerSessionComponent>());
            oldSession.Send(new G_2C_RepeaLogin());
            oldSession.SetTimeout(3000);
        }

        var info = session.AddComponent<PlayerSessionComponent>();
        info.UserName = request.UserName;
        // 当前只有一个公共世界；同场景玩家必须进入同一个 Game，不能按账号拆开。
        info.GameSceneAddress = gameScenes[0].Address;
        info.GateSceneAddress = SceneConfigData.Instance.Get(session.Scene.SceneConfigId).Address;
        info.SessionRuntimeId = session.RuntimeId;
        cache.Sessions[info.UserName] = session.RuntimeId;
        response.LoginError = AccountErrorCode.LoginSuccess;
        await FTask.CompletedTask;
    }
}
