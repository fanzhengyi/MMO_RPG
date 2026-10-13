using Fantasy;
using Fantasy.Async;
using Fantasy.MongdbModel;
using Fantasy.Network.Interface;

/// <summary>断线时清除 Gate 路由，并通知其他客户端移除该远端角色。</summary>
public sealed class G2Game_PlayerLeftHandler : Address<Scene, G2Game_PlayerLeft>
{
    protected override async FTask Run(Scene scene, G2Game_PlayerLeft request)
    {
        // 缺少账号或 Gate 路由信息时，无法确认是哪条连接离线。
        if (string.IsNullOrEmpty(request.UserName)
            || request.GateSessionRuntimeId == 0
            || request.GateSceneAddress == 0)
        {
            return;
        }

        var onlineComponent = scene.GetComponent<OnlineComponent>();
        if (!onlineComponent.PlayersByName.TryGetValue(request.UserName, out var player)
            || player.GateSessionRuntimeId != request.GateSessionRuntimeId
            || player.GateSceneAddress != request.GateSceneAddress)
        {
            // 如果同账号已经用新连接登录，旧连接的断开通知不能清掉新连接。
            return;
        }

        // 保留角色和位置数据供重连恢复，只标记当前没有 Gate 客户端连接。
        player.GateSessionRuntimeId = 0;
        player.GateSceneAddress = 0;

        foreach (var receiver in onlineComponent.PlayersByName.Values)
        {
            // 只通知仍有有效客户端连接的其他玩家。
            if (receiver.Id == player.Id
                || receiver.GateSessionRuntimeId == 0
                || receiver.GateSceneAddress == 0)
            {
                continue;
            }

            scene.Send(receiver.GateSceneAddress, new Game2G_PlayerLeft
            {
                GateSessionRuntimeId = receiver.GateSessionRuntimeId,
                RoleId = player.Id,
            });
        }

        await FTask.CompletedTask;
    }
}
