using System.Collections.Generic;
using UnityEngine;

/// <summary>管理远端模型及场景加载期间的快照缓存。</summary>
public sealed class RemotePlayerManager : MonoBehaviour
{
    /// <summary>复制协议数据，避免持有会被回收的网络消息。</summary>
    public struct Snapshot
    {
        public long RoleId;
        public Vector3 Position;
        public float RotationY;
        public int State;
        public float Speed;
        public uint Sequence;

        /// <summary>用序号判断新旧，同时兼容 uint 序号回绕。</summary>
        public bool IsNewerThan(Snapshot other)
            => unchecked((int)(Sequence - other.Sequence)) > 0;
    }

    private static RemotePlayerManager instance;
    private static readonly Dictionary<long, Snapshot> pendingEntries = new Dictionary<long, Snapshot>();
    private static readonly Dictionary<long, Snapshot> pendingMoves = new Dictionary<long, Snapshot>();
    private readonly Dictionary<long, RemotePlayerView> views = new Dictionary<long, RemotePlayerView>();
    private Animator visualTemplate;
    private Transform localRoot;
    private bool isReady;

    /// <summary>绑定本地角色的模型子物体作为临时外观模板。</summary>
    public static bool Initialize(GameObject localPlayer)
    {
        var controller = localPlayer != null ? localPlayer.GetComponent<PlayerController>() : null;
        if (controller == null || controller.VisualAnimator == null) return false;
        var model = controller.VisualAnimator;
        if (model.transform == localPlayer.transform
            || model.GetComponentInChildren<PlayerController>(true) != null
            || model.GetComponentInChildren<PlayerInputSource>(true) != null)
        {
            Debug.LogError("远端外观需要独立模型子物体；请将 Animator 与本地输入、控制脚本分开。");
            return false;
        }

        if (instance == null)
            instance = new GameObject(nameof(RemotePlayerManager)).AddComponent<RemotePlayerManager>();
        instance.visualTemplate = model;
        instance.localRoot = localPlayer.transform;
        instance.isReady = true;
        foreach (var snapshot in pendingEntries.Values) instance.Upsert(snapshot);
        pendingEntries.Clear();
        return true;
    }

    /// <summary>断线或切换世界时清空所有模型和静态缓存。</summary>
    public static void Reset()
    {
        pendingEntries.Clear();
        pendingMoves.Clear();
        if (instance == null) return;
        instance.gameObject.SetActive(false);
        Destroy(instance.gameObject);
        instance = null;
    }

    /// <summary>进入通知负责创建模型；移动通知不能单独创建已离线的角色。</summary>
    public static void PlayerEntered(Snapshot snapshot)
    {
        if (!IsRemote(snapshot.RoleId)) return;
        if (instance == null || !instance.isReady)
        {
            CacheLatest(pendingEntries, snapshot);
            return;
        }
        instance.Upsert(snapshot);
    }

    /// <summary>模型尚未准备好时只缓存最新位置。</summary>
    public static void PlayerMoved(Snapshot snapshot)
    {
        if (!IsRemote(snapshot.RoleId)) return;
        if (instance == null || !instance.isReady || !instance.views.TryGetValue(snapshot.RoleId, out var view))
        {
            CacheLatest(pendingMoves, snapshot);
            return;
        }
        view.SetSnapshot(snapshot);
    }

    /// <summary>离开通知同时清除已生成模型和未消费快照。</summary>
    public static void PlayerLeft(long roleId)
    {
        pendingEntries.Remove(roleId);
        pendingMoves.Remove(roleId);
        if (instance != null && instance.views.TryGetValue(roleId, out var view))
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
        if (!cache.TryGetValue(snapshot.RoleId, out var previous) || snapshot.IsNewerThan(previous))
            cache[snapshot.RoleId] = snapshot;
    }

    /// <summary>只克隆模型，在激活前完成位置和动画初始化。</summary>
    private void Upsert(Snapshot snapshot)
    {
        if (views.TryGetValue(snapshot.RoleId, out var view))
            view.SetSnapshot(snapshot);
        else
        {
            var avatar = new GameObject($"RemotePlayer_{snapshot.RoleId}");
            avatar.SetActive(false);
            avatar.transform.SetParent(transform, false);
            avatar.transform.localScale = localRoot.localScale;
            var visual = Instantiate(visualTemplate.gameObject, avatar.transform, false);
            visual.transform.localPosition = localRoot.InverseTransformPoint(visualTemplate.transform.position);
            visual.transform.localRotation = Quaternion.Inverse(localRoot.rotation) * visualTemplate.transform.rotation;
            // 相对世界缩放保留模型中间层的缩放，不依赖 Animator 必须是直接子物体。
            var scale = localRoot.lossyScale;
            var modelScale = visualTemplate.transform.lossyScale;
            visual.transform.localScale = new Vector3(
                modelScale.x / scale.x, modelScale.y / scale.y, modelScale.z / scale.z);
            view = avatar.AddComponent<RemotePlayerView>();
            view.Initialize(snapshot, visual.GetComponent<Animator>());
            views.Add(snapshot.RoleId, view);
            avatar.SetActive(true);
        }
        if (pendingMoves.TryGetValue(snapshot.RoleId, out var move))
        {
            view.SetSnapshot(move);
            pendingMoves.Remove(snapshot.RoleId);
        }
    }

    /// <summary>场景正常销毁时清除对应的静态引用。</summary>
    private void OnDestroy()
    {
        if (instance != this) return;
        instance = null;
        pendingEntries.Clear();
        pendingMoves.Clear();
    }

    /// <summary>兼容编辑器关闭 Domain Reload 后重新运行。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearStatics()
    {
        instance = null;
        pendingEntries.Clear();
        pendingMoves.Clear();
    }
}
