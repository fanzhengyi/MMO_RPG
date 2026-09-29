/// <summary>四个技能共用施法生命周期，各自使用独立时长和冷却。</summary>
public sealed class PlayerSkillState : PlayerActionState
{
    public override PlayerStateType Type => PlayerStateType.Skill;

    /// <summary>创建技能状态。</summary>
    public PlayerSkillState(PlayerFsmCore machine) : base(machine) { }

    /// <summary>读取已通过冷却检查的技能并开始施放。</summary>
    public override void Enter()
    {
        var index = Machine.CurrentSkillIndex;
        BeginAction(Machine.Settings.Skills[index - 1], index);
    }

    /// <summary>到表现点触发事件，施法结束后恢复移动。</summary>
    public override void Tick(float deltaTime)
    {
        if (Advance(deltaTime) && Elapsed >= Action.Duration)
            Machine.ReturnToLocomotion();
    }

    /// <summary>清除当前技能编号，冷却继续由状态机维护。</summary>
    public override void Exit()
    {
        base.Exit();
        Machine.CurrentSkillIndex = 0;
    }
}
