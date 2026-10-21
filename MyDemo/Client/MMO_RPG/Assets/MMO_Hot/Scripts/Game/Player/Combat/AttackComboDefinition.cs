using System;
using UnityEngine;

/// <summary>普攻的动画、播放速度和接段窗口；不保存运行中的连击状态。</summary>
[CreateAssetMenu(menuName = "Game/Combat/Attack Combo", fileName = "PlayerAttackCombo")]
public sealed class AttackComboDefinition : ScriptableObject
{
    [Serializable]
    public sealed class Step
    {
        /// <summary>该段使用的攻击动画素材。</summary>
        public AnimationClip Clip;
        /// <summary>动画播放倍率；1 为原速，1.7 表示以原速的 1.7 倍播放。</summary>
        [Min(0.1f)] public float PlaybackSpeed = 1.7f;
        /// <summary>最早接下一段的动画进度；0.65 表示播放到 65%，不是 0.65 秒。</summary>
        [Tooltip("挥击完成后可接下一段的归一化进度。")]
        [Range(0f, 1f)] public float ChainAt = 0.65f;
        /// <summary>未接下一段时解除攻击限制、恢复移动的动画进度。</summary>
        [Tooltip("没有续段时，恢复移动并淡出攻击的归一化进度。")]
        [Range(0f, 1f)] public float RecoverAt = 0.85f;
        /// <summary>允许按输入转向的截止进度；超过该进度后锁定出招朝向。</summary>
        [Tooltip("起手阶段允许按移动输入调整朝向的归一化进度。")]
        [Range(0f, 1f)] public float TurnUntil = 0.15f;
    }

    /// <summary>按出招顺序排列的各段配置；数组下标 0 对应第一段。</summary>
    [SerializeField] private Step[] steps;
    /// <summary>恢复移动后仍可续接下一段的秒数；超时后从第一段重来。</summary>
    [Tooltip("恢复控制后仍可接下一段的秒数；超过后重新从第一段开始。")]
    [SerializeField, Min(0f)] private float resetDelay = 0.35f;
    /// <summary>一次提前点击最多保留的秒数，用于改善接段手感。</summary>
    [Tooltip("提前点击可等待接段点的秒数；最多缓存一次。")]
    [SerializeField, Min(0f)] private float inputBuffer = 0.25f;
    /// <summary>切入攻击动画与提升攻击层权重的过渡时间，单位秒。</summary>
    [SerializeField, Min(0.01f)] private float blendIn = 0.08f;
    /// <summary>攻击结束后淡出 Combat 层的时间，单位秒。</summary>
    [SerializeField, Min(0.01f)] private float blendOut = 0.08f;

    public Step GetStep(int index) => steps[index];
    /// <summary>连击总段数。</summary>
    public int StepCount => steps.Length;
    /// <summary>恢复控制后允许续段的时间，单位秒。</summary>
    public float ResetDelay => resetDelay;
    /// <summary>切入攻击动画的过渡时间，单位秒。</summary>
    public float BlendIn => blendIn;
    /// <summary>淡出攻击动画层的时间，单位秒。</summary>
    public float BlendOut => blendOut;

    // 配置资产可共享，但连击计时不能共享；为每个角色生成独立核心，并将动画长度除以播放倍率得到秒数。
    public AttackComboCore CreateRuntime()
    {
        // timings：每段动画长度按播放倍率换算后的运行时时序数组。
        var timings = new StepTiming[steps.Length];
        // i：数组当前处理的下标，从 0 开始。
        for (var i = 0; i < steps.Length; i++)
        {
            // step：当前数组下标对应的一段攻击配置。
            var step = steps[i];
            timings[i] = new StepTiming(step.Clip.length / step.PlaybackSpeed, step.ChainAt, step.RecoverAt);
        }
        return new AttackComboCore(timings, resetDelay, inputBuffer);
    }
}
