using Fantasy;
using Fantasy.Async;
using Fantasy.MongdbModel;
using Fantasy.Network.Interface;

/// <summary>在角色锁内加载和上线角色，返回初始数据。</summary>
public sealed class G2Game_EnterGameHandler : AddressRPC<Scene, G2Game_EnterGameRequest, Game2G_EnterGameResponse>
{
    /// <summary>加载角色、绑定连接、通知在线玩家，返回完整数据。</summary>
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
            player.BindGateSession(request.GateSessionRuntimeId, request.GateSceneAddress);
            OnlinePlayerSystem.SendPresence(scene, player, isNewConnection);
            response.Result = ResultCode.Success;
            response.Info = PlayerMessageMapper.ToPlayerInfo(player);
        }
    }
}
