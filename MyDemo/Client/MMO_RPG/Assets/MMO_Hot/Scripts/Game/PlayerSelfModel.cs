/// <summary>复制本地角色数据，避免持有会被回收的网络消息对象。</summary>
public sealed class PlayerSelfModel : Singleton<PlayerSelfModel>
{
    public long RoleId;
    public string UserName;
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
    private uint correctionSequence;
    private bool hasCorrection;

    /// <summary>保存进入游戏响应中的角色初始数据。</summary>
    public void SetInfo(Fantasy.PlayerInfo info)
    {
        RoleId = info.RoleId;
        UserName = info.UserName;
        NickName = info.NickName;
        Hp = info.Hp;
        MaxHp = info.MaxHp;
        Mp = info.Mp;
        MaxMp = info.MaxMp;
        Gold = info.Gold;
        X = info.X;
        Y = info.Y;
        Z = info.Z;
        RotationY = info.RotationY;
    }

    /// <summary>断线或重新登录时清空全部数据，防止 UI 读到旧账号数值。</summary>
    public void Clear()
    {
        RoleId = Hp = MaxHp = Mp = MaxMp = Gold = 0;
        UserName = NickName = null;
        X = Y = Z = RotationY = 0f;
        IsInWorld = false;
        hasCorrection = false;
        correctionSequence = 0;
    }

    /// <summary>保存最新回正并通知同步组件，数据模型不直接操作场景物体。</summary>
    public void CorrectPosition(uint sequence, float x, float y, float z, float rotationY)
    {
        if (!IsInWorld || (hasCorrection && unchecked((int)(sequence - correctionSequence)) <= 0)) return;
        correctionSequence = sequence;
        hasCorrection = true;
        X = x;
        Y = y;
        Z = z;
        RotationY = rotationY;
        PositionCorrected?.Invoke(x, y, z, rotationY);
    }
}
