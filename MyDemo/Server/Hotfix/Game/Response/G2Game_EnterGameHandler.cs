using Fantasy;
using Fantasy.Async;
using Fantasy.MongdbModel;
using Fantasy.Network.Interface;

/// <summary>加载或复用角色数据，绑定 Gate 连接，并交换在线玩家的初始状态。</summary>
public class G2Game_EnterGameHandler: AddressRPC<Scene, G2Game_EnterGameRequest, Game2G_EnterGameResponse>
{
    protected override async FTask Run(Scene scene, G2Game_EnterGameRequest request,
        Game2G_EnterGameResponse response, Action reply)
    {
        // UserName 来自 Gate 已验证的登录 Session；缺失时不能确定要加载哪个角色。
        if (string.IsNullOrEmpty(request.UserName))
        {
            response.AccountErrorCode = (int)AccountErrorCode.ServerError;
            return;
        }
        // 在线表同时按账号和角色 ID 建索引，移动广播从这里枚举接收者。
        var onlineComponent = scene.GetComponent<OnlineComponent>();

        // 先复用当前 Game 内存里的角色；只有不在内存时才查数据库。
        if (!onlineComponent.PlayersByName.TryGetValue(request.UserName, out var onlineInfo))
        {
            // 第一次上线或 Game 重启后重新上线时，从数据库恢复角色。
            onlineInfo = await scene.World.Database
                .First<OnlineInfo>(d => d.UserName == request.UserName);
            
            // 没有角色，让客户端去创建角色
            if (onlineInfo == null)
            {
                response.AccountErrorCode = (int)AccountErrorCode.NoRole;
                return;
            }

            // Deserialize 把数据库实体恢复到当前 Scene，再加入在线索引供后续同步使用。
            onlineInfo.Deserialize(scene);
            onlineComponent.Players[onlineInfo.Id] = onlineInfo;
            onlineComponent.PlayersByName[onlineInfo.UserName] = onlineInfo;
        }

        // 保存当前连接位置；后续移动和离开消息都必须匹配这组 Gate 信息。
        onlineInfo.BindGateSession(request.GateSessionRuntimeId, request.GateSceneAddress);

        // 双向发送进入通知：新人获得现有玩家列表，老玩家获得新人的初始状态。
        foreach (var otherPlayer in onlineComponent.Players.Values)
        {
            if (otherPlayer.Id == onlineInfo.Id)
            {
                continue;
            }

            SendPlayerEntered(scene, otherPlayer, onlineInfo);
            SendPlayerEntered(scene, onlineInfo, otherPlayer);
        }

        // 将角色数据返回给 Gate，由 Gate 转给客户端刷新角色信息并恢复站位。
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

    private static void SendPlayerEntered(Scene scene, OnlineInfo receiver, OnlineInfo player)
    {
        // 没有有效 Gate 路由的角色虽还在内存中，但当前没有客户端可接收通知。
        if (receiver.GateSessionRuntimeId == 0 || receiver.GateSceneAddress == 0)
        {
            return;
        }

        scene.Send(receiver.GateSceneAddress, new Game2G_PlayerEntered
        {
            GateSessionRuntimeId = receiver.GateSessionRuntimeId,
            RoleId = player.Id,
            UserName = player.UserName,
            NickName = player.NickName,
            X = player.X,
            Y = player.Y,
            Z = player.Z,
            RotationY = player.RotationY,
            State = player.MovementState,
            Speed = player.MovementSpeed,
            Sequence = player.LastMoveSequence,
        });
    }
}
