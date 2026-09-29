/// <summary>死亡状态；禁止操作，仍由位移模块处理重力。</summary>
public sealed class PlayerDeadState : PlayerState
{
    public override PlayerStateType Type => PlayerStateType.Dead;

    /// <summary>创建死亡状态。</summary>
    public PlayerDeadState(PlayerFsmCore machine) : base(machine) { }

    /// <summary>死亡期间忽略输入，等待外部明确调用复活。</summary>
    public override void Tick(float deltaTime) { }
}
