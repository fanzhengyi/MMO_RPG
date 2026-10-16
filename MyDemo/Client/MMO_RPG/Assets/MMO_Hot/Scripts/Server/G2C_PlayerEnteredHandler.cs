using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>创建或更新远端角色。</summary>
public sealed class G2C_PlayerEnteredHandler : Message<G2C_PlayerEntered>
{
    /// <summary>复制公开快照，场景加载期间由管理器暂存。</summary>
    protected override async FTask Run(Session session, G2C_PlayerEntered message)
    {
        if (NetworkManager.Instance.GateSession != session || message.Player?.Movement == null
            || !PlayerSelfModel.Instance.AcceptsWorld(message.WorldSessionId)) return;
        var player = message.Player;
        RemotePlayerManager.PlayerEntered(
            RemotePlayerManager.Snapshot.From(player));
        await FTask.CompletedTask;
    }
}

/// <summary>只更新其他玩家的移动。</summary>
public sealed class G2C_PlayerMoveHandler : Message<G2C_PlayerMove>
{
    /// <summary>复制网络数据后交给远端管理器，不处理本人回正。</summary>
    protected override async FTask Run(Session session, G2C_PlayerMove message)
    {
        if (NetworkManager.Instance.GateSession != session || message.Movement == null
            || !PlayerSelfModel.Instance.AcceptsWorld(message.WorldSessionId)) return;
        RemotePlayerManager.PlayerMoved(
            RemotePlayerManager.Snapshot.From(message.RoleId, message.SpawnId, message.SpawnRevision, message.Version, message.ServerTimeMs, message.Movement));
        await FTask.CompletedTask;
    }
}

/// <summary>本人位置回正与远端移动分开处理。</summary>
public sealed class G2C_PlayerPositionCorrectionHandler : Message<G2C_PlayerPositionCorrection>
{
    /// <summary>模型核对确认序号，再通知本地控制器。</summary>
    protected override async FTask Run(Session session, G2C_PlayerPositionCorrection message)
    {
        if (NetworkManager.Instance.GateSession != session) return;
        PlayerSelfModel.Instance.CorrectPosition(message.WorldSessionId, message.MovementEpoch, message.AckSequence, message.Movement);
        await FTask.CompletedTask;
    }
}

/// <summary>删除离线角色及尚未消费的快照。</summary>
public sealed class G2C_PlayerLeftHandler : Message<G2C_PlayerLeft>
{
    /// <summary>只处理当前 Gate 的离开通知。</summary>
    protected override async FTask Run(Session session, G2C_PlayerLeft message)
    {
        if (NetworkManager.Instance.GateSession != session || !PlayerSelfModel.Instance.AcceptsWorld(message.WorldSessionId)) return;
        RemotePlayerManager.PlayerLeft(message.RoleId, message.SpawnId, message.SpawnRevision);
        await FTask.CompletedTask;
    }
}
