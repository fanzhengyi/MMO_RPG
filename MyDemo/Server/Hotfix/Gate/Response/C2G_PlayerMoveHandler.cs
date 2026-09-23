using Fantasy;
using Fantasy.Async;
using Fantasy.Helper;
using Fantasy.Network;
using Fantasy.Network.Interface;
using Fantasy.Platform.Net;

/// <summary>
/// 客户端上报移动：Gate 不处理业务，转发给该玩家所在的 Game 服务器。
/// </summary>
public class C2G_PlayerMoveHandler : Message<C2G_PlayerMove>
{
    protected override async FTask Run(Session session, C2G_PlayerMove message)
    {
        // 必须先登录过 Gate
        var disponse = session.GetComponent<SessionDisponse>();
        if (disponse == null || string.IsNullOrEmpty(disponse.userName))
        {
            return;
        }

        // 和进入游戏相同的哈希路由，找到该玩家所在的 Game 场景
        var gameScenes = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Game);
        if (gameScenes == null || gameScenes.Count == 0)
        {
            return;
        }
        var gameSceneConfig = gameScenes[(int)(HashCodeHelper.MurmurHash3(disponse.userName) % gameScenes.Count)];

        // 单向转发（移动是高频消息，不等响应）
        session.Scene.Send(gameSceneConfig.Address, new G2Game_PlayerMove
        {
            UserName = disponse.userName,
            GateSessionRuntimeId = session.RuntimeId,
            GateSceneAddress = SceneConfigData.Instance.Get(session.Scene.SceneConfigId).Address,
            X = message.X,
            Y = message.Y,
            Z = message.Z,
            RotationY = message.RotationY,
        });
        await FTask.CompletedTask;
    }
}
