using Fantasy;
using Fantasy.MongdbModel;

/// <summary>校验移动采样并更新内存，不处理路由或数据库。</summary>
public static class PlayerMovementRules
{
    /// <summary>本次移动采样的处理结果。</summary>
    public enum Decision
    {
        /// <summary>旧序号或重复包，不更新角色，也不广播。</summary>
        Ignored,
        /// <summary>移动通过校验，更新内存并等待下个复制周期广播。</summary>
        Accepted,
        /// <summary>位移被拒绝或回正代次不匹配，向本人返回服务端确认位置。</summary>
        Correction
    }

    /// <summary>回正后只接受新代次；旧代次重发同一回正，避免丢包后永久失步。</summary>
    public static Decision Process(OnlineInfo player, uint sequence, uint epoch, MovementSnapshot movement, long now)
    {
        if (epoch != player.MovementEpoch) return Decision.Correction;
        if (player.HasLastMoveSequence && unchecked((int)(sequence - player.LastMoveSequence)) <= 0)
            return Decision.Ignored;
        if (TryApply(player, sequence, movement, now)) return Decision.Accepted;
        player.LastMoveSequence = sequence;
        player.HasLastMoveSequence = true;
        player.LastCorrectionSequence = sequence;
        player.MovementEpoch = unchecked(player.MovementEpoch + 1);
        return Decision.Correction;
    }

    // 与客户端速度配套；宽限只按实际时间恢复，不因包数增加。
    /// <summary>允许位置变化的水平速度上限，单位米/秒，包含相对客户端跑速的宽限。</summary>
    private const float MaxHorizontalSpeed = 9f;
    /// <summary>允许位置变化的竖直速度绝对值上限，单位米/秒。</summary>
    private const float MaxVerticalSpeed = 45f;
    /// <summary>客户端提交的动画水平速度上限，单位米/秒，与位置位移预算分开检查。</summary>
    private const float MaxReportedSpeed = 8f;
    /// <summary>一次最多恢复多少秒的位移预算，避免长时间静止积攒出超大瞬移额度。</summary>
    private const float MaxAccumulatedSeconds = 1f;
    /// <summary>单轴世界坐标绝对值上限，单位米。</summary>
    private const float MaxWorldCoordinate = 100000f;

    /// <summary>拒绝非法或旧采样时保留已确认位置和预算。</summary>
    public static bool TryApply(OnlineInfo player, uint sequence, MovementSnapshot movement, long now)
    {
        if (movement == null
            || !float.IsFinite(movement.X) || !float.IsFinite(movement.Y) || !float.IsFinite(movement.Z)
            || !float.IsFinite(movement.RotationY) || !float.IsFinite(movement.Speed)
            || MathF.Abs(movement.X) > MaxWorldCoordinate || MathF.Abs(movement.Y) > MaxWorldCoordinate
            || MathF.Abs(movement.Z) > MaxWorldCoordinate
            || movement.Speed < 0f || movement.Speed > MaxReportedSpeed
            || movement.State is < PlayerMotionState.Idle or > PlayerMotionState.Fall
            || (player.HasLastMoveSequence
                && unchecked((int)(sequence - player.LastMoveSequence)) <= 0)) return false;

        // elapsed：距上次接受移动经过的时间，单位秒，限制最多恢复一秒的预算。
        var elapsed = Math.Clamp((now - player.LastMoveReceivedAt) / 1000f, 0f, MaxAccumulatedSeconds);
        // dx：本次报告位置相对已确认位置的 X 轴位移，单位米。
        var dx = movement.X - player.X;
        // dz：本次报告位置相对已确认位置的 Z 轴位移，单位米。
        var dz = movement.Z - player.Z;
        // horizontal：本次报告位置相对已确认位置的水平距离，单位米。
        var horizontal = MathF.Sqrt(dx * dx + dz * dz);
        // vertical：本次报告位置相对已确认位置的高度差绝对值，单位米。
        var vertical = MathF.Abs(movement.Y - player.Y);
        // horizontalBudget：按经过时间恢复后允许消耗的水平位移额度，单位米。
        var horizontalBudget = MaxHorizontalSpeed * elapsed + player.HorizontalAllowance;
        // verticalBudget：按经过时间恢复后允许消耗的竖直位移额度，单位米。
        var verticalBudget = MaxVerticalSpeed * elapsed + player.VerticalAllowance;
        if (horizontal > horizontalBudget || vertical > verticalBudget) return false;

        player.HorizontalAllowance = Math.Min(1f, horizontalBudget - horizontal);
        player.VerticalAllowance = Math.Min(2f, verticalBudget - vertical);
        // positionChanged：确认位置或朝向是否改变，据此决定是否需要持久化保存。
        var positionChanged = player.X != movement.X || player.Y != movement.Y || player.Z != movement.Z
            || player.RotationY != (movement.RotationY % 360f + 360f) % 360f;
        player.X = movement.X;
        player.Y = movement.Y;
        player.Z = movement.Z;
        player.RotationY = (movement.RotationY % 360f + 360f) % 360f;
        player.MovementState = movement.State;
        player.MovementSpeed = movement.Speed;
        player.LastMoveSequence = sequence;
        player.HasLastMoveSequence = true;
        player.LastMoveReceivedAt = now;
        player.MovementVersion = unchecked(player.MovementVersion + 1);
        player.IsDirty |= positionChanged;
        return true;
    }
}
