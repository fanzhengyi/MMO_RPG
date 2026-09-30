/// <summary>人物奔跑状态。</summary>
public sealed class PlayerRunState : PlayerState
{
    public override PlayerFsmCore.StateType Type => PlayerFsmCore.StateType.Run;
    public override float MovementSpeed => Fsm.RunSpeed;

    /// <summary>创建奔跑状态。</summary>
    public PlayerRunState(PlayerFsmCore fsm) : base(fsm) { }

    /// <summary>根据输入切换待机、走路、跳跃或下落。</summary>
    public override void Tick()
    {
        TickGroundState();
    }
}
