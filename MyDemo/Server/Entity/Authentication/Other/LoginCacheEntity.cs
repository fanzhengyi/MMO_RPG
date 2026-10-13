using Fantasy;
using Fantasy.Entitas;

/// <summary>登录缓存只保存验证结果，不挂载数据库账号实体。</summary>
public sealed class LoginCacheEntity : Entity
{
    public AccountErrorCode Result;
}
