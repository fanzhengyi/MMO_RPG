using System;
using Fantasy;

/// <summary>隔离网络状态与本地 FSM、Animator 的编号。</summary>
public static class PlayerStateMapping
{
    /// <summary>将本地状态转换成网络状态。</summary>
    public static PlayerMotionState ToNetwork(PlayerFsmCore.StateType state) => state switch
    {
        PlayerFsmCore.StateType.Idle => PlayerMotionState.Idle,
        PlayerFsmCore.StateType.Move => PlayerMotionState.Walk,
        PlayerFsmCore.StateType.Jump => PlayerMotionState.Jump,
        PlayerFsmCore.StateType.Run => PlayerMotionState.Run,
        PlayerFsmCore.StateType.Fall => PlayerMotionState.Fall,
        _ => throw new ArgumentOutOfRangeException(nameof(state)),
    };

    /// <summary>将网络状态转换成本地表现状态。</summary>
    public static PlayerFsmCore.StateType FromNetwork(PlayerMotionState state) => state switch
    {
        PlayerMotionState.Idle => PlayerFsmCore.StateType.Idle,
        PlayerMotionState.Walk => PlayerFsmCore.StateType.Move,
        PlayerMotionState.Jump => PlayerFsmCore.StateType.Jump,
        PlayerMotionState.Run => PlayerFsmCore.StateType.Run,
        PlayerMotionState.Fall => PlayerFsmCore.StateType.Fall,
        _ => throw new ArgumentOutOfRangeException(nameof(state)),
    };
}
