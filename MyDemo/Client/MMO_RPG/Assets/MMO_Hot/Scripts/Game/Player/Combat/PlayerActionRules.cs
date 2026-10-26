using System;

/// <summary>稳定的战斗动作编号；与协议 PlayerActionKind 一致，不使用 Animator 状态编号。</summary>
public enum PlayerActionId
{
    None = 0,
    Attack01 = 1,
    Attack02 = 2,
    Attack03 = 3,
    Skill01 = 11,
    Skill02 = 12,
    Skill03 = 13,
    Skill04 = 14,
    Slide = 20,
}

/// <summary>客户端和服务端共用的动作时序，不依赖 Unity、网络框架或动画素材。</summary>
public static class PlayerActionRules
{
    /// <summary>一次提前输入最多保留的秒数。</summary>
    public const float InputBuffer = 0.25f;
    /// <summary>动作层切入和切出的过渡秒数。</summary>
    public const float Blend = 0.08f;
    /// <summary>普攻恢复后仍能续段的秒数，超时从第一段重新开始。</summary>
    public const float ComboResetDelay = 0.35f;
    /// <summary>滑行的水平速度，单位米/秒；仍由胶囊碰撞和原有移动校验约束。</summary>
    public const float SlideSpeed = 7.5f;
    /// <summary>服务器容忍相邻命令到达抖动的毫秒数，不接受客户端自报开始时间。</summary>
    public const int TimingToleranceMs = 80;

    /// <summary>完整时长和冷却以秒计，其余时间点为完整动画中的归一化进度。</summary>
    public readonly struct Timing
    {
        /// <summary>完整动画按播放倍率调整后的时长，单位秒。</summary>
        public float Duration { get; }
        /// <summary>普通攻击最早可接下一段的位置，范围 0 到 1。</summary>
        public float ChainAt { get; }
        /// <summary>恢复移动、技能和滑行输入的位置，范围 0 到 1。</summary>
        public float RecoverAt { get; }
        /// <summary>从起手算起的再次施放间隔，单位秒。</summary>
        public float Cooldown { get; }
        /// <summary>普通攻击起手可调整朝向的截止进度，范围 0 到 1。</summary>
        public float TurnUntil { get; }
        /// <summary>完整动作时长，换算为服务器使用的整数毫秒。</summary>
        public int DurationMs => ToMilliseconds(Duration);
        /// <summary>最早接段时间，距离起手的整数毫秒。</summary>
        public int ChainAtMs => ToMilliseconds(Duration * ChainAt);
        /// <summary>恢复控制时间，距离起手的整数毫秒。</summary>
        public int RecoverAtMs => ToMilliseconds(Duration * RecoverAt);
        /// <summary>再次施放间隔，换算为整数毫秒。</summary>
        public int CooldownMs => ToMilliseconds(Cooldown);

        public Timing(float duration, float chainAt, float recoverAt, float cooldown, float turnUntil)
        {
            Duration = duration;
            ChainAt = chainAt;
            RecoverAt = recoverAt;
            Cooldown = cooldown;
            TurnUntil = turnUntil;
        }
    }

    /// <summary>一条外部动作命令的规则结果；旧命令和失败命令不会产生新动作版本。</summary>
    public enum Decision { Accepted, Ignored, InvalidAction, Busy, Cooldown }

    /// <summary>只接受协议定义的具体动作，None 专门用于显式取消。</summary>
    public static bool IsDefined(PlayerActionId id) => id == PlayerActionId.None || IsAttack(id)
        || id >= PlayerActionId.Skill01 && id <= PlayerActionId.Skill04 || id == PlayerActionId.Slide;

    public static bool IsAttack(PlayerActionId id) => id >= PlayerActionId.Attack01 && id <= PlayerActionId.Attack03;

    /// <summary>动作节奏唯一来源；Unity 用 Clip.length / Duration 计算实际 Animator 播放倍率。</summary>
    public static Timing Get(PlayerActionId id)
    {
        switch (id)
        {
            case PlayerActionId.None: return new Timing(0f, 0f, 0f, 0f, 0f);
            case PlayerActionId.Attack01: return new Timing(1.275f, 0.65f, 0.85f, 0f, 0.15f);
            case PlayerActionId.Attack02: return new Timing(1.157f, 0.65f, 0.85f, 0f, 0.15f);
            case PlayerActionId.Attack03: return new Timing(1.389f, 0.62f, 0.82f, 0f, 0.15f);
            case PlayerActionId.Skill01: return new Timing(2f, 0.9f, 0.9f, 4f, 0f);
            case PlayerActionId.Skill02: return new Timing(2.1f, 0.9f, 0.9f, 6f, 0f);
            case PlayerActionId.Skill03: return new Timing(2.2f, 0.9f, 0.9f, 8f, 0f);
            case PlayerActionId.Skill04: return new Timing(1.2f, 0.95f, 0.95f, 10f, 0f);
            case PlayerActionId.Slide: return new Timing(0.65f, 1f, 1f, 1.2f, 0f);
            default: throw new ArgumentOutOfRangeException(nameof(id), id, "未定义的角色动作。");
        }
    }

