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
        // scene：当前操作所属的 Fantasy 网络场景，负责消息、实体和定时器。
        var scene = self.Scene;
        // account：从数据库或缓存取得的账号实体，不是角色实体。
        var account = (Account)self.Parent;
        // runtimeId：开始操作时记录的对象运行时 ID，用于识别销毁或对象池复用。
        var runtimeId = account.RuntimeId;
        self.TimerClearId = scene.TimerComponent.Net.OnceTimer(timeout, () =>
        {
            if (account.IsDisposed || account.RuntimeId != runtimeId) return;
            self.TimerClearId = 0;
            scene.GetComponent<AuthenticationComponent>().RemoveRegisterCache(account.AccountUser);
        });
    }
}
