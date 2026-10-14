using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>把 Game 定向给某个 Gate Session 的进入通知转成客户端消息。</summary>
public sealed class Game2G_PlayerEnteredHandler : Address<Scene, Game2G_PlayerEntered>
{
    /// <summary>核对接收连接并转发公开资料。</summary>
    protected override async FTask Run(Scene scene, Game2G_PlayerEntered message)
    {
        if (!GateSessionHelper.TryGetSession(scene, message.GateSessionRuntimeId, out var session))
            return;

        session.Send(new G2C_PlayerEntered
        {
            Player = message.Player,
        });

        await FTask.CompletedTask;
    }
}
