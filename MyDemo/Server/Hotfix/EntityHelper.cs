using Fantasy.Entitas;

/// <summary>实体的请求限频和延迟关闭入口。</summary>
public static class EntityHelper
{
    /// <summary>第一次请求立即通过，后续按单调时钟限频。</summary>
    public static bool CheckInterval(this Entity entity, int intervalTime)
    {
        var timer = entity.GetComponent<EntityTimeComponent>() ?? entity.AddComponent<EntityTimeComponent>();
        var now = Environment.TickCount64;
        if (now < timer.NextTime) return false;
        timer.NextTime = now + intervalTime;
        return true;
    }

    /// <summary>替换旧的延迟关闭任务，保证同一实体只有一个关闭计时器。</summary>
    public static void SetTimeout(this Entity entity, int timeOut)
    {
        var timer = entity.GetComponent<EntityTimeComponent>() ?? entity.AddComponent<EntityTimeComponent>();
        if (timer.TimeId != 0) entity.Scene.TimerComponent.Net.Remove(ref timer.TimeId);
        var runtimeId = entity.RuntimeId;
        timer.TimeId = entity.Scene.TimerComponent.Net.OnceTimer(timeOut, () =>
        {
            timer.TimeId = 0;
            if (!entity.IsDisposed && entity.RuntimeId == runtimeId) entity.Dispose();
        });
    }
}
