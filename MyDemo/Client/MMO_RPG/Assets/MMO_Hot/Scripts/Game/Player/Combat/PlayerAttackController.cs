using UnityEngine;

/// <summary>将连击规则映射到独立 Combat 动画层，不依赖移动 FSM 或网络协议。</summary>
public sealed class PlayerAttackController
{
    private static readonly int AttackSpeed = Animator.StringToHash("AttackSpeed");
    private static readonly int ComboStep = Animator.StringToHash("ComboStep");
    private readonly Animator animator;
    private readonly AttackComboDefinition definition;
    private readonly AttackComboCore combo;
    private readonly int layer;
    private readonly int[] states;
    private float layerWeight;

    public bool IsAttacking => combo.IsAttacking;
    public int CurrentStep => combo.CurrentStep;
    public bool CanTurn => IsAttacking && combo.NormalizedTime < definition.GetStep(CurrentStep - 1).TurnUntil;

    public PlayerAttackController(Animator animator, AttackComboDefinition definition)
    {
        this.animator = animator;
        this.definition = definition;
        combo = definition.CreateRuntime();
        layer = animator.GetLayerIndex("Combat");
        states = new int[definition.StepCount];
        for (var i = 0; i < states.Length; i++)
            states[i] = Animator.StringToHash($"Combat.Attack{i + 1}");
        animator.SetLayerWeight(layer, 0f);
    }

    public void Tick(bool attackPressed, bool canAttack, float deltaTime)
    {
        var started = combo.Tick(deltaTime, attackPressed, canAttack);
        if (started != 0)
        {
            animator.SetInteger(ComboStep, started);
            animator.SetFloat(AttackSpeed, definition.GetStep(started - 1).PlaybackSpeed);
            animator.CrossFadeInFixedTime(states[started - 1], definition.BlendIn, layer, 0f);
        }

        var targetWeight = IsAttacking ? 1f : 0f;
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
