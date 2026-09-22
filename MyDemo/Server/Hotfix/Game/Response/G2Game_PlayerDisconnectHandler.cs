using Fantasy;
using Fantasy.Async;
using Fantasy.MongdbModel;
using Fantasy.Network.Interface;

/// <summary>
/// Gate 通知玩家掉线：OnlineInfo 落库（保存位置/血量等），移出在线容器。
/// 带 SessionRuntimeId 校验，顶号时旧 Session 的掉线通知不会误删新玩家。
/// </summary>
public class G2Game_PlayerDisconnectHandler : Address<Scene, G2Game_PlayerDisconnect>
{
    protected override async FTask Run(Scene scene, G2Game_PlayerDisconnect message)
    {
        var onlineComponent = scene.GetComponent<OnlineComponent>();

        if (!onlineComponent.PlayersByName.TryGetValue(message.UserName, out var onlineInfo))
        {
            // 不在线（可能重复通知），直接忽略
            return;
        }

        // 在线数据内存存的GateSessionId和断线时候发过来的sessionid不一样
        if (onlineInfo.GateSessionRuntimeId != message.GateSessionRuntimeId)
        {
            return;
        }

        // 存入数据库
        await scene.World.Database.Save(onlineInfo);

        // 2. 移出在线容器 + 释放实体
        onlineComponent.Players.Remove(onlineInfo.Id);
        onlineComponent.PlayersByName.Remove(onlineInfo.UserName);
        onlineInfo.Dispose();
        await FTask.CompletedTask;
    }
}
