using System.Collections.Generic;
using UnityEngine;

/// <summary>管理远端模型及场景加载期间的快照缓存。</summary>
public sealed class RemotePlayerManager : MonoBehaviour
{
    /// <summary>复制协议数据，避免持有会被回收的网络消息。</summary>
    public struct Snapshot
    {
        public long RoleId; // 角色唯一 ID，同一角色重新登录后不变。
        public Vector3 Position; // 角色的世界坐标，单位米。
        public float RotationY; // 角色绕 Y 轴的朝向，单位度。
        public PlayerFsmCore.StateType State; // 移动状态：待机、步行、奔跑、跳跃或下落。
        public float Speed; // 实际水平速度，单位米/秒，用于动画表现。
        public uint Version; // 服务端递增的移动更新序号，用来判断快照新旧。
        public long SpawnId; // 该角色本次上线的实例 ID，重新连接进入世界后改变。
        public ulong SpawnRevision; // 本世界内的上线先后序号，用来区分同一角色前后两次上线。
        public long ServerTimeMs; // 快照的服务端单调时钟毫秒值，用于排序和插值。
        public RemotePlayerAction Action; // 上线基线中的当前动作，移动包不会覆盖它。
        public bool HasAction; // 区分完整上线基线和仅含移动的数据。

        /// <summary>复制协议快照，只保留远端表现需要的值。</summary>
        public static Snapshot From(Fantasy.PlayerPresence player)
        {
            var snapshot = From(player.RoleId, player.SpawnId, player.SpawnRevision, player.Version, player.ServerTimeMs, player.Movement);
            snapshot.HasAction = player.Action != null;
            if (snapshot.HasAction) snapshot.Action = RemotePlayerAction.From(player.RoleId, player.SpawnId, player.SpawnRevision, player.Action);
            return snapshot;
        }

        public static Snapshot From(long roleId, long spawnId, ulong spawnRevision, uint version, long serverTimeMs, Fantasy.MovementSnapshot movement) => new Snapshot
        {
            RoleId = roleId,
            Version = version,
            SpawnId = spawnId,
            SpawnRevision = spawnRevision,
            ServerTimeMs = serverTimeMs,
            Position = new Vector3(movement.X, movement.Y, movement.Z),
            RotationY = movement.RotationY,
            State = PlayerStateMapping.FromNetwork(movement.State),
            Speed = movement.Speed,
        };

        /// <summary>用序号判断新旧，同时兼容 uint 序号回绕。</summary>
        public bool IsNewerThan(Snapshot other)
            => unchecked((int)(Version - other.Version)) > 0;
    }

    private static RemotePlayerManager instance; // 当前世界唯一的管理组件，进入游戏后动态创建。
    private static readonly Dictionary<long, Snapshot> pendingEntries = new Dictionary<long, Snapshot>(); // 按角色 ID 缓存尚未创建外观的上线快照。
    private static readonly Dictionary<long, Snapshot> pendingMoves = new Dictionary<long, Snapshot>(); // 按角色 ID 缓存尚未应用的最新移动快照。
    private static readonly Dictionary<long, RemotePlayerAction> pendingActions = new Dictionary<long, RemotePlayerAction>(); // 外观创建前只保留该实例最新动作。
    private static readonly RemoteSpawnTracker spawns = new RemoteSpawnTracker(); // 记录各角色最新上线实例及是否已离线，过滤旧实例消息。
    private readonly Dictionary<long, RemotePlayerView> views = new Dictionary<long, RemotePlayerView>(); // 按角色 ID 查找当前已创建的远端玩家外观。
    private Animator visualTemplate; // 当前用于复制远端外观的本地玩家模型模板。
    private Transform localRoot; // 本地角色根节点，用于还原模型相对位置和缩放。
    private PlayerCombatDefinition combatDefinition; // 本地与远端共用动画、特效配置，不复制运行状态。

