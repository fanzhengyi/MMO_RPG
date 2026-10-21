using System.Collections.Generic;

/// <summary>按 Game 的上线顺序过滤跨实例旧消息；不依赖 Unity、协议对象或外观。</summary>
public sealed class RemoteSpawnTracker
{
    /// <summary>一个角色最新一次上线的身份，即使已离线也暂时保留记录。</summary>
    private struct Spawn
    {
        /// <summary>上线实例 ID，对应 Snapshot.SpawnId。</summary>
        public long Id;
        /// <summary>上线顺序，对应 Snapshot.SpawnRevision；越大表示越新的实例。</summary>
        public ulong Revision;
        /// <summary>该实例是否已离线；同实例迟到的移动和上线包不能让它复活。</summary>
        public bool Departed;
    }
    /// <summary>按 RoleId 保存最新实例身份；清空世界时统一释放这些记录。</summary>
    private readonly Dictionary<long, Spawn> spawns = new Dictionary<long, Spawn>();

    /// <summary>移动也可先于上线到达；记录身份，但创建外观仍由上线通知负责。</summary>
    public bool Observe(long roleId, long spawnId, ulong revision, bool departed = false)
    {
        if (roleId == 0 || spawnId == 0 || revision == 0) return false;
        // current：该角色已经记录的最新实例 ID、上线顺序和离线状态。
        if (spawns.TryGetValue(roleId, out var current))
        {
            if (revision < current.Revision) return false;
            if (revision == current.Revision && (spawnId != current.Id || current.Departed)) return false;
        }
        spawns[roleId] = new Spawn { Id = spawnId, Revision = revision, Departed = departed };
        return true;
    }

    public void Clear() => spawns.Clear();
}
