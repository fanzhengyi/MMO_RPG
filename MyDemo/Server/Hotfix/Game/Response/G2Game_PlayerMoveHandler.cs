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
            || !player.IsConnected
            || player.GateSessionRuntimeId != request.GateSessionRuntimeId
            || player.GateSceneAddress != request.GateSceneAddress) return;
        if (player.HasLastMoveSequence
            && unchecked((int)(request.Sequence - player.LastMoveSequence)) <= 0) return;

        if (!PlayerMovementRules.TryApply(player, request.Sequence, request.Movement, Environment.TickCount64))
        {
            // 拒绝的序号也已处理；重发旧包不能再次触发回正。
            player.LastMoveSequence = request.Sequence;
            player.HasLastMoveSequence = true;
            scene.Send(player.GateSceneAddress, new Game2G_PlayerPositionCorrection
            {
                GateSessionRuntimeId = player.GateSessionRuntimeId,
                AckSequence = request.Sequence,
                Movement = PlayerMessageMapper.ToMovement(player),
            });
            return;
        }

        var movement = PlayerMessageMapper.ToMovement(player);
        foreach (var receiver in online.PlayersByName.Values)
        {
            if (receiver.Id == player.Id || !receiver.IsConnected) continue;
            scene.Send(receiver.GateSceneAddress, new Game2G_PlayerMove
            {
                GateSessionRuntimeId = receiver.GateSessionRuntimeId,
                RoleId = player.Id,
                Version = player.MovementVersion,
                Movement = movement,
            });
        }
        await FTask.CompletedTask;
    }
}
