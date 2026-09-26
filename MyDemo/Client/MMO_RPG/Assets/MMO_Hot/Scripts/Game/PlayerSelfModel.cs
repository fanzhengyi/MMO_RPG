using Fantasy;
using UnityEngine;

/// <summary>
/// 本地玩家自己的数据快照（从 PlayerInfo 拷贝，因为协议对象会被回收回对象池）。
/// 后续移动上报、UI刷新都从这里取。
/// </summary>
public class PlayerSelfModel : Singleton<PlayerSelfModel>
{
    public long RoleId;
    public string UserName;
    public long Hp;
    public long MaxHp;
    public long Mp;
    public long MaxMp;
    public long Gold;
    public float X;
    public float Y;
    public float Z;
    public float RotationY;

    /// <summary>是否已有角色</summary>
    public bool HasRole => RoleId != 0;

    public void SetInfo(PlayerInfo info)
    {
        RoleId = info.RoleId;
        UserName = info.UserName;
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

    /// <summary>
    /// 上报自己的移动（状态同步入口），由移动逻辑按固定频率调用。
    /// </summary>
    public void ReportMove(float x, float y, float z, float rotationY)
    {
        // 先更新本地缓存
        X = x; Y = y; Z = z; RotationY = rotationY;
    }

    public void Clear()
    {
        RoleId = 0;
        UserName = null;
        X = Y = Z = RotationY = 0;
    }
}
