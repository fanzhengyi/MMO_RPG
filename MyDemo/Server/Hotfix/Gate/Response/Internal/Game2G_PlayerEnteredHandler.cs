using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>把 Game 定向给某个 Gate Session 的进入通知转成客户端消息。</summary>
public sealed class Game2G_PlayerEnteredHandler : Address<Scene, Game2G_PlayerEntered>
{
    protected override FTask Run(Scene scene, Game2G_PlayerEntered message)
    {
        // Session 已销毁或消息带有无效 ID 时，这条定向通知已经没有接收者。
        if (message.GateSessionRuntimeId == 0
            || !scene.TryGetEntity<Session>(message.GateSessionRuntimeId, out var session)
            || session.IsDisposed)
        {
            return FTask.CompletedTask;
        }

        var sessionInfo = session.GetComponent<SessionDisponse>();
        // Session 可能仍存在但已经退出登录；这种情况下不再发送游戏内通知。
        if (sessionInfo == null || string.IsNullOrEmpty(sessionInfo.userName))
        {
            return FTask.CompletedTask;
        }

        session.Send(new G2C_PlayerEntered
        {
            RoleId = message.RoleId,
            UserName = message.UserName,
            NickName = message.NickName,
            X = message.X,
            Y = message.Y,
            Z = message.Z,
            RotationY = message.RotationY,
            State = message.State,
            Speed = message.Speed,
            Sequence = message.Sequence,
        });

        return FTask.CompletedTask;
    }
}
