/// <summary>普通移动状态。</summary>
public sealed class PlayerMoveState : PlayerGroundedState
{
    public override PlayerStateType Type => PlayerStateType.Move;
    public override float MovementSpeed => Machine.Settings.WalkSpeed;

    /// <summary>创建移动状态，复用地面切换规则。</summary>
    public PlayerMoveState(PlayerFsmCore machine) : base(machine) { }
}
