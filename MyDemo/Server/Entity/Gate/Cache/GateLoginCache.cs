using Fantasy.Entitas;

/// <summary>Gate 只保存账号对应的当前连接，不持有角色或数据库数据。</summary>
public sealed class GateLoginCache : Entity
{
    public readonly Dictionary<string, long> Sessions = new(StringComparer.Ordinal);
}
