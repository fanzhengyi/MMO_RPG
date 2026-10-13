using Fantasy.Entitas.Interface;

/// <summary>Gate 场景关闭时清空连接索引。</summary>
public sealed class GateLoginCacheDestroySystem : DestroySystem<GateLoginCache>
{
    /// <summary>清除账号到连接的索引。</summary>
    protected override void Destroy(GateLoginCache self)
    {
        self.Sessions.Clear();
    }
}
