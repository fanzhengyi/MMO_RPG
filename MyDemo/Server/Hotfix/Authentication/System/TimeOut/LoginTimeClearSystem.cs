public static class LoginTimeClearSystem
{
    public static void TimeOut(this LoginTimeClear self, string key, int timeOut)
    {
        // 保存父级 LoginCacheEntity 的运行时 ID，防止对象池复用后误删新缓存。
        var scene = self.Scene;
        var runId = self.Parent.RuntimeId;
        self.TimerClearId = scene.TimerComponent.Net.OnceTimer(timeOut, () =>
        {
            if (runId != self.Parent.RuntimeId)
            {
                return;
            }
            // 到期后移除字典记录并销毁缓存实体。
            AuthenticationHelper.RemoveLoginCache(scene, key, true);
            self.Dispose();
        });
    }
}
