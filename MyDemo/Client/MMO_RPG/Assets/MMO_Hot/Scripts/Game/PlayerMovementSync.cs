using System;
using Fantasy;
using Fantasy.Network;
using UnityEngine;

/// <summary>只采集和发送本地移动；场景与网络未就绪时暂停角色控制。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
[DefaultExecutionOrder(200)]
public sealed class PlayerMovementSync : MonoBehaviour
{
    [SerializeField, Min(0.02f)] private float sendInterval = 0.1f;
    [SerializeField, Min(0.001f)] private float positionThreshold = 0.05f;
    [SerializeField, Min(0.1f)] private float rotationThreshold = 1f;
    [SerializeField, Min(0.01f)] private float speedThreshold = 0.1f;

    private PlayerController playerController;
    private Session trackedSession;
    private long trackedSessionRuntimeId;
    private Vector3 lastPosition;
    private float lastRotationY;
    private float lastSpeed;
    private int lastState;
    private float nextSendTime;
    private uint sequence;
    private bool hasSent;

    /// <summary>初始控制权由进入世界流程开启，避免场景加载首帧上报错误位置。</summary>
    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerController.SetControlEnabled(false);
    }

    /// <summary>只监听当前场景角色的位置回正。</summary>
    private void OnEnable() => PlayerSelfModel.Instance.PositionCorrected += OnPositionCorrected;

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
        if (session != trackedSession || session.RuntimeId != trackedSessionRuntimeId)
        {
            trackedSession = session;
            trackedSessionRuntimeId = session.RuntimeId;
            sequence = 0;
            hasSent = false;
        }
        var position = transform.position;
        var rotationY = transform.eulerAngles.y;
        var state = (int)playerController.State;
        var speed = playerController.HorizontalSpeed;
        var stateChanged = !hasSent || state != lastState;
        if (!stateChanged && Time.unscaledTime < nextSendTime) return;
        if (hasSent && !stateChanged && !HasMovementChanged(position, rotationY, speed)) return;
        SendSnapshot(session, position, rotationY, state, speed);
    }

    /// <summary>没有明显变化时不重复发送网络消息。</summary>
    private bool HasMovementChanged(Vector3 position, float rotationY, float speed)
        => (position - lastPosition).sqrMagnitude >= positionThreshold * positionThreshold
            || Mathf.Abs(Mathf.DeltaAngle(lastRotationY, rotationY)) >= rotationThreshold
            || Mathf.Abs(speed - lastSpeed) >= speedThreshold;

    /// <summary>上行序号只属于当前连接；成功提交后保存本次采样值。</summary>
    private void SendSnapshot(Session session, Vector3 position, float rotationY, int state, float speed)
    {
        sequence = unchecked(sequence + 1);
        try
        {
            session.C2G_PlayerMove(position.x, position.y, position.z, rotationY, sequence, state, speed);
            lastPosition = position;
            lastRotationY = rotationY;
            lastState = state;
            lastSpeed = speed;
            nextSendTime = Time.unscaledTime + sendInterval;
            hasSent = true;
        }
        catch (Exception exception) { Debug.LogException(exception, this); }
    }
}
