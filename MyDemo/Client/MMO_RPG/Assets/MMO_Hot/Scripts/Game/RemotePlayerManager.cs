using System.Collections.Generic;
using UnityEngine;

/// <summary>在场景中创建远端角色，并缓存角色生成前抵达的网络快照。</summary>
public sealed class RemotePlayerManager : MonoBehaviour
{
    public struct Snapshot
    {
        // 服务器角色 ID 用作远端角色字典的唯一键。
        public long RoleId;
        public Vector3 Position; // 世界坐标
        public float RotationY; // 绕 Y 轴的朝向角度
        public int State; // 与本地 Animator 的 State 参数一致
        public float Speed; // 与本地 Animator 的 Speed 参数一致
    }

    // 当前场景使用的唯一管理器实例。
    private static RemotePlayerManager instance;
    // 场景管理器尚未准备好时先暂存进入通知；同一角色只保留最新一份。
    private static readonly Dictionary<long, Snapshot> pendingEntries = new Dictionary<long, Snapshot>();
    // 远端模型还没创建时先暂存移动；避免“移动通知先到”导致位置丢失。
    private static readonly Dictionary<long, Snapshot> pendingMoves = new Dictionary<long, Snapshot>();

    // 已经生成的远端模型，按角色 ID 查找。
    private readonly Dictionary<long, RemotePlayerView> views = new Dictionary<long, RemotePlayerView>();
    // 本地 Animator 的模型和相对变换，用来生成外观一致的远端角色。
    private Animator visualTemplate;
    private Vector3 visualLocalPosition;
    private Quaternion visualLocalRotation;
    private Vector3 visualLocalScale;
    private Vector3 avatarScale;
    private bool isReady;

    /// <summary>场景加载后用本地角色的模型作为远端角色外观模板。</summary>
    public static void Initialize(GameObject localPlayer)
    {
        if (localPlayer == null)
        {
            Debug.LogError("RemotePlayerManager: 找不到本地角色对象 PlayerRoot。");
            return;
        }

        if (instance == null)
        {
            var managerObject = new GameObject(nameof(RemotePlayerManager));
            instance = managerObject.AddComponent<RemotePlayerManager>();
        }

        instance.Setup(localPlayer);
    }

    /// <summary>接收玩家进入场景的初始状态，必要时先缓存到场景准备完成。</summary>
    public static void PlayerEntered(Snapshot snapshot)
    {
        if (snapshot.RoleId == 0 || snapshot.RoleId == PlayerSelfModel.Instance.RoleId)
        {
            // 不创建无效角色，也不把自己当作远端角色显示。
            return;
        }

        if (instance == null || !instance.isReady)
        {
            // 网络通知可能早于场景角色初始化，先缓存，等 Initialize 后再创建。
            pendingEntries[snapshot.RoleId] = snapshot;
            return;
        }

        instance.Upsert(snapshot);
        if (pendingMoves.TryGetValue(snapshot.RoleId, out var pendingMove))
        {
            // 先按进入消息创建模型，再应用可能已经到达的较新位置。
            instance.views[snapshot.RoleId].SetSnapshot(pendingMove);
            pendingMoves.Remove(snapshot.RoleId);
        }
    }

    /// <summary>更新遠端角色的目标位置和动画状态。</summary>
    public static void PlayerMoved(Snapshot snapshot)
    {
        if (snapshot.RoleId == 0 || snapshot.RoleId == PlayerSelfModel.Instance.RoleId)
        {
            return;
        }

        if (instance == null || !instance.isReady
            || !instance.views.TryGetValue(snapshot.RoleId, out var view))
        {
            // 暂无可更新的模型时只保留最新移动状态，后续创建模型后再应用。
            pendingMoves[snapshot.RoleId] = snapshot;
            return;
        }

        view.SetSnapshot(snapshot);
    }

    /// <summary>移除服务端确认离线的远端角色。</summary>
    public static void PlayerLeft(long roleId)
    {
        // 角色尚未生成时也要清除缓存，避免离线后又被旧缓存创建出来。
        pendingEntries.Remove(roleId);
        pendingMoves.Remove(roleId);
        if (instance != null && instance.views.TryGetValue(roleId, out var view))
        {
            instance.views.Remove(roleId);
            Destroy(view.gameObject);
        }
    }

    private void Setup(GameObject localPlayer)
    {
        visualTemplate = localPlayer.GetComponentInChildren<Animator>(true);
        if (visualTemplate == null)
        {
            Debug.LogError("RemotePlayerManager: 本地角色上找不到 Animator，无法创建远端角色。");
            return;
        }

        visualLocalPosition = visualTemplate.transform.localPosition;
        visualLocalRotation = visualTemplate.transform.localRotation;
        visualLocalScale = visualTemplate.transform.localScale;
        avatarScale = localPlayer.transform.localScale;
        isReady = true;

        // 按收到的进入通知创建所有暂存角色。
        foreach (var snapshot in pendingEntries.Values)
        {
            Upsert(snapshot);
        }
        pendingEntries.Clear();

        var resolvedMoves = new List<long>();
        foreach (var pair in pendingMoves)
        {
            if (views.TryGetValue(pair.Key, out var view))
            {
                // 只消费已经有模型的移动；尚无进入通知的移动继续留在缓存里。
                view.SetSnapshot(pair.Value);
                resolvedMoves.Add(pair.Key);
            }
        }

        foreach (var roleId in resolvedMoves)
        {
            // 遍历字典时不能直接删除元素，所以先记下键，再统一移除。
            pendingMoves.Remove(roleId);
        }
    }

    private void Upsert(Snapshot snapshot)
    {
        if (views.TryGetValue(snapshot.RoleId, out var existingView))
        {
            // 重复进入通知只刷新状态，不重复创建模型。
            existingView.SetSnapshot(snapshot);
            return;
        }

        // 根对象承载位置和朝向，子对象只复用角色外观模型。
        var avatar = new GameObject($"RemotePlayer_{snapshot.RoleId}");
        avatar.transform.SetParent(transform, false);
        avatar.transform.localScale = avatarScale;
        avatar.transform.SetPositionAndRotation(
            snapshot.Position,
            Quaternion.Euler(0f, snapshot.RotationY, 0f));

        var visual = Instantiate(visualTemplate.gameObject, avatar.transform, false);
        visual.transform.localPosition = visualLocalPosition;
        visual.transform.localRotation = visualLocalRotation;
        visual.transform.localScale = visualLocalScale;

        var view = avatar.AddComponent<RemotePlayerView>();
        view.Initialize(snapshot, visual.GetComponentInChildren<Animator>(true));
        views.Add(snapshot.RoleId, view);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            // 场景卸载时释放静态引用，下一次进入场景可重新初始化管理器。
            instance = null;
        }
    }
}
