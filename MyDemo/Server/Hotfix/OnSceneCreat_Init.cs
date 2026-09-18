
using Fantasy;
using Fantasy.Async;
using Fantasy.Event;
using System.Threading.Tasks;

public class OnSceneCreat_Init : AsyncEventSystem<OnCreateScene>
{
    protected override async FTask Handler(OnCreateScene self)
    {
        var scene = self.Scene;
        switch (scene.SceneType)
        {
            case SceneType.Authentication:
                {
                    // Authentication 负责账号校验和 JWT 签发。
                    scene.AddComponent<AuthenticationJwtComponent>();
                    scene.AddComponent<AuthenticationComponent>().UpdateServerData();
                    break;
                }
            case SceneType.Gate:
                {
                    // Gate 负责 JWT 验证和在线账号缓存。
                    scene.AddComponent<GateJwtComponent>();
                    //挂载缓存
                    scene.AddComponent<GateLoginCache>();
                    break;
                }
        }
        await FTask.CompletedTask;
    }
}
