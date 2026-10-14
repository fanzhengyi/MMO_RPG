using Fantasy;
using Fantasy.MongdbModel;

/// <summary>校验移动采样并更新内存，不处理路由或数据库。</summary>
public static class PlayerMovementRules
{
    // 与客户端速度配套；宽限只按实际时间恢复，不因包数增加。
    private const float MaxHorizontalSpeed = 9f;
    private const float MaxVerticalSpeed = 45f;
    private const float MaxReportedSpeed = 8f;
    private const float MaxAccumulatedSeconds = 1f;

    /// <summary>拒绝非法或旧采样时保留已确认位置和预算。</summary>
    public static bool TryApply(OnlineInfo player, uint sequence, MovementSnapshot movement, long now)
    {
        if (movement == null
            || !float.IsFinite(movement.X) || !float.IsFinite(movement.Y) || !float.IsFinite(movement.Z)
            || !float.IsFinite(movement.RotationY) || !float.IsFinite(movement.Speed)
            || movement.Speed < 0f || movement.Speed > MaxReportedSpeed
            || movement.State is < PlayerMotionState.Idle or > PlayerMotionState.Fall
            || (player.HasLastMoveSequence
                && unchecked((int)(sequence - player.LastMoveSequence)) <= 0)) return false;

        var elapsed = Math.Clamp((now - player.LastMoveReceivedAt) / 1000f, 0f, MaxAccumulatedSeconds);
        var dx = movement.X - player.X;
        var dz = movement.Z - player.Z;
        var horizontal = MathF.Sqrt(dx * dx + dz * dz);
        var vertical = MathF.Abs(movement.Y - player.Y);
        var horizontalBudget = MaxHorizontalSpeed * elapsed + player.HorizontalAllowance;
        var verticalBudget = MaxVerticalSpeed * elapsed + player.VerticalAllowance;
        if (horizontal > horizontalBudget || vertical > verticalBudget) return false;

        player.HorizontalAllowance = Math.Min(1f, horizontalBudget - horizontal);
        player.VerticalAllowance = Math.Min(2f, verticalBudget - vertical);
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
        player.IsDirty = true;
        return true;
    }
}
