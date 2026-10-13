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
        // 未落地时继续保持下落状态。
        if (!Fsm.IsGrounded)
            return;

        TickGroundState();
    }
}
