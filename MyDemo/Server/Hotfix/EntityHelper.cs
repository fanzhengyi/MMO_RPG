using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Network;

public static class EntityHelper
{
    // 为实体添加或复用请求间隔计时器。
    public static bool CheakInterval(this Entity entity, int intervalTime)
    {
        var sessionTimeOutComponent = entity.GetComponent<EntityTimeComponent>();
        if (sessionTimeOutComponent == null)
        {
            sessionTimeOutComponent = entity.AddComponent<EntityTimeComponent>();
            sessionTimeOutComponent.SetIntervalTime(intervalTime);
            return true;
        }
        return sessionTimeOutComponent.CheakInterval();
    }

    public static void SetTimeout(this Entity entity, int timeOut, Func<FTask> task = null)
    {
        var sessionTimeOutComponent = entity.GetComponent<EntityTimeComponent>();
        if (sessionTimeOutComponent == null)
        {
            sessionTimeOutComponent = entity.AddComponent<EntityTimeComponent>();
        }
        sessionTimeOutComponent.TimeOut(timeOut, task);
    }

    public static bool IsHasTimeComponent(this Entity entity)
    {
        return entity.GetComponent<EntityTimeComponent>() != null;
    }

    public static void CancelTimeOut(this Entity entity)
    {
        entity.RemoveComponent<EntityTimeComponent>();
    }
}
