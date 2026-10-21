using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.MongdbModel;
using Fantasy.Network.Interface;

/// <summary>创建与进入共用角色锁，每个账号只允许一个角色。</summary>
public sealed class G2Game_CreateRoleHandler : AddressRPC<Scene, G2Game_CreateRoleRequest, Game2G_CreateRoleResponse>
{
    /// <summary>校验昵称并保存新角色，不绑定连接、不重复返回资料。</summary>
    protected override async FTask Run(Scene scene, G2Game_CreateRoleRequest request,
        Game2G_CreateRoleResponse response, Action reply)
    {
        response.Result = ResultCode.ServerError;
        if (string.IsNullOrEmpty(request.UserName)) return;
        // nickname：去掉首尾空白后的角色昵称。
        var nickname = request.NickName?.Trim();
        if (string.IsNullOrEmpty(nickname) || nickname.Length > 24)
        {
            response.Result = ResultCode.InvalidNickname;
            return;
        }
        using (await scene.CoroutineLockComponent.Wait((int)LockType.PlayerData, request.UserName.GetHashCode()))
        {
            if (await OnlinePlayerSystem.LoadAsync(scene, request.UserName) != null)
            {
                response.Result = ResultCode.RoleAlreadyExists;
                return;
            }
            // player：当前处理的服务端角色数据，包含持久资料和本次在线状态。
            var player = Entity.Create<OnlineInfo>(scene, false, true);
            player.UserName = request.UserName;
            player.NickName = nickname;
            player.Hp = player.MaxHp = 100;
            player.Mp = player.MaxMp = 100;
            try { await scene.World.Database.Save(player); }
            catch { player.Dispose(); throw; }
            scene.GetComponent<OnlineComponent>().PlayersByName[player.UserName] = player;
            response.Result = ResultCode.Success;
        }
    }
}
