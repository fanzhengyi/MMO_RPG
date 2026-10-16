using Fantasy.Entitas;
using MongoDB.Bson.Serialization.Attributes;

namespace Fantasy.MongdbModel;

/// <summary>角色持久数据，以及不入库的在线移动状态。</summary>
public sealed class OnlineInfo : Entity
{
    public string UserName = "";
    public string NickName = "";
    public long Hp;
    public long MaxHp;
    public long Mp;
    public long MaxMp;
    public long Gold;
    public float X;
    public float Y;
    public float Z;
    public float RotationY;

    [BsonIgnore] public long GateSessionRuntimeId;
    [BsonIgnore] public long GateSceneAddress;
    [BsonIgnore] public uint LastMoveSequence;
    [BsonIgnore] public bool HasLastMoveSequence;
    [BsonIgnore] public long LastMoveReceivedAt;
    [BsonIgnore] public uint MovementVersion;
    [BsonIgnore] public PlayerMotionState MovementState;
    [BsonIgnore] public float MovementSpeed;
    [BsonIgnore] public bool IsDirty;
    [BsonIgnore] public float HorizontalAllowance = 1f;
    [BsonIgnore] public float VerticalAllowance = 2f;
    [BsonIgnore] public long WorldSessionId;
    [BsonIgnore] public bool IsWorldReady;
    [BsonIgnore] public bool NeedsReplication;
    [BsonIgnore] public uint MovementEpoch;
    [BsonIgnore] public uint LastCorrectionSequence;
    [BsonIgnore] public ulong SpawnRevision;

    [BsonIgnore] public bool IsConnected => GateSessionRuntimeId != 0 && GateSceneAddress != 0;
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
        }
        GateSessionRuntimeId = runtimeId;
        GateSceneAddress = sceneAddress;
    }
}
