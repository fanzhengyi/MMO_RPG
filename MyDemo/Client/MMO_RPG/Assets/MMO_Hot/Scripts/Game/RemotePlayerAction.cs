using UnityEngine;

/// <summary>从池化网络消息复制出的动作事件，不持有协议对象。</summary>
public struct RemotePlayerAction
{
    public long RoleId; // 对应角色的持久 ID。
    public long SpawnId; // 本次上线实例 ID。
    public ulong SpawnRevision; // 上线顺序，旧连接事件不能影响新外观。
    public PlayerActionId Kind; // 稳定动作编号，与移动 State 分离。
    public uint Version; // 服务端动作版本，重复包不能再次出特效。
    public long StartedAtServerTimeMs; // 动作在服务端开始或取消的单调时钟毫秒。
    public Vector3 Origin; // 起手时确认的世界位置，延迟世界特效仍使用此位置。
    public float RotationY; // 起手的世界 Y 轴朝向，单位度。

    public static RemotePlayerAction From(long roleId, long spawnId, ulong spawnRevision, Fantasy.PlayerActionSnapshot action)
        => new RemotePlayerAction
        {
            RoleId = roleId, SpawnId = spawnId, SpawnRevision = spawnRevision,
            Kind = (PlayerActionId)(int)action.Kind, Version = action.Version,
            StartedAtServerTimeMs = action.StartedAtServerTimeMs,
            Origin = new Vector3(action.OriginX, action.OriginY, action.OriginZ), RotationY = action.RotationY
        };
}
