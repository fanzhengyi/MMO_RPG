using System;

/// <summary>一段普通攻击的动画时长、接段点与恢复控制的时刻。</summary>
public readonly struct StepTiming
{
    public float Duration { get; }
    public float ChainAtNormalized { get; }
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
    private readonly StepTiming[] steps;
    private readonly double resetDelay;
    private readonly double inputBufferSeconds;
    private double clock;
    private double stepStartedAt;
    private double stepCompletedAt;
    private bool bufferedAttack;
    private double bufferedUntil;

    /// <summary>当前或刚完成的段号；续段时间耗尽后为零。</summary>
    public int CurrentStep { get; private set; }
    public bool IsAttacking { get; private set; }
    public float NormalizedTime => CurrentStep == 0 ? 0f : IsAttacking
        ? (float)Math.Min((clock - stepStartedAt) / steps[CurrentStep - 1].Duration, 1d)
        : steps[CurrentStep - 1].RecoverAtNormalized;

    public AttackComboCore(StepTiming[] steps, float resetDelay, float inputBufferSeconds)
    {
        if (steps == null || steps.Length == 0)
            throw new ArgumentException("至少配置一段攻击。", nameof(steps));
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

        var frameStartedAt = clock;
        clock += deltaTime;
        if (IsAttacking)
        {
            var timing = steps[CurrentStep - 1];
            var chainAt = stepStartedAt + (double)timing.Duration * timing.ChainAtNormalized;
            var recoverAt = stepStartedAt + (double)timing.Duration * timing.RecoverAtNormalized;
            // 旧输入按本帧第一次可接段的时刻判定，避免低帧率跨过有效窗口。
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

    private int StartNext()
    {
        CurrentStep = CurrentStep % steps.Length + 1;
        IsAttacking = true;
        bufferedAttack = false;
        stepStartedAt = clock;
        return CurrentStep;
    }
}
