using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;
using Fantasy.Platform.Net;

public class SessionDisponseDestrySystem : DestroySystem<SessionDisponse>
{
    protected override void Destroy(SessionDisponse self)
    {
        // Session 销毁时触发账号断线逻辑，而不是立即删除账号数据。
        if (self.userName != "")
        {
            GameAccountHelper.Disconnect(self.Scene, self.userName).Coroutine();
            // 通知 Game 服务器：玩家掉线了，保存 OnlineInfo 落库并移出在线容器
            NotifyGameDisconnect(self.Scene, self.userName, self.Parent?.RuntimeId ?? 0);
            self.userName = "";
        }
    }

    private static void NotifyGameDisconnect(Scene scene, string userName, long sessionRuntimeId)
    {
        var gameScenes = SceneConfigData.Instance.GetSceneBySceneType(SceneType.Game);
        if (gameScenes == null || gameScenes.Count == 0)
        {
            return;
        }
        var gameSceneConfig = gameScenes[(int)(HashCodeHelper.MurmurHash3(userName) % gameScenes.Count)];
        scene.Send(gameSceneConfig.Address, new G2Game_PlayerDisconnect
        {
            UserName = userName,
            GateSessionRuntimeId = sessionRuntimeId,
        });
    }
}
