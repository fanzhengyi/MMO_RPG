using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>把 Game 定向给某个 Gate Session 的进入通知转成客户端消息。</summary>
public sealed class Game2G_PlayerEnteredHandler : Address<Scene, Game2G_PlayerEntered>
{
    protected override async FTask Run(Scene scene, Game2G_PlayerEntered message)
    {
        if (!GateSessionHelper.TryGetSession(scene, message.GateSessionRuntimeId, out var session))
            return;

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

        await FTask.CompletedTask;
    }
}
