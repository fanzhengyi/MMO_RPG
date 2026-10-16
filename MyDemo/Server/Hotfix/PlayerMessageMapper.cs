using Fantasy;
using Fantasy.MongdbModel;

/// <summary>只把数据库实体复制成协议快照，不暴露账号或实体引用。</summary>
public static class PlayerMessageMapper
{
    /// <summary>提取当前确认的移动数据。</summary>
    public static MovementSnapshot ToMovement(OnlineInfo player) => new()
    {
        X = player.X, Y = player.Y, Z = player.Z,
        RotationY = player.RotationY,
        State = player.MovementState,
        Speed = player.MovementSpeed,
    };

    /// <summary>生成只发给本人的完整角色资料。</summary>
    public static PlayerInfo ToPlayerInfo(OnlineInfo player) => new()
    {
        RoleId = player.Id,
        NickName = player.NickName,
        Hp = player.Hp, MaxHp = player.MaxHp,
        Mp = player.Mp, MaxMp = player.MaxMp,
        Gold = player.Gold,
        Movement = ToMovement(player),
    };

    /// <summary>生成其他玩家可见的外观和移动资料。</summary>
    public static PlayerPresence ToPresence(OnlineInfo player) => new()
    {
        RoleId = player.Id,
        NickName = player.NickName,
        Movement = ToMovement(player),
        Version = player.MovementVersion,
        SpawnId = player.WorldSessionId,
        SpawnRevision = player.SpawnRevision,
        ServerTimeMs = Environment.TickCount64,
    };
}
