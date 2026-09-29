/// <summary>只处理待机、移动、奔跑、跳跃和下落状态。</summary>
public sealed class PlayerFsmCore
{
    // 数值与现有 Animator 条件保持一致。
    public enum StateType
    {
        Idle = 0,
        Move = 1,
        Jump = 2,
        Run = 6,
        Fall = 7
    }

    public StateType State { get; private set; } = StateType.Idle;
    public float MovementSpeed { get; private set; }
    public bool JumpRequested { get; private set; }

    /// <summary>根据输入和 CharacterController 结果更新当前状态。</summary>
    public void Tick(PlayerInputSource.Frame input, bool grounded, float verticalVelocity,
        float walkSpeed, float runSpeed)
    {
        JumpRequested = false;

        switch (State)
        {
            case StateType.Jump:
                if (verticalVelocity <= 0f)
                    ChangeState(grounded ? GroundState(input) : StateType.Fall);
                break;
            case StateType.Fall:
                if (grounded)
                {
                    if (input.JumpPressed)
                    {
                        ChangeState(StateType.Jump);
                        JumpRequested = true;
                    }
                    else
                        ChangeState(GroundState(input));
                }
                break;
            default:
                if (!grounded)
                    ChangeState(StateType.Fall);
                else if (input.JumpPressed)
                {
                    ChangeState(StateType.Jump);
                    JumpRequested = true;
                }
                else
                    ChangeState(GroundState(input));
                break;
        }

        var hasMove = input.Move.sqrMagnitude > 0.001f;
        if (State == StateType.Move)
            MovementSpeed = walkSpeed;
        else if (State == StateType.Run)
            MovementSpeed = runSpeed;
        else if ((State == StateType.Jump || State == StateType.Fall) && hasMove)
            MovementSpeed = input.RunHeld ? runSpeed : walkSpeed;
        else
            MovementSpeed = 0f;
    }

    /// <summary>地面上按方向和 Shift 选择待机、移动或奔跑。</summary>
    private static StateType GroundState(PlayerInputSource.Frame input)
    {
        if (input.Move.sqrMagnitude <= 0.001f)
            return StateType.Idle;
        return input.RunHeld ? StateType.Run : StateType.Move;
    }

    /// <summary>切换当前状态。</summary>
    private void ChangeState(StateType next)
    {
        State = next;
    }
}
