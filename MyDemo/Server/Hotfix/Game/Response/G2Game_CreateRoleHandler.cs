using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.MongdbModel;
using Fantasy.Network.Interface;

/// <summary>创建角色和进入游戏共用角色锁，防止并发加载出两个角色实例。</summary>
public sealed class G2Game_CreateRoleHandler : AddressRPC<Scene, G2Game_CreateRoleRequest, Game2G_CreateRoleResponse>
{
    /// <summary>已有角色直接返回；没有角色才创建并保存。</summary>
    protected override async FTask Run(Scene scene, G2Game_CreateRoleRequest request,
        Game2G_CreateRoleResponse response, Action reply)
    {
        response.AccountErrorCode = (int)AccountErrorCode.ServerError;
        if (string.IsNullOrEmpty(request.UserName) || request.GateSessionRuntimeId == 0
            || request.GateSceneAddress == 0) return;
        if (string.IsNullOrWhiteSpace(request.NickName) || request.NickName.Trim().Length > 24)
        {
            response.AccountErrorCode = (int)AccountErrorCode.NickNameNull;
            return;
        }

        using (await scene.CoroutineLockComponent.Wait((int)LockType.PlayerData, request.UserName.GetHashCode()))
        {
            var player = await OnlinePlayerSystem.LoadAsync(scene, request.UserName);
            var result = AccountErrorCode.HaveRole;
            if (player == null)
            {
                // 角色是长期数据实体，创建时不复用对象池中的旧角色。
                player = Entity.Create<OnlineInfo>(scene, false, true);
                player.UserName = request.UserName;
                player.NickName = request.NickName.Trim();
                player.Hp = player.MaxHp = 100;
                player.Mp = player.MaxMp = 100;
                try { await scene.World.Database.Save(player); }
                catch { player.Dispose(); throw; }
                scene.GetComponent<OnlineComponent>().PlayersByName[player.UserName] = player;
                result = AccountErrorCode.CreateRoleSuccess;
            }
            // 这里只创建数据；连接绑定和在线通知统一由 EnterGame 完成。
            response.AccountErrorCode = (int)result;
            response.Data = PlayerMessageMapper.ToRoleData(player);
        }
    }
}
