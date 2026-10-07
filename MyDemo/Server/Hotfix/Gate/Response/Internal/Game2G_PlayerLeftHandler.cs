using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>按 Game 提供的 Session ID，把离开通知转给对应客户端。</summary>
public sealed class Game2G_PlayerLeftHandler : Address<Scene, Game2G_PlayerLeft>
{
    protected override FTask Run(Scene scene, Game2G_PlayerLeft message)
    {
        // Session 已销毁或消息带有无效 ID 时，无需再尝试通知客户端。
        if (message.GateSessionRuntimeId == 0
            || !scene.TryGetEntity<Session>(message.GateSessionRuntimeId, out var session)
            || session.IsDisposed)
        {
            return FTask.CompletedTask;
        }

        var sessionInfo = session.GetComponent<SessionDisponse>();
        // 只给仍处于登录状态的连接发送离开通知。
        if (sessionInfo == null || string.IsNullOrEmpty(sessionInfo.userName))
        {
            return FTask.CompletedTask;
        }

        session.Send(new G2C_PlayerLeft { RoleId = message.RoleId });
        return FTask.CompletedTask;
    }
}
