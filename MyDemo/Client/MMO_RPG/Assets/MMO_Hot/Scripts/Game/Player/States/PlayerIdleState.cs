/// <summary>待机状态；水平目标速度为零。</summary>
public sealed class PlayerIdleState : PlayerGroundedState
{
    public override PlayerStateType Type => PlayerStateType.Idle;

    /// <summary>创建待机状态，复用地面切换规则。</summary>
    public PlayerIdleState(PlayerFsmCore machine) : base(machine) { }
}
