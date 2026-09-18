using Fantasy.Entitas;


public sealed class AuthenticationComponent : Entity
{
    // 当前鉴权服务器在配置表中的索引。
    public int LocalServerConfigIndex;
    // 当前鉴权服务器总数量。
    public int AuthernServerCount;

    // 注册成功后的短时缓存，避免重复注册请求频繁查询数据库。
    public readonly Dictionary<string, Account> RegisterCacheDic = new Dictionary<string, Account>();

    // 登录结果短时缓存，缓存成功和失败结果，避免重复登录持续访问数据库。
    public readonly Dictionary<string, LoginCacheEntity> LoginCacheDic = new Dictionary<string, LoginCacheEntity>();
}

