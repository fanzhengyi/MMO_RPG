/// <summary>创建并切换人物的移动状态。</summary>
public sealed class PlayerFsmCore
{
    public enum StateType
    {
        /// <summary>站立待机；这些数值对接本地 Animator，不能当作网络协议枚举直接发送。</summary>
        Idle = 0,
        /// <summary>普通步行。</summary>
        Move = 1,
        /// <summary>起跳或上升。</summary>
        Jump = 2,
        /// <summary>奔跑；协议中的 Run 值不同，发送前经过 PlayerStateMapping 转换。</summary>
        Run = 6,
        /// <summary>离地后下落。</summary>
        Fall = 7
    }

    /// <summary>当前激活的移动状态类型。</summary>
    public StateType State => current.Type;
    /// <summary>当前状态要求的目标水平速度，单位米/秒；实际碰撞后速度由控制器另算。</summary>
    public float MovementSpeed { get; private set; }
    /// <summary>只在当前 Tick 有效的起跳请求，控制器据此施加竖直速度。</summary>
    public bool JumpRequested { get; private set; }
    /// <summary>本帧输入快照，供各状态统一读取。</summary>
    public PlayerInputSource.Frame Input { get; private set; }
    /// <summary>本轮状态更新时胶囊是否接触地面。</summary>
    public bool IsGrounded { get; private set; }
    /// <summary>控制器提供的当前竖直速度，单位米/秒，用于区分上升和下落。</summary>
    public float VerticalVelocity { get; private set; }
    /// <summary>配置的步行速度，单位米/秒。</summary>
    public float WalkSpeed { get; }
    /// <summary>配置的奔跑速度，单位米/秒。</summary>
    public float RunSpeed { get; }

    /// <summary>预创建并复用的待机状态实例。</summary>
    public PlayerIdleState Idle { get; }
    /// <summary>预创建并复用的步行状态实例。</summary>
    public PlayerMoveState Move { get; }
    /// <summary>预创建并复用的奔跑状态实例。</summary>
    public PlayerRunState Run { get; }
    /// <summary>预创建并复用的跳跃状态实例。</summary>
    public PlayerJumpState Jump { get; }
    /// <summary>预创建并复用的下落状态实例。</summary>
    public PlayerFallState Fall { get; }

    /// <summary>当前正在处理 Tick 的状态对象。</summary>
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
