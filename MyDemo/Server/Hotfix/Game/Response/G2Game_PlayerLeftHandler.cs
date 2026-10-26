using Fantasy;
using Fantasy.Async;
using Fantasy.MongdbModel;
using Fantasy.Network.Interface;

/// <summary>断线通知必须匹配原连接，最终保存后移除角色缓存。</summary>
public sealed class G2Game_PlayerLeftHandler : Address<Scene, G2Game_PlayerLeft>
{
    /// <summary>解绑原连接，通知离开并保存最终位置。</summary>
    protected override async FTask Run(Scene scene, G2Game_PlayerLeft request)
    {
        if (string.IsNullOrEmpty(request.AccountUser)) return;
        using (await scene.CoroutineLockComponent.Wait((int)LockType.PlayerData, request.AccountUser.GetHashCode()))
        {
            // online：当前 Game 的角色索引和定时任务组件。
            var online = scene.GetComponent<OnlineComponent>();
            // player：当前处理的服务端角色数据，包含持久资料和本次在线状态。
            if (!online.PlayersByAccountUser.TryGetValue(request.AccountUser, out var player)
                || player.GateSessionRuntimeId != request.GateSessionRuntimeId
                || player.GateSceneAddress != request.GateSceneAddress
                || (request.WorldSessionId != 0 && player.WorldSessionId != request.WorldSessionId)) return;

            WorldReplication.PublishLeft(scene, player);//这里回给Gate
            
            player.GateSessionRuntimeId = 0;
            player.GateSceneAddress = 0;
            player.IsWorldReady = false;
            // 写入内存
            if (await OnlinePlayerSystem.SaveAsync(scene, player))
                
                //移除字典
                OnlinePlayerSystem.Remove(online, player);
        }
    }
}
