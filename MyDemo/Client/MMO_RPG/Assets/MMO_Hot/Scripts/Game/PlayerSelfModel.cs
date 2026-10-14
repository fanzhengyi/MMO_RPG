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
    public event System.Action<float, float, float, float> PositionCorrected;
    private uint lastCorrectionAck;
    private bool hasCorrection;

    /// <summary>复制进入响应，并清除上一次世界的回正序号。</summary>
    public void SetInfo(PlayerInfo info)
    {
        RoleId = info.RoleId;
        NickName = info.NickName;
        Hp = info.Hp; MaxHp = info.MaxHp;
        Mp = info.Mp; MaxMp = info.MaxMp;
        Gold = info.Gold;
        X = info.Movement.X; Y = info.Movement.Y; Z = info.Movement.Z;
        RotationY = info.Movement.RotationY;
        hasCorrection = false;
        lastCorrectionAck = 0;
    }

    /// <summary>断线时清空角色和本次连接的回正记录。</summary>
    public void Clear()
    {
        RoleId = Hp = MaxHp = Mp = MaxMp = Gold = 0;
        NickName = null;
        X = Y = Z = RotationY = 0f;
        IsInWorld = false;
        hasCorrection = false;
        lastCorrectionAck = 0;
    }

    /// <summary>只接受更新的回正确认，通知控制器重新定位。</summary>
    public void CorrectPosition(uint ackSequence, MovementSnapshot movement)
    {
        if (!IsInWorld || movement == null
            || (hasCorrection && unchecked((int)(ackSequence - lastCorrectionAck)) <= 0)) return;
        lastCorrectionAck = ackSequence;
        hasCorrection = true;
        X = movement.X; Y = movement.Y; Z = movement.Z;
        RotationY = movement.RotationY;
        PositionCorrected?.Invoke(X, Y, Z, RotationY);
    }
}
