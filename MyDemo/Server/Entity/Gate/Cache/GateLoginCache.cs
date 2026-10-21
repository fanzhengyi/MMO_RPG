using Fantasy.Entitas;

/// <summary>Gate 只保存账号对应的当前连接，不持有角色或数据库数据。</summary>
public sealed class GateLoginCache : Entity
{
    /// <summary>账号到当前客户端连接运行时 ID 的映射，用于重复登录替换旧连接。</summary>
    public readonly Dictionary<string, long> Sessions = new(StringComparer.Ordinal);
}
