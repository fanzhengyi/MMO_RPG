/// <summary>人物 FSM 状态的基础类。</summary>
public abstract class PlayerState
{
    protected readonly PlayerFsmCore Fsm;

    public abstract PlayerFsmCore.StateType Type { get; }
    public abstract float MovementSpeed { get; }

    /// <summary>保存所属状态机。</summary>
    protected PlayerState(PlayerFsmCore fsm)
    {
        Fsm = fsm;
    }

    /// <summary>进入状态时调用。</summary>
    public virtual void Enter() { }

    /// <summary>离开状态时调用。</summary>
    public virtual void Exit() { }

    /// <summary>每帧更新状态逻辑。</summary>
    public abstract void Tick();

    /// <summary>处理地面移动、跳跃和离地切换。</summary>
    protected void TickGroundState()
    {
        if (!Fsm.IsGrounded)
            Fsm.ChangeState(Fsm.Fall);
        else if (Fsm.Input.JumpPressed)
            Fsm.ChangeState(Fsm.Jump);
        else if (Fsm.Input.Move.sqrMagnitude <= 0.001f)
            Fsm.ChangeState(Fsm.Idle);
        else if (Fsm.Input.RunHeld)
            Fsm.ChangeState(Fsm.Run);
        else
            Fsm.ChangeState(Fsm.Move);
    }

    /// <summary>空中依输入保留走路或奔跑速度。</summary>
    protected float AirMovementSpeed
    {
        get
        {
            if (Fsm.Input.Move.sqrMagnitude <= 0.001f)
                return 0f;
            return Fsm.Input.RunHeld ? Fsm.RunSpeed : Fsm.WalkSpeed;
        }
    }
}
