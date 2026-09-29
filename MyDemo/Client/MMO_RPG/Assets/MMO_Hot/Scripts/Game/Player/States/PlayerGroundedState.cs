/// <summary>待机、移动、奔跑共用的地面切换规则。</summary>
public abstract class PlayerGroundedState : PlayerState
{
    /// <summary>接收所属角色的状态机。</summary>
    protected PlayerGroundedState(PlayerFsmCore machine) : base(machine) { }

    /// <summary>依次检查离地、跳跃、技能、普攻，最后更新移动状态。</summary>
    public override void Tick(float deltaTime)
    {
        if (!Machine.IsGrounded)
            Machine.ChangeState(PlayerStateType.Fall);
        else if (Input.JumpPressed)
            Machine.ChangeState(PlayerStateType.Jump);
        else if (Input.HasSkill && Machine.TryStartSkill(Input.SkillIndex))
            return;
        else if (Input.AttackPressed)
            Machine.ChangeState(PlayerStateType.Attack);
        else
            Machine.ReturnToLocomotion();
    }
}
