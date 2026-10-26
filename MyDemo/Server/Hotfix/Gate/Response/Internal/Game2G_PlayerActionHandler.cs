using Fantasy;
using Fantasy.Async;
using Fantasy.Network.Interface;

/// <summary>按 Game 给出的接收路由转发动作，旧世界或已被替换的连接不能接收。</summary>
public sealed class Game2G_PlayerActionHandler : Address<Scene, Game2G_PlayerAction>
{
    protected override async FTask Run(Scene scene, Game2G_PlayerAction message)
    {
        if (!GateSessionHelper.TryGetWorldSession(scene, message.GateSessionRuntimeId,
            message.WorldSessionId, out var session)) return;
        session.Send(new G2C_PlayerAction
        {
            RoleId = message.RoleId,
            Action = message.Action,
            WorldSessionId = message.WorldSessionId,
            SpawnId = message.SpawnId,
            SpawnRevision = message.SpawnRevision,
            ServerTimeMs = message.ServerTimeMs,
        });
        await FTask.CompletedTask;
    }
}
