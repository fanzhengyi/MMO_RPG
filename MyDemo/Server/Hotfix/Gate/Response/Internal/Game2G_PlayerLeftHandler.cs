using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>按 Game 提供的 Session ID，把离开通知转给对应客户端。</summary>
public sealed class Game2G_PlayerLeftHandler : Address<Scene, Game2G_PlayerLeft>
{
    protected override async FTask Run(Scene scene, Game2G_PlayerLeft message)
    {
        if (!GateSessionHelper.TryGetSession(scene, message.GateSessionRuntimeId, out var session))
            return;

        session.Send(new G2C_PlayerLeft { RoleId = message.RoleId });
        await FTask.CompletedTask;
    }
}
