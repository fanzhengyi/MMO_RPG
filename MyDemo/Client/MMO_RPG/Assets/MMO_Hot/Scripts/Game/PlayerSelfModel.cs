using Fantasy;

/// <summary>复制本人的角色数据，不持有被回收的网络消息。</summary>
public sealed class PlayerSelfModel : Singleton<PlayerSelfModel>
{
    public long RoleId;
    public string NickName;
    public long Hp;
    public long MaxHp;
    public long Mp;
    public long MaxMp;
    public long Gold;
    public float X;
    public float Y;
    public float Z;
    public float RotationY;
    public bool HasRole => RoleId != 0;
    public bool IsInWorld { get; set; }
    public long WorldSessionId { get; private set; }
    public int WorldId { get; private set; }
    public uint MovementEpoch { get; private set; }
    public bool AcceptsWorld(long worldSessionId) => worldSessionId != 0 && WorldSessionId == worldSessionId;
    public event System.Action<float, float, float, float> PositionCorrected;
    private uint lastCorrectionAck;
    private bool hasCorrection;

    /// <summary>复制 RoleController 已验证的进入响应，并清除上一次世界的回正序号。</summary>
    public void SetInfo(PlayerInfo info, long worldSessionId, int worldId)
    {
        WorldSessionId = worldSessionId;
        WorldId = worldId;
        IsInWorld = false;
        RoleId = info.RoleId;
        NickName = info.NickName;
        Hp = info.Hp; MaxHp = info.MaxHp;
        Mp = info.Mp; MaxMp = info.MaxMp;
        Gold = info.Gold;
        X = info.Movement.X; Y = info.Movement.Y; Z = info.Movement.Z;
        RotationY = info.Movement.RotationY;
        hasCorrection = false;
        lastCorrectionAck = 0;
        MovementEpoch = 0;
    }

    /// <summary>断线时清空角色和本次连接的回正记录。</summary>
    public void Clear()
    {
        RoleId = Hp = MaxHp = Mp = MaxMp = Gold = 0;
        NickName = null;
        X = Y = Z = RotationY = 0f;
        IsInWorld = false;
        WorldSessionId = 0;
        WorldId = 0;
        hasCorrection = false;
        lastCorrectionAck = 0;
        MovementEpoch = 0;
    }

    /// <summary>只接受更新的回正确认，通知控制器重新定位。</summary>
    public void CorrectPosition(long worldSessionId, uint movementEpoch, uint ackSequence, MovementSnapshot movement)
    {
        if (!IsInWorld || !AcceptsWorld(worldSessionId) || movement == null
            || unchecked((int)(movementEpoch - MovementEpoch)) <= 0
            || (hasCorrection && unchecked((int)(ackSequence - lastCorrectionAck)) <= 0)) return;
        MovementEpoch = movementEpoch;
        lastCorrectionAck = ackSequence;
        hasCorrection = true;
        X = movement.X; Y = movement.Y; Z = movement.Z;
        RotationY = movement.RotationY;
        PositionCorrected?.Invoke(X, Y, Z, RotationY);
    }
}
