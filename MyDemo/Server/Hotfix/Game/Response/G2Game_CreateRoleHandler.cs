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

            // 1. 先查内存（在线说明已有角色）
            if (onlineComponent.PlayersByName.TryGetValue(request.UserName, out var onlineInfo))
            {
                onlineInfo.GateSessionRuntimeId = request.GateSessionRuntimeId;
                onlineInfo.GateSceneAddress = request.GateSceneAddress;

                response.AccountErrorCode = (int)AccountErrorCode.HaveRole;
                FillData(response, onlineInfo);
                return;
            }

            // 2. 再查 MongoDB
            var exist = await scene.World.Database
                .First<OnlineInfo>(d => d.UserName == request.UserName);
            if (exist != null)
            {
                // 已有角色：直接上线（激活 + 入在线容器）
                exist.Deserialize(scene);
                exist.GateSessionRuntimeId = request.GateSessionRuntimeId;
                exist.GateSceneAddress = request.GateSceneAddress;
                onlineComponent.Players[exist.Id] = exist;
                onlineComponent.PlayersByName[exist.UserName] = exist;

                response.AccountErrorCode = (int)AccountErrorCode.HaveRole;
                FillData(response, exist);
                return;
            }

            // 3. 没角色：创建（初始数值写死，只有默认职业）
            onlineInfo = Entity.Create<OnlineInfo>(scene, true, true);
            onlineInfo.UserName = request.UserName;
            onlineInfo.NickName = request.NickName;
            onlineInfo.Hp = 100;
            onlineInfo.MaxHp = 100;
            onlineInfo.Mp = 100;
            onlineInfo.MaxMp = 100;
            onlineInfo.Gold = 0;

            await scene.World.Database.Save(onlineInfo);

            // 4. 上线：激活 + 入在线容器 + 保存 Gate 转发映射
            onlineInfo.Deserialize(scene);
            onlineInfo.GateSessionRuntimeId = request.GateSessionRuntimeId;
            onlineInfo.GateSceneAddress = request.GateSceneAddress;
            onlineComponent.Players[onlineInfo.Id] = onlineInfo;
            onlineComponent.PlayersByName[onlineInfo.UserName] = onlineInfo;

            response.AccountErrorCode = (int)AccountErrorCode.HaveRole;
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
