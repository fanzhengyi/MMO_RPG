using UnityEngine;

/// <summary>将角色结果写入 Animator；动画不反过来决定游戏状态。</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class PlayerAnimator : MonoBehaviour
{
    public const string StateParameter = "State";
    public const string SpeedParameter = "Speed";
    public const string ComboParameter = "ComboStep";
    public const string SkillParameter = "SkillIndex";

    [SerializeField] private PlayerController player;
    [SerializeField] private Animator animator;
    [SerializeField, Min(0f)] private float speedDampTime = 0.08f;

    private static readonly int StateHash = Animator.StringToHash(StateParameter);
    private static readonly int SpeedHash = Animator.StringToHash(SpeedParameter);
    private static readonly int ComboHash = Animator.StringToHash(ComboParameter);
    private static readonly int SkillHash = Animator.StringToHash(SkillParameter);

    /// <summary>自动查找角色和模型上的 Animator，并关闭根运动。</summary>
    private void Awake()
    {
        if (player == null)
            player = GetComponentInParent<PlayerController>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (animator != null)
            animator.applyRootMotion = false;
    }

    /// <summary>检查必需参数，配置有误时只报告一次。</summary>
    private void Start()
    {
        if (player == null || animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogError("PlayerAnimator: 请指定 PlayerController、Animator 和 Animator Controller。", this);
            enabled = false;
            return;
        }

        if (!HasParameter(StateHash, AnimatorControllerParameterType.Int)
            || !HasParameter(SpeedHash, AnimatorControllerParameterType.Float)
            || !HasParameter(ComboHash, AnimatorControllerParameterType.Int)
            || !HasParameter(SkillHash, AnimatorControllerParameterType.Int))
        {
            Debug.LogError("PlayerAnimator: 需要 State(int)、Speed(float)、ComboStep(int)、SkillIndex(int)。", this);
            enabled = false;
        }
    }

    /// <summary>在角色 Update 之后、动画求值之前同步当前状态和实际速度。</summary>
    private void Update()
    {
        animator.SetInteger(StateHash, (int)player.State);
        animator.SetInteger(ComboHash, player.CurrentAttackStep);
        animator.SetInteger(SkillHash, player.CurrentSkillIndex);
        animator.SetFloat(SpeedHash, player.HorizontalSpeed, speedDampTime, Time.deltaTime);
    }

    /// <summary>校验参数名和类型，避免 Animator 持续输出配置错误。</summary>
    private bool HasParameter(int hash, AnimatorControllerParameterType type)
    {
        foreach (var parameter in animator.parameters)
        {
            if (parameter.nameHash == hash && parameter.type == type)
                return true;
        }
        return false;
    }
}
