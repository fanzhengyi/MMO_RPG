using Fantasy.Entitas;
namespace Fantasy.MongdbModel;

/// <summary>当前 Game 的在线角色索引和定时保存任务。</summary>
public sealed class OnlineComponent : Entity
{
    /// <summary>按登录账号索引当前 Game 载入的角色；字典存在不代表已 WorldReady，需再看 IsInWorld。</summary>
    public readonly Dictionary<string, OnlineInfo> PlayersByName = new(StringComparer.Ordinal);
    /// <summary>定时保存角色数据的任务 ID，销毁组件时用于移除计时器。</summary>
    public long SaveTimerId;
    /// <summary>定时广播移动变化的任务 ID，当前每 50 毫秒处理一次。</summary>
    public long ReplicationTimerId;
    /// <summary>当前 Game 世界最后分配的上线顺序；新连接绑定时递增，不是角色持久 ID。</summary>
    public ulong LastSpawnRevision;
}
