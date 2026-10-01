using System;
using Fantasy;
using Fantasy.Async;
using Fantasy.MongdbModel;
using Fantasy.Network.Interface;

/// <summary>校验 Gate 转发的移动快照，更新在线位置并广播给其他玩家。</summary>
public sealed class G2Game_PlayerMoveHandler : Address<Scene, G2Game_PlayerMove>
{
    private const float MaxHorizontalSpeed = 9f;
    private const float HorizontalGraceDistance = 1f;
    private const float MaxVerticalSpeed = 45f;
    private const float VerticalGraceDistance = 2f;
    private const float MaxReportedSpeed = 8f;
    private const long PositionSaveIntervalMs = 5000;

    protected override async FTask Run(Scene scene, G2Game_PlayerMove request)
    {
        if (string.IsNullOrEmpty(request.UserName)
            || request.GateSessionRuntimeId == 0
            || request.GateSceneAddress == 0
            || !IsFinite(request.X) || !IsFinite(request.Y) || !IsFinite(request.Z)
            || !IsFinite(request.RotationY) || !IsFinite(request.Speed)
            || request.Speed < 0f || request.Speed > MaxReportedSpeed
            || !IsValidState(request.State))
        {
            return;
        }

        var onlineComponent = scene.GetComponent<OnlineComponent>();
        if (!onlineComponent.PlayersByName.TryGetValue(request.UserName, out var player))
        {
            return;
        }

        // 只有当前绑定的 Gate Session 才能更新这个角色，旧连接和未进游戏的连接会被丢弃。
        if (player.GateSessionRuntimeId != request.GateSessionRuntimeId
            || player.GateSceneAddress != request.GateSceneAddress)
        {
            return;
        }

        if (player.HasLastMoveSequence && !IsNewerSequence(request.Sequence, player.LastMoveSequence))
        {
            return;
        }

        var now = Environment.TickCount64;
        if (player.HasLastMoveSequence && !IsPlausibleMovement(player, request, now))
        {
            return;
        }

        var stateChanged = player.MovementState != request.State;
        player.X = request.X;
        player.Y = request.Y;
        player.Z = request.Z;
        player.RotationY = NormalizeAngle(request.RotationY);
        player.MovementState = request.State;
        player.MovementSpeed = request.Speed;
        player.LastMoveSequence = request.Sequence;
        player.HasLastMoveSequence = true;
        player.LastMoveReceivedAt = now;

        // 按 Gate Session 定向发送，Game 不直接持有客户端 Session。
        foreach (var receiver in onlineComponent.Players.Values)
        {
            if (receiver.Id == player.Id
                || receiver.GateSessionRuntimeId == 0
                || receiver.GateSceneAddress == 0)
            {
                continue;
            }

            scene.Send(receiver.GateSceneAddress, new Game2G_PlayerMove
            {
                GateSessionRuntimeId = receiver.GateSessionRuntimeId,
                RoleId = player.Id,
                X = player.X,
                Y = player.Y,
                Z = player.Z,
                RotationY = player.RotationY,
                Sequence = player.LastMoveSequence,
                State = player.MovementState,
                Speed = player.MovementSpeed,
            });
        }

        // 定期保存移动中的位置；进入 Idle 时立即保存最后位置。
        if (now - player.LastPositionSavedAt >= PositionSaveIntervalMs
            || (stateChanged && request.State == 0))
        {
            player.LastPositionSavedAt = now;
            await scene.World.Database.Save(player);
        }
    }

    /// <summary>比较允许 uint 回绕的客户端移动序号。</summary>
    private static bool IsNewerSequence(uint sequence, uint previous)
    {
        return unchecked((int)(sequence - previous)) > 0;
    }

    /// <summary>限制相邻快照的位移，降低客户端瞬移或伪造速度的影响。</summary>
    private static bool IsPlausibleMovement(OnlineInfo player, G2Game_PlayerMove request, long now)
    {
        var elapsedSeconds = Math.Max((now - player.LastMoveReceivedAt) / 1000f, 0.05f);
        var deltaX = request.X - player.X;
        var deltaY = request.Y - player.Y;
        var deltaZ = request.Z - player.Z;
        var horizontalDistance = MathF.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        var verticalDistance = MathF.Abs(deltaY);

        return horizontalDistance <= MaxHorizontalSpeed * elapsedSeconds + HorizontalGraceDistance
            && verticalDistance <= MaxVerticalSpeed * elapsedSeconds + VerticalGraceDistance;
    }

    /// <summary>只接受客户端状态机中定义的移动状态。</summary>
    private static bool IsValidState(int state)
    {
        return state == 0 || state == 1 || state == 2 || state == 6 || state == 7;
    }

    /// <summary>拒绝 NaN 和无穷大，避免异常浮点数污染角色状态。</summary>
    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    /// <summary>将朝向规范到 0 到 360 度区间。</summary>
    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        return angle < 0f ? angle + 360f : angle;
    }
}
