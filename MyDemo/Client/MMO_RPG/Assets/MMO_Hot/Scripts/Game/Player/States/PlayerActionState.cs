/// <summary>普攻和技能共用的计时、表现触发与清理逻辑。</summary>
public abstract class PlayerActionState : PlayerState
{
    protected PlayerActionSettings Action;
    protected float Elapsed;
    protected int ActionIndex;
    private bool impactSent;

    /// <summary>接收所属角色的状态机。</summary>
    protected PlayerActionState(PlayerFsmCore machine) : base(machine) { }

    /// <summary>开始一段动作，重置计时和单次表现触发标记。</summary>
    protected void BeginAction(PlayerActionSettings action, int index)
    {
        Action = action;
        ActionIndex = index;
        Elapsed = 0f;
        impactSent = false;
        Machine.NotifyActionStarted(Type, index);
    }

    /// <summary>推进动作；跨过触发点时也只发送一次事件，离地则中断。</summary>
    protected bool Advance(float deltaTime)
    {
        if (!Machine.IsGrounded)
        {
            Machine.ChangeState(PlayerStateType.Fall);
            return false;
        }

        Elapsed += deltaTime;
        if (!impactSent && Elapsed >= Action.Duration * Action.ImpactTime)
        {
            impactSent = true;
            Machine.NotifyActionImpact(Type, ActionIndex);
        }
        return Machine.State == Type;
    }

    /// <summary>退出时清除计时，避免死亡或下一次动作继承旧进度。</summary>
    public override void Exit()
    {
        Action = null;
        ActionIndex = 0;
        Elapsed = 0f;
        impactSent = false;
    }
}
