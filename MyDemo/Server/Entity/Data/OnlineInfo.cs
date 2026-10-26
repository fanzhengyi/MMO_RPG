using Fantasy.Entitas;
using MongoDB.Bson.Serialization.Attributes;

namespace Fantasy.MongdbModel;

/// <summary>角色持久数据，以及不入库的在线移动状态。</summary>
public sealed class OnlineInfo : Entity
{
    /// <summary>角色所属的登录账号，只供服务端查找，不作为公开角色资料发送。</summary>
    // 保留现有 MongoDB 字段名，已有角色仍能按登录账号读取。
    [BsonElement("UserName")]
    public string AccountUser = "";
    /// <summary>游戏内公开显示的昵称。</summary>
    public string NickName = "";
    /// <summary>当前生命值。</summary>
    public long Hp;
    /// <summary>生命值上限。</summary>
    public long MaxHp;
    /// <summary>当前法力值。</summary>
    public long Mp;
    /// <summary>法力值上限。</summary>
    public long MaxMp;
    /// <summary>持有金币数量。</summary>
    public long Gold;
    /// <summary>服务端最后确认的世界 X 坐标，单位米；继承的 Entity.Id 才是 RoleId。</summary>
    public float X;
    /// <summary>服务端最后确认的世界高度，单位米。</summary>
    public float Y;
    /// <summary>服务端最后确认的世界 Z 坐标，单位米。</summary>
    public float Z;
    /// <summary>服务端最后确认的 Y 轴朝向，单位度。</summary>
    public float RotationY;

    /// <summary>所属 Gate 客户端连接的运行时 ID；下行消息以它找到接收连接，不写入数据库。</summary>
    // 以下字段只描述当前在线状态；BsonIgnore 表示不存库，重连时重新建立。
    [BsonIgnore] public long GateSessionRuntimeId;
    /// <summary>所属 Gate 的内网场景路由地址，不是客户端 IP。</summary>
    [BsonIgnore] public long GateSceneAddress;
    /// <summary>最后处理的客户端上行移动序号，过滤乱序和重复采样。</summary>
    [BsonIgnore] public uint LastMoveSequence;
    /// <summary>是否已有上行序号；避免把尚无记录与序号回绕到 0 混淆。</summary>
    [BsonIgnore] public bool HasLastMoveSequence;
    /// <summary>最近接受移动或标记就绪的单调时钟毫秒值，用于按经过时间恢复移动预算。</summary>
    [BsonIgnore] public long LastMoveReceivedAt;
    /// <summary>服务端确认移动后递增的下行版本；对应客户端 Snapshot.Version，与上行 Sequence 不同。</summary>
    [BsonIgnore] public uint MovementVersion;
    /// <summary>最后确认的协议移动状态，供远端动画表现使用。</summary>
    [BsonIgnore] public PlayerMotionState MovementState;
    /// <summary>最后接受的水平速度报告，单位米/秒，用于远端动画。</summary>
    [BsonIgnore] public float MovementSpeed;
    /// <summary>持久位置或朝向是否改变，定时存库据此避免无变化写入。</summary>
    [BsonIgnore] public bool IsDirty;
    /// <summary>剩余水平位移宽限，单位米；容忍收包抖动，按实际时间恢复而非按包数恢复。</summary>
    [BsonIgnore] public float HorizontalAllowance = 1f;
    /// <summary>剩余竖直位移宽限，单位米，与水平预算分别计算。</summary>
    [BsonIgnore] public float VerticalAllowance = 2f;
    /// <summary>本次进入世界的随机非零会话 ID；公开给其他玩家时称 SpawnId，重连后重新生成。</summary>
    [BsonIgnore] public long WorldSessionId;
    /// <summary>客户端已完成场景装配并提交 WorldReady；为 true 后才参与同步和上线广播。</summary>
    [BsonIgnore] public bool IsWorldReady;
    /// <summary>已接受移动但尚未在复制周期广播；一轮多次移动合并为最新一次。</summary>
    [BsonIgnore] public bool NeedsReplication;
    /// <summary>回正代次，每次新拒绝导致回正时递增；客户端下一次采样必须携带最新值。</summary>
    [BsonIgnore] public uint MovementEpoch;
    /// <summary>触发最近一次回正的客户端移动序号，下行作为 AckSequence 返回。</summary>
    [BsonIgnore] public uint LastCorrectionSequence;
    /// <summary>当前 Game 世界分配的上线顺序，对应客户端 Snapshot.SpawnRevision。</summary>
    [BsonIgnore] public ulong SpawnRevision;
    /// <summary>本次上线独立拥有的动作、连击序号和冷却；不入库。</summary>
    [BsonIgnore] public PlayerActionState Actions = new();

    /// <summary>是否绑定了有效的 Gate 路由身份；不等于客户端场景已经就绪。</summary>
    [BsonIgnore] public bool IsConnected => GateSessionRuntimeId != 0 && GateSceneAddress != 0;
    /// <summary>连接已绑定且世界已就绪，才视作正式在线参与同步。</summary>
    [BsonIgnore] public bool IsInWorld => IsConnected && IsWorldReady;

    public bool MatchesWorld(long runtimeId, long sceneAddress, long worldSessionId)
        => IsConnected && GateSessionRuntimeId == runtimeId && GateSceneAddress == sceneAddress
            && worldSessionId != 0 && WorldSessionId == worldSessionId;

    /// <summary>就绪前不恢复移动预算，也不向其他玩家发布角色。</summary>
    public bool MarkWorldReady(long worldSessionId, long now)
    {
        if (!IsConnected || worldSessionId == 0 || WorldSessionId != worldSessionId) return false;
        if (IsWorldReady) return true;
        IsWorldReady = true;
        LastMoveReceivedAt = now;
        return true;
    }

    /// <summary>换连接重置上行采样，并发布新版本的待机状态。</summary>
    public void BindGateSession(long runtimeId, long sceneAddress)
    {
        if (GateSessionRuntimeId != runtimeId || GateSceneAddress != sceneAddress)
        {
            LastMoveSequence = 0;
            HasLastMoveSequence = false;
            LastMoveReceivedAt = Environment.TickCount64;
            HorizontalAllowance = 1f;
            VerticalAllowance = 2f;
            MovementState = PlayerMotionState.Idle;
            MovementSpeed = 0f;
            MovementVersion = unchecked(MovementVersion + 1);
            WorldSessionId = Random.Shared.NextInt64(1, long.MaxValue);
            IsWorldReady = false;
            NeedsReplication = false;
            MovementEpoch = 0;
            LastCorrectionSequence = 0;
            Actions.Reset();
        }
        GateSessionRuntimeId = runtimeId;
        GateSceneAddress = sceneAddress;
    }
}
