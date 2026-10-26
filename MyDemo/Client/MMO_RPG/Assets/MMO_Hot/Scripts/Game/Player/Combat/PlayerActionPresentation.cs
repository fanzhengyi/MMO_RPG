using UnityEngine;

/// <summary>本地与远端共用的动作动画和特效表现，不判断输入、冷却或网络身份。</summary>
public sealed class PlayerActionPresentation
{
    private static readonly int ActionSpeed = Animator.StringToHash("ActionSpeed"); // 共用的动作播放倍率参数。
    private readonly Animator animator; // 只控制 Combat 层的角色动画组件。
    private readonly PlayerCombatDefinition definition; // 动画和特效资源映射。
    private readonly PlayerActionEffects effects; // 当前角色独立拥有的特效实例。
    private readonly int layer; // Combat 层索引，装配时查找一次。
    private PlayerActionId actionId; // 正在表现的动作编号。
    private float elapsed; // 当前动作已播放秒数，续包只能向前校时。
    private float weight; // Combat 层当前权重。
    private bool finished; // 已完成正常收招，不再重复触发特效结束。

    public int ActiveEffectCount => effects.ActiveCount;

    public PlayerActionPresentation(Transform actor, Animator animator, PlayerCombatDefinition definition)
    {
        this.animator = animator;
        this.definition = definition;
        layer = animator.GetLayerIndex("Combat");
        effects = new PlayerActionEffects(actor, animator.transform, definition);
        animator.SetLayerWeight(layer, 0f);
    }

    /// <summary>一次新动作只调用一次；迟到动作直接从对应进度播放并补齐仍存活特效。</summary>
    public void Play(PlayerActionId id, float age, Vector3 castOrigin, float rotationY)
    {
        actionId = id;
        elapsed = Mathf.Max(0f, age);
        finished = false;
        var timing = PlayerActionRules.Get(id); // 两端一致的完整时长和恢复点。
        var action = definition.GetAction((int)id); // 本动作的动画与特效资源。
        var state = id == PlayerActionId.Slide ? "Slide" : PlayerActionRules.IsAttack(id)
            ? $"Attack{(int)id}" : $"Skill{(int)id - 10}";
        animator.SetFloat(ActionSpeed, action.Clip.length / timing.Duration);
        animator.CrossFadeInFixedTime(Animator.StringToHash($"Combat.{state}"), PlayerActionRules.Blend,
            layer, elapsed / timing.Duration * action.Clip.length);
        effects.Start((int)id, elapsed, timing.Duration, castOrigin, rotationY);
        if (elapsed >= timing.Duration * timing.RecoverAt) Finish();
    }

    /// <summary>进度校时不重新播放动画或生成特效；动画的自然播放由 Animator 推进。</summary>
    public void Tick(float deltaTime, float actionElapsed)
    {
        elapsed = Mathf.Max(elapsed, actionElapsed);
        if (actionId != PlayerActionId.None && !finished)
        {
            effects.Tick(elapsed);
            var timing = PlayerActionRules.Get(actionId);
            if (elapsed >= timing.Duration * timing.RecoverAt) Finish();
        }
        weight = Mathf.MoveTowards(weight, finished || actionId == PlayerActionId.None ? 0f : 1f,
            deltaTime / PlayerActionRules.Blend);
        animator.SetLayerWeight(layer, weight);
    }

    /// <summary>正常收招保留世界特效尾迹。</summary>
    public void Finish()
    {
        if (finished) return;
        finished = true;
        effects.Finish();
    }

    /// <summary>显式中断立即清除这个角色的动作层和所有特效。</summary>
    public void Cancel()
    {
        actionId = PlayerActionId.None;
        elapsed = weight = 0f;
        finished = true;
        effects.Stop();
        animator.SetLayerWeight(layer, 0f);
    }
}
