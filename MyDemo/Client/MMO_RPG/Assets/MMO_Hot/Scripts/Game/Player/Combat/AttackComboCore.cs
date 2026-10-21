using System;

/// <summary>一段普通攻击的动画时长、接段点与恢复控制的时刻。</summary>
public readonly struct StepTiming
{
    /// <summary>该段动画按播放速度换算后的完整时长，单位秒；不是动画原始长度。</summary>
    public float Duration { get; }
    /// <summary>最早可接下一段的进度，范围 0 到 1；例如 0.65 表示完整时长的 65%。</summary>
    public float ChainAtNormalized { get; }
    /// <summary>无续段时恢复控制的进度，范围 0 到 1；可早于动画播到最后一帧。</summary>
    public float RecoverAtNormalized { get; }

    public StepTiming(float duration, float chainAtNormalized, float recoverAtNormalized)
    {
        Duration = duration;
        ChainAtNormalized = chainAtNormalized;
        RecoverAtNormalized = recoverAtNormalized;
    }
}

/// <summary>管理三段或多段连击的时序与单次输入缓冲，不依赖动画、输入设备或网络。</summary>
public sealed class AttackComboCore
{
    /// <summary>各段时序的私有副本，下标从 0 开始；对外段号从 1 开始。</summary>
    private readonly StepTiming[] steps;
    /// <summary>动作恢复后保留下一段资格的秒数；超过后再次点击从第一段开始。</summary>
    private readonly double resetDelay;
    /// <summary>提前点击最多能保留多少秒；只缓存一次，不积攒多段攻击。</summary>
    private readonly double inputBufferSeconds;
    /// <summary>连击内部累计时间，单位秒，仅由 Tick 的 deltaTime 推进。</summary>
    private double clock;
    /// <summary>当前段开始时的内部时间，单位秒。</summary>
    private double stepStartedAt;
    /// <summary>最近一段恢复控制的内部时间，用于计算连击重置倒计时。</summary>
    private double stepCompletedAt;
    /// <summary>是否存在一次尚未消费的提前点击。</summary>
    private bool bufferedAttack;
    /// <summary>提前点击失效时的内部时间，超过后不能自动接段。</summary>
    private double bufferedUntil;

    /// <summary>当前或刚完成的段号；续段时间耗尽后为零。</summary>
    public int CurrentStep { get; private set; }
    /// <summary>当前是否处在出招到恢复点之间；恢复后仍可能短暂保留下一段资格。</summary>
    public bool IsAttacking { get; private set; }
    /// <summary>当前段进度，范围 0 到 1；动画层据此判断是否仍允许调整朝向。</summary>
    public float NormalizedTime => CurrentStep == 0 ? 0f : IsAttacking
        ? (float)Math.Min((clock - stepStartedAt) / steps[CurrentStep - 1].Duration, 1d)
        : steps[CurrentStep - 1].RecoverAtNormalized;

    public AttackComboCore(StepTiming[] steps, float resetDelay, float inputBufferSeconds)
    {
        if (steps == null || steps.Length == 0)
            throw new ArgumentException("至少配置一段攻击。", nameof(steps));
        // step：正在校验的某一段攻击时序配置。
        foreach (var step in steps)
        {
            if (float.IsNaN(step.Duration) || float.IsInfinity(step.Duration) || step.Duration <= 0f
                || float.IsNaN(step.ChainAtNormalized) || step.ChainAtNormalized <= 0f
                || float.IsNaN(step.RecoverAtNormalized) || step.RecoverAtNormalized > 1f
                || step.RecoverAtNormalized < step.ChainAtNormalized)
                throw new ArgumentException("攻击时长必须为有限正数，接段与恢复点须满足 0 < 接段点 <= 恢复点 <= 1。", nameof(steps));
        }
        if (float.IsNaN(resetDelay) || float.IsInfinity(resetDelay) || resetDelay < 0f)
            throw new ArgumentOutOfRangeException(nameof(resetDelay));
        if (float.IsNaN(inputBufferSeconds) || float.IsInfinity(inputBufferSeconds) || inputBufferSeconds < 0f)
            throw new ArgumentOutOfRangeException(nameof(inputBufferSeconds));
        this.steps = (StepTiming[])steps.Clone();
        this.resetDelay = resetDelay;
        this.inputBufferSeconds = inputBufferSeconds;
    }

    /// <summary>推进一次角色帧；返回这一帧新开始的段号，零表示没有新动作。</summary>
    public int Tick(float deltaTime, bool attackPressed, bool canAttack)
    {
        if (!canAttack)
        {
            Reset();
            return 0;
        }
        // 暂停时既不推进动作，也不消费输入。
        if (deltaTime <= 0f) return 0;

        // frameStartedAt：本帧推进前的连击内部时间，单位秒。
        var frameStartedAt = clock;
        clock += deltaTime;
        if (IsAttacking)
        {
            // timing：当前连击段的时长、最早接段点和恢复点。
            var timing = steps[CurrentStep - 1];
            // chainAt：当前段最早可接下一段的内部时间，单位秒。
            var chainAt = stepStartedAt + (double)timing.Duration * timing.ChainAtNormalized;
            // recoverAt：当前段恢复控制的内部时间，单位秒。
            var recoverAt = stepStartedAt + (double)timing.Duration * timing.RecoverAtNormalized;
            // 旧输入按本帧第一次可接段的时刻判定，避免低帧率跨过有效窗口。
            // consumeAt：本帧内最早能消费缓存点击的内部时间，用于避免低帧率错过接段。
            var consumeAt = Math.Max(frameStartedAt, chainAt);
            if (bufferedAttack && consumeAt <= clock && consumeAt <= recoverAt && bufferedUntil >= consumeAt)
                return StartNext();
            if (bufferedAttack && bufferedUntil < clock)
                bufferedAttack = false;

            if (clock < recoverAt)
            {
                if (attackPressed)
                {
                    if (clock >= chainAt) return StartNext();
                    bufferedAttack = true;
                    bufferedUntil = clock + inputBufferSeconds;
                }
                return 0;
            }

            IsAttacking = false;
            bufferedAttack = false;
            stepCompletedAt = recoverAt;
        }

        if (CurrentStep != 0 && clock - stepCompletedAt > resetDelay)
            CurrentStep = 0;
        return attackPressed ? StartNext() : 0;
    }

    /// <summary>离地、失控、回正或停用时丢弃动作与尚未消费的输入。</summary>
    public void Reset()
    {
        CurrentStep = 0;
        IsAttacking = false;
        bufferedAttack = false;
        bufferedUntil = 0d;
        clock = 0d;
        stepStartedAt = 0d;
        stepCompletedAt = 0d;
    }

    // 段号按 1 → 2 → 3 → 1 循环；如果续段窗口已超时，Tick 会先把 CurrentStep 清为 0。
    private int StartNext()
    {
        CurrentStep = CurrentStep % steps.Length + 1;
        IsAttacking = true;
        bufferedAttack = false;
        stepStartedAt = clock;
        return CurrentStep;
    }
}
