using UnityEngine;

/// <summary>将连击规则映射到独立 Combat 动画层，不依赖移动 FSM 或网络协议。</summary>
public sealed class PlayerAttackController
{
    /// <summary>攻击动画播放倍率参数的哈希。</summary>
    private static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed");
    /// <summary>攻击段号参数的哈希，使用从 1 开始的段号。</summary>
    private static readonly int ComboStep = Animator.StringToHash("ComboStep");
    /// <summary>接收攻击动画指令的角色 Animator。</summary>
    private readonly Animator animator;
    /// <summary>共享的攻击配置，仅保存参数，不保存该角色的当前进度。</summary>
    private readonly AttackComboDefinition definition;
    /// <summary>该角色独有的连击计时和输入缓冲状态。</summary>
    private readonly AttackComboCore combo;
    /// <summary>Animator 中 Combat 层的索引，构造时查找一次。</summary>
    private readonly int layer;
    /// <summary>Combat.Attack1 等完整状态名的哈希，按段号减一索引。</summary>
    private readonly int[] states;
    /// <summary>当前攻击层权重，0 表示不影响姿势，1 表示完全使用该层。</summary>
    private float layerWeight;

    /// <summary>当前是否处于攻击控制限制阶段。</summary>
    public bool IsAttacking => combo.IsAttacking;
    /// <summary>当前或刚完成的段号；0 表示没有连击记录。</summary>
    public int CurrentStep => combo.CurrentStep;
    /// <summary>仅在出招初期允许按移动输入调整朝向。</summary>
    public bool CanTurn => IsAttacking && combo.NormalizedTime < definition.GetStep(CurrentStep - 1).TurnUntil;

    public PlayerAttackController(Animator animator, AttackComboDefinition definition)
    {
        this.animator = animator;
        this.definition = definition;
        combo = definition.CreateRuntime();
        layer = animator.GetLayerIndex("Combat");
        states = new int[definition.StepCount];
        // i：数组当前处理的下标，从 0 开始。
        for (var i = 0; i < states.Length; i++)
            states[i] = Animator.StringToHash($"Combat.Attack{i + 1}");
        animator.SetLayerWeight(layer, 0f);
    }

    // 先让纯逻辑核心决定是否新出一段，再播放相应状态并平滑调整 Combat 权重；当前不发送攻击协议或结算伤害。
    public void Tick(bool attackPressed, bool canAttack, float deltaTime)
    {
        // started：这一帧新开始的攻击段号；0 表示没有新出招。
        var started = combo.Tick(deltaTime, attackPressed, canAttack);
        if (started != 0)
        {
            animator.SetInteger(ComboStep, started);
            animator.SetFloat(AttackSpeed, definition.GetStep(started - 1).PlaybackSpeed);
            animator.CrossFadeInFixedTime(states[started - 1], definition.BlendIn, layer, 0f);
        }

        // targetWeight：Combat 层要达到的权重：攻击时 1，结束后 0。
        var targetWeight = IsAttacking ? 1f : 0f;
        // blend：当前切入或淡出攻击层使用的过渡时长，单位秒。
        var blend = IsAttacking ? definition.BlendIn : definition.BlendOut;
        layerWeight = Mathf.MoveTowards(layerWeight, targetWeight, deltaTime / blend);
        animator.SetLayerWeight(layer, layerWeight);
    }

    /// <summary>失去控制、回正或停用时立即解除动作和已缓存的输入。</summary>
    public void Reset()
    {
        combo.Reset();
        layerWeight = 0f;
        animator.SetLayerWeight(layer, 0f);
        animator.SetInteger(ComboStep, 0);
    }
}
