using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas.Interface;

public class SessionDisponseDestrySystem : DestroySystem<SessionDisponse>
{
    protected override void Destroy(SessionDisponse self)
    {
        // Session 销毁时触发账号断线逻辑，而不是立即删除账号数据。
        if (self.userName != "")
        {
            GameAccountHelper.Disconnect(self.Scene, self.userName).Coroutine();
            self.userName = "";
        }
    }
}
