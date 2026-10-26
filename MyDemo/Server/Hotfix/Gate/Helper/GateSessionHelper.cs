using Fantasy;
using Fantasy.Network;

/// <summary>统一校验当前连接，并处理断线通知。</summary>
public static class GateSessionHelper
{
    /// <summary>只接受账号索引中仍然有效的 Session。</summary>
    public static bool TryGetPlayer(Session session, out PlayerSessionComponent info)
    {
        info = null;
        if (session == null || session.IsDisposed) return false;
        
        info = session.GetComponent<PlayerSessionComponent>();
        
        return info != null && !info.IsDisposed && info.SessionRuntimeId == session.RuntimeId
            && !string.IsNullOrEmpty(info.AccountUser)
            && info.GameSceneAddress != 0
            // runtimeId：开始操作时记录的对象运行时 ID，用于识别销毁或对象池复用。
            && session.Scene.GetComponent<GateLoginCache>().Sessions.TryGetValue(info.AccountUser, out var runtimeId)
            && runtimeId == session.RuntimeId;
    }

    /// <summary>内部推送按运行时 ID 定位客户端，并复用相同的身份检查。</summary>
    public static bool TryGetSession(Scene scene, long runtimeId, out Session session)
    {
        return scene.TryGetEntity(runtimeId, out session) && TryGetPlayer(session, out _);
    }

    public static bool TryGetWorldSession(Scene scene, long runtimeId, long worldSessionId, out Session session)
        => TryGetSession(scene, runtimeId, out session) && worldSessionId != 0
            && session.GetComponent<PlayerSessionComponent>().WorldSessionId == worldSessionId;

    /// <summary>清除当前连接映射，并且只发送一次离开通知。</summary>
    public static void Detach(PlayerSessionComponent? info)
    {
        if (info == null || string.IsNullOrEmpty(info.AccountUser))
            return;

        // scene：当前操作所属的 Fantasy 网络场景，负责消息、实体和定时器。
        var scene = info.Scene;
        // accountUser：当前请求已验证的登录账号，不是角色昵称。
        var accountUser = info.AccountUser;
        // sessionRuntimeId：开始操作时的连接运行时 ID，用于判断等待后是否仍是原连接。
        var sessionRuntimeId = info.SessionRuntimeId;
        // gameAddress：玩家所属 Game 场景的内网地址，离线时通知它清理。
        var gameAddress = info.GameSceneAddress;
        // gateAddress：该玩家当前 Gate 场景的内网地址。
        var gateAddress = info.GateSceneAddress;
        // worldSessionId：本次进入世界的会话 ID，用于排除旧世界的迟到操作。
        var worldSessionId = info.WorldSessionId;
        // 先清理全部身份字段，重复释放和对象池复用都不会再次通知下线。
        info.AccountUser = "";
        info.SessionRuntimeId = info.GameSceneAddress = info.GateSceneAddress = 0;
        info.ResetWorldState();
        if (scene == null || scene.IsDisposed) return;
        // cache：当前 Gate 的账号到在线连接映射。
        var cache = scene.GetComponent<GateLoginCache>();
        // runtimeId：开始操作时记录的对象运行时 ID，用于识别销毁或对象池复用。
        if (cache != null && cache.Sessions.TryGetValue(accountUser, out var runtimeId)
            && runtimeId == sessionRuntimeId)
            cache.Sessions.Remove(accountUser);

        if (gameAddress == 0) return;
        scene.Send(gameAddress, new G2Game_PlayerLeft
        {
            AccountUser = accountUser,
            GateSessionRuntimeId = sessionRuntimeId,
            GateSceneAddress = gateAddress,
            WorldSessionId = worldSessionId,
        });
    }
}
