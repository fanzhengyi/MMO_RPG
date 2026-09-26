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
}
