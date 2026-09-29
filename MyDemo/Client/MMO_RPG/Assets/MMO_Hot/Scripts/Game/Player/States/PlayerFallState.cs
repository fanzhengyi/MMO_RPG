/// <summary>下落状态，包含跳跃回落和直接走出平台。</summary>
public sealed class PlayerFallState : PlayerState
{
    public override PlayerStateType Type => PlayerStateType.Fall;
    public override float MovementSpeed => Input.IsRun ? Machine.Settings.RunSpeed : Machine.Settings.WalkSpeed;

    /// <summary>创建下落状态。</summary>
    public PlayerFallState(PlayerFsmCore machine) : base(machine) { }

    /// <summary>检测落地；空中不允许再次跳跃或发起地面攻击。</summary>
    public override void Tick(float deltaTime)
    {
        if (Machine.IsGrounded)
            Machine.ReturnToLocomotion();
    }
}