    /// <summary>消费新的命令序号后验证时序；失败不更新动作、冷却和版本。</summary>
    public static Decision Process(PlayerActionState state, uint sequence, PlayerActionId id,
        float rotationY, uint cancelSequence, long now)
    {
        if (state.HasRequestSequence && unchecked((int)(sequence - state.LastRequestSequence)) <= 0)
            return Decision.Ignored;
        state.HasRequestSequence = true;
        state.LastRequestSequence = sequence;
        if (!IsDefined(id) || float.IsNaN(rotationY) || float.IsInfinity(rotationY))
            return Decision.InvalidAction;

        if (id == PlayerActionId.None)
        {
            if (state.Kind == PlayerActionId.None || state.Sequence != cancelSequence)
                return Decision.Ignored;
            state.Sequence = sequence;
            state.Cancel(now);
            return Decision.Accepted;
        }

        // current：上次接受动作的完整时序，用来计算接段、恢复和连击重置点。
        var current = Get(state.Kind);
        // elapsed：服务端从上次起手到当前收包经过的毫秒，不相信客户端时钟。
        var elapsed = now - state.StartedAtServerTimeMs;
        // nextAttack：仍在续段窗口内时只能执行顺序中的下一段。
        var nextAttack = IsAttack(state.Kind)
            && elapsed <= current.RecoverAtMs + ToMilliseconds(ComboResetDelay) + TimingToleranceMs
            ? (PlayerActionId)((int)state.Kind % 3 + 1) : PlayerActionId.Attack01;
        if (IsAttack(id) && id != nextAttack) return Decision.InvalidAction;

        if (state.Kind != PlayerActionId.None && elapsed < current.RecoverAtMs - TimingToleranceMs)
        {
            // 只允许普通攻击在接段点续接；技能和滑行不提前取消现有动作。
            if (!IsAttack(state.Kind) || !IsAttack(id) || elapsed < current.ChainAtMs - TimingToleranceMs)
                return Decision.Busy;
        }
        if (now < state.ReadyAt[(int)id]) return Decision.Cooldown;

        state.Kind = id;
        state.Sequence = sequence;
        state.Version = unchecked(state.Version + 1);
        state.StartedAtServerTimeMs = now;
        state.RotationY = (rotationY % 360f + 360f) % 360f;
        state.ReadyAt[(int)id] = now + Get(id).CooldownMs;
        return Decision.Accepted;
    }

    private static int ToMilliseconds(float seconds)
        => (int)Math.Round(seconds * 1000d, MidpointRounding.AwayFromZero);
}

/// <summary>单个在线角色的动作运行状态；每名角色独立拥有，不持久化到角色数据库。</summary>
public sealed class PlayerActionState
{
    /// <summary>当前或最近一次接受的动作，恢复后仍保留以决定下一段普攻。</summary>
    public PlayerActionId Kind;
    /// <summary>触发当前确认动作的客户端命令序号。</summary>
    public uint Sequence;
    /// <summary>服务端确认动作的递增版本，取消也会产生新版本。</summary>
    public uint Version;
    /// <summary>当前确认动作的服务端起手时间，单位单调时钟毫秒。</summary>
    public long StartedAtServerTimeMs;
    /// <summary>当前动作起手时的世界 Y 轴朝向，单位度。</summary>
    public float RotationY;
    /// <summary>起手时服务端确认的世界 X 坐标，供世界特效恢复原始施放点。</summary>
    public float OriginX;
    /// <summary>起手时服务端确认的世界 Y 坐标。</summary>
    public float OriginY;
    /// <summary>起手时服务端确认的世界 Z 坐标。</summary>
    public float OriginZ;
    /// <summary>最近消费的命令序号，包含被拒命令，用于拒绝重复与乱序请求。</summary>
    public uint LastRequestSequence;
    /// <summary>是否已消费过命令，区分初始零与序号回绕到零。</summary>
    public bool HasRequestSequence;
    /// <summary>按稳定动作编号索引的冷却结束时间，单位服务端单调时钟毫秒。</summary>
    internal readonly long[] ReadyAt = new long[21];

    /// <summary>只中断动作和连击；不清除已消耗的技能冷却。</summary>
    public void Cancel(long now)
    {
        Kind = PlayerActionId.None;
        Version = unchecked(Version + 1);
        StartedAtServerTimeMs = now;
    }

    /// <summary>更换世界会话时移除旧序号、动作、连击和冷却。</summary>
    public void Reset()
    {
        Kind = PlayerActionId.None;
        Sequence = Version = LastRequestSequence = 0;
        StartedAtServerTimeMs = 0;
        RotationY = 0f;
        OriginX = OriginY = OriginZ = 0f;
        HasRequestSequence = false;
        Array.Clear(ReadyAt, 0, ReadyAt.Length);
    }
}
