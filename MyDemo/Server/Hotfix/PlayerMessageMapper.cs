using Fantasy;
using Fantasy.MongdbModel;

/// <summary>角色数据与网络 DTO 的转换集中在一处，Handler 只负责流程。</summary>
public static class PlayerMessageMapper
{
    /// <summary>复制 Game 的角色数据到内部响应。</summary>
    public static RoleData ToRoleData(OnlineInfo player) => new()
    {
        RoleId = player.Id,
        UserName = player.UserName,
        NickName = player.NickName,
        Hp = player.Hp,
        MaxHp = player.MaxHp,
        Mp = player.Mp,
        MaxMp = player.MaxMp,
        Gold = player.Gold,
        X = player.X,
        Y = player.Y,
        Z = player.Z,
        RotationY = player.RotationY,
    };

    /// <summary>Gate 只复制 DTO，不访问 Game 的数据库实体。</summary>
    public static PlayerInfo ToPlayerInfo(RoleData data) => new()
    {
        RoleId = data.RoleId,
        UserName = data.UserName,
        NickName = data.NickName,
        Hp = data.Hp,
        MaxHp = data.MaxHp,
        Mp = data.Mp,
        MaxMp = data.MaxMp,
        Gold = data.Gold,
        X = data.X,
        Y = data.Y,
        Z = data.Z,
        RotationY = data.RotationY,
    };
}
