namespace Fantasy.MongdbModel;
using Fantasy.Entitas;
using MongoDB.Bson.Serialization.Attributes;
public sealed class OnlineInfo : Entity
{
    // RoleId 就是 Entity.Id（long 雪花ID，自动映射 MongoDB 的 _id），不用自己存
    public string UserName;
    public string NickName;
    public long Hp;//血量
    public long MaxHp; //最大血量
    public long Mp; //法力
    public long MaxMp; //最大法力
    public long Gold;//金币

    // ↓位置（入库，下次登录恢复站位）
    public float X;
    public float Y;
    public float Z;
    public float RotationY; // 朝向

    // ↓运行时转发映射（不入库，Game 推送消息给 Gate 时用）
    [BsonIgnore]
    public long GateSessionRuntimeId;  // 玩家挂在哪个 Gate Session 上
    [BsonIgnore]
    public long GateSceneAddress;      // 那个 Gate 场景的地址

    // 以下字段只用于 Game 内存态，避免随角色数据写入 MongoDB。
    [BsonIgnore]
    public uint LastMoveSequence; //当前连接最后接受的移动序号
    [BsonIgnore]
    public bool HasLastMoveSequence; //当前连接是否已接受过移动；新连接首包不和旧连接快照比较
    [BsonIgnore]
    public long LastMoveReceivedAt; //上一次接收客户端消息的服务器时间
    [BsonIgnore]
    public long LastPositionSavedAt; //上次把位置保存到数据库的服务器时间
    [BsonIgnore]
    public int MovementState; //客户端状态机状态，用于远端角色播放动画
    [BsonIgnore]
    public float MovementSpeed; //客户端上报的动画速度参数

    /// <summary>记录玩家当前所在的 Gate 连接；换连接时重置只属于旧连接的移动统计。</summary>
    public void BindGateSession(long gateSessionRuntimeId, long gateSceneAddress)
    {
        if (GateSessionRuntimeId != gateSessionRuntimeId)
        {
            // 新连接会从自己的第一条移动序号重新开始，旧连接的时间和动画状态也不再适用。
            LastMoveSequence = 0;
            HasLastMoveSequence = false;
            LastMoveReceivedAt = 0;
            LastPositionSavedAt = 0;
            MovementState = 0;
            MovementSpeed = 0f;
        }

        GateSessionRuntimeId = gateSessionRuntimeId;
        GateSceneAddress = gateSceneAddress;
    }
}
