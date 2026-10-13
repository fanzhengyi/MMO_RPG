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
                || player.GateSceneAddress != request.GateSceneAddress) return;

            player.GateSessionRuntimeId = 0;
            player.GateSceneAddress = 0;
            foreach (var receiver in online.PlayersByName.Values)
            {
                if (receiver.Id != player.Id && receiver.IsConnected)
                    scene.Send(receiver.GateSceneAddress, new Game2G_PlayerLeft
                    {
                        GateSessionRuntimeId = receiver.GateSessionRuntimeId,
                        RoleId = player.Id,
                    });
            }
            // 保存失败则暂存离线角色，由定时任务重试，避免丢失最终位置。
            if (await OnlinePlayerSystem.SaveAsync(scene, player))
                OnlinePlayerSystem.Remove(online, player);
        }
    }
}
