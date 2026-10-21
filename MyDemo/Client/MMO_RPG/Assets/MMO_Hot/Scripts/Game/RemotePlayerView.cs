using UnityEngine;

/// <summary>在服务端快照时间线上插值；不读取输入，也不预测远端碰撞。</summary>
public sealed class RemotePlayerView : MonoBehaviour
{
    /// <summary>相邻确认位置相距超过此米数时直接定位，避免把传送表现成快速滑行。</summary>
    private const float TeleportDistance = 10f;
    /// <summary>Animator 中移动状态参数的哈希，避免每帧按字符串查找。</summary>
    private static readonly int StateParameter = Animator.StringToHash("State");
    /// <summary>Animator 中水平速度参数的哈希。</summary>
    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    /// <summary>快照播放队列；默认延后 0.15 秒，在已收到的两个位置之间平滑播放。</summary>
    private readonly SnapshotTimeline<RemotePlayerManager.Snapshot> timeline = new SnapshotTimeline<RemotePlayerManager.Snapshot>();
    /// <summary>远端模型的动画组件；只表现收到的状态，不读取本机输入。</summary>
    private Animator animator;
    /// <summary>本外观最后接受的移动版本，拒绝重复或更旧的更新。</summary>
    private uint version;
    /// <summary>最后接受的快照位置，用于检测传送；不等于插值过程中当前显示的位置。</summary>
    private Vector3 latestPosition;
    /// <summary>此外观对应的上线实例 ID；同一角色重连后需要创建新实例。</summary>
    public long SpawnId { get; private set; }
    /// <summary>此外观的上线顺序，用于避免旧离线消息误删新外观。</summary>
    public ulong SpawnRevision { get; private set; }

    /// <summary>初次生成直接定位，避免从原点滑入。</summary>
    public void Initialize(RemotePlayerManager.Snapshot snapshot, Animator characterAnimator)
    {
        animator = characterAnimator;
        // 位置由网络快照控制；如果再让动画根运动推动物体，会产生重复位移或下沉。
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
        // teleport：最新确认位置与上一确认位置是否相距过远，需要直接定位。
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
        // from：包围当前播放时刻的较早快照。；to：包围当前播放时刻的较晚快照；样本不足时与 from 相同。；fraction：两个快照之间的插值比例，0 表示 from，1 表示 to。
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
