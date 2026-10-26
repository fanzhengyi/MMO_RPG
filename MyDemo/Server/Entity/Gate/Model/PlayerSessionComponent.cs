using Fantasy.Entitas;

/// <summary>当前客户端身份和路由；Session 销毁时用这份数据通知 Game。</summary>
public sealed class PlayerSessionComponent : Entity
{
    /// <summary>Token 验证后绑定的登录账号；转发 Game 时从这里取，不能相信客户端自报账号。</summary>
    public string AccountUser = "";
    /// <summary>该玩家角色所在 Game 场景的内网路由地址。</summary>
    public long GameSceneAddress;
    /// <summary>当前 Gate 场景的内网路由地址，Game 回传消息时使用。</summary>
    public long GateSceneAddress;
    /// <summary>当前客户端连接的运行时 ID，识别对象池复用后的不同连接。</summary>
    public long SessionRuntimeId;
    /// <summary>Game 返回的本次世界会话 ID；进入就绪、移动和离线通知都要匹配。</summary>
    public long WorldSessionId;
    /// <summary>是否已有进入世界请求正在等待 Game 返回，避免重复转发。</summary>
    public bool IsEntering;
    /// <summary>Game 是否已经确认本次世界就绪；未就绪时不转发移动。</summary>
    public bool IsWorldReady;
    /// <summary>移动限频剩余令牌；最多累积 5 个，每转发一条消耗 1 个。</summary>
    private double movementTokens = 5;
    /// <summary>上次计算令牌补充的服务端单调时钟毫秒值。</summary>
    private long lastMovementAt;
    /// <summary>动作请求的独立限频令牌，避免刷动作 RPC 放大内网流量。</summary>
    private double actionTokens = 4;
    /// <summary>上次补充动作令牌的单调时钟毫秒值。</summary>
    private long lastActionAt;

    public void ResetWorldState()
    {
        WorldSessionId = 0;
        IsEntering = IsWorldReady = false;
        movementTokens = 5;
        lastMovementAt = 0;
        actionTokens = 4;
        lastActionAt = 0;
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

    /// <summary>允许少量切换突发，长期最多每秒转发十条动作命令。</summary>
    public bool TryAcceptAction(long now)
    {
        if (lastActionAt != 0)
            actionTokens = Math.Min(4, actionTokens + Math.Max(0, now - lastActionAt) * 0.01);
        lastActionAt = now;
        if (actionTokens < 1) return false;
        actionTokens -= 1;
        return true;
    }
}
