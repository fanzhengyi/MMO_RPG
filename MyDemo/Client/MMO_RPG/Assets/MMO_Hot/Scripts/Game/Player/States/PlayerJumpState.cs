/// <summary>起跳和上升状态；只在进入时申请一次起跳。</summary>
public sealed class PlayerJumpState : PlayerState
{
    public override PlayerStateType Type => PlayerStateType.Jump;
    public override float MovementSpeed => Input.IsRun ? Machine.Settings.RunSpeed : Machine.Settings.WalkSpeed;

    /// <summary>创建跳跃状态。</summary>
    public PlayerJumpState(PlayerFsmCore machine) : base(machine) { }

    /// <summary>向位移模块申请起跳，避免持续按键重复起跳。</summary>
    public override void Enter()
    {
        Machine.JumpRequested = true;
    }

    /// <summary>落地后恢复移动，到达最高点或撞顶后转为下落。</summary>
    public override void Tick(float deltaTime)
    {
        if (Machine.IsGrounded && Machine.VerticalVelocity <= 0f)
            Machine.ReturnToLocomotion();
        else if (Machine.VerticalVelocity <= 0f)
            Machine.ChangeState(PlayerStateType.Fall);
    }
}
