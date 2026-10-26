using Fantasy.Entitas;
using MongoDB.Bson.Serialization.Attributes;

public sealed class Account : Entity
{
    // 登录账号。该实体会保存到数据库。
    /// <summary>用于登录和账号分片的账号名，区别于角色昵称。</summary>
    // 保留现有 MongoDB 字段名，改代码命名不影响旧账号查询。
    [BsonElement("Username")]
    public string AccountUser { get; set; }
    /// <summary>新数据保存 PBKDF2 哈希字符串，包含算法、迭代次数、盐和哈希；旧明文仅为迁移兼容。</summary>
    public string Password { get; set; }
    // 创建时间和最近一次登录时间。
    /// <summary>注册创建时间，取自 TimeHelper.Now；沿用现有 CreatTime 拼写以保持存储字段一致。</summary>
    public long CreatTime { get; set; }
    /// <summary>最近一次密码验证成功的时间，取自 TimeHelper.Now。</summary>
    public long LoginTime { get; set; }
}
