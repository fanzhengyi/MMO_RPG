using Fantasy;
using Fantasy.MongdbModel;

/// <summary>移动快照校验和内存更新，不访问网络或数据库。</summary>
public static class PlayerMovementRules
{
    // 与客户端走路、奔跑和终端下落速度配套；修改角色速度时同步调整。
    private const float MaxHorizontalSpeed = 9f;
    private const float MaxVerticalSpeed = 45f;
    private const float MaxReportedSpeed = 8f;
    private const float MaxAccumulatedSeconds = 1f;

    /// <summary>只接受合法的新快照；拒绝时保留原位置和位移预算。</summary>
    public static bool TryApply(OnlineInfo player, G2Game_PlayerMove request, long now)
    {
        if (!float.IsFinite(request.X) || !float.IsFinite(request.Y) || !float.IsFinite(request.Z)
            || !float.IsFinite(request.RotationY) || !float.IsFinite(request.Speed)
            || request.Speed < 0f || request.Speed > MaxReportedSpeed
            // Idle、Move、Jump、Run、Fall，对应客户端 FSM 和 Animator 的 State。
            || !(request.State is 0 or 1 or 2 or 6 or 7)
            || (player.HasLastMoveSequence
                && unchecked((int)(request.Sequence - player.LastMoveSequence)) <= 0)) return false;

        // 长时间站立不能累计出一次任意传送的预算。
        var elapsed = Math.Clamp((now - player.LastMoveReceivedAt) / 1000f, 0f, MaxAccumulatedSeconds);
        var dx = request.X - player.X;
        var dz = request.Z - player.Z;
        var horizontal = MathF.Sqrt(dx * dx + dz * dz);
        var vertical = MathF.Abs(request.Y - player.Y);
        var horizontalBudget = MaxHorizontalSpeed * elapsed + player.HorizontalAllowance;
        var verticalBudget = MaxVerticalSpeed * elapsed + player.VerticalAllowance;
        if (horizontal > horizontalBudget || vertical > verticalBudget) return false;

        player.HorizontalAllowance = Math.Min(1f, horizontalBudget - horizontal);
        player.VerticalAllowance = Math.Min(2f, verticalBudget - vertical);
        player.X = request.X;
        player.Y = request.Y;
        player.Z = request.Z;
        player.RotationY = (request.RotationY % 360f + 360f) % 360f;
        player.MovementState = request.State;
        player.MovementSpeed = request.Speed;
        player.LastMoveSequence = request.Sequence;
        player.HasLastMoveSequence = true;
        player.LastMoveReceivedAt = now;
        player.MovementVersion = unchecked(player.MovementVersion + 1);
        player.IsDirty = true;
        return true;
    }
}
