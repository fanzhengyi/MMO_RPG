using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>把 Game 的玩家移动广播转给目标 Gate 上的客户端 Session。</summary>
public sealed class Game2G_PlayerMoveHandler : Address<Scene, Game2G_PlayerMove>
{
    protected override async FTask Run(Scene scene, Game2G_PlayerMove message)
    {
        if (message.GateSessionRuntimeId == 0
            || !scene.TryGetEntity<Session>(message.GateSessionRuntimeId, out var session)
            || session.IsDisposed)
        {
            return;
        }

        var sessionInfo = session.GetComponent<SessionDisponse>();
        if (sessionInfo == null || string.IsNullOrEmpty(sessionInfo.userName))
        {
            return;
        }

        session.Send(new G2C_PlayerMove
        {
            RoleId = message.RoleId,
            X = message.X,
            Y = message.Y,
            Z = message.Z,
            RotationY = message.RotationY,
            Sequence = message.Sequence,
            State = message.State,
            Speed = message.Speed,
        });

        await FTask.CompletedTask;
    }
}
