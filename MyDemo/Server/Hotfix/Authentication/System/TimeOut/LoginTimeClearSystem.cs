using Fantasy.Entitas.Interface;

/// <summary>缓存提前释放时也要取消计时器。</summary>
public sealed class LoginTimeClearDestroySystem : DestroySystem<LoginTimeClear>
{
    protected override void Destroy(LoginTimeClear self)
    {
        if (self.TimerClearId != 0) self.Scene.TimerComponent.Net.Remove(ref self.TimerClearId);
    }
}

public static class LoginTimeClearSystem
{
    /// <summary>到期后移除缓存；父实体的销毁负责释放组件。</summary>
    public static void TimeOut(this LoginTimeClear self, string key, int timeOut)
    {
        var scene = self.Scene;
        var runtimeId = self.Parent.RuntimeId;
        self.TimerClearId = scene.TimerComponent.Net.OnceTimer(timeOut, () =>
        {
            if (self.IsDisposed || self.Parent.RuntimeId != runtimeId) return;
            self.TimerClearId = 0;
            scene.GetComponent<AuthenticationComponent>().RemoveLoginCache(key);
        });
    }
}
