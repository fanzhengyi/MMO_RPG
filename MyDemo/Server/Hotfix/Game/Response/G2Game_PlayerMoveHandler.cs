using Fantasy;
using Fantasy.Async;
using Fantasy.MongdbModel;
using Fantasy.Network.Interface;

/// <summary>合法移动广播给别人，非法移动单独回正本人。</summary>
public sealed class G2Game_PlayerMoveHandler : Address<Scene, G2Game_PlayerMove>
{
    /// <summary>匹配在线连接，丢弃旧包，再校验并发送确认快照。</summary>
    protected override async FTask Run(Scene scene, G2Game_PlayerMove request)
    {
        if (string.IsNullOrEmpty(request.UserName)) return;
        var online = scene.GetComponent<OnlineComponent>();
        if (!online.PlayersByName.TryGetValue(request.UserName, out var player)
            || !player.IsInWorld
            || !player.MatchesWorld(request.GateSessionRuntimeId, request.GateSceneAddress, request.WorldSessionId)) return;
        var decision = PlayerMovementRules.Process(player, request.Sequence, request.MovementEpoch,
            request.Movement, Environment.TickCount64);
        if (decision == PlayerMovementRules.Decision.Ignored) return;
        if (decision == PlayerMovementRules.Decision.Correction)
        {
            scene.Send(player.GateSceneAddress, new Game2G_PlayerPositionCorrection
            {
                GateSessionRuntimeId = player.GateSessionRuntimeId,
                WorldSessionId = player.WorldSessionId,
                AckSequence = player.LastCorrectionSequence,
                MovementEpoch = player.MovementEpoch,
                Movement = PlayerMessageMapper.ToMovement(player),
            });
            return;
        }

        player.NeedsReplication = true;
        await FTask.CompletedTask;
    }
}
