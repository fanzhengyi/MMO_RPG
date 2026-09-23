using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>
/// Game 广播某玩家移动过来，按 SessionRuntimeId 找到 Session 转发给客户端。
/// </summary>
public class Game2G_PlayerMoveHandler : Address<Scene, Game2G_PlayerMove>
{
    protected override async FTask Run(Scene scene, Game2G_PlayerMove message)
    {
        if (!scene.TryGetEntity<Session>(message.GateSessionRuntimeId, out var session))
        {
            // 目标 Session 已掉线，丢弃
            return;
        }

        session.Send(new G2C_PlayerMove
        {
            RoleId = message.RoleId,
            X = message.X,
            Y = message.Y,
            Z = message.Z,
            RotationY = message.RotationY,
        });
        await FTask.CompletedTask;
    }
}
