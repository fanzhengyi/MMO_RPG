using Fantasy.Entitas;

/// <summary>请求限频时间和延迟关闭计时器。</summary>
public sealed class EntityTimeComponent : Entity
{
    public long TimeId;
    public long NextTime;
}
