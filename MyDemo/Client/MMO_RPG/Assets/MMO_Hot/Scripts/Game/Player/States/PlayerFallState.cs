/// <summary>人物下落状态。</summary>
public sealed class PlayerFallState : PlayerState
{
    public override PlayerFsmCore.StateType Type => PlayerFsmCore.StateType.Fall;
    public override float MovementSpeed => AirMovementSpeed;

    /// <summary>创建下落状态。</summary>
    public PlayerFallState(PlayerFsmCore fsm) : base(fsm) { }

    /// <summary>落地后回到地面移动状态，落地瞬间可起跳。</summary>
    public override void Tick()
    {
        //还没有落地的时候就一直报错落地状态
        if (!Fsm.IsGrounded)
            return;

        //落地后
        if (Fsm.Input.JumpPressed)
            Fsm.ChangeState(Fsm.Jump);
        else
            TickGroundState();
    }
}
