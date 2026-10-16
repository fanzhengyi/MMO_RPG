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
        if (string.IsNullOrEmpty(request.UserName)) return;
        using (await scene.CoroutineLockComponent.Wait((int)LockType.PlayerData, request.UserName.GetHashCode()))
        {
            var online = scene.GetComponent<OnlineComponent>();
            if (!online.PlayersByName.TryGetValue(request.UserName, out var player)
                || player.GateSessionRuntimeId != request.GateSessionRuntimeId
                || player.GateSceneAddress != request.GateSceneAddress
                || (request.WorldSessionId != 0 && player.WorldSessionId != request.WorldSessionId)) return;

            WorldReplication.PublishLeft(scene, player);
            player.GateSessionRuntimeId = 0;
            player.GateSceneAddress = 0;
            player.IsWorldReady = false;
            // 保存失败则暂存离线角色，由定时任务重试，避免丢失最终位置。
            if (await OnlinePlayerSystem.SaveAsync(scene, player))
                OnlinePlayerSystem.Remove(online, player);
        }
    }
}
