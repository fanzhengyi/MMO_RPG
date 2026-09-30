/// <summary>人物待机状态。</summary>
public sealed class PlayerIdleState : PlayerState
{
    public override PlayerFsmCore.StateType Type => PlayerFsmCore.StateType.Idle;
    public override float MovementSpeed => 0f;

    /// <summary>创建待机状态。</summary>
    public PlayerIdleState(PlayerFsmCore fsm) : base(fsm) { }

    /// <summary>根据输入切换移动、跳跃或下落。</summary>
    public override void Tick()
    {
        TickGroundState();
    }
}
