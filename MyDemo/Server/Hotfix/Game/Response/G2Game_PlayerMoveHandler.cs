using Fantasy;
using Fantasy.Async;
using Fantasy.MongdbModel;
using Fantasy.Network.Interface;

/// <summary>
/// Gate 转发客户端移动上来：更新 OnlineInfo 内存位置，并广播给其他在线玩家。
/// 后期做 AOI 时在这里过滤广播目标，而不是全服广播。
/// </summary>
public class G2Game_PlayerMoveHandler : Address<Scene, G2Game_PlayerMove>
{
    protected override async FTask Run(Scene scene, G2Game_PlayerMove message)
    {
        var onlineComponent = scene.GetComponent<OnlineComponent>();

        if (!onlineComponent.PlayersByName.TryGetValue(message.UserName, out var self))
        {
            // 不在线（没进入过游戏），丢弃
            return;
        }

        // 1. 更新内存位置（下次登录从数据库恢复的就是这里定时保存的值）
        self.X = message.X;
        self.Y = message.Y;
        self.Z = message.Z;
        self.RotationY = message.RotationY;

        // 2. 广播给其他所有在线玩家（暂时全服广播，后期换 AOI）
        foreach (var (roleId, other) in onlineComponent.Players)
        {
            if (roleId == self.Id)
            {
                continue;
            }

            // 发到其他玩家所在的 Gate，由 Gate 转给对应客户端
            scene.Send(other.GateSceneAddress, new Game2G_PlayerMove
            {
                GateSessionRuntimeId = other.GateSessionRuntimeId,
                RoleId = self.Id,
                X = self.X,
                Y = self.Y,
                Z = self.Z,
                RotationY = self.RotationY,
            });
        }
        await FTask.CompletedTask;
    }
}
