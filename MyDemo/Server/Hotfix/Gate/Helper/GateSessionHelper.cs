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
            && !string.IsNullOrEmpty(info.UserName)
            && info.GameSceneAddress != 0
            && session.Scene.GetComponent<GateLoginCache>().Sessions.TryGetValue(info.UserName, out var runtimeId)
            && runtimeId == session.RuntimeId;
    }

    /// <summary>内部推送按运行时 ID 定位客户端，并复用相同的身份检查。</summary>
    public static bool TryGetSession(Scene scene, long runtimeId, out Session session)
    {
        return scene.TryGetEntity(runtimeId, out session) && TryGetPlayer(session, out _);
    }

    /// <summary>清除当前连接映射，并且只发送一次离开通知。</summary>
    public static void Detach(PlayerSessionComponent? info)
    {
        if (info == null || string.IsNullOrEmpty(info.UserName))
            return;

        var scene = info.Scene;
        var userName = info.UserName;
        var sessionRuntimeId = info.SessionRuntimeId;
        var gameAddress = info.GameSceneAddress;
        var gateAddress = info.GateSceneAddress;
        // 先清理全部身份字段，重复释放和对象池复用都不会再次通知下线。
        info.UserName = "";
        info.SessionRuntimeId = info.GameSceneAddress = info.GateSceneAddress = 0;
        if (scene == null || scene.IsDisposed) return;
        var cache = scene.GetComponent<GateLoginCache>();
        if (cache != null && cache.Sessions.TryGetValue(userName, out var runtimeId)
            && runtimeId == sessionRuntimeId)
            cache.Sessions.Remove(userName);

        if (gameAddress == 0) return;
        scene.Send(gameAddress, new G2Game_PlayerLeft
        {
            UserName = userName,
            GateSessionRuntimeId = sessionRuntimeId,
            GateSceneAddress = gateAddress,
        });
    }
}
