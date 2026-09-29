/// <summary>按住奔跑键时使用奔跑速度。</summary>
public sealed class PlayerRunState : PlayerGroundedState
{
    public override PlayerStateType Type => PlayerStateType.Run;
    public override float MovementSpeed => Machine.Settings.RunSpeed;

    /// <summary>创建奔跑状态，复用地面切换规则。</summary>
    public PlayerRunState(PlayerFsmCore machine) : base(machine) { }
}
