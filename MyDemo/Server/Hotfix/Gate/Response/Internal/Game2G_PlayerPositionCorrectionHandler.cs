using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>只向仍然有效的本人连接转发位置回正。</summary>
public sealed class Game2G_PlayerPositionCorrectionHandler : Address<Scene, Game2G_PlayerPositionCorrection>
{
    /// <summary>按连接运行时 ID 转发，旧连接不能影响新登录。</summary>
    protected override async FTask Run(Scene scene, Game2G_PlayerPositionCorrection message)
    {
        if (!GateSessionHelper.TryGetWorldSession(scene, message.GateSessionRuntimeId, message.WorldSessionId, out var session)) return;
        session.Send(new G2C_PlayerPositionCorrection
        {
            AckSequence = message.AckSequence,
            Movement = message.Movement,
            WorldSessionId = message.WorldSessionId,
            MovementEpoch = message.MovementEpoch,
        });
        await FTask.CompletedTask;
    }
}
