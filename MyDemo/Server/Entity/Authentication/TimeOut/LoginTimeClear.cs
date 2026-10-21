using Fantasy.Entitas;

public class LoginTimeClear : Entity
{
    // 用于清理登录缓存的定时器 ID。
    /// <summary>登录验证结果缓存的过期任务 ID，避免旧结果长期驻留。</summary>
    public long TimerClearId;
}
