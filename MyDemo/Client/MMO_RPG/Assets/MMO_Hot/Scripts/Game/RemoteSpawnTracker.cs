using System.Collections.Generic;

/// <summary>按 Game 的上线顺序过滤跨实例旧消息；不依赖 Unity、协议对象或外观。</summary>
public sealed class RemoteSpawnTracker
{
    private struct Spawn { public long Id; public ulong Revision; public bool Departed; }
    private readonly Dictionary<long, Spawn> spawns = new Dictionary<long, Spawn>();

    /// <summary>移动也可先于上线到达；记录身份，但创建外观仍由上线通知负责。</summary>
    public bool Observe(long roleId, long spawnId, ulong revision, bool departed = false)
    {
        if (roleId == 0 || spawnId == 0 || revision == 0) return false;
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
