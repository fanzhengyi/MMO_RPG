using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>顶号时主动关闭旧连接，业务断线事件会清理角色和远端模型。</summary>
public sealed class G2C_RepeatLoginHandler : Message<G_2C_RepeaLogin>
{
    protected override async FTask Run(Session session, G_2C_RepeaLogin message)
    {
        if (NetworkManager.Instance.GateSession != session) return;
        NetworkManager.Instance.DisconnectGate();
        TipManager.Instance.ShowTip("账号已在其他地方登录，请重新登录。");
        UIManager.Instance.ShowPanel<LoginPanel>();
        await FTask.CompletedTask;
    }
}
