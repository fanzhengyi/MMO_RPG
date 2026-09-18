using Fantasy.Entitas;
using MongoDB.Bson.Serialization.Attributes;

public sealed class GameAccount : Entity
{
    // Gate 上的游戏账号运行时数据，会被 GateLoginCache 持有。
    public string UserName;
    public long CreatTime;
    public long LoginTime;

    // 当前账号对应的客户端 Session，不加入数据库。
    [BsonIgnore]
    public long SessionRunTime;
}
