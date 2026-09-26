using Fantasy;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Platform.Net;

public class C2G_EnterGameHandler : MessageRPC<C2G_EnterGameRequest, G2C_EnterGameResponse>
{
    protected override async FTask Run(Session session, C2G_EnterGameRequest request,
        G2C_EnterGameResponse response, Action reply)
    {
        // 验证是否登录
        var disponse = session.GetComponent<SessionDisponse>();
        if (disponse == null || string.IsNullOrEmpty(disponse.userName))
        {
            response.AccountErrorCode = (int)AccountErrorCode.NoLogin;
            return;
        }
        if (disponse.gameSceneAddress == 0)
        {
            // 兜底：极早期版本登录时没算过（正常流程走不到）
            response.AccountErrorCode = (int)AccountErrorCode.ServerError;
            return;
        }

        // RPC 转发给 Game 服务器查库（Gate 不碰数据库）
        //    同时带上自己的 Session 信息，Game 保存后用于反向推送
        //    Game场景地址用登录时缓存的值，不用每次重复哈希路由
        var gameResponse = (Game2G_EnterGameResponse)await session.Scene.Call(
            disponse.gameSceneAddress,
            new G2Game_EnterGameRequest
            {
                UserName = disponse.userName,
                GateSessionRuntimeId = session.RuntimeId,
                GateSceneAddress = SceneConfigData.Instance.Get(session.Scene.SceneConfigId).Address,
            });
        //转发失败
        if (gameResponse == null)
        {
            Log.Error($"进入游戏转发 Game 失败，用户: {disponse.userName}");
            response.AccountErrorCode = (int)AccountErrorCode.ServerError;
            return;
        }

        // 把结果原样转给客户端（HaveRole=有角色 NoRole=无角色去创角）
        response.AccountErrorCode = gameResponse.AccountErrorCode;
        if (gameResponse.AccountErrorCode == (int)AccountErrorCode.HaveRole)
        {
            response.Info = new PlayerInfo
            {
                RoleId   = gameResponse.Data.RoleId,
                UserName = gameResponse.Data.UserName,
                Hp       = gameResponse.Data.Hp,
                MaxHp    = gameResponse.Data.MaxHp,
                Mp       = gameResponse.Data.Mp,
                MaxMp    = gameResponse.Data.MaxMp,
                Gold     = gameResponse.Data.Gold,
                X        = gameResponse.Data.X,
                Y        = gameResponse.Data.Y,
                Z        = gameResponse.Data.Z,
                RotationY = gameResponse.Data.RotationY,
                NickName = gameResponse.Data.NickName,
            };
        }
    }
}
