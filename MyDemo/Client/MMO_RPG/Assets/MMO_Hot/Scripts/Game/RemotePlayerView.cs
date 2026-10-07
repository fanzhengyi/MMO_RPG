using UnityEngine;

/// <summary>以平滑插值跟随服务端快照的单个远端角色。</summary>
public sealed class RemotePlayerView : MonoBehaviour
{
    // 跟随目标的平滑程度；数值越大，模型越快贴近新快照位置。
    private const float FollowSharpness = 15f;
    // 每次收到网络快照只更新目标值，实际 Transform 在 Update 中平滑追过去。
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Animator animator;

    public void Initialize(RemotePlayerManager.Snapshot snapshot, Animator characterAnimator)
    {
        animator = characterAnimator;
        targetPosition = snapshot.Position;
        targetRotation = Quaternion.Euler(0f, snapshot.RotationY, 0f);
        // 第一次创建直接放到进入位置，避免从世界原点滑过来。
        transform.SetPositionAndRotation(targetPosition, targetRotation);
        ApplyAnimation(snapshot);
    }

    public void SetSnapshot(RemotePlayerManager.Snapshot snapshot)
    {
        targetPosition = snapshot.Position;
        targetRotation = Quaternion.Euler(0f, snapshot.RotationY, 0f);
        ApplyAnimation(snapshot);
    }

    private void Update()
    {
        // 指数插值会依据帧时间调整，避免插值速度明显受帧率影响。
        var blend = 1f - Mathf.Exp(-FollowSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, blend);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, blend);
    }

    private void ApplyAnimation(RemotePlayerManager.Snapshot snapshot)
    {
        // Animator 或 Controller 不存在时只跳过动画，不影响位置同步。
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        // Animator 控制器需要包含名为 State（整数）和 Speed（浮点数）的参数。
        animator.SetInteger("State", snapshot.State);
        animator.SetFloat("Speed", snapshot.Speed);
    }
}