    /// <summary>绑定本地角色的模型子物体作为临时外观模板。</summary>
    public static void Initialize(PlayerController localPlayer)
    {
        // EnterGame 在装配前已 Reset；场景中 Animator 位于独立模型子物体。
        instance = new GameObject(nameof(RemotePlayerManager)).AddComponent<RemotePlayerManager>();
        instance.visualTemplate = localPlayer.VisualAnimator;
        instance.localRoot = localPlayer.transform;
        instance.combatDefinition = localPlayer.CombatDefinition;
        // snapshot：场景加载期间缓存的某个远端角色上线快照。
        foreach (var snapshot in pendingEntries.Values)
            if (spawns.Observe(snapshot.RoleId, snapshot.SpawnId, snapshot.SpawnRevision)) instance.Upsert(snapshot);
        pendingEntries.Clear();
    }

    /// <summary>断线或切换世界时清空所有模型和静态缓存。</summary>
    public static void Reset()
    {
        pendingEntries.Clear();
        pendingMoves.Clear();
        pendingActions.Clear();
        spawns.Clear();
        if (instance == null) return;
        instance.gameObject.SetActive(false);
        Destroy(instance.gameObject);
        instance = null;
    }

    /// <summary>进入通知负责创建模型；移动通知不能单独创建已离线的角色。</summary>
    public static void PlayerEntered(Snapshot snapshot)
    {
        if (!IsRemote(snapshot.RoleId) || !spawns.Observe(snapshot.RoleId, snapshot.SpawnId, snapshot.SpawnRevision)) return;
        if (instance == null)
        {
            CacheLatest(pendingEntries, snapshot);
            return;
        }
        instance.Upsert(snapshot);
    }

    /// <summary>模型尚未准备好时只缓存最新位置。</summary>
    public static void PlayerMoved(Snapshot snapshot)
    {
        if (!IsRemote(snapshot.RoleId) || !spawns.Observe(snapshot.RoleId, snapshot.SpawnId, snapshot.SpawnRevision)) return;
        // view：按 RoleId 查找的远端外观，查找失败时为 null。
        if (instance == null || !instance.views.TryGetValue(snapshot.RoleId, out var view))
        {
            CacheLatest(pendingMoves, snapshot);
            return;
        }
        if (view.SpawnId == snapshot.SpawnId) view.SetSnapshot(snapshot);
        else CacheLatest(pendingMoves, snapshot);
    }

    /// <summary>一次性动作立即入队，不与移动合并，也不单凭动作消息生成离线角色。</summary>
    public static void PlayerActed(RemotePlayerAction action)
    {
        if (!IsRemote(action.RoleId) || !spawns.Observe(action.RoleId, action.SpawnId, action.SpawnRevision)) return;
        if (instance != null && instance.views.TryGetValue(action.RoleId, out var view) && view.SpawnId == action.SpawnId)
        {
            view.SetAction(action);
            return;
        }
        if (!pendingActions.TryGetValue(action.RoleId, out var previous) || action.SpawnRevision > previous.SpawnRevision
            || action.SpawnId == previous.SpawnId && unchecked((int)(action.Version - previous.Version)) > 0)
            pendingActions[action.RoleId] = action;
    }

    /// <summary>离开通知同时清除已生成模型和未消费快照。</summary>
    public static void PlayerLeft(long roleId, long spawnId, ulong spawnRevision)
    {
        if (!IsRemote(roleId) || !spawns.Observe(roleId, spawnId, spawnRevision, true)) return;
        // entry：该角色尚未消费的上线快照。
        if (pendingEntries.TryGetValue(roleId, out var entry) && entry.SpawnRevision <= spawnRevision) pendingEntries.Remove(roleId);
        // move：该角色尚未消费的最新移动快照。
        if (pendingMoves.TryGetValue(roleId, out var move) && move.SpawnRevision <= spawnRevision) pendingMoves.Remove(roleId);
        if (pendingActions.TryGetValue(roleId, out var action) && action.SpawnRevision <= spawnRevision) pendingActions.Remove(roleId);
        // view：按 RoleId 查找的远端外观，查找失败时为 null。
        if (instance != null && instance.views.TryGetValue(roleId, out var view) && view.SpawnRevision <= spawnRevision)
        {
            instance.views.Remove(roleId);
            Destroy(view.gameObject);
        }
    }

