using Fantasy;
using Fantasy.Async;
using Fantasy.MongdbModel;
using Fantasy.Network.Interface;

/// <summary>合法移动广播给其他玩家，非法移动回正发送者；这里不写数据库。</summary>
public sealed class G2Game_PlayerMoveHandler : Address<Scene, G2Game_PlayerMove>
{
    /// <summary>核对连接和序号，执行规则后广播或回正。</summary>
    protected override async FTask Run(Scene scene, G2Game_PlayerMove request)
    {
        if (string.IsNullOrEmpty(request.UserName)) return;
        var online = scene.GetComponent<OnlineComponent>();
        if (!online.PlayersByName.TryGetValue(request.UserName, out var player)
            || !player.IsConnected
            || player.GateSessionRuntimeId != request.GateSessionRuntimeId
            || player.GateSceneAddress != request.GateSceneAddress) return;
        // 旧包直接丢弃，不能让旧包触发回正覆盖刚刚接受的新位置。
        if (player.HasLastMoveSequence
            && unchecked((int)(request.Sequence - player.LastMoveSequence)) <= 0) return;

        if (!PlayerMovementRules.TryApply(player, request, Environment.TickCount64))
        {
            player.LastMoveSequence = request.Sequence;
            player.HasLastMoveSequence = true;
            player.MovementVersion = unchecked(player.MovementVersion + 1);
            SendSnapshot(scene, player, player);
            return;
        }

        foreach (var receiver in online.PlayersByName.Values)
            if (receiver.Id != player.Id && receiver.IsConnected)
                SendSnapshot(scene, receiver, player);
        await FTask.CompletedTask;
    }

    /// <summary>下行版本由 Game 生成；发给角色本人时表示位置回正。</summary>
    private static void SendSnapshot(Scene scene, OnlineInfo receiver, OnlineInfo player)
    {
        scene.Send(receiver.GateSceneAddress, new Game2G_PlayerMove
        {
            GateSessionRuntimeId = receiver.GateSessionRuntimeId,
            RoleId = player.Id,
            X = player.X,
            Y = player.Y,
            Z = player.Z,
            RotationY = player.RotationY,
            Sequence = player.MovementVersion,
            State = player.MovementState,
            Speed = player.MovementSpeed,
        });
    }
}
