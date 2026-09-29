using System;

/// <summary>管理状态生命周期；不读取设备、不移动物体，也不操作 Animator。</summary>
public sealed class PlayerFsmCore
{
    public PlayerSettings Settings { get; }
    public PlayerInputFrame Input { get; private set; }
    public PlayerStateType State => current.Type;
    public bool IsGrounded { get; private set; }
    public float VerticalVelocity { get; private set; }
    public float MovementSpeed => current.MovementSpeed;
    public bool BlocksMovement => State == PlayerStateType.Attack || State == PlayerStateType.Skill || State == PlayerStateType.Dead;
    public bool JumpRequested { get; internal set; }
    public int CurrentAttackStep { get; internal set; }
    public int CurrentSkillIndex { get; internal set; }

    public event Action<PlayerStateType, PlayerStateType> StateChanged;
    public event Action<PlayerStateType, int> ActionStarted;
    public event Action<PlayerStateType, int> ActionImpact;

    private readonly PlayerState[] states;
    private readonly float[] skillCooldowns = new float[PlayerSettings.SkillCount];
    private PlayerState current;

    /// <summary>创建并复用所有状态，切换状态时不会创建新对象。</summary>
    public PlayerFsmCore(PlayerSettings settings)
    {
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Settings.Validate();
        states = new PlayerState[Enum.GetValues(typeof(PlayerStateType)).Length];
        Register(new PlayerIdleState(this));
        Register(new PlayerMoveState(this));
        Register(new PlayerRunState(this));
        Register(new PlayerJumpState(this));
        Register(new PlayerFallState(this));
        Register(new PlayerAttackState(this));
        Register(new PlayerSkillState(this));
        Register(new PlayerDeadState(this));
        current = states[(int)PlayerStateType.Idle];
        current.Enter();
    }

    /// <summary>按状态编号缓存实例，避免依赖注册顺序。</summary>
    private void Register(PlayerState state)
    {
        states[(int)state.Type] = state;
    }

    /// <summary>接收本帧输入和物理结果，推进冷却及当前状态。</summary>
    public void Tick(PlayerInputFrame input, float deltaTime, bool isGrounded, float verticalVelocity)
    {
        JumpRequested = false;
        if (deltaTime <= 0f)
            return;

        Input = input;
        IsGrounded = isGrounded;
        VerticalVelocity = verticalVelocity;
        for (var i = 0; i < skillCooldowns.Length; i++)
            skillCooldowns[i] = Math.Max(0f, skillCooldowns[i] - deltaTime);
        current.Tick(deltaTime);
    }

    /// <summary>退出旧状态、进入新状态，再通知外部观察者。</summary>
    internal void ChangeState(PlayerStateType next)
    {
        if (State == next)
            return;

        var previous = State;
        current.Exit();
        current = states[(int)next];
        current.Enter();
        if (State == next)
            StateChanged?.Invoke(previous, next);
    }

    /// <summary>动作结束后，根据落地和移动输入恢复基础状态。</summary>
    internal void ReturnToLocomotion()
    {
        var next = !IsGrounded ? PlayerStateType.Fall
            : !Input.HasMove ? PlayerStateType.Idle
            : Input.IsRun ? PlayerStateType.Run : PlayerStateType.Move;
        ChangeState(next);
    }

    /// <summary>尝试施放指定技能；编号非法或冷却中时返回 false。</summary>
    internal bool TryStartSkill(int index)
    {
        if (index < 1 || index > skillCooldowns.Length || GetSkillCooldown(index) > 0f)
            return false;

        CurrentSkillIndex = index;
        skillCooldowns[index - 1] = Settings.Skills[index - 1].Cooldown;
        ChangeState(PlayerStateType.Skill);
        return true;
    }

    /// <summary>读取技能剩余冷却，供技能栏显示。</summary>
    public float GetSkillCooldown(int index)
    {
        if (index < 1 || index > skillCooldowns.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        return skillCooldowns[index - 1];
    }

    /// <summary>通知音效、特效和战斗适配层某段动作开始。</summary>
    internal void NotifyActionStarted(PlayerStateType type, int index)
    {
        ActionStarted?.Invoke(type, index);
    }

    /// <summary>通知表现触发点；此事件不代表服务器确认造成伤害。</summary>
    internal void NotifyActionImpact(PlayerStateType type, int index)
    {
        ActionImpact?.Invoke(type, index);
    }

    /// <summary>中断当前动作并进入死亡状态。</summary>
    public void SetDead()
    {
        JumpRequested = false;
        ChangeState(PlayerStateType.Dead);
    }

    /// <summary>复活时清理输入、动作和冷却，恢复待机。</summary>
    public void Reset()
    {
        Input = default;
        JumpRequested = false;
        CurrentAttackStep = 0;
        CurrentSkillIndex = 0;
        Array.Clear(skillCooldowns, 0, skillCooldowns.Length);
        ChangeState(PlayerStateType.Idle);
    }
}
