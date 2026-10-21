using Fantasy;
using Fantasy.Entitas;

/// <summary>登录缓存只保存验证结果，不挂载数据库账号实体。</summary>
public sealed class LoginCacheEntity : Entity
{
    /// <summary>短时缓存的登录验证结果；缓存项生命周期由鉴权超时系统管理。</summary>
    public ResultCode Result;
}
