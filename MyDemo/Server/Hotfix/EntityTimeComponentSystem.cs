using Fantasy.Entitas.Interface;

/// <summary>实体提前销毁时取消延迟关闭任务。</summary>
public sealed class EntityTimeComponentDestroySystem : DestroySystem<EntityTimeComponent>
{
    protected override void Destroy(EntityTimeComponent self)
    {
        if (self.TimeId != 0) self.Scene.TimerComponent.Net.Remove(ref self.TimeId);
        self.NextTime = 0;
    }
}
