/// <summary>创建并切换人物的移动状态。</summary>
public sealed class PlayerFsmCore
{
    public enum StateType
    {
        Idle = 0,
        Move = 1,
        Jump = 2,
        Run = 6,
        Fall = 7
    }

    public StateType State => current.Type;
    public float MovementSpeed { get; private set; }
    public bool JumpRequested { get; private set; }
    public PlayerInputSource.Frame Input { get; private set; }
    public bool IsGrounded { get; private set; }
    public float VerticalVelocity { get; private set; }
    public float WalkSpeed { get; }
    public float RunSpeed { get; }

    public PlayerIdleState Idle { get; }
    public PlayerMoveState Move { get; }
    public PlayerRunState Run { get; }
    public PlayerJumpState Jump { get; }
    public PlayerFallState Fall { get; }

    private PlayerState current;

    /// <summary>创建并保存五种状态实例。</summary>
    public PlayerFsmCore(float walkSpeed, float runSpeed)
    {
        WalkSpeed = walkSpeed;
        RunSpeed = runSpeed;
        Idle = new PlayerIdleState(this);
        Move = new PlayerMoveState(this);
        Run = new PlayerRunState(this);
        Jump = new PlayerJumpState(this);
        Fall = new PlayerFallState(this);
        current = Idle;
        current.Enter();
    }

    /// <summary>把本帧输入和碰撞结果交给当前状态处理。</summary>
    public void Tick(PlayerInputSource.Frame input, bool grounded, float verticalVelocity)
    {
        Input = input;
        IsGrounded = grounded;
        VerticalVelocity = verticalVelocity;
        JumpRequested = false;
        current.Tick();
        MovementSpeed = current.MovementSpeed;
    }

    /// <summary>通知控制器在本帧施加起跳速度。</summary>
    internal void RequestJump()
    {
        JumpRequested = true;
    }

    /// <summary>切换状态并调用进入、离开回调。</summary>
    public void ChangeState(PlayerState next)
    {
        if (current == next)
            return;

        current.Exit();
        current = next;
        current.Enter();
    }
}
