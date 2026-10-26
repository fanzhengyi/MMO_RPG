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
        if (string.IsNullOrEmpty(request.AccountUser)) return;
        // online：当前 Game 的角色索引和定时任务组件。
        var online = scene.GetComponent<OnlineComponent>();
        // player：当前处理的服务端角色数据，包含持久资料和本次在线状态。
        if (!online.PlayersByAccountUser.TryGetValue(request.AccountUser, out var player)
            || !player.IsInWorld
            || !player.MatchesWorld(request.GateSessionRuntimeId, request.GateSceneAddress, request.WorldSessionId)) return;
        // decision：移动校验的处理结果：忽略、接受或回正。
        var decision = PlayerMovementRules.Process(player, request.Sequence, request.MovementEpoch,
            request.Movement, Environment.TickCount64);
        if (decision == PlayerMovementRules.Decision.Ignored) return;
        if (decision == PlayerMovementRules.Decision.Correction)
        {
            if (player.Actions.Kind != PlayerActionId.None)
            {
                // 回正立即中断其他客户端的旧动作，冷却仍保留。
                var now = Environment.TickCount64;
                player.Actions.Cancel(now);
                WorldReplication.PublishAction(scene, player, now);
            }
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
