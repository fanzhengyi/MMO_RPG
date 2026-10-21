using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>按 Game 提供的 Session ID，把离开通知转给对应客户端。</summary>
public sealed class Game2G_PlayerLeftHandler : Address<Scene, Game2G_PlayerLeft>
{
    /// <summary>核对接收连接，再通知客户端删除外观。</summary>
    protected override async FTask Run(Scene scene, Game2G_PlayerLeft message)
    {
        // session：本次操作使用的客户端网络连接。
        if (!GateSessionHelper.TryGetWorldSession(scene, message.GateSessionRuntimeId, message.WorldSessionId, out var session))
            return;

        session.Send(new G2C_PlayerLeft { RoleId = message.RoleId,
            WorldSessionId = message.WorldSessionId, SpawnId = message.SpawnId, SpawnRevision = message.SpawnRevision });
        await FTask.CompletedTask;
    }
}
