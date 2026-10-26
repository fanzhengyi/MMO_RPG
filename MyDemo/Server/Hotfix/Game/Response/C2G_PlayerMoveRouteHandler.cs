using Fantasy;
using Fantasy.Async;
using Fantasy.MongdbModel;
using Fantasy.Network.Interface;

/// <summary>玩家移动自定义路由；Gate 不再为每条移动消息编写业务转发。</summary>
public sealed class C2G_PlayerMoveRouteHandler : Address<OnlineInfo, C2G_PlayerMove>
{
    protected override async FTask Run(OnlineInfo player, C2G_PlayerMove request)
    {
        var now = Environment.TickCount64;
        if (!player.IsInWorld || request.WorldSessionId != player.WorldSessionId
            || !player.TryAcceptMovement(now)) return;

        var decision = PlayerMovementRules.Process(player, request.Sequence, request.MovementEpoch,
            request.Movement, now);
        if (decision == PlayerMovementRules.Decision.Ignored) return;
        if (decision == PlayerMovementRules.Decision.Correction)
        {
            if (player.Actions.Kind != PlayerActionId.None)
            {
                player.Actions.Cancel(now);
                WorldReplication.PublishAction(player.Scene, player, now);
            }

            player.Scene.Send(player.GateSceneAddress, new Game2G_PlayerPositionCorrection
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
