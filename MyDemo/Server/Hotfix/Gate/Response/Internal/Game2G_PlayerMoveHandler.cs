using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>按 Game 提供的 Session ID，把移动快照转发给对应客户端。</summary>
public sealed class Game2G_PlayerMoveHandler : Address<Scene, Game2G_PlayerMove>
{
    protected override async FTask Run(Scene scene, Game2G_PlayerMove message)
    {
        // Session 已销毁或消息带有无效 ID 时，这条定向快照已经过期。
        if (message.GateSessionRuntimeId == 0
            || !scene.TryGetEntity<Session>(message.GateSessionRuntimeId, out var session)
            || session.IsDisposed)
        {
            return;
        }

        var sessionInfo = session.GetComponent<SessionDisponse>();
        // 防止消息到达时目标连接已退出登录。
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
