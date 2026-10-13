using UnityEngine;

/// <summary>远端角色只接受网络快照，平滑位置和朝向，不读取玩家输入。</summary>
public sealed class RemotePlayerView : MonoBehaviour
{
    private const float FollowSharpness = 15f;
    private const float TeleportDistance = 10f;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Animator animator;
    private uint sequence;
    private bool moving;

    /// <summary>初次生成直接定位，避免从原点滑入。</summary>
    public void Initialize(RemotePlayerManager.Snapshot snapshot, Animator characterAnimator)
    {
        animator = characterAnimator;
        if (animator != null) animator.applyRootMotion = false;
        sequence = snapshot.Sequence;
        targetPosition = snapshot.Position;
        targetRotation = Quaternion.Euler(0f, snapshot.RotationY, 0f);
        transform.SetPositionAndRotation(targetPosition, targetRotation);
        ApplyAnimation(snapshot);
    }

    /// <summary>只应用新版本；过远的位置变化直接定位，用于传送或长时间延迟。</summary>
    public void SetSnapshot(RemotePlayerManager.Snapshot snapshot)
    {
        if (unchecked((int)(snapshot.Sequence - sequence)) <= 0) return;
        sequence = snapshot.Sequence;
        targetPosition = snapshot.Position;
        targetRotation = Quaternion.Euler(0f, snapshot.RotationY, 0f);
        if ((targetPosition - transform.position).sqrMagnitude > TeleportDistance * TeleportDistance)
            transform.SetPositionAndRotation(targetPosition, targetRotation);
        moving = true;
        ApplyAnimation(snapshot);
    }

    /// <summary>到达目标后停止每帧写 Transform，减少静止角色的更新开销。</summary>
    private void Update()
    {
        if (!moving) return;
        var blend = 1f - Mathf.Exp(-FollowSharpness * Time.deltaTime);
        transform.SetPositionAndRotation(
            Vector3.Lerp(transform.position, targetPosition, blend),
            Quaternion.Slerp(transform.rotation, targetRotation, blend));
        if ((targetPosition - transform.position).sqrMagnitude < 0.000001f
            && Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
        {
            transform.SetPositionAndRotation(targetPosition, targetRotation);
            moving = false;
        }
    }

    /// <summary>动画参数与本地角色一致，位置仍完全由快照控制。</summary>
    private void ApplyAnimation(RemotePlayerManager.Snapshot snapshot)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        animator.SetInteger("State", snapshot.State);
        animator.SetFloat("Speed", snapshot.Speed);
    }
}
