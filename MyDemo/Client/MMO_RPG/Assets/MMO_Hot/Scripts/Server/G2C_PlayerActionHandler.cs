using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>复制服务端确认的一次性动作，外观和特效不持有会被消息池回收的对象。</summary>
public sealed class G2C_PlayerActionHandler : Message<G2C_PlayerAction>
{
    protected override async FTask Run(Session session, G2C_PlayerAction message)
    {
        if (NetworkManager.Instance.GateSession != session || message.Action == null
            || !PlayerSelfModel.Instance.AcceptsWorld(message.WorldSessionId)) return;
        RemotePlayerManager.PlayerActed(RemotePlayerAction.From(message.RoleId, message.SpawnId, message.SpawnRevision, message.Action));
        await FTask.CompletedTask;
    }
}
