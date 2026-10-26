using System;

/// <summary>本地出招规则：普攻短缓存、技能冷却和一次滑行；不依赖输入设备、动画或网络。</summary>
public sealed class PlayerCombatCore
{
    private readonly AttackComboCore combo; // 当前玩家独立的普攻连击计时。
    private readonly double[] readyAt = new double[21]; // 各动作下次可用的本地秒数。
    private double clock; // 仅由 Tick 推进的本地动作时钟。
    private double startedAt; // 当前动作的本地起手时间。
    private float lastElapsed; // 正常收招后保留最后进度，供表现层完成淡出。

    public PlayerActionId ActionId { get; private set; } // 当前控制角色的动作；None 表示可移动。
    public uint Generation { get; private set; } // 起手或显式取消时递增，用于匹配预测与服务器响应。
    public bool IsBusy => ActionId != PlayerActionId.None;
    public bool IsAttacking => PlayerActionRules.IsAttack(ActionId);
    public int ComboStep => combo.CurrentStep;
    public float ElapsedSeconds => IsBusy ? (float)(clock - startedAt) : lastElapsed;
    public bool CanTurn => IsAttacking && ElapsedSeconds < PlayerActionRules.Get(ActionId).Duration * PlayerActionRules.Get(ActionId).TurnUntil;

    public PlayerCombatCore()
    {
        var steps = new StepTiming[3]; // 连击时序与服务端共用规则，避免两端出招窗口漂移。
        for (var i = 0; i < steps.Length; i++)
        {
            var timing = PlayerActionRules.Get((PlayerActionId)(i + 1));
            steps[i] = new StepTiming(timing.Duration, timing.ChainAt, timing.RecoverAt);
        }
        combo = new AttackComboCore(steps, PlayerActionRules.ComboResetDelay, PlayerActionRules.InputBuffer);
    }

    /// <summary>同帧滑行优先，其次数字最小的技能，再处理普攻；忙碌中的技能输入不排队。</summary>
    public void Tick(float deltaTime, bool attackPressed, int skillPressed, bool slidePressed, bool canAct)
    {
        if (deltaTime <= 0f) return;
        clock += deltaTime;
        if (!canAct) { Cancel(); return; }

        if (IsAttacking)
        {
            var timing = PlayerActionRules.Get(ActionId);
            if (ElapsedSeconds >= timing.Duration * timing.RecoverAt && TrySpecial(skillPressed, slidePressed)) return;
            var step = combo.Tick(deltaTime, attackPressed, true);
            if (step != 0) { Start((PlayerActionId)step); return; }
            if (combo.IsAttacking) return;
            Finish();
            TrySpecial(skillPressed, slidePressed);
            return;
        }
        else if (IsBusy)
        {
            if (ElapsedSeconds < PlayerActionRules.Get(ActionId).Duration * PlayerActionRules.Get(ActionId).RecoverAt) return;
            Finish();
        }
        if (TrySpecial(skillPressed, slidePressed)) return;
        var started = combo.Tick(deltaTime, attackPressed, true);
        if (started != 0) Start((PlayerActionId)started);
    }

    public float CooldownRemaining(PlayerActionId id) => (float)Math.Max(0d, readyAt[(int)id] - clock);

    /// <summary>失控、离地和回正取消动作及连击资格，已消耗的技能冷却不返还。</summary>
    public void Cancel()
    {
        if (IsBusy || combo.CurrentStep != 0) Generation = unchecked(Generation + 1);
        ActionId = PlayerActionId.None;
        lastElapsed = 0f;
        combo.Reset();
    }

    private void StartSpecial(PlayerActionId id)
    {
        combo.Reset();
        Start(id);
        readyAt[(int)id] = clock + PlayerActionRules.Get(id).Cooldown;
    }

    private bool TrySpecial(int skillPressed, bool slidePressed)
    {
        if (slidePressed && clock >= readyAt[(int)PlayerActionId.Slide]) { StartSpecial(PlayerActionId.Slide); return true; }
        if (skillPressed < 1 || skillPressed > 4) return false;
        var skill = (PlayerActionId)(10 + skillPressed);
        if (clock < readyAt[(int)skill]) return false;
        StartSpecial(skill);
        return true;
    }

    private void Start(PlayerActionId id)
    {
        ActionId = id;
        startedAt = clock;
        lastElapsed = 0f;
        Generation = unchecked(Generation + 1);
    }

    private void Finish()
    {
        lastElapsed = ElapsedSeconds;
        ActionId = PlayerActionId.None;
    }
}
