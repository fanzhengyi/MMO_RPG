/// <summary>人物普通移动状态。</summary>
public sealed class PlayerMoveState : PlayerState
{
    /// <summary>步行状态。</summary>
    public override PlayerFsmCore.StateType Type => PlayerFsmCore.StateType.Move;
    /// <summary>步行目标水平速度，单位米/秒。</summary>
    public override float MovementSpeed => Fsm.WalkSpeed;

    /// <summary>创建普通移动状态。</summary>
    public PlayerMoveState(PlayerFsmCore fsm) : base(fsm) { }

    /// <summary>根据输入切换待机、奔跑、跳跃或下落。</summary>
    public override void Tick()
    {
        TickGroundState();
    }
}
