using Fantasy;
using Fantasy.Async;
using Fantasy.MongdbModel;
using Fantasy.Network.Interface;
using Fantasy.Entitas;

/// <summary>
/// Gate 转发创建角色请求：查库判断是否已有角色，没有则创建并上线。
/// </summary>
public class G2Game_CreateRoleHandler : AddressRPC<Scene, G2Game_CreateRoleRequest, Game2G_CreateRoleResponse>
{
    protected override async FTask Run(Scene scene, G2Game_CreateRoleRequest request,
        Game2G_CreateRoleResponse response, Action reply)
    {
        if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.NickName))
        {
            response.AccountErrorCode = (int)AccountErrorCode.NickNameNull;
            return;
        }

        // 协程锁：同一账号并发创建不会插出两条数据
        using (await scene.CoroutineLockComponent.Wait(
            (int)LockType.RegisterLock, request.UserName.GetHashCode()))
        {
            var onlineComponent = scene.GetComponent<OnlineComponent>();

            var result = AccountErrorCode.HaveRole;
            if (!onlineComponent.PlayersByName.TryGetValue(request.UserName, out var onlineInfo))
            {
                // 内存中没有角色时再查数据库；确实不存在才创建。
                onlineInfo = await scene.World.Database
                    .First<OnlineInfo>(d => d.UserName == request.UserName);
                if (onlineInfo == null)
                {
                    onlineInfo = Entity.Create<OnlineInfo>(scene, true, true);
                    onlineInfo.UserName = request.UserName;
                    onlineInfo.NickName = request.NickName;
                    onlineInfo.Hp = 100;
                    onlineInfo.MaxHp = 100;
                    onlineInfo.Mp = 100;
                    onlineInfo.MaxMp = 100;
                    onlineInfo.Gold = 0;
                    await scene.World.Database.Save(onlineInfo);
                    result = AccountErrorCode.CreateRoleSuccess;
                }

                onlineInfo.Deserialize(scene);
                onlineComponent.PlayersByName[onlineInfo.UserName] = onlineInfo;
            }

            onlineInfo.BindGateSession(request.GateSessionRuntimeId, request.GateSceneAddress);
            response.AccountErrorCode = (int)result;
            FillData(response, onlineInfo);
        }
    }

    private static void FillData(Game2G_CreateRoleResponse response, OnlineInfo info)
    {
        response.Data.RoleId = info.Id;
        response.Data.UserName = info.UserName;
        response.Data.NickName = info.NickName;
        response.Data.Hp = info.Hp;
        response.Data.MaxHp = info.MaxHp;
        response.Data.Mp = info.Mp;
        response.Data.MaxMp = info.MaxMp;
        response.Data.Gold = info.Gold;
        response.Data.X = info.X;
        response.Data.Y = info.Y;
        response.Data.Z = info.Z;
        response.Data.RotationY = info.RotationY;
    }
}
