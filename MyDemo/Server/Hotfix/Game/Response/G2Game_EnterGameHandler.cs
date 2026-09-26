using Fantasy;
using Fantasy.Async;
using Fantasy.MongdbModel;
using Fantasy.Network.Interface;

public class G2Game_EnterGameHandler: AddressRPC<Scene, G2Game_EnterGameRequest, Game2G_EnterGameResponse>
{
    protected override async FTask Run(Scene scene, G2Game_EnterGameRequest request,
        Game2G_EnterGameResponse response, Action reply)
    {
        // Gate发过来的时候没有UserName的时候返回
        if (string.IsNullOrEmpty(request.UserName))
        {
            response.AccountErrorCode = (int)AccountErrorCode.ServerError;
            return;
        }
        //拿字典
        var onlineComponent = scene.GetComponent<OnlineComponent>();

        //先查内存
        if (!onlineComponent.PlayersByName.TryGetValue(request.UserName, out var onlineInfo))
        {
            // 3. 不在线才查 MongoDB，判断是否有角色
            onlineInfo = await scene.World.Database
                .First<OnlineInfo>(d => d.UserName == request.UserName);
            
            // 没有角色，让客户端去创建角色
            if (onlineInfo == null)
            {
                response.AccountErrorCode = (int)AccountErrorCode.NoRole;
                return;
            }

            // 有角色：激活到当前场景 + 存入全局在线容器（状态同步从这里取）
            onlineInfo.Deserialize(scene);
            onlineComponent.Players[onlineInfo.Id] = onlineInfo;
            onlineComponent.PlayersByName[onlineInfo.UserName] = onlineInfo;
        }

        // 5. 保存 Gate 转发映射
        onlineInfo.GateSessionRuntimeId = request.GateSessionRuntimeId;
        onlineInfo.GateSceneAddress = request.GateSceneAddress;

        // 6. 返回角色数据（Gate 再转给客户端刷新UI，位置用于恢复站位）
        response.AccountErrorCode = (int)AccountErrorCode.HaveRole;
        response.Data.RoleId   = onlineInfo.Id;
        response.Data.UserName = onlineInfo.UserName;
        response.Data.Hp       = onlineInfo.Hp;
        response.Data.MaxHp    = onlineInfo.MaxHp;
        response.Data.Mp       = onlineInfo.Mp;
        response.Data.MaxMp    = onlineInfo.MaxMp;
        response.Data.Gold     = onlineInfo.Gold;
        response.Data.X        = onlineInfo.X;
        response.Data.Y        = onlineInfo.Y;
        response.Data.Z        = onlineInfo.Z;
        response.Data.RotationY = onlineInfo.RotationY;
        response.Data.NickName = onlineInfo.NickName;
    }
}
