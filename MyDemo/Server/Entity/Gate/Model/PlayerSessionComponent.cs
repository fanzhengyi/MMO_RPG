using Fantasy.Entitas;

/// <summary>当前客户端身份和路由；Session 销毁时用这份数据通知 Game。</summary>
public sealed class PlayerSessionComponent : Entity
{
    public string UserName = "";
    public long GameSceneAddress;
    public long GateSceneAddress;
    public long SessionRuntimeId;
    public long WorldSessionId;
    public bool IsEntering;
    public bool IsWorldReady;
    private double movementTokens = 5;
    private long lastMovementAt;

    public void ResetWorldState()
    {
        WorldSessionId = 0;
        IsEntering = IsWorldReady = false;
        movementTokens = 5;
        lastMovementAt = 0;
    }

    /// <summary>允许少量状态切换突发；长期转发上限每秒 50 条。</summary>
    public bool TryAcceptMovement(long now)
    {
        if (lastMovementAt != 0)
            movementTokens = Math.Min(5, movementTokens + Math.Max(0, now - lastMovementAt) * 0.05);
        lastMovementAt = now;
        if (movementTokens < 1) return false;
        movementTokens -= 1;
        return true;
    }
}
