using Fantasy;

/// <summary>复制本人的角色数据，不持有被回收的网络消息。</summary>
public sealed class PlayerSelfModel : Singleton<PlayerSelfModel>
{
    /// <summary>本人角色的持久 ID；0 表示当前没有绑定角色。</summary>
    public long RoleId;
    /// <summary>游戏内显示的昵称，与用于登录的账号不同。</summary>
    public string NickName;
    /// <summary>当前生命值。</summary>
    public long Hp;
    /// <summary>生命值上限。</summary>
    public long MaxHp;
    /// <summary>当前法力值。</summary>
    public long Mp;
    /// <summary>法力值上限。</summary>
    public long MaxMp;
    /// <summary>角色持有的金币数量。</summary>
    public long Gold;
    /// <summary>进入世界或服务端回正时记录的世界 X 坐标，单位米；本地逐帧移动不会持续回写这里。</summary>
    public float X;
    /// <summary>进入世界或回正时记录的世界高度，单位米。</summary>
    public float Y;
    /// <summary>进入世界或回正时记录的世界 Z 坐标，单位米。</summary>
    public float Z;
    /// <summary>进入世界或回正时记录的 Y 轴朝向，单位度。</summary>
    public float RotationY;
    /// <summary>本地是否已绑定角色数据；不等于已经完成进入世界流程。</summary>
    public bool HasRole => RoleId != 0;
    /// <summary>WorldReady 已成功且可开始同步；场景加载期间为 false。</summary>
    public bool IsInWorld { get; set; }
    /// <summary>本人本次进入世界的会话 ID；用于过滤上次登录或旧世界的迟到消息。</summary>
    public long WorldSessionId { get; private set; }
    /// <summary>服务端分配的世界编号；GameWorldView 当前将 1 映射为 GameScene。</summary>
    public int WorldId { get; private set; }
    /// <summary>服务端回正的代次；每次新回正递增，后续移动必须携带最新代次，不能继续发旧轨迹。</summary>
    public uint MovementEpoch { get; private set; }
    /// <summary>判断消息是否属于本人当前这次进入世界，拒绝旧世界残留消息。</summary>
    public bool AcceptsWorld(long worldSessionId) => worldSessionId != 0 && WorldSessionId == worldSessionId;
    /// <summary>接受服务端回正后触发；四个参数依次为世界 X、Y、Z（米）和 Y 轴朝向（度）。</summary>
    public event System.Action<float, float, float, float> PositionCorrected;
    /// <summary>上次接受回正时，服务端确认的客户端移动序号。</summary>
    private uint lastCorrectionAck;
    /// <summary>是否已接受过回正；用单独标记区分尚无记录与序号恰好为 0。</summary>
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
