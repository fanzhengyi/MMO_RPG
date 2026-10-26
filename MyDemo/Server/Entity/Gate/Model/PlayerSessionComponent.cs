using Fantasy.Entitas;

/// <summary>当前客户端身份和路由；Session 销毁时用这份数据通知 Game。</summary>
public sealed class PlayerSessionComponent : Entity
{
    /// <summary>Token 验证后绑定的登录账号；只在 Gate 内部使用。</summary>
    public string AccountUser = "";
    /// <summary>该玩家角色所在 Game 场景的内网地址。</summary>
    public long GameSceneAddress;
    /// <summary>Game 中当前 OnlineInfo 实体的 Address；Gate 的 PlayerRouteType 指向它。</summary>
    public long PlayerAddress;
    /// <summary>当前 Gate 场景的内网地址，Game 回传消息时使用。</summary>
    public long GateSceneAddress;
    /// <summary>当前客户端连接的运行时 ID，识别对象池复用后的不同连接。</summary>
    public long SessionRuntimeId;
    /// <summary>Game 返回的本次世界会话 ID；进入就绪和离线通知都要匹配。</summary>
    public long WorldSessionId;
    /// <summary>是否已有进入世界请求正在等待 Game 返回。</summary>
    public bool IsEntering;
    /// <summary>Game 是否已经确认本次世界就绪。</summary>
    public bool IsWorldReady;

    public void ResetWorldState()
    {
        WorldSessionId = 0;
        PlayerAddress = 0;
        IsEntering = IsWorldReady = false;
    }
}
