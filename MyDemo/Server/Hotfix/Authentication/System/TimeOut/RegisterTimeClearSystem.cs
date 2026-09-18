

using Fantasy.Entitas.Interface;

public sealed class RegisterTimeClearDestorySystem : DestroySystem<RegisterTimeClear>
{
    //以防其它地方已经销毁Register还是执行时间回调函数
    protected override void Destroy(RegisterTimeClear self)
    {
        if (self.TimerClearId != 0)
        {
            //移除时间回调函数
            self.Scene.TimerComponent.Net.Remove(ref self.TimerClearId);
        }
    }
}
public static class RegisterTimeClearSystem
{
    public static void RegisterCacheClear(this RegisterTimeClear self, int timeout)
    {
        // 注册缓存只保留很短时间，避免重复注册请求反复访问数据库。
        var scene = self.Scene;
        var account = (Account)self.Parent;//拿到字典的Account

        var runId = account.RuntimeId;//每一次使用都是唯一的
        self.TimerClearId = scene.TimerComponent.Net.OnceTimer(timeout, () =>
        {
            if (runId != account.RuntimeId)//避免在倒计时内从对象池再一次拿出来使用的时候就被销毁
            {
                return;
            }
            // 确认对象仍是本次计时对应的对象后，再移除缓存。
            AuthenticationHelper.RemoveRegisterCache(scene, account.Username, true);
        });
    }
}
