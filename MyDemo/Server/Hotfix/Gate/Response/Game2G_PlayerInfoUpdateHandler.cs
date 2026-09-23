using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

/// <summary>
/// Game 推送玩家状态变化过来，按 SessionRuntimeId 找到对应 Session 转发给客户端。
/// </summary>
public class Game2G_PlayerInfoUpdateHandler : Address<Scene, Game2G_PlayerInfoUpdate>
{
    protected override async FTask Run(Scene scene, Game2G_PlayerInfoUpdate message)
    {
        // 按记录的 SessionRuntimeId 找 Session（找不到=玩家已掉线，直接丢弃）
        if (!scene.TryGetEntity<Session>(message.GateSessionRuntimeId, out var session))
        {
            Log.Warning($"推送玩家状态失败，Session 已不存在: {message.GateSessionRuntimeId}");
            return;
        }

        // 转成外网消息发给客户端（客户端用它刷新UI）
        session.Send(new G2C_PlayerInfoUpdate
        {
            RoleId = message.RoleId,
            Hp = message.Hp,
            Mp = message.Mp,
            Gold = message.Gold,
        });
        await FTask.CompletedTask;
    }
}
