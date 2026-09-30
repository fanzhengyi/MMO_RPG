/// <summary>人物上升跳跃状态。</summary>
public sealed class PlayerJumpState : PlayerState
{
    public override PlayerFsmCore.StateType Type => PlayerFsmCore.StateType.Jump;
    public override float MovementSpeed => AirMovementSpeed;

    /// <summary>创建跳跃状态。</summary>
    public PlayerJumpState(PlayerFsmCore fsm) : base(fsm) { }

    /// <summary>起跳时通知控制器施加跳跃速度。</summary>
    public override void Enter()
    {
        Fsm.RequestJump();
    }

    /// <summary>到达跳跃顶点后转入下落。</summary>
    public override void Tick()
    {
        if (Fsm.VerticalVelocity <= 0f)
            Fsm.ChangeState(Fsm.Fall);
    }
}
