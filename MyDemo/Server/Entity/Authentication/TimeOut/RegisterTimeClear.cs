using Fantasy.Entitas;

public class RegisterTimeClear : Entity
{
    /// <summary>注册缓存清理任务的 ID，组件销毁时用它移除计时器。</summary>
    public long TimerClearId;
}