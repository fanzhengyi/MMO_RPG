using System;
using Fantasy;
using Fantasy.Network;
using UnityEngine;

/// <summary>只采集和发送本地移动；场景与网络未就绪时暂停角色控制。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
// PlayerController 的执行顺序为 100；本组件晚于它执行，LateUpdate 再读取移动后的结果。
[DefaultExecutionOrder(200)]
public sealed class PlayerMovementSync : MonoBehaviour
{
    /// <summary>普通移动采样的最小发送间隔，单位秒；0.1 表示最多约每秒 10 次，状态变化可立即发送。</summary>
    [SerializeField, Min(0.02f)] private float sendInterval = 0.1f;
    /// <summary>相比上次已发送位置，至少移动多少米才认为位置明显变化。</summary>
    [SerializeField, Min(0.001f)] private float positionThreshold = 0.05f;
    /// <summary>相比上次已发送朝向，至少旋转多少度才认为朝向明显变化。</summary>
    [SerializeField, Min(0.1f)] private float rotationThreshold = 1f;
    /// <summary>水平速度变化阈值，单位米/秒；小幅波动不额外触发发送。</summary>
    [SerializeField, Min(0.01f)] private float speedThreshold = 0.1f;
    /// <summary>移动快照最长静默间隔，单位秒；站着不动也定期发送，与连接心跳是两回事。</summary>
    [SerializeField, Min(0.1f)] private float heartbeatInterval = 0.5f;

    /// <summary>本地角色控制器；同步层只读取其状态和实际速度，不负责计算移动。</summary>
    private PlayerController playerController;
    /// <summary>上一轮采样绑定的 Gate 连接，用于发现断线重连。</summary>
    private Session trackedSession;
    /// <summary>该连接的运行时身份；Session 可能来自对象池，仅比较对象引用还不足以识别新连接。</summary>
    private long trackedSessionRuntimeId;
    /// <summary>本轮采样所属的世界会话；换世界会话后必须重新发送完整快照。</summary>
    private long trackedWorldSessionId;
    /// <summary>上次成功提交到网络发送接口的位置，不是角色上一帧的位置。</summary>
    private Vector3 lastPosition;
    /// <summary>上次发送的 Y 轴朝向，单位度。</summary>
    private float lastRotationY;
    /// <summary>上次发送的实际水平速度，单位米/秒。</summary>
    private float lastSpeed;
    /// <summary>上次发送的移动状态；切换状态时不等待普通发送间隔。</summary>
    private PlayerFsmCore.StateType lastState;
    private uint lastActionGeneration; // 动作起手也立即采样，让朝向和滑行起步及时跟上。
    /// <summary>下次允许发送普通移动采样的本机时间，使用不受 Time.timeScale 影响的秒数。</summary>
    private float nextSendTime;
    /// <summary>当前连接和世界是否已经发送过基线；为 false 时下一次必须发送。</summary>
    private bool hasSent;
    /// <summary>最近一次发送快照的本机时间，单位秒，用于判断是否需要补发静止快照。</summary>
    private float lastSentTime;

    /// <summary>角色先完成 OnEnable 装配，再绑定网络控制权和回正事件。</summary>
    private void OnEnable()
    {
        playerController = GetComponent<PlayerController>();
        playerController.SetControlEnabled(false);
        PlayerSelfModel.Instance.PositionCorrected += OnPositionCorrected;
    }

    /// <summary>释放事件订阅，避免切场景后仍操作旧角色。</summary>
    private void OnDisable() => PlayerSelfModel.Instance.PositionCorrected -= OnPositionCorrected;

    /// <summary>服务端拒绝位移后回到确认位置，下一帧重新发送移动快照。</summary>
    private void OnPositionCorrected(float x, float y, float z, float rotationY)
    {
        playerController.Place(new Vector3(x, y, z), rotationY);
        hasSent = false;
    }

    /// <summary>在人物移动完成后采集快照；状态变化立即发送，其余变化限频。</summary>
    private void LateUpdate()
    {
        // session：本次操作使用的客户端网络连接。
        var session = NetworkManager.Instance.GateSession;
        if (session == null || session.IsDisposed || !PlayerSelfModel.Instance.IsInWorld)
        {
            playerController.SetControlEnabled(false);
            trackedSession = null;
            trackedSessionRuntimeId = 0;
            hasSent = false;
            return;
        }

        playerController.SetControlEnabled(true);
        if (session != trackedSession || session.RuntimeId != trackedSessionRuntimeId
            || trackedWorldSessionId != PlayerSelfModel.Instance.WorldSessionId)
        {
            trackedSession = session;
            trackedSessionRuntimeId = session.RuntimeId;
            trackedWorldSessionId = PlayerSelfModel.Instance.WorldSessionId;
            hasSent = false;
        }
        // position：这一帧移动后的角色世界位置，单位米。
        var position = transform.position;
        // rotationY：这一帧角色绕 Y 轴的朝向，单位度。
        var rotationY = transform.eulerAngles.y;
        // state：这一帧的本地移动状态。
        var state = playerController.State;
        // speed：碰撞后实际产生的水平速度，单位米/秒。
        var speed = playerController.HorizontalSpeed;
        // stateChanged：是否尚未发送基线，或移动状态相比上次发送已经改变。
        var stateChanged = !hasSent || state != lastState || playerController.ActionGeneration != lastActionGeneration;
        if (!stateChanged && Time.unscaledTime < nextSendTime) return;
        if (hasSent && !stateChanged && Time.unscaledTime - lastSentTime < heartbeatInterval
            && !HasMovementChanged(position, rotationY, speed)) return;
        SendSnapshot(session, position, rotationY, state, speed);
    }

    /// <summary>没有明显变化时不重复发送网络消息。</summary>
    private bool HasMovementChanged(Vector3 position, float rotationY, float speed)
        => (position - lastPosition).sqrMagnitude >= positionThreshold * positionThreshold
            || Mathf.Abs(Mathf.DeltaAngle(lastRotationY, rotationY)) >= rotationThreshold
            || Mathf.Abs(speed - lastSpeed) >= speedThreshold;

    /// <summary>上行序号只属于当前连接；成功提交后保存本次采样值。</summary>
    private void SendSnapshot(Session session, Vector3 position, float rotationY, PlayerFsmCore.StateType state, float speed)
    {
        // sequence：本次上行移动的递增序号，属于当前 Gate 连接。
        var sequence = NetworkManager.Instance.NextMovementSequence();
        try
        {
            session.C2G_PlayerMove(trackedWorldSessionId, sequence, PlayerSelfModel.Instance.MovementEpoch, new MovementSnapshot
            {
                X = position.x, Y = position.y, Z = position.z,
                RotationY = rotationY,
                State = PlayerStateMapping.ToNetwork(state),
                Speed = speed,
            });
            lastPosition = position;
            lastRotationY = rotationY;
            lastState = state;
            lastActionGeneration = playerController.ActionGeneration;
            lastSpeed = speed;
            nextSendTime = Time.unscaledTime + sendInterval;
            lastSentTime = Time.unscaledTime;
            hasSent = true;
        }
        // exception：本次操作抛出的异常，用于记录具体失败原因。
        catch (Exception exception) { Debug.LogException(exception, this); }
    }
}
