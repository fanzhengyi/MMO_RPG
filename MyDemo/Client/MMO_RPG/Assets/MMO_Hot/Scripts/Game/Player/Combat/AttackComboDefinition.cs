using System;
using UnityEngine;

/// <summary>普攻的动画、播放速度和接段窗口；不保存运行中的连击状态。</summary>
[CreateAssetMenu(menuName = "Game/Combat/Attack Combo", fileName = "PlayerAttackCombo")]
public sealed class AttackComboDefinition : ScriptableObject
{
    [Serializable]
    public sealed class Step
    {
        public AnimationClip Clip;
        [Min(0.1f)] public float PlaybackSpeed = 1.7f;
        [Tooltip("挥击完成后可接下一段的归一化进度。")]
        [Range(0f, 1f)] public float ChainAt = 0.65f;
        [Tooltip("没有续段时，恢复移动并淡出攻击的归一化进度。")]
        [Range(0f, 1f)] public float RecoverAt = 0.85f;
        [Tooltip("起手阶段允许按移动输入调整朝向的归一化进度。")]
        [Range(0f, 1f)] public float TurnUntil = 0.15f;
    }

    [SerializeField] private Step[] steps;
    [Tooltip("恢复控制后仍可接下一段的秒数；超过后重新从第一段开始。")]
    [SerializeField, Min(0f)] private float resetDelay = 0.35f;
    [Tooltip("提前点击可等待接段点的秒数；最多缓存一次。")]
    [SerializeField, Min(0f)] private float inputBuffer = 0.25f;
    [SerializeField, Min(0.01f)] private float blendIn = 0.08f;
    [SerializeField, Min(0.01f)] private float blendOut = 0.08f;

    public Step GetStep(int index) => steps[index];
    public int StepCount => steps.Length;
    public float ResetDelay => resetDelay;
    public float BlendIn => blendIn;
    public float BlendOut => blendOut;

    public AttackComboCore CreateRuntime()
    {
        var timings = new StepTiming[steps.Length];
        for (var i = 0; i < steps.Length; i++)
        {
            var step = steps[i];
            timings[i] = new StepTiming(step.Clip.length / step.PlaybackSpeed, step.ChainAt, step.RecoverAt);
        }
        return new AttackComboCore(timings, resetDelay, inputBuffer);
    }
}
