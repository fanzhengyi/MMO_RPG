using UnityEngine;

/// <summary>在服务端快照时间线上插值；不读取输入，也不预测远端碰撞。</summary>
public sealed class RemotePlayerView : MonoBehaviour
{
    private const float TeleportDistance = 10f;
    private static readonly int StateParameter = Animator.StringToHash("State");
    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private readonly SnapshotTimeline<RemotePlayerManager.Snapshot> timeline = new SnapshotTimeline<RemotePlayerManager.Snapshot>();
    private Animator animator;
    private uint version;
    private Vector3 latestPosition;
    public long SpawnId { get; private set; }
    public ulong SpawnRevision { get; private set; }

    /// <summary>初次生成直接定位，避免从原点滑入。</summary>
    public void Initialize(RemotePlayerManager.Snapshot snapshot, Animator characterAnimator)
    {
        animator = characterAnimator;
        animator.applyRootMotion = false;
        SpawnId = snapshot.SpawnId;
        SpawnRevision = snapshot.SpawnRevision;
        version = snapshot.Version;
        latestPosition = snapshot.Position;
        timeline.Clear();
        timeline.Add(snapshot.ServerTimeMs, Time.unscaledTimeAsDouble, snapshot);
        Apply(snapshot, snapshot, 1);
    }

    /// <summary>只应用新版本；过远的位置变化直接定位，用于传送或长时间延迟。</summary>
    public void SetSnapshot(RemotePlayerManager.Snapshot snapshot)
    {
        if (snapshot.SpawnId != SpawnId || unchecked((int)(snapshot.Version - version)) <= 0) return;
        var teleport = (snapshot.Position - latestPosition).sqrMagnitude > TeleportDistance * TeleportDistance;
        if (teleport) timeline.Clear();
        if (!timeline.Add(snapshot.ServerTimeMs, Time.unscaledTimeAsDouble, snapshot)) return;
        version = snapshot.Version;
        latestPosition = snapshot.Position;
        if (teleport) Apply(snapshot, snapshot, 1);
    }

    /// <summary>按播放时刻采样，无新包时停在最后确认位置。</summary>
    private void Update()
    {
        if (timeline.TrySample(Time.unscaledTimeAsDouble, out var from, out var to, out var fraction))
            Apply(from, to, fraction);
    }

    /// <summary>动画参数与本地角色一致，位置仍完全由快照控制。</summary>
    private void Apply(RemotePlayerManager.Snapshot from, RemotePlayerManager.Snapshot to, float fraction)
    {
        transform.SetPositionAndRotation(Vector3.Lerp(from.Position, to.Position, fraction),
            Quaternion.Euler(0, Mathf.LerpAngle(from.RotationY, to.RotationY, fraction), 0));
        animator.SetInteger(StateParameter, (int)(fraction < 1 ? from.State : to.State));
        animator.SetFloat(SpeedParameter, Mathf.Lerp(from.Speed, to.Speed, fraction));
    }
}
