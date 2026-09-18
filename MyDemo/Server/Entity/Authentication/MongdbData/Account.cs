using Fantasy.Entitas;

public sealed class Account : Entity
{
    // 登录账号。该实体会保存到数据库。
    public string Username { get; set; }
    public string Password { get; set; }
    // 创建时间和最近一次登录时间。
    public long CreatTime { get; set; }
    public long LoginTime { get; set; }
}
