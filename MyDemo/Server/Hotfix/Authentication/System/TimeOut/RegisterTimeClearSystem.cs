using Fantasy.Entitas.Interface;

/// <summary>注册缓存提前释放时取消计时器。</summary>
public sealed class RegisterTimeClearDestroySystem : DestroySystem<RegisterTimeClear>
{
    protected override void Destroy(RegisterTimeClear self)
    {
        if (self.TimerClearId != 0) self.Scene.TimerComponent.Net.Remove(ref self.TimerClearId);
    }
}

public static class RegisterTimeClearSystem
{
    /// <summary>注册缓存到期后移除账号实体。</summary>
    public static void RegisterCacheClear(this RegisterTimeClear self, int timeout)
    {
        var scene = self.Scene;
        var account = (Account)self.Parent;
        var runtimeId = account.RuntimeId;
        self.TimerClearId = scene.TimerComponent.Net.OnceTimer(timeout, () =>
        {
            if (account.IsDisposed || account.RuntimeId != runtimeId) return;
            self.TimerClearId = 0;
            scene.GetComponent<AuthenticationComponent>().RemoveRegisterCache(account.Username);
        });
    }
}
