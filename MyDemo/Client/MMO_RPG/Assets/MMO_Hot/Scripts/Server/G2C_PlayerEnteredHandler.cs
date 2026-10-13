using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>把 Game 发来的进入、移动、离开消息交给客户端远端角色管理器。</summary>
public sealed class G2C_PlayerEnteredHandler : Message<G2C_PlayerEntered>
{
    /// <summary>进入通知提供初始位置；管理器据此创建或更新远端角色。</summary>
    protected override async FTask Run(Session session, G2C_PlayerEntered message)
    {
        if (NetworkManager.Instance.GateSession != session) return;
        RemotePlayerManager.PlayerEntered(new RemotePlayerManager.Snapshot
        {
            RoleId = message.RoleId,
            Position = new UnityEngine.Vector3(message.X, message.Y, message.Z),
            RotationY = message.RotationY,
            State = message.State,
            Speed = message.Speed,
            Sequence = message.Sequence,
        });

        await FTask.CompletedTask;
    }
}

/// <summary>移动通知更新目标位置和动画；远端角色尚未生成时由管理器暂存。</summary>
public sealed class G2C_PlayerMoveHandler : Message<G2C_PlayerMove>
{
    protected override async FTask Run(Session session, G2C_PlayerMove message)
    {
        if (NetworkManager.Instance.GateSession != session) return;
        if (message.RoleId == PlayerSelfModel.Instance.RoleId)
        {
            PlayerSelfModel.Instance.CorrectPosition(message.Sequence, message.X, message.Y, message.Z, message.RotationY);
            return;
        }
        RemotePlayerManager.PlayerMoved(new RemotePlayerManager.Snapshot
        {
            RoleId = message.RoleId,
            Position = new UnityEngine.Vector3(message.X, message.Y, message.Z),
            RotationY = message.RotationY,
            State = message.State,
            Speed = message.Speed,
            Sequence = message.Sequence,
        });

        await FTask.CompletedTask;
    }
}

/// <summary>离开通知清除该角色及其尚未应用的缓存快照。</summary>
public sealed class G2C_PlayerLeftHandler : Message<G2C_PlayerLeft>
{
    protected override async FTask Run(Session session, G2C_PlayerLeft message)
    {
        if (NetworkManager.Instance.GateSession != session) return;
        RemotePlayerManager.PlayerLeft(message.RoleId);
        await FTask.CompletedTask;
    }
}
