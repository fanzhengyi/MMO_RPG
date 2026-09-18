using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;

public sealed class EntityTimeComponentDestorySystem : DestroySystem<EntityTimeComponent>
{
    protected override void Destroy(EntityTimeComponent self)
    {
        if (self.TimeId != 0)
        {
            self.Scene.TimerComponent.Net.Remove(ref self.TimeId);
        }
        self.IntervalTime = 0;
        self.NextTime = 0;
    }
}

public static class EntityTimeComponentSystem
{
    // 设置实体下一次允许请求的时间，用于注册请求限流。
    public static void SetIntervalTime(this EntityTimeComponent self, int intervalTime)
    {
        if (intervalTime < 0)
        {
            return;
        }
        self.IntervalTime = intervalTime;
        self.NextTime = TimeHelper.Now + intervalTime;
    }

    public static bool CheakInterval(this EntityTimeComponent self)
    {
        if (self.NextTime > TimeHelper.Now)
        {
            Log.Warning("请求间隔过小");
            return false;
        }
        self.NextTime = TimeHelper.Now + self.IntervalTime;
        return true;
    }

    public static void TimeOut(this EntityTimeComponent self, int timeOut, Func<FTask>? task = null)
    {
        // 延迟执行回调，然后销毁父实体，常用于断开 Session。
        var scene = self.Scene;
        var runTimeId = self.Parent.RuntimeId;
        self.TimeId = scene.TimerComponent.Net.OnceTimer(timeOut, () =>
        {
            self.Handler(runTimeId, task).Coroutine();
        });
    }

    private static async FTask Handler(this EntityTimeComponent self, long paremtRunTimeId, Func<FTask>? func = null)
    {
        var parent = self.Parent;
        if (parent == null || paremtRunTimeId != self.Parent.RuntimeId)
        {
            return;
        }
        if (func != null)
        {
            await func();
        }
        self.TimeId = 0;
        parent.Dispose();
    }
}