    /// <summary>只管理其他玩家的外观。</summary>
    private static bool IsRemote(long roleId) => roleId != 0 && roleId != PlayerSelfModel.Instance.RoleId;

    /// <summary>加载期间同一角色只保留最新快照。</summary>
    private static void CacheLatest(Dictionary<long, Snapshot> cache, Snapshot snapshot)
    {
        // previous：缓存中同一角色的上一份快照，用于比较实例和移动版本。
        if (!cache.TryGetValue(snapshot.RoleId, out var previous) || snapshot.SpawnRevision > previous.SpawnRevision
            || (snapshot.SpawnId == previous.SpawnId && snapshot.IsNewerThan(previous)))
            cache[snapshot.RoleId] = snapshot;
    }

    /// <summary>只克隆外观，同一帧完成激活和快照初始化。</summary>
    private void Upsert(Snapshot snapshot)
    {
        // view：按 RoleId 查找的远端外观，查找失败时为 null。
        if (views.TryGetValue(snapshot.RoleId, out var view) && view.SpawnId != snapshot.SpawnId)
        {
            views.Remove(snapshot.RoleId);
            view.gameObject.SetActive(false);
            Destroy(view.gameObject);
            view = null;
        }
        if (view != null)
            view.SetSnapshot(snapshot);
        else
        {
            // avatar：远端角色的运行时根物体，其位置由网络快照驱动。
            var avatar = new GameObject($"RemotePlayer_{snapshot.RoleId}");
            avatar.SetActive(false);
            avatar.transform.SetParent(transform, false);
            avatar.transform.localScale = localRoot.localScale;
            // visual：从本地角色复制出的视觉模型子物体。
            var visual = Instantiate(visualTemplate.gameObject, avatar.transform, false);
            visual.transform.localPosition = localRoot.InverseTransformPoint(visualTemplate.transform.position);
            visual.transform.localRotation = Quaternion.Inverse(localRoot.rotation) * visualTemplate.transform.rotation;
            // 相对世界缩放保留模型中间层的缩放，不依赖 Animator 必须是直接子物体。
            // scale：本地角色根节点的世界缩放。
            var scale = localRoot.lossyScale;
            // modelScale：视觉模型的世界缩放，用它还原相对根节点的缩放。
            var modelScale = visualTemplate.transform.lossyScale;
            visual.transform.localScale = new Vector3(
                modelScale.x / scale.x, modelScale.y / scale.y, modelScale.z / scale.z);
            view = avatar.AddComponent<RemotePlayerView>();
            views.Add(snapshot.RoleId, view);
            // Animator 激活时会初始化参数，快照必须在激活后写入。
            avatar.SetActive(true);
            view.Initialize(snapshot, visual.GetComponent<Animator>(), combatDefinition);
        }
        if (snapshot.HasAction) view.SetAction(snapshot.Action);
        // move：该角色尚未消费的最新移动快照。
        if (pendingMoves.TryGetValue(snapshot.RoleId, out var move))
        {
            if (view.SpawnId == move.SpawnId) view.SetSnapshot(move);
            pendingMoves.Remove(snapshot.RoleId);
        }
        if (pendingActions.TryGetValue(snapshot.RoleId, out var action))
        {
            if (view.SpawnId == action.SpawnId) view.SetAction(action);
            pendingActions.Remove(snapshot.RoleId);
        }
    }

    /// <summary>场景正常销毁时清除对应的静态引用。</summary>
    private void OnDestroy()
    {
        if (instance != this) return;
        instance = null;
        pendingEntries.Clear();
        pendingMoves.Clear();
        pendingActions.Clear();
        spawns.Clear();
    }

    /// <summary>兼容编辑器关闭 Domain Reload 后重新运行。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearStatics()
    {
        instance = null;
        pendingEntries.Clear();
        pendingMoves.Clear();
        pendingActions.Clear();
        spawns.Clear();
    }
}
