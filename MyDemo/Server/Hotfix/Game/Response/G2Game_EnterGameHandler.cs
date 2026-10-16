using Fantasy;
using Fantasy.Async;
using Fantasy.MongdbModel;
using Fantasy.Network.Interface;

/// <summary>在角色锁内预备角色，正式上线由 WorldReady 提交。</summary>
public sealed class G2Game_EnterGameHandler : AddressRPC<Scene, G2Game_EnterGameRequest, Game2G_EnterGameResponse>
{
    /// <summary>加载角色、绑定待就绪连接，返回数据和世界会话。</summary>
    protected override async FTask Run(Scene scene, G2Game_EnterGameRequest request,
        Game2G_EnterGameResponse response, Action reply)
    {
        response.Result = ResultCode.ServerError;
        if (string.IsNullOrEmpty(request.UserName) || request.GateSessionRuntimeId == 0
            || request.GateSceneAddress == 0) return;

        using (await scene.CoroutineLockComponent.Wait((int)LockType.PlayerData, request.UserName.GetHashCode()))
        {
            var player = await OnlinePlayerSystem.LoadAsync(scene, request.UserName);
            if (player == null)
            {
                response.Result = ResultCode.RoleNotFound;
                return;
            }
            var isNewConnection = player.GateSessionRuntimeId != request.GateSessionRuntimeId
                || player.GateSceneAddress != request.GateSceneAddress;
            if (isNewConnection) WorldReplication.PublishLeft(scene, player);
            player.BindGateSession(request.GateSessionRuntimeId, request.GateSceneAddress);
            if (isNewConnection)
                player.SpawnRevision = checked(++scene.GetComponent<OnlineComponent>().LastSpawnRevision);
            response.Result = ResultCode.Success;
            response.Info = PlayerMessageMapper.ToPlayerInfo(player);
            response.WorldSessionId = player.WorldSessionId;
            response.WorldId = 1;
        }
    }
}
