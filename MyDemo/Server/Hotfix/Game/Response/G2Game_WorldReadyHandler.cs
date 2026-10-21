using Fantasy;
using Fantasy.Async;
using Fantasy.MongdbModel;
using Fantasy.Network.Interface;

/// <summary>准备角色和正式上线分开；重复 Ready 不重复广播上线事件。</summary>
public sealed class G2Game_WorldReadyHandler : AddressRPC<Scene, G2Game_WorldReadyRequest, Game2G_WorldReadyResponse>
{
    protected override async FTask Run(Scene scene, G2Game_WorldReadyRequest request,
        Game2G_WorldReadyResponse response, Action reply)
    {
        response.Result = ResultCode.InvalidWorldSession;
        if (string.IsNullOrEmpty(request.UserName)) return;
        using (await scene.CoroutineLockComponent.Wait((int)LockType.PlayerData, request.UserName.GetHashCode()))
        {
            // online：当前 Game 的角色索引和定时任务组件。
            var online = scene.GetComponent<OnlineComponent>();
            // player：当前处理的服务端角色数据，包含持久资料和本次在线状态。
            if (!online.PlayersByName.TryGetValue(request.UserName, out var player)
                || !player.MatchesWorld(request.GateSessionRuntimeId, request.GateSceneAddress, request.WorldSessionId)) return;
            // wasReady：处理请求前是否已经世界就绪，用于避免重复发布上线通知。
            var wasReady = player.IsWorldReady;
            if (!player.MarkWorldReady(request.WorldSessionId, Environment.TickCount64)) return;
            // other：当前遍历到的另一个可见玩家。
            foreach (var other in WorldReplication.VisiblePlayers(scene, player))
                response.Players.Add(PlayerMessageMapper.ToPresence(other));
            if (!wasReady) WorldReplication.PublishEntered(scene, player);
            response.Result = ResultCode.Success;
        }
    }
}
