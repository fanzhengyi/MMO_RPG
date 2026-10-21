using Fantasy.Entitas;

/// <summary>请求限频时间和延迟关闭计时器。</summary>
public sealed class EntityTimeComponent : Entity
{
    /// <summary>延迟关闭实体的定时任务 ID，提前销毁时用它取消任务。</summary>
    public long TimeId;
    /// <summary>允许下一次请求的服务端单调时钟毫秒值，用于限频，不是现实日期。</summary>
    public long NextTime;
}
