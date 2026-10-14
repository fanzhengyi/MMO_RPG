using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>按 Game 提供的 Session ID，把移动快照转发给对应客户端。</summary>
public sealed class Game2G_PlayerMoveHandler : Address<Scene, Game2G_PlayerMove>
{
    /// <summary>将服务端移动版本转发给有效连接。</summary>
    protected override async FTask Run(Scene scene, Game2G_PlayerMove message)
    {
        if (!GateSessionHelper.TryGetSession(scene, message.GateSessionRuntimeId, out var session))
            return;

        session.Send(new G2C_PlayerMove
        {
            RoleId = message.RoleId,
            Version = message.Version,
            Movement = message.Movement,
        });

        await FTask.CompletedTask;
    }
}
