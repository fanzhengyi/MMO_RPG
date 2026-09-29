/// <summary>管理四段普攻；每段最多预约一次下一段，收招后连段清零。</summary>
public sealed class PlayerAttackState : PlayerActionState
{
    public override PlayerStateType Type => PlayerStateType.Attack;
    private bool nextStepQueued;

    /// <summary>创建普攻状态。</summary>
    public PlayerAttackState(PlayerFsmCore machine) : base(machine) { }

    /// <summary>每次重新进入普攻都从第一段开始。</summary>
    public override void Enter()
    {
        StartStep(1);
    }

    /// <summary>窗口内记录点击，到衔接点播放下一段，否则收招。</summary>
    public override void Tick(float deltaTime)
    {
        if (!Advance(deltaTime))
            return;

        var step = Machine.Settings.Attacks[ActionIndex - 1];
        var progress = Elapsed / step.Duration;
        if (ActionIndex < PlayerSettings.AttackCount && Input.AttackPressed
            && progress >= step.ComboOpen && progress <= step.ComboClose)
            nextStepQueued = true;

        if (nextStepQueued && progress >= step.ComboTransition)
            StartStep(ActionIndex + 1);
        else if (Elapsed >= step.Duration)
            Machine.ReturnToLocomotion();
    }

    /// <summary>清除段数及预约，死亡中断也不会残留连招。</summary>
    public override void Exit()
    {
        base.Exit();
        nextStepQueued = false;
        Machine.CurrentAttackStep = 0;
    }

    /// <summary>启动指定段；同为攻击状态时仍通知动画切换段数。</summary>
    private void StartStep(int index)
    {
        nextStepQueued = false;
        Machine.CurrentAttackStep = index;
        BeginAction(Machine.Settings.Attacks[index - 1], index);
    }
}
