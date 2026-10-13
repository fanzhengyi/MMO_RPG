using System;
using Fantasy;
using Fantasy.Network;
using UnityEngine;

/// <summary>把本地角色移动快照定频发送到 Gate。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
[DefaultExecutionOrder(200)]
public sealed class PlayerMovementSync : MonoBehaviour
{
    // 正常移动时的最大发送频率；状态切换会立即发送，不等这个间隔。
    [SerializeField, Min(0.02f)] private float sendInterval = 0.1f;
    // 位置、朝向、速度的变化小于阈值时，不发送重复快照。
    [SerializeField, Min(0.001f)] private float positionThreshold = 0.05f;
    [SerializeField, Min(0.1f)] private float rotationThreshold = 1f;
    [SerializeField, Min(0.01f)] private float speedThreshold = 0.1f;

    private PlayerController playerController;
    // 网络重连后序号从头开始，服务端也会在绑定新连接时重置序号。
    private Session trackedSession;
    // 最近一次成功发送的状态，用来判断角色有没有明显变化。
    private Vector3 lastPosition;
    private float lastRotationY;
    private float lastSpeed;
    private int lastState;
    // 允许发送下一条普通移动快照的时间。
    private float nextSendTime;
    // 同一条连接内递增，用于服务端丢弃重复或过期的快照。
    private uint sequence;
    // 区分“还没发过第一条”与“状态刚好没有变化”。
    private bool hasSent;

    /// <summary>获取角色控制器。</summary>
    private void Awake()
    {
        playerController = GetComponent<PlayerController>();

        var player = PlayerSelfModel.Instance;
        if (player.HasRole)
        {
            // 场景首帧同步前先恢复服务端保存的位置。
            transform.SetPositionAndRotation(
                new Vector3(player.X, player.Y, player.Z),
                Quaternion.Euler(0f, player.RotationY, 0f));
        }
    }

    /// <summary>在角色本帧移动完成后采集并发送快照。</summary>
    private void LateUpdate()
    {
        var session = NetworkManager.Instance.GateSession;
        // 未连接或尚未进入角色时，不发送移动消息。
        if (session == null || session.IsDisposed || !PlayerSelfModel.Instance.HasRole)
            return;

        if (session != trackedSession)
        {
            // 新连接有独立的序号序列，并且需要马上发送第一条快照。
            trackedSession = session;
            sequence = 0;
            hasSent = false;
        }

        var position = transform.position;
        var rotationY = transform.eulerAngles.y;
        // 状态值沿用 PlayerFsmCore.StateType：Idle=0、Move=1、Jump=2、Run=6、Fall=7。
        var state = (int)playerController.State;
        // 用于远端动画混合，不用于服务端计算实际位移。
        var speed = playerController.HorizontalSpeed;
        var stateChanged = !hasSent || state != lastState;

        // 状态变化需要马上通知；其余快照按 sendInterval 限频。
        if (!stateChanged && Time.unscaledTime < nextSendTime)
            return;

        // 到发送时间了但角色几乎没动，就不发重复数据。
        if (hasSent && !stateChanged && !HasMovementChanged(position, rotationY, speed))
            return;

        SendSnapshot(session, position, rotationY, state, speed);
    }

    /// <summary>检查位置、朝向或速度是否超过同步阈值。</summary>
    private bool HasMovementChanged(Vector3 position, float rotationY, float speed)
    {
        // 用平方距离比较，避免为了判断阈值额外开平方。
        return (position - lastPosition).sqrMagnitude >= positionThreshold * positionThreshold
            || Mathf.Abs(Mathf.DeltaAngle(lastRotationY, rotationY)) >= rotationThreshold
            || Mathf.Abs(speed - lastSpeed) >= speedThreshold;
    }

    /// <summary>发送移动快照并记录本次发送的数据。</summary>
    private void SendSnapshot(Session session, Vector3 position, float rotationY, int state, float speed)
    {
        // 先递增，因此本连接发送的第一条快照序号为 1。
        sequence = unchecked(sequence + 1);

        try
        {
            session.C2G_PlayerMove(
                position.x,
                position.y,
                position.z,
                rotationY,
                sequence,
                state,
                speed);

            // 发送调用完成后记录当前状态，供下次判断变化。
            lastPosition = position;
            lastRotationY = rotationY;
            lastState = state;
            lastSpeed = speed;
            nextSendTime = Time.unscaledTime + sendInterval;
            hasSent = true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }
}
