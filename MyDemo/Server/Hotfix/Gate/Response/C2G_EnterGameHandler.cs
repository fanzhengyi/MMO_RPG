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

        // 根据UserName选择一个Game服务器
        var gameScenes = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Game);
        //FantasyConfig没有配置
        if (gameScenes == null || gameScenes.Count == 0)
        {
            Log.Error("没有可用的 Game 场景，请检查 Fantasy.config");
            response.AccountErrorCode = (int)AccountErrorCode.ServerError;
            return;
        }
        var gameSceneConfig = gameScenes[(int)(HashCodeHelper.MurmurHash3(disponse.userName) % gameScenes.Count)];

        // RPC 转发给 Game 服务器查库（Gate 不碰数据库）
        //    同时带上自己的 Session 信息，Game 保存后用于反向推送
        var gameResponse = (Game2G_EnterGameResponse)await session.Scene.Call(
            gameSceneConfig.Address,
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
                RoleId   = gameResponse.RoleId,
                UserName = gameResponse.UserName,
                Hp       = gameResponse.Hp,
                MaxHp    = gameResponse.MaxHp,
                Mp       = gameResponse.Mp,
                MaxMp    = gameResponse.MaxMp,
                Gold     = gameResponse.Gold,
                X        = gameResponse.X,
                Y        = gameResponse.Y,
                Z        = gameResponse.Z,
                RotationY = gameResponse.RotationY,
            };
        }
    }
}
