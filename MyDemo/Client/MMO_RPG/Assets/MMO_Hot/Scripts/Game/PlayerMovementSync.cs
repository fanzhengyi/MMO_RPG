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
    [SerializeField, Min(0.02f)] private float sendInterval = 0.1f;
    [SerializeField, Min(0.001f)] private float positionThreshold = 0.05f;
    [SerializeField, Min(0.1f)] private float rotationThreshold = 1f;
    [SerializeField, Min(0.01f)] private float speedThreshold = 0.1f;

    private PlayerController playerController;
    private Session trackedSession;
    private Vector3 lastPosition;
    private float lastRotationY;
    private float lastSpeed;
    private int lastState;
    private float nextSendTime;
    private uint sequence;
    private bool hasSent;

    /// <summary>获取角色控制器。</summary>
    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    /// <summary>在角色本帧移动完成后采集并发送快照。</summary>
    private void LateUpdate()
    {
        var session = NetworkManager.Instance.GateSession;
        if (session == null || session.IsDisposed || !PlayerSelfModel.Instance.HasRole)
            return;

        if (session != trackedSession)
        {
            trackedSession = session;
            sequence = 0;
            hasSent = false;
        }

        var position = transform.position;
        var rotationY = transform.eulerAngles.y;
        var state = (int)playerController.State;
        var speed = playerController.HorizontalSpeed;
        var stateChanged = !hasSent || state != lastState;

        if (!stateChanged && Time.unscaledTime < nextSendTime)
            return;

        if (hasSent && !stateChanged && !HasMovementChanged(position, rotationY, speed))
            return;

        SendSnapshot(session, position, rotationY, state, speed);
    }

    /// <summary>检查位置、朝向或速度是否超过同步阈值。</summary>
    private bool HasMovementChanged(Vector3 position, float rotationY, float speed)
    {
        return (position - lastPosition).sqrMagnitude >= positionThreshold * positionThreshold
            || Mathf.Abs(Mathf.DeltaAngle(lastRotationY, rotationY)) >= rotationThreshold
            || Mathf.Abs(speed - lastSpeed) >= speedThreshold;
    }

    /// <summary>发送移动快照并记录本次发送的数据。</summary>
    private void SendSnapshot(Session session, Vector3 position, float rotationY, int state, float speed)
    {
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
